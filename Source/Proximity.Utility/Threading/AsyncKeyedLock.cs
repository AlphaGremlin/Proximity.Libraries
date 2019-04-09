/****************************************\
 AsyncKeyedLock.cs
 Created: 2016-04-11
\****************************************/
#if !NET40
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides an async primitive for ordered locking based on a key
	/// </summary>
	/// <typeparam name="TKey">The type of key to lock on</typeparam>
	public sealed class AsyncKeyedLock<TKey> : IDisposable
	{	//****************************************
		private readonly ConcurrentDictionary<TKey, ImmutableQueue<TaskCompletionSource<IDisposable>>> _Locks;
		private TaskCompletionSource<IDisposable> _Dispose;
		//****************************************

		/// <summary>
		/// Creates a new keyed lock with the default equality comparer
		/// </summary>
		public AsyncKeyedLock() : this(EqualityComparer<TKey>.Default)
		{
		}

		/// <summary>
		/// Creates a new keyed lock with a custom equality comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use for matching keys</param>
		public AsyncKeyedLock(IEqualityComparer<TKey> comparer)
		{
			_Locks = new ConcurrentDictionary<TKey, ImmutableQueue<TaskCompletionSource<IDisposable>>>(comparer);
		}

		//****************************************

		/// <summary>
		/// Disposes of the keyed lock
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
			if (_Dispose != null || Interlocked.CompareExchange(ref _Dispose, new TaskCompletionSource<IDisposable>(), null) != null)
				return;

			DisposeWaiters();
		}

		/// <summary>
		/// Attempts to take a lock
		/// </summary>
		/// <param name="key">The key to lock on</param>
		/// <returns>A task that completes when the lock is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Lock(TKey key)
		{
			return Lock(key, CancellationToken.None);
		}

		/// <summary>
		/// Attempts to take a lock
		/// </summary>
		/// <param name="key">The key to lock on</param>
		/// <param name="timeout">The amount of time to wait for the lock</param>
		/// <returns>A task that completes when the lock is taken, giving an IDisposable to release the lock</returns>
		public Task<IDisposable> Lock(TKey key, TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource();
			var MyTask = Lock(key, MySource.Token);
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
		/// Attempts to take a lock
		/// </summary>
		/// <param name="key">The key to lock on</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when the lock is taken, giving an IDisposable to release the lock</returns>
		public Task<IDisposable> Lock(TKey key, CancellationToken token)
		{	//****************************************
			TaskCompletionSource<IDisposable> NewWaiter;
			ImmutableQueue<TaskCompletionSource<IDisposable>> OldValue, NewValue;
			//****************************************

			// A null key means it's a disposal, so we can't allow the user to lock on it
			if (key == null)
				throw new ArgumentNullException("key", "Key cannot be null");

			NewWaiter = new TaskCompletionSource<IDisposable>(key);

			// Is this keyed lock disposing?
			if (_Dispose != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException("AsyncKeyedLock", "Keyed Lock has been disposed of"));

			// Try and add ourselves to the lock queue
			for (; ;)
			{
				if (_Locks.TryGetValue(key, out OldValue))
				{
					// Has the lock been disposed? We may have been disposed while adding
					if (!OldValue.IsEmpty && OldValue.Peek().Task.AsyncState == null)
						return Task.FromException<IDisposable>(new ObjectDisposedException("AsyncKeyedLock", "Keyed Lock has been disposed of"));

					// No, so add ourselves to the queue
					NewValue = OldValue.Enqueue(NewWaiter);

					if (_Locks.TryUpdate(key, NewValue, OldValue))
						break;
				}
				else if (_Locks.TryAdd(key, NewValue = ImmutableQueue<TaskCompletionSource<IDisposable>>.Empty))
				{
					if (_Dispose == null)
						break;

					// If we disposed during this, abort
					((IDictionary<TKey, ImmutableQueue<TaskCompletionSource<IDisposable>>>)_Locks).Remove(key);

					return Task.FromException<IDisposable>(new ObjectDisposedException("AsyncKeyedLock", "Keyed Lock has been disposed of"));
				}
			}

			// Were we added (ie: did we take the lock)?
			if (NewValue.IsEmpty)
				return Task.FromResult<IDisposable>(new AsyncKeyedLockInstance(this, key));

			// No, so we're waiting then. Check if we can get cancelled
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

		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************

			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}

		private void Release(TKey key)
		{	//****************************************
			ImmutableQueue<TaskCompletionSource<IDisposable>> OldQueue, NewQueue;
			TaskCompletionSource<IDisposable> NextWaiter;
			//****************************************

				// Retrieve the current lock state
			while (_Locks.TryGetValue(key, out OldQueue))
			{
				NewQueue = OldQueue;

				for (; ; )
				{
					// If it's empty, there's no waiters or they've all cancelled
					if (NewQueue.IsEmpty)
					{
						// Release the lock
						if (_Locks.TryRemovePair(key, OldQueue))
							return;

						// Someone modified the lock queue, probably a new waiter, so retry
						break;
					}

					// Remove the next waiter from the queue
					NewQueue = NewQueue.Dequeue(out NextWaiter);

					// Check if we can release this Waiter
					if (NextWaiter.Task.IsCompleted)
						// Waiter has cancelled. Try and pull another off the queue
						continue;

					// Is this a disposal?
					if (NextWaiter.Task.AsyncState == null)
					{
						// Yes, try and release the lock
						if (!_Locks.TryRemovePair(key, OldQueue))
							break;

						NextWaiter.SetResult(null);

						return;
					}

					// Apply the changes to the lock queue
					if (!_Locks.TryUpdate(key, NewQueue, OldQueue))
						// Someone modified the lock queue, probably a new waiter, so retry
						break;

					// Don't want to activate waiters on the calling thread, since it can cause a stack overflow if Wait gets called from a Release continuation
#if NETSTANDARD1_3
					Task.Factory.StartNew(ReleaseWaiter, NextWaiter);
#else
					ThreadPool.QueueUserWorkItem(ReleaseWaiter, NextWaiter);
					//					ThreadPool.UnsafeQueueUserWorkItem(ReleaseWaiter, NextWaiter);
#endif
					return;
				}
			}

			throw new InvalidOperationException("No lock is currently held for key");
		}

		private void ReleaseWaiter(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			var MyKey = (TKey)MyWaiter.Task.AsyncState;
			//****************************************

			// Activate our Waiter
			if (!MyWaiter.TrySetResult(new AsyncKeyedLockInstance(this, MyKey)))
				Release(MyKey); // Failed so it was cancelled. We hold the lock so release it
		}

		private void DisposeWaiters()
		{	//****************************************
			var MyWaitTasks = new List<Task>();
			bool DisposedLock;
			//****************************************

			do
			{
				DisposedLock = false;

				// Dispose of every keyed lock
				foreach (var MyPair in _Locks)
				{
					// Have we already disposed this lock?
					if (!MyPair.Value.IsEmpty && MyPair.Value.Peek().Task.AsyncState == null)
						continue;

					// No, so we're going to make the attempt. Flag it so we retry if it fails
					DisposedLock = true;

					var MyDisposeWaiter = new TaskCompletionSource<IDisposable>();

					// Try and replace the pending waiters
					if (_Locks.TryUpdate(MyPair.Key, ImmutableQueue<TaskCompletionSource<IDisposable>>.Empty.Enqueue(MyDisposeWaiter), MyPair.Value))
					{
						MyWaitTasks.Add(MyDisposeWaiter.Task);

						// Done, now we cancel the existing waiters
						foreach (var MyWaiter in MyPair.Value)
							MyWaiter.TrySetException(new ObjectDisposedException("AsyncKeyedLock", "Keyed Lock has been disposed of"));
					}
				}

				// Repeat until we don't find any more locks to dispose
			} while (DisposedLock);

			//****************************************

			// Now all our locks have been disposed and we have tasks that will notify us when they're done, we wait
			Task.WhenAll(MyWaitTasks).ContinueWith(CompleteDispose);
		}

		private void CompleteDispose(Task task)
		{
			// We don't care if the parent task cancelled, and it will never fault, so just complete the disposal operation
			_Dispose.SetResult(null);
		}

		//****************************************

		/// <summary>
		/// Gets an enumeration of the keys that are currently locked
		/// </summary>
		public IEnumerable<TKey> KeysHeld
		{
			get { return _Locks.Keys; }
		}

		//****************************************

		private class AsyncKeyedLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncKeyedLock<TKey> _Source;
			private readonly TKey _Key;
			private int _Released;
			//****************************************

			internal AsyncKeyedLockInstance(AsyncKeyedLock<TKey> source, TKey key)
			{
				_Source = source;
				_Key = key;
			}

			//****************************************

			public void Dispose()
			{
				if (_Source != null && Interlocked.Exchange(ref _Released, 1) == 0)
				{
					_Source.Release(_Key);
				}
			}
		}
	}
}
#endif