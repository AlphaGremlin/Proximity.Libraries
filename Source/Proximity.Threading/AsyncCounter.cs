using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for a counter that is always positive or zero supporting async/await
	/// </summary>
	public sealed class AsyncCounter : IDisposable
	{ //****************************************
		private readonly WaiterQueue<AsyncCounterDecrement> _Waiters = new WaiterQueue<AsyncCounterDecrement>();
		private readonly WaiterQueue<AsyncCounterDecrement> _PeekWaiters = new WaiterQueue<AsyncCounterDecrement>();

		private int _CurrentCount;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Counter with a zero count
		/// </summary>
		public AsyncCounter() : this(0)
		{
		}

		/// <summary>
		/// Creates a new Asynchronous Counter with the given count
		/// </summary>
		/// <param name="initialCount">The initial count</param>
		public AsyncCounter(int initialCount)
		{
			if (initialCount < 0)
				throw new ArgumentException("Initial Count is invalid");

			_CurrentCount = initialCount;
		}

		//****************************************

		/// <summary>
		/// Disposes of the counter, cancelling any waiters
		/// </summary>
		public void Dispose()
		{ //****************************************
			var MyWait = new SpinWait();
			int OldCount;
			//****************************************

			for (; ; )
			{
				OldCount = _CurrentCount;

				// Are we already disposed?
				if (OldCount < 0)
					return;

				// Try and update the counter
				if (Interlocked.CompareExchange(ref _CurrentCount, ~OldCount, OldCount) == OldCount)
				{
					// Success, now close any pending waiters
					DisposeWaiters();
					return;
				}

				// Failed. Spin and try again
				MyWait.SpinOnce();
			}
		}

		/// <summary>
		/// Disposes of the counter if the count is zero
		/// </summary>
		/// <returns>True if the counter was disposed of, otherwise False</returns>
		public bool DisposeIfZero()
		{ //****************************************
			var MyWait = new SpinWait();
			int OldCount;
			//****************************************

			for (; ; )
			{
				OldCount = _CurrentCount;

				// Are we zero?
				if (OldCount != 0)
					return false;

				// Try and update the counter
				if (Interlocked.CompareExchange(ref _CurrentCount, ~OldCount, OldCount) == OldCount)
				{
					// Success, now close any pending waiters
					DisposeWaiters();
					return true;
				}

				// Failed. Spin and try again
				MyWait.SpinOnce();
			}
		}

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		public ValueTask Decrement(TimeSpan timeout)
		{
			if (timeout == Timeout.InfiniteTimeSpan)
				return Decrement();

			if (timeout == TimeSpan.Zero)
			{
				if (TryDecrement())
					return default;

				throw new OperationCanceledException();
			}

			if (timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// Try and decrement without waiting
			if (_Waiters.IsEmpty && InternalTryDecrement())
				return default;

			// No free counters, are we disposed?
			if (_CurrentCount == -1)
				throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");

			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, false, false);

			var CancelSource = new CancellationTokenSource(timeout);

			Instance.ApplyCancellation(CancelSource.Token, CancelSource);

			// No free counters, add ourselves to the queue waiting on a counter
			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy?
			if (InternalTryDecrement())
				ForceIncrement(); // Try and activate a waiter, or at least increment

			return new ValueTask(Instance, Instance.Version);
		}

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		public ValueTask Decrement(CancellationToken token = default)
		{
			// Try and decrement without waiting
			if (_Waiters.IsEmpty && InternalTryDecrement())
				return default;

			// No free counters, are we disposed?
			if (_CurrentCount == -1)
				throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");

			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, false, false);

			Instance.ApplyCancellation(token, null);

			// No free counters, add ourselves to the queue waiting on a counter
			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy?
			if (InternalTryDecrement())
				ForceIncrement(); // Try and activate a waiter, or at least increment

			return new ValueTask(Instance, Instance.Version);
		}

		/// <summary>
		/// Tries to decrement the counter without waiting
		/// </summary>
		/// <returns>True if the counter was decremented without waiting, otherwise False</returns>
		public bool TryDecrement() => _Waiters.IsEmpty && InternalTryDecrement();

		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented without waiting, otherwise False due to disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryDecrement(CancellationToken token) => TryDecrement(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented, otherwise False due to timeout or disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryDecrement(TimeSpan timeout, CancellationToken token = default)
		{
			// First up, try and take the counter if possible
			if (_Waiters.IsEmpty && InternalTryDecrement())
				return true;

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero)
				return false;

			// No free counters, are we disposed?
			if (_CurrentCount == -1)
				return false;

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, false, false);

			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy?
			if (InternalTryDecrement())
				ForceIncrement(); // Try and activate a waiter, or at least increment

			//****************************************

			// Are we okay with blocking indefinitely (or until the token is raised)?
			if (timeout != Timeout.InfiniteTimeSpan)
			{
				var TokenSource = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token) : new CancellationTokenSource();

				TokenSource.CancelAfter(timeout);

				// Prepare the cancellation token
				Instance.ApplyCancellation(TokenSource.Token, TokenSource);
			}
			else
			{
				// Prepare the cancellation token (if any)
				Instance.ApplyCancellation(token, null);
			}

			try
			{
				lock (Instance)
				{
					// Queue this inside the lock, so Pulse cannot execute until we've reached Wait
					Instance.OnCompleted((state) => { lock (state!) Monitor.Pulse(state); }, Instance, Instance.Version, ValueTaskSourceOnCompletedFlags.None);

					// Wait for a definitive result
					Monitor.Wait(Instance);
				}

				// Result retrieved, now check if we were cancelled
				Instance.GetResult(Instance.Version);

				return true;
			}
			catch
			{
				// We may get a cancel or dispose exception
				if (!token.IsCancellationRequested)
					return false; // If it's not the original token, we cancelled due to a Timeout. Return false

				throw; // Throw the cancellation
			}
		}

		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <remarks>The counter is not guaranteed to be incremented when this method returns, as waiters are evaluated on the ThreadPool. It will be incremented 'soon'.</remarks>
		public bool TryIncrement()
		{
			// Try and retrieve a waiter
			while (_Waiters.TryDequeue(out var NextInstance))
			{
				// Try and release this Instance
				if (NextInstance.TrySwitchToCompleted())
					return true;
			}

			//****************************************

			// Nobody is waiting, so we can try and increment the counter. This will release anybody peeking
			if (!InternalTryIncrement())
				return false;

			// Is anybody waiting now?
			if (_Waiters.IsEmpty)
				return true; // No, so nothing to do

			// Someone is waiting. Try and take the counter back
			if (!InternalTryDecrement())
				return true; // Failed to take the counter back (pre-empted by a peek waiter or concurrent decrement), so we're done

			// Success. Forcibly increment, even if we've been disposed
			// This will attempt to pass the counter to a waiter and if it fails, will put it back even if we dispose
			ForceIncrement();

			return true;
		}

		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <remarks>The counter is not guaranteed to be incremented when this method returns, as waiters are evaluated on the ThreadPool. It will be incremented 'soon'.</remarks>
		public void Increment()
		{
			if (!TryIncrement())
				throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");
		}

		/// <summary>
		/// Waits until it's possible to decrement this counter
		/// </summary>
		/// <param name="token">A cancellation token to stop waiting</param>
		/// <returns>A task that completes when the counter is available for immediate decrementing</returns>
		/// <remarks>This will only succeed when nobody is waiting on a Decrement operation, so Decrement operations won't be waiting while the counter is non-zero</remarks>
		public ValueTask PeekDecrement(CancellationToken token = default)
		{
			if (!TryPeekDecrement(token, out var Instance))
				throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");

			return new ValueTask(Instance, Instance.Version);
		}

		/// <summary>
		/// Checks if it's possible to decrement this counter
		/// </summary>
		/// <returns>True if the counter can be decremented, otherwise False</returns>
		public bool TryPeekDecrement()
		{
			var MyCount = _CurrentCount;

			return (MyCount > 0 || MyCount < -1);
		}

		//****************************************

		/// <summary>
		/// Always increments, regardless of disposal state
		/// </summary>
		/// <remarks>Used for DecrementAny</remarks>
		internal void ForceIncrement()
		{ //****************************************
			var MyWait = new SpinWait();
			int OldCount, NewCount;
			//****************************************

			do
			{
				// Try and retrieve a waiter
				while (_Waiters.TryDequeue(out var NextInstance))
				{
					// Try and release this Instance
					if (NextInstance.TrySwitchToCompleted())
						return;
				}

				//****************************************

				// Forcibly increment the counter
				for (; ; )
				{
					OldCount = _CurrentCount;

					// Are we disposed?
					if (OldCount < 0)
						NewCount = OldCount - 1; // Yes, subtract to take us further from -1
					else
						NewCount = OldCount + 1; // No, add to take us further from 0

					// Update the counter
					if (Interlocked.CompareExchange(ref _CurrentCount, NewCount, OldCount) == OldCount)
						break;

					// Failed, spin and try again
					MyWait.SpinOnce();
				}

				// Let any peekers know they can try and take a counter
				ReleasePeekers();

				// Is anybody waiting now?
				if (_Waiters.IsEmpty)
					return; // No, so nothing to do

				// Someone is waiting. Try and take the counter back, regardless of if there's a waiter
			} while (InternalTryDecrement());

			// Failed to take the counter back, so we're done
			if (_CurrentCount == -1)
			{
				DisposeWaiters();

				return;
			}
		}

		internal bool TryPeekDecrement(CancellationToken token, out AsyncCounterDecrement decrement)
		{
			var MyCount = _CurrentCount;

			// No free counters, are we disposed?
			if (MyCount == -1)
			{
				decrement = null!;

				return false;
			}

			// Are we able to decrement?
			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, true, MyCount > 0 || MyCount < -1);

			if (Instance.GetStatus(Instance.Version) == ValueTaskSourceStatus.Pending)
			{
				Instance.ApplyCancellation(token, null);

				// Create a new waiter and add it to the queue
				_PeekWaiters.Enqueue(Instance);

				// Was a counter added while we were busy?
				if (TryPeekDecrement())
					// Let any peekers know they can try and take a counter
					ReleasePeekers();
			}

			decrement = Instance;

			return true;
		}

		internal bool CancelDecrement(AsyncCounterDecrement decrement)
		{
			if (decrement.IsPeek)
				return _PeekWaiters.Erase(decrement);
			else
				return _Waiters.Erase(decrement);
		}

		//****************************************

		private bool InternalTryIncrement()
		{ //****************************************
			var MyWait = new SpinWait();
			int OldCount;
			//****************************************

			for (; ; )
			{
				OldCount = _CurrentCount;

				// Are we disposed?
				if (OldCount < 0)
					return false;

				// No, so we can add a counter to the pool
				if (Interlocked.CompareExchange(ref _CurrentCount, OldCount + 1, OldCount) == OldCount)
				{
					ReleasePeekers();

					return true;
				}

				// Failed, spin and try again
				MyWait.SpinOnce();
			}
		}

		private bool InternalTryDecrement()
		{ //****************************************
			var MyWait = new SpinWait();
			int OldCount, NewCount;
			//****************************************

			for (; ; )
			{
				OldCount = _CurrentCount;

				// If we're equal or less than zero, we've either no counter or we're being disposed of
				if (OldCount <= 0)
				{
					// If we're 0, there's no counter to decrement
					// If we're -1, there's no counter to decrement and we're disposed
					if (OldCount >= -1)
						return false;

					// Add one to bring us closer to -1 
					NewCount = OldCount + 1;
				}
				else
				{
					// Subtract one to bring us closer to 0
					NewCount = OldCount - 1;
				}

				// Try and update the counter
				if (Interlocked.CompareExchange(ref _CurrentCount, NewCount, OldCount) == OldCount)
					return true;

				// Failed. Spin and try again
				MyWait.SpinOnce();
			}
		}

		private void DisposeWaiters()
		{ //****************************************
			AsyncCounterDecrement? Instance;
			//****************************************

			// Success, now close any pending waiters
			while (_Waiters.TryDequeue(out Instance))
				Instance.SwitchToDisposed();

			while (_PeekWaiters.TryDequeue(out Instance))
				Instance.SwitchToDisposed();
		}

		private void ReleasePeekers()
		{
			// Try and release every Peek Waiter
			while (_PeekWaiters.TryDequeue(out var NextInstance))
			{
				// Try and release this Instance
				NextInstance.TrySwitchToCompleted();
			}
		}

		//****************************************

		/// <summary>
		/// Gets the current count
		/// </summary>
		public int CurrentCount
		{
			get
			{
				var MyCount = _CurrentCount;

				return MyCount < 0 ? ~MyCount : MyCount;
			}
		}

		/// <summary>
		/// Gets the number of operations waiting to decrement the counter
		/// </summary>
		public int WaitingCount => _Waiters.Count;

		//****************************************

		/// <summary>
		/// Decrements the first available counter
		/// </summary>
		/// <param name="counters">The counters to try and decrement</param>
		/// <returns>A task returning the counter that was decremented</returns>
		public static ValueTask<AsyncCounter> DecrementAny(params AsyncCounter[] counters) => DecrementAny(counters, CancellationToken.None);

		/// <summary>
		/// Decrements the first available counter
		/// </summary>
		/// <param name="token">A cancellation token to abort decrementing</param>
		/// <param name="counters">The counters to try and decrement</param>
		/// <returns>A task returning the counter that was decremented</returns>
		public static ValueTask<AsyncCounter> DecrementAny(CancellationToken token, params AsyncCounter[] counters) => DecrementAny(counters, token);

		/// <summary>
		/// Decrements the first available counter
		/// </summary>
		/// <param name="counters">The counters to try and decrement</param>
		/// <param name="token">A cancellation token to abort decrementing</param>
		/// <returns>A task returning the counter that was decremented</returns>
		public static ValueTask<AsyncCounter> DecrementAny(IEnumerable<AsyncCounter> counters, CancellationToken token = default)
		{ //****************************************
			var CounterSet = counters.ToArray();
			//****************************************

			// Can we immediately decrement any of the counters?
			for (var Index = 0; Index < CounterSet.Length; Index++)
			{
				if (CounterSet[Index].TryDecrement())
					return new ValueTask<AsyncCounter>(CounterSet[Index]);
			}

			// No, so assign PeekDecrement on all the counters
			var Operation = AsyncCounterDecrementAny.GetOrCreate(token);

			for (var Index = 0; Index < CounterSet.Length; Index++)
				Operation.Attach(CounterSet[Index]);

			Operation.ApplyCancellation(null);

			return new ValueTask<AsyncCounter>(Operation, Operation.Version);
		}

		/// <summary>
		/// Tries to decrement one of the given counters without waiting
		/// </summary>
		/// <param name="counters">The set of counters to try and decrement</param>
		/// <param name="counter">The counter that was successfully decremented, or null if none were able to be immediately decremented</param>
		/// <returns>True if a counter was decremented, otherwise False</returns>
		public static bool TryDecrementAny(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
# endif
			out AsyncCounter counter, params AsyncCounter[] counters) => TryDecrementAny(counters, out counter!);

		/// <summary>
		/// Tries to decrement one of the given counters without waiting
		/// </summary>
		/// <param name="counters">The set of counters to try and decrement</param>
		/// <param name="counter">The counter that was successfully decremented, or null if none were able to be immediately decremented</param>
		/// <returns>True if a counter was decremented, otherwise False</returns>
		public static bool TryDecrementAny(IEnumerable<AsyncCounter> counters,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out AsyncCounter counter)
		{
			foreach (var MyCounter in counters)
			{
				if (MyCounter.TryDecrement())
				{
					counter = MyCounter;

					return true;
				}
			}

			counter = null!;

			return false;
		}
	}
}
