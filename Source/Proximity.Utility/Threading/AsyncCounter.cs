/****************************************\
 AsyncCounter.cs
 Created: 2014-07-07
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a primitive for a counter that is always positive or zero supporting async/await
	/// </summary>
	public sealed class AsyncCounter : IDisposable
	{	//****************************************
		private readonly Task<AsyncCounter> _CompleteTask;
		private readonly Queue<TaskCompletionSource<VoidStruct>> _Waiters = new Queue<TaskCompletionSource<VoidStruct>>();
		private readonly List<TaskCompletionSource<AsyncCounter>> _PeekWaiters = new List<TaskCompletionSource<AsyncCounter>>();
		private int _CurrentCount;
		private bool _IsDisposed;
		//****************************************
		
		/// <summary>
		/// Creates a new Asynchronous Counter with a zero count
		/// </summary>
		public AsyncCounter()
		{
#if NET40
			_CompleteTask = TaskEx.FromResult<AsyncCounter>(this);
#else
			_CompleteTask = Task.FromResult<AsyncCounter>(this);
#endif
			_CurrentCount = 0;
		}
		
		/// <summary>
		/// Creates a new Asynchronous Counter with the given count
		/// </summary>
		/// <param name="initialCount">The initial count</param>
		public AsyncCounter(int initialCount) : this()
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
		{	//****************************************
			TaskCompletionSource<VoidStruct>[] Waiters;
			TaskCompletionSource<AsyncCounter>[] PeekWaiters;
			//****************************************
			
			lock (_Waiters)
			{
				// We must set the task results outside the lock, because otherwise the continuation could run on this thread and modify state within the lock
				Waiters = new TaskCompletionSource<VoidStruct>[_Waiters.Count];
				
				_Waiters.CopyTo(Waiters, 0);
				_Waiters.Clear();
				
				PeekWaiters = new TaskCompletionSource<AsyncCounter>[_PeekWaiters.Count];
				
				_PeekWaiters.CopyTo(PeekWaiters, 0);
				
				_IsDisposed = true;
			}
			
			foreach (var MyWaiter in Waiters)
			{
				MyWaiter.TrySetException(new ObjectDisposedException("Counter has been disposed of"));
			}
	
			foreach (var MyWaiter in PeekWaiters)
			{
				MyWaiter.TrySetException(new ObjectDisposedException("Counter has been disposed of"));
			}
		}

		/// <summary>
		/// Disposes of the counter if the count is zero
		/// </summary>
		/// <returns>True if the counter was disposed of, otherwise False</returns>
		public bool DisposeIfZero()
		{
			lock (_Waiters)
			{
				if (_CurrentCount == 0)
				{
					Dispose();
					
					return true;
				}
			}
			
			return false;
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
		{	//****************************************
			TaskCompletionSource<VoidStruct> NewWaiter;
			//****************************************
			
			lock (_Waiters)
			{
				// Is there a free counter?
				if (_CurrentCount > 0)
				{
					// Yes, so we can just subtract one and return the completed task
					_CurrentCount--;
					
					return _CompleteTask;
				}
				
				// No free counters, are we disposed?
				if (_IsDisposed)
					throw new ObjectDisposedException("Counter has been disposed of");
				
				// Still active, add ourselves to the queue waiting on a counter
				NewWaiter = new TaskCompletionSource<VoidStruct>();
				
				_Waiters.Enqueue(NewWaiter);
			}
			
			// Check if we can get cancelled
			if (token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(Cancel, NewWaiter);
				
				// If we complete and haven't been cancelled, dispose of the registration
				NewWaiter.Task.ContinueWith((Action<Task<VoidStruct>, object>)CleanupCancelRegistration, MyRegistration, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			return NewWaiter.Task;
		}
		
		/// <summary>
		/// Tries to decrement the counter without waiting
		/// </summary>
		/// <returns>True if the counter was decremented without waiting, otherwise False</returns>
		public bool TryDecrement()
		{
			lock (_Waiters)
			{
				// Is there a free counter?
				if (_CurrentCount > 0)
				{
					// Yes, so we can just subtract one and return true
					_CurrentCount--;
					
					return true;
				}
			}
			
			return false;
		}
		
		/// <summary>
		/// Increments the Counter
		/// </summary>
		/// <remarks>The counter is not guaranteed to be incremented when this method returns, as waiters are evaluated on the ThreadPool. It will be incremented 'soon'.</remarks>
		public void Increment()
		{
			Increment(false);
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
		{	//****************************************
			TaskCompletionSource<AsyncCounter> NewWaiter;
			//****************************************
			
			lock (_Waiters)
			{
				// Is there a free counter?
				if (_CurrentCount > 0)
				{
					// Yes, so we can just return immediately
					return _CompleteTask;
				}
				
				// No free counters, are we disposed?
				if (_IsDisposed)
					throw new ObjectDisposedException("Counter has been disposed of");
				
				// Create a peek waiter
				NewWaiter = new TaskCompletionSource<AsyncCounter>();
				
				_PeekWaiters.Add(NewWaiter);
			}
			
			// Check if we can get cancelled
			if (token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(CancelPeekDecrement, NewWaiter);
				
				// If we complete and haven't been cancelled, dispose of the registration
				NewWaiter.Task.ContinueWith((Action<Task<AsyncCounter>, object>)CleanupCancelRegistration, MyRegistration, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
			}
			
			return NewWaiter.Task;
		}
		
		/// <summary>
		/// Checks if it's possible to decrement this counter
		/// </summary>
		/// <returns>True if the counter can be decremented, otherwise False</returns>
		public bool TryPeekDecrement()
		{
			return _CurrentCount > 0;
		}
		
		//****************************************
		
		private void Increment(bool onThreadPool)
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextWaiter = null;
			TaskCompletionSource<AsyncCounter>[] NextPeekWaiter = null;
			//****************************************
			
			do
			{
				lock (_Waiters)
				{
					if (_IsDisposed)
					{
						if (!onThreadPool)
							throw new ObjectDisposedException("Counter has been disposed of");
						
						return;
					}
					
					// Is there anybody waiting, or has MaxCount been dropped below the currently held number?
					if (_Waiters.Count == 0 || _CurrentCount < 0)
					{
						// No, free a counter for someone else and return
						_CurrentCount++;
						
						// Is there anyone peeking?
						if (_PeekWaiters.Count == 0)
							return;
						
						// Yes, pull them out and clear the list
						NextPeekWaiter = _PeekWaiters.ToArray();
						_PeekWaiters.Clear();
					}
					else
					{
						// Yes, don't change the counter
						NextWaiter = _Waiters.Dequeue();
					}
				}
				
				if (NextPeekWaiter != null)
				{
#if PORTABLE
					TryPeekDecrement(NextPeekWaiter);
#else
					if (onThreadPool)
					{
						foreach (var MyWaiter in NextPeekWaiter)
							MyWaiter.TrySetResult(this);
					}
					else
					{
						ThreadPool.UnsafeQueueUserWorkItem(TryPeekDecrement, NextPeekWaiter);
					}
#endif
					return;
				}

#if !PORTABLE
				if (!onThreadPool)
				{
					ThreadPool.UnsafeQueueUserWorkItem(TryIncrement, NextWaiter);
					
					return;
				}
#endif
				
				// Try to hand the counter over to the next waiter
				// It may cancel (or have already been cancelled), and if so we loop back and try again
			} while (!NextWaiter.TrySetResult(VoidStruct.Empty));
		}
		
		private void TryIncrement(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<VoidStruct>)state;
			//****************************************
			
			if (!MyWaiter.TrySetResult(VoidStruct.Empty))
				Increment(true);
		}
		
		private void TryPeekDecrement(object state)
		{	//****************************************
			var MyWaiters = (TaskCompletionSource<AsyncCounter>[])state;
			//****************************************

			foreach (var MyWaiter in MyWaiters)
				MyWaiter.TrySetResult(this);
		}
			
		private static void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<VoidStruct>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}
		
		private static void CancelPeekDecrement(object state)
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
			get { return _CurrentCount; }
		}
		
		/// <summary>
		/// Gets the number of operations waiting to decrement the counter
		/// </summary>
		public int WaitingCount
		{
			get { return _Waiters.Count; }
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
