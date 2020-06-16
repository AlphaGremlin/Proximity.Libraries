using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Threading
{
	internal sealed class AsyncCounterDecrement : BaseCancellable, IValueTaskSource, IValueTaskSource<int>
	{ //****************************************
		private static readonly ConcurrentBag<AsyncCounterDecrement> Instances = new ConcurrentBag<AsyncCounterDecrement>();
		//****************************************
		private volatile int _InstanceState;

		private ManualResetValueTaskSourceCore<int> _TaskSource = new ManualResetValueTaskSourceCore<int>();
		//****************************************

		internal AsyncCounterDecrement() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		internal void Initialise(AsyncCounter owner, bool isPeek, bool toZero, bool wasDecremented)
		{
			Owner = owner;
			IsPeek = isPeek;
			Counters = toZero ? -1 : 1;

			if (wasDecremented)
			{
				_InstanceState = Status.Decremented;
				_TaskSource.SetResult(0); // Can only happen on a Peek, so the result is 0 (nothing was taken)
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

		internal bool TrySwitchToCompleted(ref int count)
		{
			// Try and assign the counter to this Instance
			if (Interlocked.CompareExchange(ref _InstanceState, Status.Decremented, Status.Pending) == Status.Pending)
			{
				if (Counters == -1)
					Counters = Interlocked.Exchange(ref count, 0);
				else
					count--;

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

		//****************************************

		protected override void SwitchToCancelled()
		{
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
				return; // Instance is no longer in a cancellable state

			if (Owner!.CancelDecrement(this))
			{
				if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledNotWaiting, Status.Cancelled) != Status.Cancelled)
					throw new InvalidOperationException("Decrement is in an invalid state (Not cancelled)");
			}

			// The cancellation token was raised
			_TaskSource.SetException(CreateCancellationException());
		}

		protected override void UnregisteredCancellation()
		{
			switch (_InstanceState)
			{
			case Status.Disposed:
				_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of"));
				break;

			case Status.Decremented:
				_TaskSource.SetResult(Counters); // When we're waiting, we always take only one counter
				break;
			}
		}

		public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

		public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		public int GetResult(short token)
		{
			try
			{
				return _TaskSource.GetResult(token);
			}
			finally
			{
				// Don't release if we're on the wait queue. If we are, let TrySwitchToCompleted know it can release when ready
				if (_InstanceState == Status.CancelledNotWaiting || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
					Release();
			}
		}

		void IValueTaskSource.GetResult(short token) => GetResult(token);

		//****************************************

		private void Release()
		{
			Owner = null;
			IsPeek = false;
			Counters = 0;

			_TaskSource.Reset();
			_InstanceState = Status.Unused;
			ResetCancellation();

			Instances.Add(this);
		}

		//****************************************

		public AsyncCounter? Owner { get; private set; }

		public bool IsPending => _InstanceState == Status.Pending;

		public bool IsPeek { get; private set; }

		public int Counters { get; private set; }

		public short Version => _TaskSource.Version;

		//****************************************

		internal static AsyncCounterDecrement GetOrCreateFor(AsyncCounter counter, bool isPeek, bool toZero, bool isTaken)
		{
			if (!Instances.TryTake(out var Instance))
				Instance = new AsyncCounterDecrement();

			Instance.Initialise(counter, isPeek, toZero, isTaken);

			return Instance;
		}

		//****************************************

		private static class Status
		{
			/// <summary>
			/// The decrement was cancelled and is currently on the list of waiters
			/// </summary>
			internal const int CancelledGotResult = -4;
			/// <summary>
			/// The decrement is cancelled and waiting for GetResult
			/// </summary>
			internal const int CancelledNotWaiting = -3;
			/// <summary>
			/// The decrement was cancelled and is currently on the list of waiters while waiting for GetResult
			/// </summary>
			internal const int Cancelled = -2;
			/// <summary>
			/// The decrement was disposed of
			/// </summary>
			internal const int Disposed = -1;
			/// <summary>
			/// A decrement starts in the Unused state
			/// </summary>
			internal const int Unused = 0;
			/// <summary>
			/// The decrement has been applied, and waiting for GetResult
			/// </summary>
			internal const int Decremented = 1;
			/// <summary>
			/// The decrement is on the list of waiters
			/// </summary>
			internal const int Pending = 2;
		}
	}
}
