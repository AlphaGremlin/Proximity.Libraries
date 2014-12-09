﻿/****************************************\
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
		private int _MaxCount, _CurrentCount;
		private bool _IsDisposed;
		//****************************************
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphore()
		{
			_CompleteTask = Task.FromResult<IDisposable>(new AsyncLockInstance(this));
			_CurrentCount = _MaxCount = 1;
		}
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphore(int initialCount) : this()
		{
			if (initialCount <= 1)
				throw new ArgumentException("Initial Count is invalid");
			
			_CurrentCount = _MaxCount = initialCount;
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the semaphore, cancelling any waiters
		/// </summary>
		public void Dispose()
		{
			lock (_Waiters)
			{
				_IsDisposed = true;
				
				while (_Waiters.Count > 0)
				{
					_Waiters.Dequeue().TrySetException(new ObjectDisposedException("Semaphore has been disposed of"));
				}
				
				_CurrentCount = 0;
				_MaxCount = 0;
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
		{	//****************************************
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = Wait(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource, TaskContinuationOptions.ExecuteSynchronously);
			
			return MyTask;
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
				if (_IsDisposed)
					throw new ObjectDisposedException("Semaphore has been disposed of");
				
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
					NewWaiter.Task.ContinueWith((task, state) => ((CancellationTokenRegistration)state).Dispose(), MyRegistration, TaskContinuationOptions.ExecuteSynchronously);
				}
				
				return NewWaiter.Task;
			}
		}
		
		//****************************************
		
		private void Release(bool onThreadPool)
		{	//****************************************
			TaskCompletionSource<IDisposable> NextWaiter = null;
			//****************************************
			
			do
			{
				lock (_Waiters)
				{
					if (_IsDisposed)
						return;
					
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
				
				// If we're not on the threadpool, run TrySetResult on there, so we don't blow the stack if the result calls Release too (and so on)
				if (!onThreadPool)
				{
					ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextWaiter);
					
					return;
				}
				
				// Try to hand the counter over to the next waiter
				// It may cancel (or have already been cancelled), and if so we loop back and try again
			} while (!NextWaiter.TrySetResult(_CompleteTask.Result));
		}
		
		private void TryRelease(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			if (!MyWaiter.TrySetResult(_CompleteTask.Result))
				Release(true);
		}
		
		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}
		
		private void TryReleaseWaiters(object state)
		{	//****************************************
			TaskCompletionSource<IDisposable> NextWaiter = null;
			bool HasCounter = false;
			//****************************************
			
			while (true)
			{
				lock (_Waiters)
				{
					if (_IsDisposed)
						return;
					
					// Is there anybody waiting?
					if (_Waiters.Count == 0)
						break;

					// Are there any free counters?
					if (_CurrentCount <= 0)
						break;
						
					// Yes, take a counter (if necessary) and remove a waiter
					if (!HasCounter)
					{
						_CurrentCount--;
						
						HasCounter = true;
					}
					
					NextWaiter = _Waiters.Dequeue();
				}
				
				// Try to hand the counter over to the next waiter
				// It may cancel (or have already been cancelled), and if so we loop back and check for another waiter
				if (NextWaiter.TrySetResult(_CompleteTask.Result))
				{
					// The waiting task will take responsibility for releasing it, so we clear the flag
					HasCounter = false;
				}
			}
			
			// If we've taken a counter and it hasn't been used, we need to release it
			if (HasCounter)
				Release(true);
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
		
		/// <summary>
		/// Gets/Sets the maximum number of operations
		/// </summary>
		public int MaxCount
		{
			get { return _MaxCount; }
			set
			{
				if (_MaxCount == value)
					return;
				
				if (value <= 1)
					throw new ArgumentException("Maximum is invalid");
				
				lock (_Waiters)
				{
					// Adjust the number of slots based on the difference
					_CurrentCount += (value - _MaxCount);
					
					_MaxCount = value;
				}
				
				// Release any waiters that might now have free slots
				ThreadPool.QueueUserWorkItem(TryReleaseWaiters);
			}
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
					_Source.Release(false);
			}
		}
	}
}
