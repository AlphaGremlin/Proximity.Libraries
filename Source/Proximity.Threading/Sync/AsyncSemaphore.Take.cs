using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Sources;

namespace System.Threading
{
	public sealed partial class AsyncSemaphore
	{
		internal sealed class SemaphoreInstance : BaseCancellable, IValueTaskSource<Instance>
		{ //****************************************
			private static readonly ConcurrentBag<SemaphoreInstance> Instances = new ConcurrentBag<SemaphoreInstance>();
			//****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<Instance> _TaskSource = new ManualResetValueTaskSourceCore<Instance>();
			//****************************************

			internal SemaphoreInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			~SemaphoreInstance()
			{
				if (_InstanceState != Status.Unused)
				{
					// TODO: Lock instance was garbage collected without being released
				}
			}

			//****************************************

			internal void Initialise(AsyncSemaphore owner, bool isHeld)
			{
				Owner = owner;

				GC.ReRegisterForFinalize(this);

				if (isHeld)
				{
					_InstanceState = Status.Held;
					_TaskSource.SetResult(new Instance(this));
				}
				else
				{
					_InstanceState = Status.Pending;
				}
			}

			internal void ApplyCancellation(CancellationToken token, TimeSpan timeout)
			{
				if (_InstanceState != Status.Pending)
					throw new InvalidOperationException("Cannot register for cancellation when not pending");

				RegisterCancellation(token, timeout);
			}

			internal void SwitchToDisposed()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				UnregisterCancellation();
			}

			internal bool TrySwitchToCompleted()
			{
				// Try and assign the counter to this Instance
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Held, Status.Pending) == Status.Pending)
				{
					UnregisterCancellation();

					return true;
				}

				// Assignment failed, so it was cancelled
				// If the result has already been retrieved, return the Instance to the pool
				int InstanceState, NewInstanceState;

				do
				{
					InstanceState = _InstanceState;

					switch (InstanceState)
					{
					case Status.Cancelled:
						NewInstanceState = Status.CancelledNotWaiting;
						break;

					case Status.CancelledGotResult:
						NewInstanceState = Status.Unused;
						break;

					default:
						return false;
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				if (InstanceState == Status.CancelledGotResult)
					Release(); // GetResult has been called, so we can return to the pool

				return false;
			}

			internal void Release(short token)
			{
				if (_TaskSource.Version != token || Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.Held) != Status.Held)
					throw new InvalidOperationException("Semaphore cannot be released multiple times");

				// Release the counter and then return to the pool
				Owner!.Decrement();
				Release();
			}

			//****************************************

			protected override void SwitchToCancelled()
			{
				if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				// The cancellation token was raised
				_TaskSource.SetException(CreateCancellationException());
			}

			protected override void UnregisteredCancellation()
			{
				switch (_InstanceState)
				{
				case Status.Disposed:
					_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncSemaphore), "Semaphore has been disposed of"));
					break;

				case Status.Held:
					_TaskSource.SetResult(new Instance(this));
					break;
				}
			}

			ValueTaskSourceStatus IValueTaskSource<Instance>.GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public Instance GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					if (_InstanceState == Status.CancelledNotWaiting || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
						Release(); // We're cancelled and no longer on the Wait queue, so we can return to the pool
				}
			}

			//****************************************

			private void Release()
			{
				Owner = null;

				_TaskSource.Reset();
				_InstanceState = Status.Unused;
				ResetCancellation();

				GC.SuppressFinalize(this);
				Instances.Add(this);
			}

			//****************************************

			public AsyncSemaphore? Owner { get; private set; }

			public bool IsPending => _InstanceState == Status.Pending;

			public short Version => _TaskSource.Version;

			//****************************************

			internal static SemaphoreInstance GetOrCreate(AsyncSemaphore owner, bool isTaken)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new SemaphoreInstance();

				Instance.Initialise(owner, isTaken);

				return Instance;
			}

			private static class Status
			{
				/// <summary>
				/// The lock was cancelled and is currently on the list of waiters
				/// </summary>
				internal const int CancelledGotResult = -4;
				/// <summary>
				/// The lock is cancelled and waiting for GetResult
				/// </summary>
				internal const int CancelledNotWaiting = -3;
				/// <summary>
				/// The lock was cancelled and is currently on the list of waiters while waiting for GetResult
				/// </summary>
				internal const int Cancelled = -2;
				/// <summary>
				/// The lock was disposed of
				/// </summary>
				internal const int Disposed = -1;
				/// <summary>
				/// A lock starts in the Unused state
				/// </summary>
				internal const int Unused = 0;
				/// <summary>
				/// The lock is currently held, and waiting for GetResult and then Dispose
				/// </summary>
				internal const int Held = 1;
				/// <summary>
				/// The lock is on the list of waiters
				/// </summary>
				internal const int Pending = 2;
			}
		}
	}
}
