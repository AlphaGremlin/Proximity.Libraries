using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides the functionality for AsyncCounter.DecrementAny
	/// </summary>
	internal sealed class AsyncCounterDecrementAny : BaseCancellable, IValueTaskSource<AsyncCounter>
	{ //****************************************
		private static readonly ConcurrentBag<AsyncCounterDecrementAny> Instances = new ConcurrentBag<AsyncCounterDecrementAny>();
		private static readonly ConcurrentBag<PeekDecrementWaiter> Waiters = new ConcurrentBag<PeekDecrementWaiter>();
		//****************************************
		private int _InstanceState;
		private int _IsStarted;

		private ManualResetValueTaskSourceCore<AsyncCounter> _TaskSource = new ManualResetValueTaskSourceCore<AsyncCounter>();

		private int _OutstandingCounters;
		private AsyncCounter? _WinningCounter;
		//****************************************

		internal AsyncCounterDecrementAny() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		internal void Initialise(CancellationToken token, TimeSpan timeout)
		{
			_InstanceState = Status.Pending;

			RegisterCancellation(token, timeout);
		}

		internal void Attach(AsyncCounter counter)
		{
			if (!counter.TryPeekDecrement(AsyncCounterFlags.None, Timeout.InfiniteTimeSpan, Token, out var Decrement))
				return; // Counter is disposed, ignore it

			var Waiter = PeekDecrementWaiter.GetOrCreate(this, Decrement);

			Interlocked.Increment(ref _OutstandingCounters);

			Waiter.Attach(Decrement);
		}

		internal void Activate()
		{
			Interlocked.Exchange(ref _IsStarted, 1);

			if (_InstanceState != Status.Pending)
				return; // No need to do anything with cancellation, because we've already decremented/disposed

			if (Volatile.Read(ref _OutstandingCounters) == 0)
			{
				// All the counters have activated, but we're still pending, which means they're all disposed
				TrySwitchToDisposed();

				return;
			}
		}

		//****************************************

		protected override void SwitchToCancelled()
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return; // Too late, we already have a result

			// The cancellation token was raised
			_TaskSource.SetException(CreateCancellationException());
		}

		protected override void UnregisteredCancellation()
		{
			switch (_InstanceState)
			{
			case Status.Disposed:
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Disposed) != Status.Disposed)
					throw new InvalidOperationException("Counter Decrement Any is in an invalid state");

				_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncCounterDecrementAny), "All counters have been disposed"));
				break;

			case Status.Waiting:
				_TaskSource.SetResult(_WinningCounter!);
				break;
			}
		}

		ValueTaskSourceStatus IValueTaskSource<AsyncCounter>.GetStatus(short token) => _TaskSource.GetStatus(token);

		void IValueTaskSource<AsyncCounter>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		AsyncCounter IValueTaskSource<AsyncCounter>.GetResult(short token)
		{
			try
			{
				return _TaskSource.GetResult(token);
			}
			finally
			{
				// We've received our result, mark as such
				if (Interlocked.CompareExchange(ref _InstanceState, Status.GotResult, Status.Waiting) != Status.Waiting)
					throw new InvalidOperationException("Counter Decrement Any is in an invalid state");

				// If there are no more counters we're attached to, try and release us
				if (Volatile.Read(ref _OutstandingCounters) == 0)
					TryRelease();
			}
		}

		//****************************************

		private bool TrySwitchToCompleted(AsyncCounter counter)
		{
			// State can be one of: Pending, Waiting, Disposed, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return false; // Too late, we already have a result

			_WinningCounter = counter;

			UnregisterCancellation();

			return true;
		}

		private bool TrySwitchToDisposed()
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Disposed
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
				return false; // Too late, we already have a result

			UnregisterCancellation();

			return true;
		}

		private void TryRelease()
		{
			if (_InstanceState == Status.GotResult || Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.GotResult) != Status.GotResult)
				return; // We're waiting on GetResult to execute

			_TaskSource.Reset();
			_IsStarted = 0;
			_WinningCounter = null;
			ResetCancellation();

			Instances.Add(this);
		}

		private void OnPeekCompleted(AsyncCounter counter)
		{
			// Can be one of: Pending, Waiting, GotResult
			if (_InstanceState == Status.Pending)
			{
				if (counter.TryDecrement())
				{
					if (TrySwitchToCompleted(counter))
						return; // Success, no need to try and release since we'll do that after GetResult

					// We can fail to switch to completion if we're cancelled - in which case we should return the counter we just took
					// This may fail if the counter was disposed mid-operation
					if (counter.TryIncrement())
						return;
				}
				else
				{
					// We failed to decrement this counter, but we're still pending, so try again
					if (counter.TryPeekDecrement(AsyncCounterFlags.None, Timeout.InfiniteTimeSpan, Token, out var Decrement))
					{
						var Waiter = PeekDecrementWaiter.GetOrCreate(this, Decrement);

						Waiter.Attach(Decrement);

						// NOTE: If we implement early-clear on the queue, we could accidentally revive the object here

						return;
					}
				}

				// Counter is disposed, fail
				OnPeekFailed();

				return;
			}

			// If we're the last peek waiter, try and release
			if (Interlocked.Decrement(ref _OutstandingCounters) <= 0)
				TryRelease();
		}

		private void OnPeekFailed()
		{
			if (Interlocked.Decrement(ref _OutstandingCounters) > 0)
				return; // There are other counters still waiting

			if (Volatile.Read(ref _IsStarted) == 0)
				return; // Still adding counters

			if (TrySwitchToDisposed())
				return; // Dispose
		}

		//****************************************

		public short Version => _TaskSource.Version;

		//****************************************

		internal static AsyncCounterDecrementAny GetOrCreate(CancellationToken token, TimeSpan timeout)
		{
			if (!Instances.TryTake(out var Instance))
				Instance = new AsyncCounterDecrementAny();

			Instance.Initialise(token, timeout);

			return Instance;
		}

		//****************************************

		private sealed class PeekDecrementWaiter
		{ //****************************************
			private readonly Action _ContinuePeekDecrement;

			private AsyncCounterDecrementAny? _Operation;
			private AsyncCounterDecrement? _Decrement;

			private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			public PeekDecrementWaiter()
			{
				_ContinuePeekDecrement = OnContinuePeekDecrement;
			}

			//****************************************

			public void Attach(AsyncCounterDecrement decrement)
			{
				_Decrement = decrement;

				var Task = new ValueTask(decrement, decrement.Version);

				_Awaiter = Task.ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					OnContinuePeekDecrement();
				else
					_Awaiter.OnCompleted(_ContinuePeekDecrement);
			}

			//****************************************

			private void OnContinuePeekDecrement()
			{
				var Owner = _Decrement!.Owner!;

				try
				{
					_Awaiter.GetResult();

					_Operation!.OnPeekCompleted(Owner);
				}
				catch
				{
					_Operation!.OnPeekFailed();
				}
				finally
				{
					Release();
				}
			}

			private void Release()
			{
				_Operation = null;
				_Decrement = null;
				_Awaiter = default;

				Waiters.Add(this);
			}

			//****************************************

			internal static PeekDecrementWaiter GetOrCreate(AsyncCounterDecrementAny operation, AsyncCounterDecrement decrement)
			{
				if (!Waiters.TryTake(out var Waiter))
					Waiter = new PeekDecrementWaiter();

				Waiter._Operation = operation;
				Waiter._Decrement = decrement;

				return Waiter;
			}
		}

		private static class Status
		{
			/// <summary>
			/// The decrement has been disposed, and waiting for cancellation cleanup
			/// </summary>
			internal const int Disposed = -1;
			/// <summary>
			/// A decrement-any starts in the Unused state
			/// </summary>
			internal const int Unused = 0;
			/// <summary>
			/// The decrement-any is waiting for a counter to decrement
			/// </summary>
			internal const int Pending = 1;
			/// <summary>
			/// The decrement has been applied/cancelled/disposed, and waiting for GetResult
			/// </summary>
			internal const int Waiting = 2;
			/// <summary>
			/// The decrement has been applied/cancelled/disposed, but is still attached to one or more counters
			/// </summary>
			internal const int GotResult = 3;
		}
	}
}
