using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for a counter that is always positive or zero supporting async/await
	/// </summary>
	public sealed class AsyncCounter : IDisposable
	{ //****************************************
		private readonly WaiterQueue<AsyncCounterDecrement> _Waiters = new();
		private readonly WaiterQueue<AsyncCounterDecrement> _PeekWaiters = new();

		private int _CurrentCount;

		private LockDisposer? _Disposer;
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
		/// Increments the Counter
		/// </summary>
		/// <param name="count">The amount to increment the counter by</param>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		public void Add(int count)
		{
			if (!TryAdd(count))
				throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");
		}

		/// <summary>
		/// Disposes of the counter, cancelling any waiters
		/// </summary>
		/// <returns>A Task that completes when the Counter drops to zero</returns>
		public ValueTask DisposeAsync()
		{
			if (_Disposer == null && Interlocked.CompareExchange(ref _Disposer, new LockDisposer(), null) == null)
			{
				// Success, now close any pending waiters
				DisposeWaiters();

				// If there's no counters, we can complete the dispose task
				if (Interlocked.CompareExchange(ref _CurrentCount, -1, 0) == 0)
					_Disposer.SwitchToComplete();
			}

			return new ValueTask(_Disposer.Task);
		}

		void IDisposable.Dispose() => DisposeAsync();

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		public ValueTask Decrement(CancellationToken token = default) => Decrement(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		public ValueTask Decrement(TimeSpan timeout, CancellationToken token = default)
		{
			if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			if (timeout == TimeSpan.Zero)
			{
				if (TryDecrement())
					return default;

				if (_Disposer != null)
					throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");

				throw new TimeoutException();
			}

			// Try and decrement without waiting
			if (_Waiters.IsEmpty && InternalTryDecrement())
				return default;

			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");

			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, 1, AsyncCounterFlags.ThrowOnDispose, false);

			Instance.ApplyCancellation(token, timeout);

			// No free counters, add ourselves to the queue waiting on a counter
			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy? If so, try to pass it to a waiter
			if (!(InternalTryDecrement() && TryIncrement()) && _Disposer != null)
				DisposeWaiters(); // We disposed during processing

			return new ValueTask(Instance, Instance.Version);
		}

		/// <summary>
		/// Attempts to decrement the Counter at most <paramref name="maximum"/> times
		/// </summary>
		/// <param name="maximum">The maximum number of times to decrement the Counter</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		/// <remarks>The result will be between one and <paramref name="maximum"/></remarks>
		public ValueTask<int> Decrement(int maximum, CancellationToken token = default) => InternalDecrement(maximum, AsyncCounterFlags.ThrowOnDispose, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to decrement the Counter at most <paramref name="maximum"/> times
		/// </summary>
		/// <param name="maximum">The maximum number of times to decrement the Counter</param>
		/// <param name="timeout">The amount of time to wait</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		/// <remarks>The result will be between one and <paramref name="maximum"/></remarks>
		public ValueTask<int> Decrement(int maximum, TimeSpan timeout, CancellationToken token = default) => InternalDecrement(maximum, AsyncCounterFlags.ThrowOnDispose, timeout, token);

		/// <summary>
		/// Attempts to decrement the Counter one or more times
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		public ValueTask<int> DecrementToZero(CancellationToken token = default) => InternalDecrement(0, AsyncCounterFlags.ThrowOnDispose, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to decrement the Counter one or more times
		/// </summary>
		/// <param name="timeout">The amount of time to wait</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		public ValueTask<int> DecrementToZero(TimeSpan timeout, CancellationToken token = default) => InternalDecrement(0, AsyncCounterFlags.ThrowOnDispose, timeout, token);

		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <exception cref="ObjectDisposedException">The counter has been disposed and has reached zero</exception>
		public void Increment()
		{
			if (!TryIncrement())
				throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");
		}

		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <param name="count">The amount to increment the counter by</param>
		public bool TryAdd(int count)
		{
			if (_Disposer != null)
				return false;

			for (; ; )
			{
				// Try and retrieve a waiter
				while (_Waiters.TryDequeue(out var NextInstance))
				{
					// Try and release this Instance
					if (NextInstance.TrySwitchToCompleted(ref count) && count == 0)
					{
						return true;
					}
				}

				//****************************************

				// Nobody is waiting, so we can try and add any remaining counters. This will release anybody peeking
				if (!InternalTryAdd(count))
					return false; // Disposed

				// Is anybody waiting now?
				if (_Waiters.IsEmpty)
					return true; // No, so nothing to do

				// Someone is waiting. Try and take the counter(s) back
				if (!InternalTryDecrement(0, out count))
					return true; // Failed to take the counter back (pre-empted by a peek waiter or concurrent decrement), so we're done
			}
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
		/// <returns>True if the counter was decremented without waiting, False if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryDecrement(CancellationToken token) => InternalTryDecrementBlocking(1, out _, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented, False if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryDecrement(TimeSpan timeout, CancellationToken token = default) => InternalTryDecrementBlocking(1, out _, timeout, token);

		/// <summary>
		/// Tries to decrement the Counter at most <paramref name="maximum"/> times without waiting
		/// </summary>
		/// <param name="maximum">The maximum number of times to decrement the Counter</param>
		/// <param name="count">The number of times the counter was decremented</param>
		/// <returns>True if the counter was decremented without waiting, otherwise False</returns>
		public bool TryDecrement(int maximum, out int count)
		{
			if (_Waiters.IsEmpty)
				return InternalTryDecrement(maximum, out count);

			count = 0;

			return false;
		}

		/// <summary>
		/// Blocks attempting to decrement the Counter at most <paramref name="maximum"/> times
		/// </summary>
		/// <param name="maximum">The maximum number of times to decrement the Counter</param>
		/// <param name="count">The number of times the counter was decremented</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented without waiting, False if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryDecrement(int maximum, out int count, CancellationToken token) => InternalTryDecrementBlocking(maximum, out count, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks attempting to decrement the Counter at most <paramref name="maximum"/> times
		/// </summary>
		/// <param name="maximum">The maximum number of times to decrement the Counter</param>
		/// <param name="count">The number of times the counter was decremented</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented, False if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryDecrement(int maximum, out int count, TimeSpan timeout, CancellationToken token = default) => InternalTryDecrementBlocking(maximum, out count, timeout, token);

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that returns True when we were able to decrement the counter, False if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<bool> TryDecrementAsync(CancellationToken token = default) => TryDecrementAsync(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that returns True when we were able to decrement the counter, False if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<bool> TryDecrementAsync(TimeSpan timeout, CancellationToken token = default)
		{
			if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			if (timeout == TimeSpan.Zero)
			{
				if (TryDecrement())
					return new ValueTask<bool>(true);

				if (_Disposer != null)
					return new ValueTask<bool>(false);

				throw new TimeoutException();
			}

			// Try and decrement without waiting
			if (_Waiters.IsEmpty && InternalTryDecrement())
				return new ValueTask<bool>(true);

			if (_Disposer != null)
				return new ValueTask<bool>(false);

			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, 1, AsyncCounterFlags.None, false);

			Instance.ApplyCancellation(token, timeout);

			// No free counters, add ourselves to the queue waiting on a counter
			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy? If so, try to pass it to a waiter
			if (!(InternalTryDecrement() && TryIncrement()) && _Disposer != null)
				DisposeWaiters(); // We disposed during processing

			return new ValueTask<bool>(Instance, Instance.Version);
		}

		/// <summary>
		/// Attempts to decrement the Counter at most <paramref name="maximum"/> times
		/// </summary>
		/// <param name="maximum">The maximum number of times to decrement the Counter</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented, or -1 if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <remarks>The result will be between one and <paramref name="maximum"/>, or -1 if disposed</remarks>
		public ValueTask<int> TryDecrementAsync(int maximum, CancellationToken token = default) => InternalDecrement(maximum, AsyncCounterFlags.None, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to decrement the Counter at most <paramref name="maximum"/> times
		/// </summary>
		/// <param name="maximum">The maximum number of times to decrement the Counter</param>
		/// <param name="timeout">The amount of time to wait</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented, or -1 if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		/// <remarks>The result will be between one and <paramref name="maximum"/>, or -1 if disposed</remarks>
		public ValueTask<int> TryDecrementAsync(int maximum, TimeSpan timeout, CancellationToken token = default) => InternalDecrement(maximum, AsyncCounterFlags.None, timeout, token);

		/// <summary>
		/// Tries to decrement the counter without waiting
		/// </summary>
		/// <param name="count">The number of times the counter was decremented</param>
		/// <returns>True if the counter was decremented at least once without waiting, otherwise False</returns>
		public bool TryDecrementToZero(out int count)
		{
			if (_Waiters.IsEmpty)
				return InternalTryDecrement(0, out count);

			count = 0;

			return false;
		}

		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="count">The number of times the counter was decremented</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented at least once without waiting, otherwise False due to disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryDecrementToZero(out int count, CancellationToken token) => InternalTryDecrementBlocking(0, out count, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="count">The number of times the counter was decremented</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented at least once, False if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryDecrementToZero(out int count, TimeSpan timeout, CancellationToken token = default) => InternalTryDecrementBlocking(0, out count, timeout, token);

		/// <summary>
		/// Attempts to decrement the Counter one or more times
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented, or -1 if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<int> TryDecrementToZeroAsync(CancellationToken token = default) => InternalDecrement(0, AsyncCounterFlags.None, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to decrement the Counter one or more times
		/// </summary>
		/// <param name="timeout">The amount of time to wait</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter, returning the number of times it was decremented, or -1 if the counter was disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<int> TryDecrementToZeroAsync(TimeSpan timeout, CancellationToken token = default) => InternalDecrement(0, AsyncCounterFlags.None, timeout, token);

		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <remarks>The counter is not guaranteed to be incremented when this method returns, as waiters are evaluated on the ThreadPool. It will be incremented 'soon'.</remarks>
		public bool TryIncrement() => TryAdd(1);

		/// <summary>
		/// Waits until it's possible to decrement this counter
		/// </summary>
		/// <param name="token">A cancellation token to stop waiting</param>
		/// <returns>A task that returns True when the counter is available for immediate decrementing, False if the counter has been disposed</returns>
		/// <remarks>This will only succeed when nobody is waiting on a Decrement operation, so Decrement operations won't be waiting while the counter is non-zero</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<bool> PeekDecrement(CancellationToken token = default)
		{
			if (!TryPeekDecrement(AsyncCounterFlags.None, Timeout.InfiniteTimeSpan, token, out var Instance))
				return new ValueTask<bool>(false);

			return new ValueTask<bool>(Instance, Instance.Version);
		}

		/// <summary>
		/// Waits until it's possible to decrement this counter
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token to stop waiting</param>
		/// <returns>A task that returns True when the counter is available for immediate decrementing, False if the counter has been disposed</returns>
		/// <remarks>This will only succeed when nobody is waiting on a Decrement operation, so Decrement operations won't be waiting while the counter is non-zero</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<bool> PeekDecrement(TimeSpan timeout, CancellationToken token = default)
		{
			if (!TryPeekDecrement(AsyncCounterFlags.None, timeout, token, out var Instance))
				return new ValueTask<bool>(false);

			return new ValueTask<bool>(Instance, Instance.Version);
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

		/// <summary>
		/// Blocks until it's possible to decrement the counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter can be decremented, False if the counter is disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryPeekDecrement(CancellationToken token) => TryPeekDecrement(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks until it's possible to decrement the counter
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter can be decremented, False if the counter is disposed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryPeekDecrement(TimeSpan timeout, CancellationToken token = default)
		{
			{
				var MyCount = _CurrentCount;

				// First up, check the counter as-is
				if (MyCount > 0)
					return true;

				// If we're not okay with blocking, then we fail
				if (timeout == TimeSpan.Zero && !token.CanBeCanceled)
					return false;

				// No free counters, are we disposed?
				if (MyCount == -1)
					return false;
			}

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, 1, AsyncCounterFlags.Peek, false);

			_PeekWaiters.Enqueue(Instance);

			// Was a counter added while we were busy? If so, try to pass it to a waiter
			if (_Disposer != null)
			{
				DisposeWaiters(); // We disposed during processing

				// Disposed, this should throw ObjectDisposedException
				return Instance.GetResult(Instance.Version) != -1;
			}

			//****************************************

			Instance.ApplyCancellation(token, timeout);

			try
			{
				lock (Instance)
				{
					// Queue this inside the lock, so Pulse cannot execute until we've reached Wait, even if we've already completed
					Instance.OnCompleted(static (state) => { lock (state!) Monitor.Pulse(state); }, Instance, Instance.Version, ValueTaskSourceOnCompletedFlags.None);

					// Wait for a definitive result
					Monitor.Wait(Instance);
				}

				// Result retrieved, now check if we were cancelled
				return Instance.GetResult(Instance.Version) != -1;
			}
			catch
			{
				// We may get a cancel or dispose exception
				if (!token.IsCancellationRequested)
					return false; // If it's not the original token, we cancelled due to a Timeout. Return false

				throw; // Throw the cancellation
			}
		}

		//****************************************

		internal bool TryPeekDecrement(AsyncCounterFlags flags, TimeSpan timeout, CancellationToken token, out AsyncCounterDecrement decrement)
		{
			var MyCount = _CurrentCount;

			// No free counters, are we disposed?
			if (MyCount == -1)
			{
				decrement = null!;

				return false;
			}

			flags |= AsyncCounterFlags.Peek;

			// Are we able to decrement?
			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, 1, flags, MyCount > 0);

			if (Instance.GetStatus(Instance.Version) == ValueTaskSourceStatus.Pending)
			{
				Instance.ApplyCancellation(token, timeout);

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
			if ((decrement.Flags & AsyncCounterFlags.Peek) == AsyncCounterFlags.Peek)
				return _PeekWaiters.Erase(decrement);
			else
				return _Waiters.Erase(decrement);
		}

		//****************************************

		private ValueTask<int> InternalDecrement(int maximum, AsyncCounterFlags flags, TimeSpan timeout, CancellationToken token = default)
		{
			int Counter;

			if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			if (timeout == TimeSpan.Zero)
			{
				if (TryDecrementToZero(out Counter))
					return new ValueTask<int>(Counter);

				if (_Disposer != null)
				{
					if ((flags & AsyncCounterFlags.ThrowOnDispose) == AsyncCounterFlags.ThrowOnDispose)
						throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");

					return new ValueTask<int>(-1);
				}

				throw new TimeoutException();
			}

			// Try and decrement without waiting
			if (_Waiters.IsEmpty && InternalTryDecrement(0, out Counter))
				return new ValueTask<int>(Counter);

			if (_Disposer != null)
			{
				if ((flags & AsyncCounterFlags.ThrowOnDispose) == AsyncCounterFlags.ThrowOnDispose)
					throw new ObjectDisposedException(nameof(AsyncCounter), "Counter has been disposed of");

				return new ValueTask<int>(-1);
			}

			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, 0, flags, false);

			Instance.ApplyCancellation(token, timeout);

			// No free counters, add ourselves to the queue waiting on a counter
			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy? If so, try to pass it to a waiter
			if (!(InternalTryDecrement(0, out Counter) && TryAdd(Counter)) && _Disposer != null)
				DisposeWaiters(); // We disposed during processing

			return new ValueTask<int>(Instance, Instance.Version);
		}

		private bool InternalTryDecrementBlocking(int maximum, out int count, TimeSpan timeout, CancellationToken token = default)
		{
			// First up, try and take the counter if possible
			if (_Waiters.IsEmpty && InternalTryDecrement(maximum, out count))
				return true;

			count = 0;

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero && !token.CanBeCanceled)
				return false;

			// No free counters, are we disposed?
			if (_CurrentCount == -1)
				return false;

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			var Instance = AsyncCounterDecrement.GetOrCreateFor(this, maximum, AsyncCounterFlags.None, false);

			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy? If so, try to pass it to a waiter
			if (!(InternalTryDecrement(maximum, out var Counter) && TryAdd(Counter)) && _Disposer != null)
			{
				DisposeWaiters(); // We disposed during processing

				// Disposed, this should return -1
				return Instance.GetResult(Instance.Version) != -1;
			}

			//****************************************

			Instance.ApplyCancellation(token, timeout);

			try
			{
				lock (Instance)
				{
					// Queue this inside the lock, so Pulse cannot execute until we've reached Wait, even if we've already completed
					Instance.OnCompleted(static (state) => { lock (state!) Monitor.Pulse(state); }, Instance, Instance.Version, ValueTaskSourceOnCompletedFlags.None);

					// Wait for a definitive result
					Monitor.Wait(Instance);
				}

				// Result retrieved, now check if we were cancelled
				count = Instance.GetResult(Instance.Version);

				return count > 0;
			}
			catch
			{
				// We may get a cancel or dispose exception
				if (!token.IsCancellationRequested)
					return false; // If it's not the original token, we cancelled due to a Timeout. Return false

				throw; // Throw the cancellation
			}
		}

		private bool InternalTryAdd(int count)
		{ //****************************************
			var MyWait = new SpinWait();
			int OldCount;
			//****************************************

			for (; ; )
			{
				OldCount = Volatile.Read(ref _CurrentCount);

				// Are we disposed?
				if (OldCount == -1)
					return false;

				// No, so we can add a counter to the pool
				if (Interlocked.CompareExchange(ref _CurrentCount, OldCount + count, OldCount) == OldCount)
				{
					ReleasePeekers();

					return true;
				}

				// Failed, spin and try again
				MyWait.SpinOnce();
			}
		}

		private bool InternalTryDecrement() => InternalTryDecrement(1, out _);

		private bool InternalTryDecrement(int maximum, out int count)
		{ //****************************************
			var MyWait = new SpinWait();
			int OldCount, NewCount;
			//****************************************

			for (; ; )
			{
				OldCount = Volatile.Read(ref _CurrentCount);

				if (OldCount <= 0)
				{
					count = 0;

					return false; // No counter (or already disposed), cannot decrement again
				}

				if (maximum > 0)
					count = Math.Min(OldCount, maximum);
				else
					count = OldCount;

				if (_Disposer != null)
					NewCount = -1; // We're disposing, switch to disposed
				else
					NewCount = OldCount - count; // Just take to the maximum

				// Try and update the counter
				if (Interlocked.CompareExchange(ref _CurrentCount, NewCount, OldCount) == OldCount)
				{
					if (NewCount == -1)
						_Disposer!.SwitchToComplete();

					return true;
				}

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
				var Count = 1;

				// Try and release this Instance
				NextInstance.TrySwitchToCompleted(ref Count);
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

				return MyCount < 0 ? 0 : MyCount;
			}
		}

		/// <summary>
		/// Gets the number of operations waiting to decrement the counter
		/// </summary>
		public int WaitingCount => _Waiters.Count;

		/// <summary>
		/// Gets whether the Counter has disposed
		/// </summary>
		/// <remarks>Decrement operations will still succeed until <see cref="CurrentCount"/> is zero</remarks>
		public bool IsDisposed => _Disposer != null;

		//****************************************

		/// <summary>
		/// Decrements the first available counter
		/// </summary>
		/// <param name="counters">The counters to try and decrement</param>
		/// <param name="token">A cancellation token to abort decrementing</param>
		/// <returns>A task returning the counter that was decremented</returns>
		/// <exception cref="ArgumentException">No counters were supplied</exception>
		/// <exception cref="ObjectDisposedException">All given counters are disposed</exception>
		public static ValueTask<AsyncCounter> DecrementAny(IEnumerable<AsyncCounter> counters, CancellationToken token = default)
		{ //****************************************
			var CounterSet = counters.ToArray();
			//****************************************

			if (CounterSet.Length == 0)
				throw new ArgumentException("No counters were supplied", nameof(counters));

			// Can we immediately decrement any of the counters?
			for (var Index = 0; Index < CounterSet.Length; Index++)
			{
				if (CounterSet[Index].TryDecrement())
					return new ValueTask<AsyncCounter>(CounterSet[Index]);
			}

			// No, so assign PeekDecrement on all the counters
			var Operation = AsyncCounterDecrementAny.GetOrCreate(token, Timeout.InfiniteTimeSpan);

			for (var Index = 0; Index < CounterSet.Length; Index++)
				Operation.Attach(CounterSet[Index]);

			Operation.Activate();

			return new ValueTask<AsyncCounter>(Operation, Operation.Version);
		}

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
