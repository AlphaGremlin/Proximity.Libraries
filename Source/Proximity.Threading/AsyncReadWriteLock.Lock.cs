using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading
{
	public sealed partial class AsyncReadWriteLock
	{
		internal sealed class LockInstance : ILockWaiter, IValueTaskSource<Instance>
		{ //****************************************
			private static readonly ConcurrentBag<LockInstance> Instances = new ConcurrentBag<LockInstance>();
			//****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<Instance> _TaskSource = new ManualResetValueTaskSourceCore<Instance>();

			private CancellationTokenRegistration _Registration;
			private CancellationTokenSource? _TokenSource;
			//****************************************

			internal LockInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			~LockInstance()
			{
				if (_InstanceState != Status.Unused)
				{
					// TODO: Lock instance was garbage collected without being released
				}
			}

			//****************************************

			internal void ApplyCancellation(CancellationToken token, CancellationTokenSource? tokenSource)
			{
				if (token.CanBeCanceled)
				{
					Token = token;

					if (_InstanceState != Status.Pending)
						throw new InvalidOperationException("Cannot register for cancellation when not pending");

					_TokenSource = tokenSource;

					_Registration = token.Register((state) => ((LockInstance)state).SwitchToCancelled(), this);
				}
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
				Debug.Assert(isWriter == IsWriter, "Read Write lock waiter not on the correct queue");

				// Try and assign the counter to this Instance
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Held, Status.Pending) == Status.Pending)
				{
					if (IsWriter)
						Debug.Assert(Owner!._Counter == LockState.Writer, "Read Write lock is in an invalid state (Unheld writer)");
					else
						Debug.Assert(Owner!._Counter >= LockState.Reader, "Read Write lock is in an invalid state (Unheld reader)");

					_Registration.Dispose();
					_TaskSource.SetResult(new Instance(this));

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

					case Status.Unused:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Unused)");

					case Status.Pending:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Pending)");

					case Status.Held:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Held)");

					case Status.CancelledNotWaiting:
						_Registration.Dispose();

						return false;

					default:
						throw new InvalidOperationException($"Read Write Lock instance is in an invalid state ({InstanceState})");
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				_Registration.Dispose();

				if (InstanceState == Status.CancelledGotResult)
					Release(); // GetResult has been called, so we can return to the pool

				return false;
			}

			internal ValueTask<bool> Upgrade(short token, CancellationToken cancellationToken)
			{
				if (_TaskSource.Version != token || _InstanceState != Status.Held)
					throw new InvalidOperationException("Lock has already been released");

				if (IsWriter)
					return new ValueTask<bool>(true);

				return Owner!.Upgrade(this, false, cancellationToken);
			}

			internal ValueTask<bool> TryUpgrade(short token, CancellationToken cancellationToken)
			{
				if (_TaskSource.Version != token || _InstanceState != Status.Held)
					throw new InvalidOperationException("Lock has already been released");

				if (IsWriter)
					return new ValueTask<bool>(true);

				return Owner!.Upgrade(this, true, cancellationToken);
			}

			internal void CompleteUpgrade()
			{
				Debug.Assert(!IsWriter, "Lock is already a writer");

				IsWriter = true;
			}

			internal void Downgrade(short token)
			{
				if (_TaskSource.Version != token || _InstanceState != Status.Held)
					throw new InvalidOperationException("Lock has already been released");

				if (!IsWriter)
					return;

				Owner!.Downgrade();

				IsWriter = false;
			}

			internal void Release(short token)
			{
				if (_TaskSource.Version != token || Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.Held) != Status.Held)
					throw new InvalidOperationException("Lock cannot be released multiple times");

				// Release the counter and then return to the pool
				Owner!.Release(IsWriter);
				Release();
			}

			//****************************************

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
					switch (_InstanceState)
					{
					case Status.Disposed:
					case Status.CancelledNotWaiting:
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

			private void Initialise(AsyncReadWriteLock owner, bool isWriter, bool isHeld)
			{
				Owner = owner;
				IsWriter = isWriter;

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

			private void SwitchToCancelled()
			{
				if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				if (Owner!.CancelWaiter(this, IsWriter))
				{
					if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledNotWaiting, Status.Cancelled) != Status.Cancelled)
						throw new InvalidOperationException("Lock was completed while erased");
				}

				_TaskSource.SetException(new OperationCanceledException(Token));
			}

			private void Release()
			{
				Owner = null;
				Token = default;
				_TaskSource.Reset();
				_TokenSource?.Dispose();
				_TokenSource = null;
				_InstanceState = Status.Unused;
				IsWriter = false;
				IsUpgrading = false;

				GC.SuppressFinalize(this);
				Instances.Add(this);
			}

			//****************************************

			public AsyncReadWriteLock? Owner { get; private set; }

			public bool IsWriter { get; private set; }

			public bool IsUpgrading { get; private set; }

			public bool IsPending => _InstanceState == Status.Pending;

			public CancellationToken Token { get; private set; }

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
