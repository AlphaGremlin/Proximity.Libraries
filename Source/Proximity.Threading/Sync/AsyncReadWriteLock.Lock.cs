using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Threading
{
	public sealed partial class AsyncReadWriteLock
	{
		internal sealed class LockInstance : BaseCancellable, ILockWaiter, IValueTaskSource<Instance>
		{ //****************************************
			private static readonly ConcurrentBag<LockInstance> Instances = new();
			//****************************************
			private int _InstanceState;

			private ManualResetValueTaskSourceCore<Instance> _TaskSource = new();
			//****************************************

			internal LockInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			~LockInstance()
			{
				if (Volatile.Read(ref _InstanceState) != Status.Unused)
				{
					// TODO: Lock instance was garbage collected without being released
				}
			}

			//****************************************

			internal void ApplyCancellation(CancellationToken token, TimeSpan timeout)
			{
				if (Volatile.Read(ref _InstanceState) != Status.Pending)
					throw new InvalidOperationException("Cannot register for cancellation when not pending");

				RegisterCancellation(token, timeout);
			}

			public void SwitchToDisposed()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				UnregisterCancellation();
			}

			public bool TrySwitchToCompleted(bool isWriter)
			{
				Debug.Assert(isWriter == IsWriter, "Read Write lock waiter not on the correct queue");

				// Try and assign the counter to this Instance
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Held, Status.Pending) == Status.Pending)
				{
					if (IsWriter)
						Debug.Assert(Owner!._Counter == LockState.Writer, "Read Write lock is in an invalid state (Unheld writer)");
					else
						Debug.Assert(Owner!._Counter >= LockState.Reader, "Read Write lock is in an invalid state (Unheld reader)");

					UnregisterCancellation();

					return true;
				}

				// Assignment failed, so it was cancelled
				// If the result has already been retrieved, return the Instance to the pool
				int InstanceState, NewInstanceState;

				do
				{
					InstanceState = Volatile.Read(ref _InstanceState);

					switch (InstanceState)
					{
					case Status.Cancelled:
						NewInstanceState = Status.CancelledNotWaiting;
						break;

					case Status.CancelledGotResult:
						NewInstanceState = Status.Unused;
						break;

					case Status.Unused:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Unused)");

					case Status.Pending:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Pending)");

					case Status.Held:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Held)");

					default:
						throw new InvalidOperationException($"Read Write Lock instance is in an invalid state ({InstanceState})");
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				if (InstanceState == Status.CancelledGotResult)
					Release(); // GetResult has been called, so we can return to the pool

				return false;
			}

			internal ValueTask<bool> Upgrade(short token, CancellationToken cancellationToken, TimeSpan timeout)
			{
				if (_TaskSource.Version != token || _InstanceState != Status.Held)
					throw new InvalidOperationException("Lock has already been released");

				if (IsWriter)
					return new ValueTask<bool>(true);

				return Owner!.Upgrade(this, false, cancellationToken, timeout);
			}

			internal ValueTask<bool> TryUpgrade(short token, CancellationToken cancellationToken, TimeSpan timeout)
			{
				if (_TaskSource.Version != token || Volatile.Read(ref _InstanceState) != Status.Held)
					throw new InvalidOperationException("Lock has already been released");

				if (IsWriter)
					return new ValueTask<bool>(true);

				return Owner!.Upgrade(this, true, cancellationToken, timeout);
			}

			internal void CompleteUpgrade()
			{
				Debug.Assert(!IsWriter, "Lock is already a writer");

				IsWriter = true;
			}

			internal void Downgrade(short token)
			{
				if (_TaskSource.Version != token || Volatile.Read(ref _InstanceState) != Status.Held)
					throw new InvalidOperationException("Lock has already been released");

				if (!IsWriter)
					return;

				Owner!.Downgrade();

				IsWriter = false;
			}

			internal void Release(short token)
			{
				if (_TaskSource.Version != token)
					throw new InvalidOperationException("Lock cannot be released multiple times");

				if (Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.Held) != Status.Held)
					throw new InvalidOperationException($"Lock cannot be released multiple times ({_InstanceState})");

				// Release the counter and then return to the pool
				Owner!.Release(IsWriter);
				Release();
			}

			//****************************************

			protected override void SwitchToCancelled()
			{
				if (Volatile.Read(ref _InstanceState) != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				if (Owner!.CancelWaiter(this, IsWriter))
				{
					if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledNotWaiting, Status.Cancelled) != Status.Cancelled)
						throw new InvalidOperationException("Lock was completed while erased");
				}

				// The cancellation token was raised
				_TaskSource.SetException(CreateCancellationException());
			}

			protected override void UnregisteredCancellation()
			{
				switch (Volatile.Read(ref _InstanceState))
				{
				case Status.Disposed:
					_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of"));
					break;

				case Status.Held:
					_TaskSource.SetResult(new Instance(this));
					break;

				default:
					throw new InvalidOperationException($"Lock is in an invalid state ({_InstanceState})");
				}
			}

			ValueTaskSourceStatus IValueTaskSource<Instance>.GetStatus(short token) => _TaskSource.GetStatus(token);

			void IValueTaskSource<Instance>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			Instance IValueTaskSource<Instance>.GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					switch (Volatile.Read(ref _InstanceState))
					{
					case Status.Disposed:
					case Status.CancelledNotWaiting:
						Release();
						break;

					case Status.Cancelled:
						if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
							Release(); // We're cancelled/disposed and no longer on the Wait queue, so we can return to the pool
						break;

					case Status.Pending:
						throw new InvalidOperationException("Lock is in an invalid state");
					}
				}
			}

			//****************************************

			private void Initialise(AsyncReadWriteLock owner, bool isWriter, bool isHeld)
			{
				Debug.Assert(Volatile.Read(ref _InstanceState) == Status.Unused, "Lock Instance has been reused");

				Owner = owner;
				IsWriter = isWriter;

				GC.ReRegisterForFinalize(this);

				if (isHeld)
				{
					Interlocked.Exchange(ref _InstanceState, Status.Held);
					_TaskSource.SetResult(new Instance(this));
				}
				else
				{
					Interlocked.Exchange(ref _InstanceState, Status.Pending);
				}
			}

			private void Release()
			{
				Debug.Assert(Owner != null, "Double Release");
				Debug.Assert(Volatile.Read(ref _InstanceState) != Status.Pending && _InstanceState != Status.Held, "Release not valid at this time");

				Owner = null;
				IsWriter = false;
				IsUpgrading = false;

				_TaskSource.Reset();
				Interlocked.Exchange(ref _InstanceState, Status.Unused);
				ResetCancellation();

				GC.SuppressFinalize(this);
				if (Instances.Count < MaxInstanceCache)
					Instances.Add(this);
			}

			//****************************************

			public AsyncReadWriteLock? Owner { get; private set; }

			public bool IsWriter { get; private set; }

			public bool IsUpgrading { get; private set; }

			public bool IsPending => Volatile.Read(ref _InstanceState) == Status.Pending;

			public short Version => _TaskSource.Version;

			//****************************************

			internal static LockInstance GetOrCreate(AsyncReadWriteLock owner, bool isWriter, bool isTaken)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new LockInstance();

				Instance.Initialise(owner, isWriter, isTaken);

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
