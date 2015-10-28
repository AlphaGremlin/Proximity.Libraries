/****************************************\
 AsyncSemaphoreSlim.cs
 Created: 2014-02-18
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for semaphores supporting async/await and a disposable model for releasing
	/// </summary>
	public sealed class AsyncSemaphoreSlim : IDisposable
	{	//****************************************
		private readonly ConcurrentQueue<TaskCompletionSource<IDisposable>> _Waiters = new ConcurrentQueue<TaskCompletionSource<IDisposable>>();
		private int _MaxCount, _CurrentCount;

		private TaskCompletionSource<AsyncSemaphoreSlim> _Dispose;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphoreSlim() : this(1)
		{
		}

		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphoreSlim(int initialCount)
		{
			if (initialCount < 1)
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
		{
			if (_Dispose != null || Interlocked.CompareExchange(ref _Dispose, new TaskCompletionSource<AsyncSemaphoreSlim>(), null) != null)
				return;

			CheckWaiters();
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
		{
			// Try and take a counter as long as nobody is waiting on it
			if (TryIncrement(true))
			{
#if NET40
				return TaskEx.FromResult<IDisposable>(new AsyncLockInstance(this));
#else
				return Task.FromResult<IDisposable>(new AsyncLockInstance(this));
#endif
			}
			
			// Are we disposed?
			if (_Dispose != null)
				throw new ObjectDisposedException("AsyncSemaphoreSlim", "Semaphore has been disposed of");

			// No free counters, add ourselves to the queue waiting on a counter
			var NewWaiter = new TaskCompletionSource<IDisposable>();

			_Waiters.Enqueue(NewWaiter);

			CheckWaiters();

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

		private bool TryIncrement(bool checkWaiters)
		{	//****************************************
			var MyWait = new SpinWait();

			int OldCount;
			//****************************************

			for (; ; )
			{
				// Are we disposed?
				if (_Dispose != null)
					return false;

				OldCount = _CurrentCount;

				// Are there any free counters?
				if (OldCount >= _MaxCount)
					return false;

				// There's a free counter, but if someone is waiting, we shouldn't take it. The thread that did the free will handle the waiter
				if (checkWaiters && !_Waiters.IsEmpty)
					return false;

				// Free counter, attempt to take it
				if (Interlocked.CompareExchange(ref _CurrentCount, OldCount + 1, OldCount) == OldCount)
				{
					// If we got disposed of, release it immediately
					if (_Dispose != null)
					{
						Decrement();

						return false;
					}

					return true;
				}

				// Failed. Spin and try again
				MyWait.SpinOnce();
			}
		}

		private void Decrement()
		{
			// Is there anybody waiting?
			if (_Waiters.IsEmpty)
			{
				// Release a counter
				Interlocked.Decrement(ref _CurrentCount);

				CheckWaiters();

				return;
			}

			// Somebody is waiting
#if PORTABLE
			Task.Factory.StartNew(ReleaseWaiters, true);
#else
			ThreadPool.UnsafeQueueUserWorkItem(ReleaseWaiters, true);
#endif
		}

		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************

			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}

		private void CheckWaiters()
		{
			// Is there anybody waiting?
			if (_Waiters.IsEmpty)
			{
				// If we've disposed and there's no waiters or counters, we can complete the dispose task
				if (_Dispose != null && _CurrentCount == 0)
					_Dispose.TrySetResult(this);

				return;
			}

#if PORTABLE
			Task.Factory.StartNew(ReleaseWaiters, false);
#else
			ThreadPool.UnsafeQueueUserWorkItem(ReleaseWaiters, false);
#endif
		}

		private void ReleaseWaiters(object state)
		{	//****************************************
			TaskCompletionSource<IDisposable> NextWaiter = null;
			AsyncLockInstance MyInstance = null;
			bool IsSemaphoreHeld = (bool)state;
			//****************************************

			for (; ; )
			{
				// Are there any waiters?
				if (_Waiters.IsEmpty)
				{
					// No, do we hold a counter?
					if (IsSemaphoreHeld)
						Decrement(); // Yes, so release it first. Will set disposed if necessary

					return;
				}

				if (IsSemaphoreHeld)
				{
					// Have we been disposed?
					if (_Dispose != null)
					{
						DisposeWaiters(); // Dump all the waiters first

						Decrement(); // Now release our counter

						return;
					}
				}
				// Can we take a semaphore?
				else if (!TryIncrement(false))
				{
					// Were we disposed?
					if (_Dispose != null)
						DisposeWaiters();

					// Can't take a semaphore, so leave the waiters to someone else
					return;
				}

				// Try and retrieve a waiter
				while (_Waiters.TryDequeue(out NextWaiter))
				{
					if (MyInstance == null)
						MyInstance = new AsyncLockInstance(this);

					// Got a waiter. Can we complete it?
					if (NextWaiter.TrySetResult(MyInstance))
						return; // Success, semaphore has been passed on. We're done

					// No, so it was cancelled. Try to get another
				}

				// The waiter was stolen by another thread.
				// Loop back and try again
				IsSemaphoreHeld = true;
			}
		}
			
		private void DisposeWaiters()
		{	//****************************************
			TaskCompletionSource<IDisposable> MyWaiter;
			//****************************************

			// Success, now close any pending waiters
			while (_Waiters.TryDequeue(out MyWaiter))
				MyWaiter.TrySetException(new ObjectDisposedException("AsyncSemaphoreSlim", "Counter has been disposed of"));
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

				if (_Dispose != null)
					throw new ObjectDisposedException("AsyncSemaphoreSlim", "Object has been disposed");

				_MaxCount = value;

				// Release any waiters that might now have free slots
				CheckWaiters();
			}
		}

		//****************************************

		private class AsyncLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncSemaphoreSlim _Source;
			private int _Released;
			//****************************************

			internal AsyncLockInstance(AsyncSemaphoreSlim source)
			{
				_Source = source;
			}

			//****************************************

			public void Dispose()
			{
				if (_Source != null && Interlocked.Exchange(ref _Released, 1) == 0)
				{
					_Source.Decrement();
				}
			}
		}
	}
}
