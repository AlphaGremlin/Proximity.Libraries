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
	/// 
	/// </summary>
	public sealed class AsyncReadWriteLock
	{	//****************************************
		private readonly Task<IDisposable> _ReaderCompleted;
		private readonly Task<IDisposable> _WriterCompleted;
		
		private readonly Queue<TaskCompletionSource<IDisposable>> _Writers = new Queue<TaskCompletionSource<IDisposable>>();
		private TaskCompletionSource<IDisposable> _Reader = new TaskCompletionSource<IDisposable>();
		
		private int _ReadersWaiting = 0, _Counter = 0;
		//****************************************
		
		public AsyncReadWriteLock()
		{
			//_ReaderCompleted = Task.FromResult<IDisposable>(new AsyncReadLockInstance(this));
			//_WriterCompleted = Task.FromResult<IDisposable>(new AsyncWriteLockInstance(this));
		}
		
		//****************************************
		
		public Task<IDisposable> LockRead()
		{
			lock (_Writers)
			{
				// If there are zero or more readers and no waiting writers...
				if (_Counter >=0 && _Writers.Count == 0)
				{
					// Add another reader and return a completed task the caller can use to release the lock
					Interlocked.Increment(ref _Counter);
					
					return Task.FromResult<IDisposable>(new AsyncReadLockInstance(this));
				}
				
				// There's a writer in progress, or one waiting
				Interlocked.Increment(ref _ReadersWaiting);
				
				// Return a reader task the caller can wait on
				// Create a continuation, so they don't run serialised
				return _Reader.Task.ContinueWith(t => t.Result);
			}
		}
		
		public Task<IDisposable> LockWrite()
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
					// Take the writer spot and find the writer to release
					Interlocked.Decrement(ref _Counter);
					
					NextWriter = _Writers.Dequeue();
				}
			}
			
			// If there's a writer to release, release them
			if (NextWriter != null)
				NextWriter.SetResult(new AsyncWriteLockInstance(this));
		}

		private void ReleaseWrite()
		{	//****************************************
			TaskCompletionSource<IDisposable> NextRelease = null;
			IDisposable Result = null;
			//****************************************
			
			lock (_Writers)
			{
				if (_Counter != -1)
					throw new InvalidOperationException("No writer lock is currently held");
				
				// If there are one or more other writers waiting...
				if (_Writers.Count > 0)
				{
					// Find the next one to release and set the result appropriately
					NextRelease = _Writers.Dequeue();
					
					Result = new AsyncWriteLockInstance(this);
				}
				// No writers, are there any readers?
				else if (_ReadersWaiting > 0)
				{
					// Yes, let's swap out the existing reader task with a new one (so we can release all the waiting readers)
					NextRelease = Interlocked.Exchange(ref _Reader, new TaskCompletionSource<IDisposable>());
					
					// Swap the waiting readers with zero and update the counter
					_Counter = Interlocked.Exchange(ref _ReadersWaiting, 0);
					
					Result = new AsyncReadLockInstance(this);
				}
				else
				{
					// Nothing waiting, set the counter to zero
					_Counter = 0;
				}
			}
			
			// If there's a reader or writer waiting, release it
			if (NextRelease != null)
				NextRelease.SetResult(Result);
		}
		
		//****************************************
		
		private class AsyncReadLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncReadWriteLock _Source;
			private readonly StackTrace _Trace;
			private bool _Released;
			//****************************************
			
			internal AsyncReadLockInstance(AsyncReadWriteLock source)
			{
				_Source = source;
				_Trace = new StackTrace(2);
			}
			
			~AsyncReadLockInstance()
			{
				if (!_Released)
					Log.Error("Read Instance finalised without disposal, location: {0}", _Trace);
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null)
				{
					_Released = true;
					_Source.ReleaseRead();
				}
			}
		}

		private class AsyncWriteLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncReadWriteLock _Source;
			private readonly StackTrace _Trace;
			private bool _Released;
			//****************************************
			
			internal AsyncWriteLockInstance(AsyncReadWriteLock source)
			{
				_Source = source;
				
			}
			
			~AsyncWriteLockInstance()
			{
				if (!_Released)
					Log.Error("Write Instance finalised without disposal, location: {0}", _Trace);
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null)
				{
					_Released = true;
					_Source.ReleaseWrite();
				}
			}
		}
	}
}
