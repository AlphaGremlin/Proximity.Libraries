using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Threading
{
	public sealed partial class AsyncKeyedLock<TKey>
	{
		internal sealed class KeyedLockInstance : BaseCancellable, IValueTaskSource<Instance>
		{ //****************************************
			private static readonly ConcurrentBag<KeyedLockInstance> Instances = new();
			//****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<Instance> _TaskSource = new();
			//****************************************

			internal KeyedLockInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			~KeyedLockInstance()
			{
				if (_InstanceState != Status.Unused)
				{
					// TODO: Lock instance was garbage collected without being released
				}
			}

			//****************************************

			internal void Initialise(AsyncKeyedLock<TKey> owner, TKey key)
			{
				Owner = owner;
				Key = key;

				GC.ReRegisterForFinalize(this);

				_InstanceState = Status.Pending;
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
					throw new InvalidOperationException("Keyed Lock cannot be released multiple times");

				// Release the lock and return this instance to the pool
				Owner!.Release(Key!);
				Release();
			}

			//****************************************

			protected override void SwitchToCancelled()
			{
				if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				Owner!.Cancel(this);

				// The cancellation token was raised
				_TaskSource.SetException(CreateCancellationException());
			}

			protected override void UnregisteredCancellation()
			{
				switch (_InstanceState)
				{
				case Status.Disposed:
					_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncKeyedLock<TKey>), "Keyed Lock has been disposed of"));
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
				Key = default;

				_TaskSource.Reset();
				_InstanceState = Status.Unused;
				ResetCancellation();

				GC.SuppressFinalize(this);
				Instances.Add(this);
			}

			//****************************************

			public AsyncKeyedLock<TKey>? Owner { get; private set; }

			public TKey? Key { get; private set; }

			public bool IsPending => _InstanceState == Status.Pending;

			public short Version => _TaskSource.Version;

			//****************************************

			internal static KeyedLockInstance GetOrCreate(AsyncKeyedLock<TKey> owner, TKey key)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new KeyedLockInstance();

				Instance.Initialise(owner, key);

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
