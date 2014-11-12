/****************************************\
 AsyncReadWriteLock.cs
 Created: 2014-02-20
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a reader/writer synchronisation object for async/await, and a disposable model for releasing
	/// </summary>
	public sealed class AsyncReadWriteLock : IDisposable
	{	//****************************************
		private readonly Queue<TaskCompletionSource<IDisposable>> _Writers = new Queue<TaskCompletionSource<IDisposable>>();
		private TaskCompletionSource<IDisposable> _Reader = new TaskCompletionSource<IDisposable>();
		
		private int _ReadersWaiting = 0, _Counter = 0;
		
		private bool _IsUnfair = false;
		//****************************************
		
		/// <summary>
		/// Creates a new asynchronous reader/writer lock
		/// </summary>
		public AsyncReadWriteLock()
		{
		}
		
		/// <summary>
		/// Creates a new asynchronous reader/writer lock
		/// </summary>
		/// <param name="isUnfair">Whether to prefer fairness when switching</param>
		public AsyncReadWriteLock(bool isUnfair)
		{
			_IsUnfair = isUnfair;
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the reader/writer lock, cancelling any readers and writers
		/// </summary>
		public void Dispose()
		{
			lock (_Writers)
			{
				_ReadersWaiting = 0;
				_Counter = 0;
				
				while (_Writers.Count > 0)
				{
					_Writers.Dequeue().TrySetCanceled();
				}
				
				_Reader.SetCanceled();
			}
		}
		
		/// <summary>
		/// Take a read lock, running concurrently with other read locks
		/// </summary>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRead()
		{
			return LockRead(CancellationToken.None);
		}
		
		/// <summary>
		/// Take a read lock, running concurrently with other read locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the read lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRead(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = LockRead(MySource.Token);
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
		/// Take a read lock, running concurrently with other read locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRead(CancellationToken token)
		{
			lock (_Writers)
			{
				// If there are zero or more readers and no waiting writers (or we're in unfair mode)...
				if (_Counter >=0 && (_IsUnfair || _Writers.Count == 0))
				{
					// Add another reader and return a completed task the caller can use to release the lock
					Interlocked.Increment(ref _Counter);
					
					return Task.FromResult<IDisposable>(new AsyncReadLockInstance(this));
				}
				
				// There's a writer in progress, or one waiting
				Interlocked.Increment(ref _ReadersWaiting);
				
				// Return a reader task the caller can wait on. This is a continuation, so readers don't run serialised
				var MyTask = _Reader.Task.ContinueWith(t => (IDisposable)new AsyncReadLockInstance(this), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
				
				// If we can be cancelled, queue a task that runs if we get cancelled to release the waiter
				if (token.CanBeCanceled)
					MyTask.ContinueWith(CancelRead, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
				
				return MyTask;
			}
		}
		
		/// <summary>
		/// Take a write lock, running exclusively
		/// </summary>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockWrite()
		{
			return LockWrite(CancellationToken.None);
		}
		
		/// <summary>
		/// Take a write lock, running exclusively, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the write lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockWrite(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = LockWrite(MySource.Token);
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
		/// Take a write lock, running exclusively, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockWrite(CancellationToken token)
		{
			lock (_Writers)
			{
				// If there are no readers or writers...
				if (_Counter == 0)
				{
					// Take the writer spot and return a completed task the caller can use to release the lock
					_Counter = -1;
					
					return Task.FromResult<IDisposable>(new AsyncWriteLockInstance(this));
				}
				
				// There's a reader or another writer working, add ourselves to the writer queue
				var MyWaiter = new TaskCompletionSource<IDisposable>();
				
				_Writers.Enqueue(MyWaiter);
				
				// Check if we can get cancelled
				if (token.CanBeCanceled)
				{
					// Register for cancellation
					var MyRegistration = token.Register(CancelWrite, MyWaiter);
					
					// When we complete, dispose of the registration
					MyWaiter.Task.ContinueWith((task, state) => ((CancellationTokenRegistration)state).Dispose(), MyRegistration, TaskContinuationOptions.ExecuteSynchronously);
				}
				
				return MyWaiter.Task;
			}
		}
		
		//****************************************

		private void ReleaseRead()
		{	//****************************************
			TaskCompletionSource<IDisposable> NextWriter = null;
			//****************************************
			
			lock (_Writers)
			{
				if (_Counter <= 0)
					throw new InvalidOperationException("No reader lock is currently held");
				
				Interlocked.Decrement(ref _Counter);
				
				// If there are no more readers and one or more writers waiting...
				if (_Counter == 0 && _Writers.Count > 0)
				{
					// Take the writer spot and find the writer to activate
					Interlocked.Decrement(ref _Counter);
					
					NextWriter = _Writers.Dequeue();
				}
				else
				{
					// If we don't have a writer waiting, abort early
					return;
				}
			}
			
			ThreadPool.UnsafeQueueUserWorkItem(TryReleaseRead, NextWriter);
		}
		
		private void TryReleaseRead(object state)
		{	//****************************************
			var NextWriter = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			// A writer may cancel, so we need to check for that and loop if it fails
			while (!NextWriter.TrySetResult(new AsyncWriteLockInstance(this)))
			{
				lock (_Writers)
				{
					// If there are no more writers, we can abort
					if (_Writers.Count == 0)
					{
						_Counter = 0;
						
						return;
					}
					
					// Get the next writer to try and activate
					NextWriter = _Writers.Dequeue();
				}
			}
		}

		private void ReleaseWrite(bool onThreadPool)
		{	//****************************************
			TaskCompletionSource<IDisposable> NextRelease = null;
			//****************************************
			
			do
			{
				lock (_Writers)
				{
					if (_Counter != -1)
						throw new InvalidOperationException("No writer lock is currently held");
					
					// If there are one or more other writers waiting...
					if (_Writers.Count > 0)
					{
						// Find the next one to activate and set the result appropriately
						NextRelease = _Writers.Dequeue();
					}
					// No writers, are there any readers?
					else if (_ReadersWaiting > 0)
					{
						// Yes, let's swap out the existing reader task with a new one (so we can activate all the waiting readers)
						NextRelease = Interlocked.Exchange(ref _Reader, new TaskCompletionSource<IDisposable>());
						
						// Swap the waiting readers with zero and update the counter
						_Counter = Interlocked.Exchange(ref _ReadersWaiting, 0);
					}
					else
					{
						// Nothing waiting, set the counter to zero and exit
						_Counter = 0;
						
						return;
					}
				}
				
				// If it's a reader, we can just set and forget
				if (_Counter > 0)
				{
					NextRelease.SetResult(null);
					
					break;
				}
				
				// If we're not on the threadpool, run TrySetResult on there, so we don't blow the stack if the result calls Release too (and so on)
				if (!onThreadPool)
				{
					ThreadPool.UnsafeQueueUserWorkItem(TryReleaseWrite, NextRelease);
					
					return;
				}
				
				// A writer, however, may cancel, so we need to check for that and loop back if it fails
			} while (!NextRelease.TrySetResult(new AsyncWriteLockInstance(this)));
		}
		
		private void TryReleaseWrite(object state)
		{	//****************************************
			var NextRelease = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			if (!NextRelease.TrySetResult(new AsyncWriteLockInstance(this)))
				ReleaseWrite(true);
		}
		
		private void CancelWrite(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already activate and been removed from the Writers list
			MyWaiter.TrySetCanceled();
		}
		
		private void CancelRead(Task<IDisposable> task)
		{
			lock (_Writers)
			{
				// A waiting Reader was cancelled. Is it still waiting?
				if (_Counter < 0)
				{
					// A writer is still working, so we can just decrement the waiting readers count
					Interlocked.Decrement(ref _ReadersWaiting);
					
					return;
				}
			}
			
			// Writer finished between the task cancelling and this continuation running
			// Need to release the reader
			ReleaseRead();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets whether any concurrent reading operations are in progress
		/// </summary>
		public bool IsReading
		{
			get { return _Counter > 0; }
		}
		
		/// <summary>
		/// Gets whether an exclusive writing operation is in progress
		/// </summary>
		public bool IsWriting
		{
			get { return _Counter == -1; }
		}
		
		/// <summary>
		/// Gets the number of readers queued
		/// </summary>
		public int WaitingReaders
		{
			get { return _ReadersWaiting; }
		}
		
		/// <summary>
		/// Gets the number of writers queued
		/// </summary>
		public int WaitingWriters
		{
			get { return _Writers.Count; }
		}
		
		/// <summary>
		/// Gets if the read/write lock favours an unfair algorithm
		/// </summary>
		/// <remarks>In unfair mode, a read lock will succeed if it's already held elsewhere, even if there are writers waiting</remarks>
		public bool IsUnfair
		{
			get { return _IsUnfair; }
		}
		
		//****************************************
		
		private class AsyncReadLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncReadWriteLock _Source;
			private int _Released;
			//****************************************
			
			internal AsyncReadLockInstance(AsyncReadWriteLock source)
			{
				_Source = source;
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null && Interlocked.Exchange(ref _Released, 1) == 0)
				{
					_Source.ReleaseRead();
				}
			}
		}

		private class AsyncWriteLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncReadWriteLock _Source;
			private int _Released;
			//****************************************
			
			internal AsyncWriteLockInstance(AsyncReadWriteLock source)
			{
				_Source = source;
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null && Interlocked.Exchange(ref _Released, 1) == 0)
				{
					_Source.ReleaseWrite(false);
				}
			}
		}
	}
}
