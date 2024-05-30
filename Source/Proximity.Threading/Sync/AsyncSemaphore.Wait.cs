using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Threading
{
	public sealed partial class AsyncSemaphore
	{
		internal sealed class SemaphoreWaitInstance : BaseCancellable, IValueTaskSource<bool>
		{ //****************************************
			private static readonly ConcurrentBag<SemaphoreWaitInstance> Instances = new();
			//****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<bool> _TaskSource = new();
			//****************************************

			internal SemaphoreWaitInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			~SemaphoreWaitInstance()
			{
				if (_InstanceState != Status.Unused)
				{
					// TODO: Lock instance was garbage collected without being released
				}
			}

			//****************************************

			public void SwitchToDisposed()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				UnregisterCancellation();
			}

			public bool TrySwitchToCompleted()
			{
				return true;
			}

			public void Release()
			{
				Owner = null;

				_TaskSource.Reset();
				_InstanceState = Status.Unused;
				ResetCancellation();

				GC.SuppressFinalize(this);
				if (Instances.Count < MaxInstanceCache)
					Instances.Add(this);
			}

			//****************************************

			protected override void SwitchToCancelled()
			{

			}


			private void Initialise(AsyncSemaphore owner)
			{
				Owner = owner;

				GC.ReRegisterForFinalize(this);

				_InstanceState = Status.Wait;
			}

			protected override void UnregisteredCancellation()
			{
				switch (_InstanceState)
				{
				case Status.Disposed:
					_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncSemaphore), "Semaphore has been disposed of"));
					break;

				case Status.Locked:
					_TaskSource.SetResult(false);
					break;
				}
			}

			//****************************************

			ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			bool IValueTaskSource<bool>.GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					switch (_InstanceState)
					{
					case Status.Disposed:
					case Status.CancelledNotWaiting:
					case Status.Locked:
						Release();
						break;

					case Status.Cancelled:
						if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
							Release(); // We're cancelled/disposed and no longer on the Wait queue, so we can return to the pool
						break;
					}
				}
			}

			//****************************************

			//****************************************

			public AsyncSemaphore? Owner { get; private set; }

			public bool IsPending => _InstanceState == Status.Pending;

			public short Version => _TaskSource.Version;

			internal static SemaphoreWaitInstance GetOrCreate(AsyncSemaphore owner)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new SemaphoreWaitInstance();

				Instance.Initialise(owner);

				return Instance;
			}

			private static class Status
			{
				/// <summary>
				/// The wait was cancelled and is currently on the list of waiters
				/// </summary>
				internal const int CancelledGotResult = -4;
				/// <summary>
				/// The wait is cancelled and waiting for GetResult
				/// </summary>
				internal const int CancelledNotWaiting = -3;
				/// <summary>
				/// The wait was cancelled and is currently on the list of waiters while waiting for GetResult
				/// </summary>
				internal const int Cancelled = -2;
				/// <summary>
				/// The wait was disposed of
				/// </summary>
				internal const int Disposed = -1;
				/// <summary>
				/// A wait starts in the Unused state
				/// </summary>
				internal const int Unused = 0;
				/// <summary>
				/// The lock is on the list of pulse waiters and can be cancelled
				/// </summary>
				internal const int Wait = 1;
				/// <summary>
				/// The lock is waiting to reacquire and cannot be cancelled
				/// </summary>
				internal const int Pending = 2;
				/// <summary>
				/// The lock is currently held, and waiting for GetResult and then Dispose
				/// </summary>
				internal const int Locked = 3;
			}
		}
	}
}
