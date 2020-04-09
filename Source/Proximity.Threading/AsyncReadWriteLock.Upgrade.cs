using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks.Sources;

namespace System.Threading
{
	public sealed partial class AsyncReadWriteLock
	{
		internal sealed class LockUpgradeInstance : ILockWaiter, IValueTaskSource<bool>
		{ //****************************************
			private static readonly ConcurrentBag<LockUpgradeInstance> Instances = new ConcurrentBag<LockUpgradeInstance>();
			//****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<bool> _TaskSource = new ManualResetValueTaskSourceCore<bool>();

			private CancellationTokenRegistration _Registration;

			private bool _IsFirst;
			//****************************************

			internal LockUpgradeInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			//****************************************

			public void Prepare(bool isFirst, CancellationToken token)
			{
				_IsFirst = isFirst;

				if (_InstanceState != Status.Pending)
					throw new InvalidOperationException($"Read Write Lock upgrade is in an invalid state (Prepare)");

				if(token.CanBeCanceled)
				{
					Token = token;

					_Registration = token.Register((state) => ((LockUpgradeInstance)state).SwitchToCancelled(), this);
				}
			}

			public void Release()
			{
				Owner = null;
				Token = default;

				_TaskSource.Reset();
				_Registration.Dispose();
				_IsFirst = false;

				Instances.Add(this);
			}

			public void SwitchToDisposed()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				_Registration.Dispose();
				_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of"));
			}

			public bool TrySwitchToCompleted(bool isWriter)
			{
				int InstanceState, NewInstanceState;

				do
				{
					InstanceState = _InstanceState;

					switch (InstanceState)
					{
					case Status.Cancelled:
					case Status.CancelledNotWaiting:
						return false;

					case Status.CancelledGotResult:
						NewInstanceState = Status.Unused;
						break;

					case Status.Downgrading:
						if (isWriter)
						{
							// Trying to retake the reader as part of cancellation, but the writer made it first
							NewInstanceState = Status.UpgradedDowngrade;
						}
						else
						{
							// Retook the reader as part of cancellation
							NewInstanceState = Status.CancelledNotWaiting;
						}
						break;

					case Status.Pending:
						Debug.Assert(!isWriter, "Read Write lock upgrade is in an invalid state (Reader)");

						NewInstanceState = Status.Upgraded;
						break;

					default:
						throw new InvalidOperationException($"Read Write Lock upgrade is in an invalid state ({InstanceState})");
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				switch (NewInstanceState)
				{
				case Status.Unused:
					Release(); // GetResult has been called, so we can return to the pool

					return false;

				case Status.CancelledNotWaiting:
					// We retook the read lock after a cancellation

					// Cancel the write waiter
					Owner!.Owner!.CancelWaiter(this, true);

					_TaskSource.SetException(new OperationCanceledException(Token));

					return true;

				case Status.UpgradedDowngrade:
					// We were attempting to retake the read lock, but got the write instead
					// Cancel trying to take the read lock
					Owner!.Owner!.CancelWaiter(this, false);

					goto case Status.Upgraded;

				case Status.Upgraded:
					_Registration.Dispose();

					// We've upgraded and now hold the write lock, ensure our instance is up-to-date first
					Owner!.CompleteUpgrade();

					_TaskSource.SetResult(_IsFirst);

					return true;

				default:
					throw new InvalidOperationException($"Read Write Lock upgrade transitioned to an invalid state ({InstanceState})");
				}
			}

			//****************************************

			ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) => _TaskSource.GetStatus(token);

			void IValueTaskSource<bool>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

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
					case Status.Upgraded:
						Release();
						break;

					case Status.UpgradedDowngrade:
						if (Interlocked.CompareExchange(ref _InstanceState, Status.UpgradedGotResult, Status.UpgradedDowngrade) == Status.Upgraded)
							Release(); // We're cancelled/disposed and no longer on the Wait queue, so we can return to the pool
						break;

					case Status.Cancelled:
						if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
							Release(); // We're cancelled/disposed and no longer on the Wait queue, so we can return to the pool
						break;
					}
				}
			}

			private void Initialise(LockInstance owner)
			{
				Owner = owner;

				_InstanceState = Status.Pending;
			}

			//****************************************

			private void SwitchToCancelled()
			{
				var Owner = this.Owner!.Owner!;

				if (Interlocked.CompareExchange(ref _InstanceState, Status.Downgrading, Status.Pending) != Status.Pending)
					return; // Instance was not in a cancellable state (did we get activated?)

				// Try and downgrade by retaking the read-lock, ignoring unfair mode
				if (Owner.TryTakeReader(true))
				{
					// We have a reader lock, but we may have already gotten upgraded
					if (Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Downgrading) != Status.Pending)
					{
						Owner.Release(false);

						// If we've already gotten our result, we can release
						if (Interlocked.CompareExchange(ref _InstanceState, Status.Upgraded, Status.UpgradedDowngrade) == Status.UpgradedGotResult)
							Release();

						return;
					}

					// We switched back to the read lock, try to cancel our waiting writer (should succeed)
					if (Owner.CancelWaiter(this, true))
					{
						if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledNotWaiting, Status.Cancelled) != Status.Cancelled)
							throw new InvalidOperationException("Lock was completed while erased");
					}

					// Cancelled, complete the Task
					_TaskSource.SetException(new OperationCanceledException(Token));
				}
				else
				{
					// We've switched to write mode, but it's not us - add to the waiting readers before we can cancel
					// Add to the read queue 
					Owner.Downgrade(this);
				}
			}

			//****************************************

			internal LockInstance? Owner { get; private set; }

			public CancellationToken Token { get; private set; }

			public short Version => _TaskSource.Version;

			//****************************************

			internal static LockUpgradeInstance GetOrCreate(LockInstance owner)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new LockUpgradeInstance();

				Instance.Initialise(owner);

				return Instance;
			}

			private static class Status
			{
				/// <summary>
				/// The upgrade was cancelled and is currently on the list of waiters
				/// </summary>
				internal const int CancelledGotResult = -4;
				/// <summary>
				/// The upgrade is cancelled and waiting for GetResult
				/// </summary>
				internal const int CancelledNotWaiting = -3;
				/// <summary>
				/// The upgrade was cancelled and is currently on the list of waiters while waiting for GetResult
				/// </summary>
				internal const int Cancelled = -2;
				/// <summary>
				/// The upgrade was cancelled and is currently trying to retake the read lock
				/// </summary>
				internal const int Downgrading = -1;
				/// <summary>
				/// The lock was disposed of
				/// </summary>
				internal const int Disposed = -1;
				/// <summary>
				/// An upgrader starts in the Unused state
				/// </summary>
				internal const int Unused = 0;
				/// <summary>
				/// The lock has been upgraded, and is waiting for GetResult
				/// </summary>
				internal const int Upgraded = 1;
				/// <summary>
				/// The lock is on the list of waiters
				/// </summary>
				internal const int Pending = 2;
				/// <summary>
				/// The lock has been upgraded while trying to downgrade, and is waiting for cancellation and GetResult
				/// </summary>
				internal const int UpgradedDowngrade = 10;
				/// <summary>
				/// The lock has been upgraded while trying to downgrade (cancellation may still be running), and is waiting for cancellation
				/// </summary>
				internal const int UpgradedGotResult = 10;
			}
		}

	}
}
