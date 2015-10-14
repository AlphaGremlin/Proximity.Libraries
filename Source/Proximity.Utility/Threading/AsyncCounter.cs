/****************************************\
 AsyncCounter.cs
 Created: 2014-07-07
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

		private readonly ConcurrentQueue<WaiterNode> _Waiters = new ConcurrentQueue<WaiterNode>();

		private int _CurrentCount;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Counter with a zero count
		/// </summary>
		public AsyncCounter()
			: this(0)
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
					CheckWaiters();
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
					CheckWaiters();
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
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		public Task Decrement(CancellationToken token)
		{
			// Try and decrement without waiting
			if (TryDecrement())
				return _CompleteTask;

			// No free counters, are we disposed?
			if (_CurrentCount == -1)
				throw new ObjectDisposedException("AsyncCounter", "Counter has been disposed of");

			return CreateWaiter(false, token);
		}

		/// <summary>
		/// Tries to decrement the counter without waiting
		/// </summary>
		/// <returns>True if the counter was decremented without waiting, otherwise False</returns>
		public bool TryDecrement()
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

		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <remarks>The counter is not guaranteed to be incremented when this method returns, as waiters are evaluated on the ThreadPool. It will be incremented 'soon'.</remarks>
		public void Increment()
		{
			// Try and increment the counter
			if (!TryIncrement())
				throw new ObjectDisposedException("AsyncCounter", "Counter has been disposed of");

			// Success, see if there are any waiters
			CheckWaiters();
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
			if (TryPeekDecrement())
				return _CompleteTask;

			// No free counters, are we disposed?
			if (_CurrentCount == -1)
				throw new ObjectDisposedException("AsyncCounter", "Counter has been disposed of");

			return CreateWaiter(true, token);
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
				{
					CheckWaiters();

					return;
				}

				// Failed, spin and try again
				MyWait.SpinOnce();
			}
		}

		//****************************************

		private bool TryIncrement()
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

				// Yes, so we can subtract one
				if (Interlocked.CompareExchange(ref _CurrentCount, OldCount + 1, OldCount) == OldCount)
					return true;

				// Failed, spin and try again
				MyWait.SpinOnce();
			}
		}

		private Task<AsyncCounter> CreateWaiter(bool isPeek, CancellationToken token)
		{	//****************************************
			WaiterNode NewNode;
			//****************************************

			// Create a waiter and add to the queue
			NewNode = new WaiterNode(isPeek);

			_Waiters.Enqueue(NewNode);

			// Verify we weren't incremented in the meantime
			CheckWaiters();

			// Check if we can get cancelled
			if (token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(Cancel, NewNode.Waiter);

				// If we complete and haven't been cancelled, dispose of the registration
				NewNode.Waiter.Task.ContinueWith((Action<Task<AsyncCounter>, object>)CleanupCancelRegistration, MyRegistration, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}

			return NewNode.Waiter.Task;
		}

		private void CheckWaiters()
		{
			// Are there any waiters?
			if (_Waiters.IsEmpty)
				return; // No, so nothing to do

			// Yes. Don't want to activate waiters on the calling thread though, since it can cause a stack overflow if Increment gets called from a Decrement continuation
#if PORTABLE
			Task.Factory.StartNew(ReleaseWaiters, null);
#else
			ThreadPool.UnsafeQueueUserWorkItem(ReleaseWaiters, null);
#endif
		}

		private void ReleaseWaiters(object state)
		{	//****************************************
			WaiterNode NextNode;
			//****************************************

			for (; ; )
			{
				// Are there any waiters?
				if (_Waiters.IsEmpty)
					return; // No, so nothing to do

				// Yes, can we activate one of them?
				if (!TryDecrement())
				{
					// If we've been disposed, cleanup waiters
					if (_CurrentCount == -1)
						DisposeWaiters();

					return; // No, so we keep waiting
				}

				// Try and retrieve a waiter
				while (_Waiters.TryDequeue(out NextNode))
				{
					// Got a waiter. Can we complete it?
					if (!NextNode.Waiter.TrySetResult(this))
						continue; // No, so it was cancelled. Try to get another

					// Yes. Is it a peek?
					if (!NextNode.IsPeek)
						return; // No, so we're done

					// Yes, so keep looking for a waiter
				}

				// The waiter was stolen by another thread.
				// Return the counter we took and try again
				if (!TryIncrement())
				{
					// We've been disposed, cleanup waiters and exit
					DisposeWaiters();

					return;
				}
			}
		}

		private void DisposeWaiters()
		{	//****************************************
			WaiterNode MyNode;
			//****************************************

			// Success, now close any pending waiters
			while (_Waiters.TryDequeue(out MyNode))
				MyNode.Waiter.TrySetException(new ObjectDisposedException("AsyncCounter", "Counter has been disposed of"));
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
			get { return _Waiters.Count(node => !node.Waiter.Task.IsCompleted); }
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

		//****************************************

		private struct WaiterNode
		{	//****************************************
			private readonly TaskCompletionSource<AsyncCounter> _Waiter;
			private readonly bool _IsPeek;
			//****************************************

			public WaiterNode(bool isPeek)
			{
				_Waiter = new TaskCompletionSource<AsyncCounter>();
				_IsPeek = isPeek;
			}

			//****************************************

			public TaskCompletionSource<AsyncCounter> Waiter
			{
				get { return _Waiter; }
			}

			public bool IsPeek
			{
				get { return _IsPeek; }
			}
		}
	}
}
