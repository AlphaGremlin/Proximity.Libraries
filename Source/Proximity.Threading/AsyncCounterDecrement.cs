using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace System.Threading
{
	internal sealed class AsyncCounterDecrement : IValueTaskSource
	{ //****************************************
		private static readonly ConcurrentBag<AsyncCounterDecrement> _Instances = new ConcurrentBag<AsyncCounterDecrement>();
		//****************************************
		private volatile int _InstanceState;

		private ManualResetValueTaskSourceCore<VoidStruct> _TaskSource = new ManualResetValueTaskSourceCore<VoidStruct>();

		private CancellationTokenRegistration _Registration;
		private CancellationTokenSource? _TokenSource;
		//****************************************

		internal AsyncCounterDecrement() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		internal void Initialise(AsyncCounter owner, bool isPeek, bool wasDecremented)
		{
			Owner = owner;
			IsPeek = IsPeek;

			if (wasDecremented)
			{
				_InstanceState = Status.Decremented;
				_TaskSource.SetResult(default);
			}
			else
			{
				_InstanceState = Status.Pending;
			}
		}

		internal void ApplyCancellation(CancellationToken token, CancellationTokenSource? tokenSource)
		{
			if (token.CanBeCanceled)
			{
				Token = token;

				if (_InstanceState != Status.Pending)
					throw new InvalidOperationException("Cannot register for cancellation when not pending");

				_TokenSource = tokenSource;

				_Registration = token.Register((state) => ((AsyncCounterDecrement)state).SwitchToCancelled(), this);
			}
		}

		internal void SwitchToDisposed()
		{
			// Called when an Instance is removed from the Waiters queue due to a Dispose
			if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
				return;

			_Registration.Dispose();
			_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of"));
		}

		internal void SwitchToCancelled()
		{
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
				return; // Instance is no longer in a cancellable state

			var Owner = this.Owner!;

			_Registration.Dispose();
			_TaskSource.SetException(new OperationCanceledException(Token));

			Owner.CancelDecrement(this);
		}

		internal bool TrySwitchToCompleted()
		{
			// Try and assign the counter to this Instance
			if (Interlocked.CompareExchange(ref _InstanceState, Status.Decremented, Status.Pending) == Status.Pending)
			{
				_Registration.Dispose();
				_TaskSource.SetResult(default);

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
					_Registration.Dispose();

					return false;
				}
			}
			while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

			_Registration.Dispose();

			if (InstanceState == Status.CancelledGotResult)
				Release(); // GetResult has been called, so we can return to the pool

			return false;
		}

		//****************************************

		public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

		public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		public void GetResult(short token)
		{
			try
			{
				_TaskSource.GetResult(token);
			}
			finally
			{
				// Don't release if we're on the wait queue. If we are, let TrySwitchToCompleted know it can release when ready
				if (_InstanceState != Status.Cancelled || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
					Release();
			}
		}

		//****************************************

		private void Release()
		{
			Owner = null;
			Token = default;
			IsPeek = false;
			_TaskSource.Reset();
			_TokenSource?.Dispose();
			_TokenSource = null;
			_InstanceState = Status.Unused;

			_Instances.Add(this);
		}

		//****************************************

		public AsyncCounter? Owner { get; private set; }

		public bool IsPending => _InstanceState == Status.Pending;

		public bool IsPeek { get; private set; }

		public CancellationToken Token { get; private set; }

		public short Version => _TaskSource.Version;

		//****************************************

		internal static AsyncCounterDecrement GetOrCreateFor(AsyncCounter counter, bool isPeek, bool isTaken)
		{
			if (!_Instances.TryTake(out var Instance))
				Instance = new AsyncCounterDecrement();

			Instance.Initialise(counter, isPeek, isTaken);

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
