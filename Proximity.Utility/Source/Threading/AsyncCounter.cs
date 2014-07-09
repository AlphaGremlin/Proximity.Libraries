/****************************************\
 AsyncCounter.cs
 Created: 2014-07-07
\****************************************/
using System;
using System.Collections.Generic;
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
		private readonly Task<VoidStruct> _CompleteTask;
		private readonly Queue<TaskCompletionSource<VoidStruct>> _Waiters = new Queue<TaskCompletionSource<VoidStruct>>();
		private int _CurrentCount;
		private bool _IsDisposed;
		//****************************************
		
		/// <summary>
		/// Creates a new Asynchronous Counter with a zero count
		/// </summary>
		public AsyncCounter()
		{
			_CompleteTask = Task.FromResult<VoidStruct>(VoidStruct.Empty);
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
		{
			lock (_Waiters)
			{
				while (_Waiters.Count > 0)
				{
					_Waiters.Dequeue().TrySetCanceled();
				}
				
				_IsDisposed = true;
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
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = Decrement(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Attempts to decrement the Counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when we were able to decrement the counter</returns>
		public Task Decrement(CancellationToken token)
		{
			lock (_Waiters)
			{
				if (_IsDisposed)
					throw new OperationCanceledException("Counter has been disposed of");
				
				// Is there a free counter?
				if (_CurrentCount > 0)
				{
					// Yes, so we can just subtract one and return the completed task
					_CurrentCount--;
					
					return _CompleteTask;
				}
				
				// No free counters, add ourselves to the queue waiting on a counter
				var NewWaiter = new TaskCompletionSource<VoidStruct>();
				
				_Waiters.Enqueue(NewWaiter);
				
				// Check if we can get cancelled
				if (token.CanBeCanceled)
				{
					// Register for cancellation
					var MyRegistration = token.Register(Cancel, NewWaiter);
					
					// If we complete and haven't been cancelled, dispose of the registration
					NewWaiter.Task.ContinueWith((task, state) => ((CancellationTokenRegistration)state).Dispose(), MyRegistration);
				}
				
				return NewWaiter.Task;
			}
		}
		
		/// <summary>
		/// Increments the Counter
		/// </summary>
		public void Increment()
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextWaiter = null;
			//****************************************
			
			do
			{
				lock (_Waiters)
				{
					// Is there anybody waiting, or has MaxCount been dropped below the currently held number?
					if (_Waiters.Count == 0 || _CurrentCount < 0)
					{
						// No, free a counter for someone else and return
						_CurrentCount++;
						
						return;
					}
						
					// Yes, don't change the counter
					NextWaiter = _Waiters.Dequeue();
				}
				
				// Try to hand the counter over to the next waiter
				// It may cancel (or have already been cancelled), and if so we loop back and try again
			} while (!NextWaiter.TrySetResult(VoidStruct.Empty));
		}
		
		//****************************************
		
		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<VoidStruct>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
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
	}
}
