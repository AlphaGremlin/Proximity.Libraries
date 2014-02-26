/****************************************\
 AsyncSemaphore.cs
 Created: 2014-02-18
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
	/// Provides a primitive for semaphores supporting async/await and a disposable model for releasing
	/// </summary>
	public sealed class AsyncSemaphore : IDisposable
	{	//****************************************
		private readonly Task<IDisposable> _CompleteTask;
		private readonly Queue<TaskCompletionSource<IDisposable>> _Waiters = new Queue<TaskCompletionSource<IDisposable>>();
		private int _CurrentCount;
		//****************************************
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphore()
		{
			_CompleteTask = Task.FromResult<IDisposable>(new AsyncLockInstance(this));
			_CurrentCount = 1;
		}
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphore(int initialCount) : this()
		{
			_CurrentCount = initialCount;
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the semaphore, cancelling any waiters
		/// </summary>
		public void Dispose()
		{
			lock (_Waiters)
			{
				while (_Waiters.Count > 0)
				{
					_Waiters.Dequeue().TrySetCanceled();
				}
			}
		}
		
		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Wait()
		{
			return Wait(CancellationToken.None);
		}
		
		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait for a counters</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Wait(TimeSpan timeout)
		{
			return Lock(new CancellationTokenSource(timeout));
		}
		
		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Wait(CancellationToken token)
		{
			lock (_Waiters)
			{
				// Is there a free counter?
				if (_CurrentCount > 0)
				{
					// Yes, so we can just subtract one and return the completed task
					_CurrentCount--;
					
					return _CompleteTask;
				}
				
				// No free counters, add ourselves to the queue waiting on a counter
				var NewWaiter = new TaskCompletionSource<IDisposable>();
				
				_Waiters.Enqueue(NewWaiter);
				
				// Check if we can get cancelled
				if (token.CanBeCanceled)
				{
					// Register for cancellation
					var MyRegistration = token.Register(Cancel, NewWaiter);
					
					// If we complete and haven't been cancelled, dispose of the registration
					NewWaiter.Task.ContinueWith((task) => MyRegistration.Dispose());
				}
				
				return NewWaiter.Task;
			}
		}
		
		//****************************************
		
		private Task<IDisposable> Lock(CancellationTokenSource tokenSource)
		{	//****************************************
			var MyTask = Wait(tokenSource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				tokenSource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith(task => tokenSource.Dispose());
			
			return MyTask;
		}
		
		private void Release()
		{	//****************************************
			TaskCompletionSource<IDisposable> NextWaiter = null;
			//****************************************
			
			do
			{
				lock (_Waiters)
				{
					// Is there anybody waiting?
					if (_Waiters.Count == 0)
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
			} while (!NextWaiter.TrySetResult(_CompleteTask.Result));
		}
		
		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of counters available for taking
		/// </summary>
		public int CurrentCount
		{
			get { return _CurrentCount; }
		}
		
		/// <summary>
		/// Gets the number of operations waiting for a counter
		/// </summary>
		public int WaitingCount
		{
			get { return _Waiters.Count; }
		}
		
		//****************************************
		
		private struct AsyncLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncSemaphore _Source;
			//****************************************
			
			internal AsyncLockInstance(AsyncSemaphore source)
			{
				_Source = source;
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null)
					_Source.Release();
			}
		}
	}
}
