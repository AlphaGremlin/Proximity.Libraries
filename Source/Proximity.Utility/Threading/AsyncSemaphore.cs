/****************************************\
 AsyncSemaphore.cs
 Created: 2014-02-18
\****************************************/
using System;
using System.Collections.Generic;
using System.Security;
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
		private TaskCompletionSource<IDisposable> _Dispose;
		//****************************************
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphore()
		{
#if NET40
			_CompleteTask = TaskEx.FromResult<IDisposable>(new AsyncLockInstance(this));
#else
			_CompleteTask = Task.FromResult<IDisposable>(new AsyncLockInstance(this));
#endif
			_CurrentCount = 0;
			_MaxCount = 1;
		}
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphore(int initialCount) : this()
		{
			if (initialCount <= 1)
				throw new ArgumentException("Initial Count is invalid");

			_CurrentCount = 0;
			_MaxCount = initialCount;
		}
		
		//****************************************

		/// <summary>
		/// Disposes of the semaphore
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public Task Dispose()
		{
			((IDisposable)this).Dispose();

			return _Dispose.Task;
		}

		void IDisposable.Dispose()
		{	//****************************************
			TaskCompletionSource<IDisposable>[] Waiters;
			//****************************************
			
			lock (_Waiters)
			{
				if (_Dispose != null)
					return;

				_Dispose = new TaskCompletionSource<IDisposable>();
				
				// We must set the task results outside the lock, because otherwise the continuation could run on this thread and modify state within the lock
				Waiters = new TaskCompletionSource<IDisposable>[_Waiters.Count];
				_Waiters.CopyTo(Waiters, 0);
				_Waiters.Clear();

				if (_CurrentCount == 0)
					_Dispose.SetResult(null);
			}
			
			foreach (var MyWaiter in Waiters)
			{
				MyWaiter.TrySetException(new ObjectDisposedException("AsyncSemaphore", "Semaphore has been disposed of"));
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
			var MySource = new CancellationTokenSource();
			var MyTask = Wait(MySource.Token);
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
		/// Attempts to take a counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Wait(CancellationToken token)
		{	//****************************************
			TaskCompletionSource<IDisposable> NewWaiter;
			//****************************************
			
			lock (_Waiters)
			{
				if (_Dispose != null)
					throw new ObjectDisposedException("AsyncSemaphore", "Semaphore has been disposed of");
				
				// Is there a free counter?
				if (_CurrentCount < _MaxCount)
				{
					// Yes, so we can just add one and return the completed task
					_CurrentCount++;
					
					return _CompleteTask;
				}
				
				// No free counters, add ourselves to the queue waiting on a counter
				NewWaiter = new TaskCompletionSource<IDisposable>();
				
				_Waiters.Enqueue(NewWaiter);
			}
			
			// Check if we can get cancelled
			if (token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(Cancel, NewWaiter);
				
				// If we complete and haven't been cancelled, dispose of the registration
				NewWaiter.Task.ContinueWith((task, state) => ((CancellationTokenRegistration)state).Dispose(), MyRegistration, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			return NewWaiter.Task;
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
					if (_Dispose != null)
					{
						_CurrentCount--;

						if (_CurrentCount > 0)
							return;

						NextWaiter = _Dispose;
					}
					else
					{
						// Is there anybody waiting, or has MaxCount been dropped below the currently held number?
						if (_Waiters.Count == 0 || _CurrentCount > _MaxCount)
						{
							// No, free a counter for someone else and return
							_CurrentCount--;

							return;
						}

						// Yes, don't change the counter
						NextWaiter = _Waiters.Dequeue();
					}
				}
				
#if !PORTABLE
				// If we're not on the threadpool, run TrySetResult on there, so we don't blow the stack if the result calls Release too (and so on)
				if (!onThreadPool)
				{
					ThreadPool.QueueUserWorkItem(TryRelease, NextWaiter);
					
					return;
				}
#endif

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
					if (_Dispose != null)
						return;
					
					// Is there anybody waiting?
					if (_Waiters.Count == 0)
						break;

					// Are there any free counters?
					if (_CurrentCount >= _MaxCount)
						break;
						
					// Yes, take a counter (if necessary) and remove a waiter
					if (!HasCounter)
					{
						_CurrentCount++;
						
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
			get { return _MaxCount - _CurrentCount; }
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
					if (_Dispose != null)
						throw new ObjectDisposedException("AsyncSemaphore", "Object has been disposed");

					_MaxCount = value;
				}
				
				// Release any waiters that might now have free slots
#if PORTABLE
				TryReleaseWaiters(null);
#else
				ThreadPool.QueueUserWorkItem(TryReleaseWaiters);
#endif
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
