/****************************************\
 AsyncCounter.cs
 Created: 2014-07-07
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for a counter that is always positive or zero supporting async/await
	/// </summary>
	public class AsyncCounter : IDisposable
	{	//****************************************
		private readonly Task<AsyncCounter> _CompleteTask;

		private readonly ConcurrentQueue<TaskCompletionSource<AsyncCounter>> _Waiters = new ConcurrentQueue<TaskCompletionSource<AsyncCounter>>();
		private readonly ConcurrentQueue<TaskCompletionSource<AsyncCounter>> _PeekWaiters = new ConcurrentQueue<TaskCompletionSource<AsyncCounter>>();

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

#if NET40
			_CompleteTask = TaskEx.FromResult<AsyncCounter>(this);
#else
			_CompleteTask = Task.FromResult<AsyncCounter>(this);
#endif

			_CurrentCount = initialCount;
		}

		//****************************************

		/// <summary>
		/// Disposes of the counter, cancelling any waiters
		/// </summary>
		public void Dispose()
		{	//****************************************
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
		{	//****************************************
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
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		public Task Decrement()
		{
			return Decrement(CancellationToken.None);
		}

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		public Task Decrement(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource();
			var MyTask = Decrement(MySource.Token);
			//****************************************

			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();

				return MyTask;
			}

			MySource.CancelAfter(timeout);

			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

			return MyTask;
		}

		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		public Task Decrement(CancellationToken token)
		{	//****************************************
			TaskCompletionSource<AsyncCounter> NewWaiter;
			//****************************************

			// Try and decrement without waiting
			if (_Waiters.IsEmpty && InternalTryDecrement())
				return _CompleteTask;

			// No free counters, are we disposed?
			if (_CurrentCount == -1)
				return Task.FromException(new ObjectDisposedException("AsyncCounter", "Counter has been disposed of"));

			//****************************************

			// Create a new waiter and add it to the queue
			NewWaiter = new TaskCompletionSource<AsyncCounter>();

			_Waiters.Enqueue(NewWaiter);

			// Was a counter added while we were busy?
			if (InternalTryDecrement())
				ForceIncrement(); // Try and activate a waiter, or at least increment

			return PrepareWaiter(NewWaiter, token);
		}

		/// <summary>
		/// Tries to decrement the counter without waiting
		/// </summary>
		/// <returns>True if the counter was decremented without waiting, otherwise False</returns>
		public bool TryDecrement()
		{
			return _Waiters.IsEmpty && InternalTryDecrement();
		}
		
		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented without waiting, otherwise False due to disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryDecrement(CancellationToken token)
		{
#if NET40
			return TryDecrement(new TimeSpan(0, 0, 0, 0, -1), token);
#else
			return TryDecrement(Timeout.InfiniteTimeSpan, token);
#endif
		}
		
		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <returns>True if the counter was decremented, otherwise False due to timeout or disposal</returns>
		public bool TryDecrement(TimeSpan timeout)
		{
			return TryDecrement(timeout, CancellationToken.None);
		}

		/// <summary>
		/// Blocks attempting to decrement the counter
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was decremented, otherwise False due to timeout or disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryDecrement(TimeSpan timeout, CancellationToken token)
		{	//****************************************
			TaskCompletionSource<AsyncCounter> NewWaiter;
			//****************************************

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

#if NET40
			if (timeout == new TimeSpan(0, 0, 0, 0, -1) && timeout < TimeSpan.Zero)
#else
			if (timeout == Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
#endif
				throw new ArgumentOutOfRangeException("timeout", "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			NewWaiter = new TaskCompletionSource<AsyncCounter>();

			_Waiters.Enqueue(NewWaiter);

			// Was a counter added while we were busy?
			if (InternalTryDecrement())
				ForceIncrement(); // Try and activate a waiter, or at least increment

			//****************************************

			// Are we okay with blocking indefinitely (or until the token is raised)?
#if NET40
			if (timeout == new TimeSpan(0, 0, 0, 0, -1))
#else
			if (timeout == Timeout.InfiniteTimeSpan)
#endif
			{
				// Prepare the cancellation token (if any)
				PrepareWaiter(NewWaiter, token);

				try
				{
					// Wait indefinitely for a definitive result
					NewWaiter.Task.Wait();

					return true;
				}
				catch
				{
					// We may get a cancel or dispose exception
					if (!token.IsCancellationRequested)
						return false; // Original token was not cancelled, so return false

					throw; // Throw the cancellation
				}
			}

			// We want to be able to timeout and possibly cancel too
			using (var MySource = CancellationTokenSource.CreateLinkedTokenSource(token))
			{
				// Prepare the cancellation token (if any)
				PrepareWaiter(NewWaiter, MySource.Token);

				// If the task is completed, all good!
				if (NewWaiter.Task.IsCompleted)
					return true;

				// We can't just use Decrement(token).Wait(timeout) because that won't cancel the decrement attempt
				// Instead, we have to cancel on the original token
				MySource.CancelAfter(timeout);

				try
				{
					// Wait for the task to complete, knowing it will either cancel due to the token, the timeout, or because we're zero and disposed
					NewWaiter.Task.Wait();

					return true;
				}
				catch
				{
					// We may get a cancel or dispose exception
					if (!token.IsCancellationRequested)
						return false; // Original token was not cancelled, so either we timed out or are disposed. Return false

					throw; // Throw the cancellation
				}
			}
		}

		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <remarks>The counter is not guaranteed to be incremented when this method returns, as waiters are evaluated on the ThreadPool. It will be incremented 'soon'.</remarks>
		public bool TryIncrement()
		{
			// Try and retrieve a waiter
			while (_Waiters.TryDequeue(out var NextWaiter))
			{
				// Is it completed?
				if (NextWaiter.Task.IsCompleted)
					continue; // Yes, so find another

				// Don't want to activate waiters on the calling thread though, since it can cause a stack overflow if Increment gets called from a Decrement continuation
#if NETSTANDARD1_3
				Task.Factory.StartNew(ReleaseWaiter, NextWaiter);
#else
				ThreadPool.QueueUserWorkItem(ReleaseWaiter, NextWaiter);
#endif
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
				throw new ObjectDisposedException("AsyncCounter", "Counter has been disposed of");
		}

		/// <summary>
		/// Waits until it's possible to decrement this counter
		/// </summary>
		/// <returns>A task that completes when the counter is available for immediate decrementing</returns>
		public Task<AsyncCounter> PeekDecrement()
		{
			return PeekDecrement(CancellationToken.None);
		}

		/// <summary>
		/// Waits until it's possible to decrement this counter
		/// </summary>
		/// <param name="token">A cancellation token to stop waiting</param>
		/// <returns>A task that completes when the counter is available for immediate decrementing</returns>
		/// <remarks>This will only succeed when nobody is waiting on a Decrement operation, so Decrement operations won't be waiting while the counter is non-zero</remarks>
		public Task<AsyncCounter> PeekDecrement(CancellationToken token)
		{
			var MyCount = _CurrentCount;

			// Are we able to decrement?
			if (MyCount > 0 || MyCount < -1)
				return _CompleteTask;

			// No free counters, are we disposed?
			if (MyCount == -1)
				return Task.FromException<AsyncCounter>(new ObjectDisposedException("AsyncCounter", "Counter has been disposed of"));

			//****************************************

			// Create a new waiter and add it to the queue
			var NewWaiter = new TaskCompletionSource<AsyncCounter>();

			_PeekWaiters.Enqueue(NewWaiter);

			// Was a counter added while we were busy?
			if (TryPeekDecrement())
			{
				// Let any peekers know they can try and take a counter
				ReleasePeekers();
			}

			return PrepareWaiter(NewWaiter, token);
		}

		/// <summary>
		/// Checks if it's possible to decrement this counter
		/// </summary>
		/// <returns>True if the counter can be decremented, otherwise False</returns>
		public bool TryPeekDecrement()
		{
			int MyCount = _CurrentCount;

			return (MyCount > 0 || MyCount < -1);
		}

		/// <summary>
		/// Always increments, regardless of disposal state
		/// </summary>
		/// <remarks>Used for DecrementAny</remarks>
		internal void ForceIncrement()
		{	//****************************************
			var MyWait = new SpinWait();
			int OldCount, NewCount;
			//****************************************

			do
			{
				// Try and retrieve a waiter
				while (_Waiters.TryDequeue(out var NextWaiter))
				{
					// Is it completed?
					if (NextWaiter.Task.IsCompleted)
						continue; // Yes, so find another

					// Don't want to activate waiters on the calling thread though, since it can cause a stack overflow if Increment gets called from a Decrement continuation
#if NETSTANDARD1_3
					Task.Factory.StartNew(ReleaseWaiter, NextWaiter);
#else
					ThreadPool.QueueUserWorkItem(ReleaseWaiter, NextWaiter);
#endif
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

		//****************************************

		private bool InternalTryIncrement()
		{	//****************************************
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
		{	//****************************************
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
		
		private Task<AsyncCounter> PrepareWaiter(TaskCompletionSource<AsyncCounter> waiter, CancellationToken token)
		{
			// Check if we can get cancelled
			if (token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(Cancel, waiter);

				// If we complete and haven't been cancelled, dispose of the registration
				waiter.Task.ContinueWith((Action<Task<AsyncCounter>, object>)CleanupCancelRegistration, MyRegistration, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}

			return waiter.Task;
		}

		private void DisposeWaiters()
		{	//****************************************
			TaskCompletionSource<AsyncCounter> MyWaiter;
			//****************************************

			// Success, now close any pending waiters
			while (_Waiters.TryDequeue(out MyWaiter))
				MyWaiter.TrySetException(new ObjectDisposedException("AsyncCounter", "Counter has been disposed of"));

			while (_PeekWaiters.TryDequeue(out MyWaiter))
				MyWaiter.TrySetException(new ObjectDisposedException("AsyncCounter", "Counter has been disposed of"));
		}

		private void ReleasePeekers()
		{	//****************************************
			TaskCompletionSource<AsyncCounter> NextWaiter;
			//****************************************

			// Is there a Peek Waiter?
			if (_PeekWaiters.TryDequeue(out NextWaiter))
			{
				// Yes, release it on another thread so we don't hold up the caller
#if NETSTANDARD1_3
				Task.Factory.StartNew(ReleasePeeker, NextWaiter);
#else
				ThreadPool.QueueUserWorkItem(ReleasePeeker, NextWaiter);
#endif
			}
		}

		private void ReleaseWaiter(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<AsyncCounter>)state;
			//****************************************

			// Release our Waiter
			if (!MyWaiter.TrySetResult(this))
				ForceIncrement(); // Failed so it was cancelled. Make sure we add the counter back, even if we're disposing
		}

		private void ReleasePeeker(object state)
		{	//****************************************
			var FirstWaiter = (TaskCompletionSource<AsyncCounter>)state;
			TaskCompletionSource<AsyncCounter> NextWaiter;
			List<TaskCompletionSource<AsyncCounter>> MyWaiters;
			//****************************************

			// If there are more peek waiters, release them too
			if (_PeekWaiters.TryDequeue(out NextWaiter))
			{
				// We want to avoid a situation where the peek continuation queues another peek waiter, causing an infinite loop
				MyWaiters = new List<TaskCompletionSource<AsyncCounter>>();

				MyWaiters.Add(NextWaiter);
				
				// If they run synchronously, this also lets peek waiters have an order to run their continuations in
				while (_PeekWaiters.TryDequeue(out NextWaiter))
					MyWaiters.Add(NextWaiter);

				// Now the queue is empty, it's safe to release our first Peek Waiter
				FirstWaiter.TrySetResult(this);

				// Release the rest of them
				foreach (var MyWaiter in MyWaiters)
					MyWaiter.TrySetResult(this);
			}
			else
			{
				// Release our Peek Waiter
				FirstWaiter.TrySetResult(this);
			}
		}

		private static void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<AsyncCounter>)state;
			//****************************************

			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}

		private static void CleanupCancelRegistration(Task task, object state)
		{
			((CancellationTokenRegistration)state).Dispose();
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
		public int WaitingCount
		{
			get { return _Waiters.Count(waiter => !waiter.Task.IsCompleted); }
		}

		//****************************************

		/// <summary>
		/// Decrements the first available counter
		/// </summary>
		/// <param name="counters">The counters to try and decrement</param>
		/// <returns>A task returning the counter that was decremented</returns>
		public static Task<AsyncCounter> DecrementAny(params AsyncCounter[] counters)
		{
			return DecrementAny(counters, CancellationToken.None);
		}

		/// <summary>
		/// Decrements the first available counter
		/// </summary>
		/// <param name="counters">The counters to try and decrement</param>
		/// <returns>A task returning the counter that was decremented</returns>
		public static Task<AsyncCounter> DecrementAny(IEnumerable<AsyncCounter> counters)
		{
			return DecrementAny(counters, CancellationToken.None);
		}

		/// <summary>
		/// Decrements the first available counter
		/// </summary>
		/// <param name="counters">The counters to try and decrement</param>
		/// <param name="token">A cancellation token to abort decrementing</param>
		/// <returns>A task returning the counter that was decremented</returns>
		public static Task<AsyncCounter> DecrementAny(IEnumerable<AsyncCounter> counters, CancellationToken token)
		{	//****************************************
			var CounterSet = counters.ToArray();
			//****************************************

			// Can we immediately decrement any of the counters?
			for (int Index = 0; Index < CounterSet.Length; Index++)
			{
				if (CounterSet[Index].TryDecrement())
				{
#if NET40
					return TaskEx.FromResult(CounterSet[Index]);
#else
					return Task.FromResult(CounterSet[Index]);
#endif
				}
			}

			// No, so assign PeekDecrement on all the counters
			var MyOperation = new AsyncCounterDecrementAny(token);

			for (int Index = 0; Index < CounterSet.Length; Index++)
			{
				MyOperation.Attach(CounterSet[Index]);
			}

			return MyOperation.Task;
		}

		/// <summary>
		/// Tries to decrement one of the given counters without waiting
		/// </summary>
		/// <param name="counters">The set of counters to try and decrement</param>
		/// <returns>The counter that was successfully decremented, or null if none were able to be immediately decremented</returns>
		public static AsyncCounter TryDecrementAny(params AsyncCounter[] counters)
		{
			return TryDecrementAny((IEnumerable<AsyncCounter>)counters);
		}

		/// <summary>
		/// Tries to decrement one of the given counters without waiting
		/// </summary>
		/// <param name="counters">The set of counters to try and decrement</param>
		/// <returns>The counter that was successfully decremented, or null if none were able to be immediately decremented</returns>
		public static AsyncCounter TryDecrementAny(IEnumerable<AsyncCounter> counters)
		{
			foreach (var MyCounter in counters)
			{
				if (MyCounter.TryDecrement())
					return MyCounter;
			}

			return null;
		}
	}
}
