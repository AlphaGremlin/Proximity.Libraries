using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
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

		private TaskCompletionSource<IDisposable> _Disposed;
		
		private readonly HashSet<AsyncReadLockInstance> _ReadersWaiting = new HashSet<AsyncReadLockInstance>();
		private int _Counter = 0;
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
			IsUnfair = isUnfair;
		}
		
		//****************************************

		/// <summary>
		/// Disposes of the reader/writer lock
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public Task Dispose()
		{
			((IDisposable)this).Dispose();

			return _Disposed.Task;
		}

		void IDisposable.Dispose()
		{	//****************************************
			TaskCompletionSource<IDisposable>[] Writers;
			TaskCompletionSource<IDisposable> Reader;
			//****************************************
			
			lock (_Writers)
			{
				if (_Disposed != null)
					return;

				_Disposed = new TaskCompletionSource<IDisposable>();

				if (_Counter == 0)
					_Disposed.SetResult(null);

				_ReadersWaiting.Clear();
				
				// We must set the task results outside the lock, because otherwise the continuation could run on this thread and modify state within the lock
				Writers = new TaskCompletionSource<IDisposable>[_Writers.Count];
				_Writers.CopyTo(Writers, 0);
				_Writers.Clear();
				
				Reader = _Reader;
			}
			
			foreach (var MyWriter in Writers)
			{
				MyWriter.TrySetException(new ObjectDisposedException("AsyncReadWriteLock", "Lock has been disposed of"));
			}
			
			Reader.TrySetCanceled();
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
			var MySource = new CancellationTokenSource();
			var MyTask = LockRead(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}

			MySource.CancelAfter(timeout);
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((Action<Task<IDisposable>, object>)CleanupCancelSource, MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Take a read lock, running concurrently with other read locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRead(CancellationToken token)
		{	//****************************************
			AsyncReadLockInstance NewInstance;
			Task<IDisposable> NewTask;
			//****************************************
			
			lock (_Writers)
			{
				if (_Disposed != null)
					return Task.FromException<IDisposable>(new ObjectDisposedException("AsyncReadWriteLock", "Lock has been disposed of"));
				
				// If there are zero or more readers and no waiting writers (or we're in unfair mode)...
				if (_Counter >=0 && (IsUnfair || _Writers.Count == 0))
				{
					// Add another reader and return a completed task the caller can use to release the lock
					Interlocked.Increment(ref _Counter);
					
					return Task.FromResult<IDisposable>(new AsyncReadLockInstance(this));
				}
				
				// There's a writer in progress, or one waiting
				NewInstance = new AsyncReadLockInstance(this);
				
				_ReadersWaiting.Add(NewInstance);
				
				// Return a reader task the caller can wait on. This is a continuation, so readers don't run serialised
				NewTask = _Reader.Task.ContinueWith(NewInstance.LockRead, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			// If we can be cancelled, queue a task that runs if we get cancelled to release the waiter
			if (token.CanBeCanceled)
				NewTask.ContinueWith(NewInstance.CancelRead, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
			
			return NewTask;
		}

		/// <summary>
		/// Take a write lock, running exclusively
		/// </summary>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockWrite() => LockWrite(CancellationToken.None);

		/// <summary>
		/// Take a write lock, running exclusively, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the write lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockWrite(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource();
			var MyTask = LockWrite(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}

			MySource.CancelAfter(timeout);
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith(CleanupCancelSource, MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Take a write lock, running exclusively, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockWrite(CancellationToken token)
		{	//****************************************
			TaskCompletionSource<IDisposable> NewWaiter;
			//****************************************
			
			lock (_Writers)
			{
				if (_Disposed != null)
					return Task.FromException<IDisposable>(new ObjectDisposedException("AsyncReadWriteLock", "Lock has been disposed of"));
				
				// If there are no readers or writers...
				if (_Counter == 0)
				{
					// Take the writer spot and return a completed task the caller can use to release the lock
					_Counter = -1;
					
					return Task.FromResult<IDisposable>(new AsyncWriteLockInstance(this));
				}
				
				// There's a reader or another writer working, add ourselves to the writer queue
				NewWaiter = new TaskCompletionSource<IDisposable>();
				
				_Writers.Enqueue(NewWaiter);
			}
			
			// Check if we can get cancelled
			if (token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(CancelWrite, NewWaiter);
				
				// When we complete, dispose of the registration
				NewWaiter.Task.ContinueWith((task, state) => ((CancellationTokenRegistration)state).Dispose(), MyRegistration, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			return NewWaiter.Task;
		}
		
		//****************************************

		[SecuritySafeCritical]
		private void ReleaseRead()
		{	//****************************************
			TaskCompletionSource<IDisposable> NextTask = null;
			//****************************************
			
			lock (_Writers)
			{
				if (_Counter <= 0)
					throw new InvalidOperationException($"No reader lock is currently held: {_Writers.Count} writers, {_ReadersWaiting.Count} waiting, writing={_Counter == -1}");
				
				Interlocked.Decrement(ref _Counter);

				if (_Disposed != null)
				{
					if (_Counter > 0)
						return;

					NextTask = _Disposed;
				}
				// If there are no more readers and one or more writers waiting...
				else if (_Counter == 0 && _Writers.Count > 0)
				{
					// Take the writer spot and find the writer to activate
					Interlocked.Decrement(ref _Counter);
					
					NextTask = _Writers.Dequeue();
				}
				else
				{
					// If we don't have a writer waiting, abort early
					return;
				}
			}
			
			ThreadPool.UnsafeQueueUserWorkItem(TryReleaseRead, NextTask);
		}
		
		private void TryReleaseRead(object state)
		{	//****************************************
			var NextRelease = (TaskCompletionSource<IDisposable>)state;
			var LockInstance = new AsyncWriteLockInstance(this);
			//****************************************
			
			// A writer may cancel, so we need to check for that and loop if it fails
			while (!NextRelease.TrySetResult(LockInstance))
			{
				lock (_Writers)
				{
					// If there are no more writers, we can abort
					if (_Writers.Count == 0)
					{
						// A reader may have been queued up while we're processing
						if (_ReadersWaiting.Count == 0)
						{
							_Counter = 0;
							
							// We might have been disposed while we're processing
							if (_Disposed != null)
								_Disposed.SetResult(null);

							return;
						}
						
						// Yes, let's swap out the existing reader task with a new one (so we can activate all the waiting readers)
						NextRelease = Interlocked.Exchange(ref _Reader, new TaskCompletionSource<IDisposable>());
						
						// Move the waiting readers over to active and clear them out
						_Counter = _ReadersWaiting.Count;
						_ReadersWaiting.Clear();
					}
					else
					{
						
						// Get the next writer to try and activate
						NextRelease = _Writers.Dequeue();
					}
				}
				
				// If we're releasing a writer, fire and forget
				if (_Counter > 0)
				{
					NextRelease.SetResult(null);
					
					return;
				}
			}
		}

		[SecuritySafeCritical]
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

					// Have we been disposed of?
					if (_Disposed != null)
					{
						_Counter = 0;

						NextRelease = _Disposed;
					}
					// If there are one or more other writers waiting...
					else if (_Writers.Count > 0)
					{
						// Find the next one to activate and set the result appropriately
						NextRelease = _Writers.Dequeue();
					}
					// No writers, are there any readers?
					else if (_ReadersWaiting.Count > 0)
					{
						// Yes, let's swap out the existing reader task with a new one (so we can activate all the waiting readers)
						NextRelease = Interlocked.Exchange(ref _Reader, new TaskCompletionSource<IDisposable>());
						
						// Move the waiting readers over to active and clear them out
						_Counter = _ReadersWaiting.Count;
						_ReadersWaiting.Clear();
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
					
					return;
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
		
		private void CancelRead(AsyncReadLockInstance instance)
		{
			lock (_Writers)
			{
				if (_Disposed != null)
					return;
				
				// Is this reader still waiting?
				if (_ReadersWaiting.Remove(instance))
				{
					// Yes, and we've removed it
					return;
				}
			}
			
			// Writer finished between the task cancelling and this continuation running
			// Need to release the reader
			ReleaseRead();
		}
		
		private void CancelWrite(object state)
		{	//****************************************
			var NextRelease = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already activated and been removed from the Writers list
			if (!NextRelease.TrySetCanceled())
				return;
			
			lock (_Writers)
			{
				if (_Disposed != null)
					return;

				// If a writer is already running, they will take care of things when they finish
				if (_Counter == -1)
					return;
				
				// If we're the only writer in the list, we should remove ourselves
				if (_Writers.Count == 1 && _Writers.Peek() == NextRelease)
					_Writers.Clear();
				
				// If there's other writers, or no other readers waiting, nothing to do
				if (_Writers.Count != 0 || _ReadersWaiting.Count == 0)
					return;
				
				// Yes, let's swap out the existing reader task with a new one (so we can activate all the waiting readers)
				NextRelease = Interlocked.Exchange(ref _Reader, new TaskCompletionSource<IDisposable>());
				
				// Add the waiting readers to the count and clear them
				_Counter += _ReadersWaiting.Count;
				_ReadersWaiting.Clear();
			}
			
			// Release the readers
			NextRelease.SetResult(null);
		}
		
		private static void CleanupCancelSource(Task task, object state)
		{
			((CancellationTokenSource)state).Dispose();
		}

		//****************************************

		/// <summary>
		/// Gets whether any concurrent reading operations are in progress
		/// </summary>
		public bool IsReading => _Counter > 0;

		/// <summary>
		/// Gets whether an exclusive writing operation is in progress
		/// </summary>
		public bool IsWriting => _Counter == -1;

		/// <summary>
		/// Gets the number of readers queued
		/// </summary>
		public int WaitingReaders => _ReadersWaiting.Count;

		/// <summary>
		/// Gets the number of writers queued
		/// </summary>
		public int WaitingWriters => _Writers.Count;

		/// <summary>
		/// Gets if the read/write lock favours an unfair algorithm
		/// </summary>
		/// <remarks>In unfair mode, a read lock will succeed if it's already held elsewhere, even if there are writers waiting</remarks>
		public bool IsUnfair { get; } = false;

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
			
			public IDisposable LockRead(Task<IDisposable> task)
			{
				if (task.IsCanceled)
					throw new ObjectDisposedException("AsyncReadWriteLock", "Lock has been disposed of");
				
				return this;
			}

			public void CancelRead(Task<IDisposable> task) => _Source.CancelRead(this);

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
