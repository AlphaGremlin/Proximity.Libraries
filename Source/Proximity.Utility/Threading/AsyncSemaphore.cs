/****************************************\
 AsyncSemaphoreSlim.cs
 Created: 2014-02-18
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for semaphores supporting async/await and a disposable model for releasing
	/// </summary>
	public sealed class AsyncSemaphore : IDisposable
	{	//****************************************
		private readonly ConcurrentQueue<TaskCompletionSource<IDisposable>> _Waiters = new ConcurrentQueue<TaskCompletionSource<IDisposable>>();
		private int _MaxCount, _CurrentCount;

		private TaskCompletionSource<AsyncSemaphore> _Dispose;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphore() : this(1)
		{
		}

		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphore(int initialCount)
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
			if (_Dispose != null || Interlocked.CompareExchange(ref _Dispose, new TaskCompletionSource<AsyncSemaphore>(), null) != null)
				return;

			DisposeWaiters();

			// If there's no counters, we can complete the dispose task
			if (_CurrentCount == 0)
				_Dispose.TrySetResult(this);
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
			// Are we disposed?
			if (_Dispose != null)
				throw new ObjectDisposedException("AsyncSemaphore", "Semaphore has been disposed of");

			// Try and add a counter as long as nobody is waiting on it
			if (_Waiters.IsEmpty && TryIncrement())
			{
#if NET40
				return TaskEx.FromResult<IDisposable>(new AsyncLockInstance(this));
#else
				return Task.FromResult<IDisposable>(new AsyncLockInstance(this));
#endif
			}
			
			// No free counters, add ourselves to the queue waiting on a counter
			var NewWaiter = new TaskCompletionSource<IDisposable>();

			_Waiters.Enqueue(NewWaiter);

			// Did a counter become available while we were busy?
			if (TryIncrement())
				Decrement();

			return PrepareWaiter(NewWaiter, token);
		}

		/// <summary>
		/// Tries to take a counter without waiting
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <returns>True if the counter was taken without waiting, otherwise False</returns>
		public bool TryTake(out IDisposable handle)
		{
			// Are we disposed?
			if (_Dispose == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = new AsyncLockInstance(this);
				return true;
			}

			handle = null;

			return false;
		}
		
		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on a counter</param>
		/// <returns>True if the counter was taken without waiting, otherwise False due to disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryTake(out IDisposable handle, CancellationToken token)
		{
#if NET40
			return TryTake(out handle, new TimeSpan(0, 0, 0, 0, -1), token);
#else
			return TryTake(out handle, Timeout.InfiniteTimeSpan, token);
#endif
		}
		
		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <returns>True if the counter was taken, otherwise False due to timeout or disposal</returns>
		public bool TryTake(out IDisposable handle, TimeSpan timeout)
		{
			return TryTake(out handle, timeout, CancellationToken.None);
		}

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was taken, otherwise False due to timeout or disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryTake(out IDisposable handle, TimeSpan timeout, CancellationToken token)
		{	//****************************************
			TaskCompletionSource<IDisposable> NewWaiter;
			//****************************************

			// First up, try and take the counter if possible
			if (_Dispose == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = new AsyncLockInstance(this);
				return true;
			}

			handle = null;

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero)
				return false;

			//****************************************

#if NET40
			if (timeout != new TimeSpan(0, 0, 0, 0, -1) && timeout < TimeSpan.Zero)
#else
			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
#endif
				throw new ArgumentOutOfRangeException("timeout", "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			NewWaiter = new TaskCompletionSource<IDisposable>();

			_Waiters.Enqueue(NewWaiter);

			// Was a counter added while we were busy?
			if (TryIncrement())
				Decrement(); // Try and activate a waiter, or at least increment

			//****************************************

			// Are we okay with blocking indefinitely (or until the token is raised)?
#if NET40
			if (timeout == new TimeSpan(0, 0, 0, 0, -1))
#else
			if (timeout == Timeout.InfiniteTimeSpan)
#endif
			{
				// Prepare the cancellation token (if any)
				PrepareWaiter(NewWaiter, token);

				try
				{
					// Wait indefinitely for a definitive result
					handle = NewWaiter.Task.GetAwaiter().GetResult();

					return true;
				}
				catch
				{
					// We may get a cancel or dispose exception
					if (!token.IsCancellationRequested)
						return false; // Original token was not cancelled, so return false

					throw; // Throw the cancellation
				}
			}

			// We want to be able to timeout and possibly cancel too
			using (var MySource = CancellationTokenSource.CreateLinkedTokenSource(token))
			{
				// Prepare the cancellation token (if any)
				PrepareWaiter(NewWaiter, MySource.Token);

				// If the task is completed, all good!
				if (NewWaiter.Task.IsCompleted)
				{
					handle = NewWaiter.Task.Result;

					return true;
				}

				// We can't just use Decrement(token).Wait(timeout) because that won't cancel the decrement attempt
				// Instead, we have to cancel on the original token
				MySource.CancelAfter(timeout);

				try
				{
					// Wait for the task to complete, knowing it will either cancel due to the token, the timeout, or because we're zero and disposed
					handle = NewWaiter.Task.GetAwaiter().GetResult();

					return true;
				}
				catch
				{
					// We may get a cancel or dispose exception
					if (!token.IsCancellationRequested)
						return false; // Original token was not cancelled, so either we timed out or are disposed. Return false

					throw; // Throw the cancellation
				}
			}
		}

		//****************************************

		private Task<IDisposable> PrepareWaiter(TaskCompletionSource<IDisposable> waiter, CancellationToken token)
		{
			// Check if we can get cancelled
			if (token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(Cancel, waiter);

				// If we complete and haven't been cancelled, dispose of the registration
				waiter.Task.ContinueWith((Action<Task<IDisposable>, object>)CleanupCancelRegistration, MyRegistration, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}

			return waiter.Task;
		}

		private bool TryIncrement()
		{	//****************************************
			var MyWait = new SpinWait();

			int OldCount;
			//****************************************

			for (; ; )
			{
				OldCount = _CurrentCount;

				// Are there any free counters?
				if (OldCount >= _MaxCount)
					return false;

				// Free counter, attempt to take it
				if (Interlocked.CompareExchange(ref _CurrentCount, OldCount + 1, OldCount) == OldCount)
					return true;

				// Failed. Spin and try again
				MyWait.SpinOnce();
			}
		}

		private void Decrement()
		{	//****************************************
			TaskCompletionSource<IDisposable> NextWaiter;
			int NewCount;
			//****************************************

			do
			{
				// Try and retrieve a waiter while we're not disposed
				while (_Waiters.TryDequeue(out NextWaiter))
				{
					// Is it completed?
					if (NextWaiter.Task.IsCompleted)
						continue; // Yes, so find another

					// Don't want to activate waiters on the calling thread though, since it can cause a stack overflow if Increment gets called from a Decrement continuation
#if PORTABLE
					Task.Factory.StartNew(ReleaseWaiter, NextWaiter);
#else
					ThreadPool.QueueUserWorkItem(ReleaseWaiter, NextWaiter);
//					ThreadPool.UnsafeQueueUserWorkItem(ReleaseWaiter, NextWaiter);
#endif
					return;
				}

				// Release a counter
				NewCount = Interlocked.Decrement(ref _CurrentCount);

				// Is there anybody waiting?
				if (_Waiters.IsEmpty)
					break;

				// Someone is waiting. Try and put the counter back. If it fails, someone else can take care of the waiter
			} while (TryIncrement());

			// If we've disposed and there's no counters, we can complete the dispose task
			if (_Dispose != null && NewCount == 0)
				_Dispose.TrySetResult(this);
		}

		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************

			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}

		private void ReleaseWaiter(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************

			// Release our Waiter
			if (!MyWaiter.TrySetResult(new AsyncLockInstance(this)))
				Decrement(); // Failed so it was cancelled. Make sure we remove the counter
		}

		private void DisposeWaiters()
		{	//****************************************
			TaskCompletionSource<IDisposable> MyWaiter;
			//****************************************

			// Success, now close any pending waiters
			while (_Waiters.TryDequeue(out MyWaiter))
				MyWaiter.TrySetException(new ObjectDisposedException("AsyncSemaphore", "Counter has been disposed of"));
		}

		private static void CleanupCancelRegistration(Task task, object state)
		{
			((CancellationTokenRegistration)state).Dispose();
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
			get { return _Waiters.Count(waiter => !waiter.Task.IsCompleted); }
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
					throw new ObjectDisposedException("AsyncSemaphore", "Object has been disposed");

				_MaxCount = value;

				// Release any waiters that might now have free slots
				if (!_Waiters.IsEmpty && TryIncrement())
					Decrement();
			}
		}

		//****************************************

		private class AsyncLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncSemaphore _Source;
			private int _Released;
			//****************************************

			internal AsyncLockInstance(AsyncSemaphore source)
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
