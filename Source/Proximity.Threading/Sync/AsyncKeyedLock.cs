using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides an async primitive for ordered locking based on a key
	/// </summary>
	/// <typeparam name="TKey">The type of key to lock on</typeparam>
	public sealed partial class AsyncKeyedLock<TKey> : IAsyncDisposable
	{	//****************************************
		private readonly ConcurrentDictionary<TKey, ImmutableQueue<TaskCompletionSource<Instance>>> _Locks;
		private TaskCompletionSource<VoidStruct>? _Dispose;
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
			_Locks = new ConcurrentDictionary<TKey, ImmutableQueue<TaskCompletionSource<Instance>>>(comparer);
		}

		//****************************************

		/// <summary>
		/// Disposes of the keyed lock
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public ValueTask DisposeAsync()
		{
			if (_Dispose != null || Interlocked.CompareExchange(ref _Dispose, new TaskCompletionSource<VoidStruct>(), null) != null)
				return default;

			DisposeWaiters();

			return new ValueTask(_Dispose.Task);
		}

		/// <summary>
		/// Attempts to take a lock
		/// </summary>
		/// <param name="key">The key to lock on</param>
		/// <param name="timeout">The amount of time to wait for the lock</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when the lock is taken, giving an IDisposable to release the lock</returns>
		public ValueTask<Instance> Lock(TKey key, TimeSpan timeout, CancellationToken token = default)
		{	//****************************************
			var MySource = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token) : new CancellationTokenSource();
			var MyTask = InternalLock(key, MySource.Token);
			//****************************************

			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();

				return new ValueTask<Instance>(MyTask);
			}

			MySource.CancelAfter(timeout);

			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

			return new ValueTask<Instance>(MyTask);
		}

		/// <summary>
		/// Attempts to take a lock
		/// </summary>
		/// <param name="key">The key to lock on</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when the lock is taken, giving an IDisposable to release the lock</returns>
		public ValueTask<Instance> Lock(TKey key, CancellationToken token = default) => new ValueTask<Instance>(InternalLock(key, token));

		//****************************************

		private Task<Instance> InternalLock(TKey key, CancellationToken token)
		{ //****************************************
			TaskCompletionSource<Instance> NewWaiter;
			ImmutableQueue<TaskCompletionSource<Instance>> NewValue;
			//****************************************

			// A null key means it's a disposal, so we can't allow the user to lock on it
			if (key == null)
				throw new ArgumentNullException(nameof(key), "Key cannot be null");

			NewWaiter = new TaskCompletionSource<Instance>(key);

			// Is this keyed lock disposing?
			if (_Dispose != null)
				throw new ObjectDisposedException(nameof(AsyncKeyedLock<TKey>), "Keyed Lock has been disposed of");

			// Try and add ourselves to the lock queue
			for (; ; )
			{
				if (_Locks.TryGetValue(key, out var OldValue))
				{
					// Has the lock been disposed? We may have been disposed while adding
					if (!OldValue.IsEmpty && OldValue.Peek().Task.AsyncState == null)
						throw new ObjectDisposedException(nameof(AsyncKeyedLock<TKey>), "Keyed Lock has been disposed of");

					// No, so add ourselves to the queue
					NewValue = OldValue.Enqueue(NewWaiter);

					if (_Locks.TryUpdate(key, NewValue, OldValue))
						break;
				}
				else if (_Locks.TryAdd(key, NewValue = ImmutableQueue<TaskCompletionSource<Instance>>.Empty))
				{
					if (_Dispose == null)
						break;

					// If we disposed during this, abort
					((IDictionary<TKey, ImmutableQueue<TaskCompletionSource<Instance>>>)_Locks).Remove(key);

					throw new ObjectDisposedException(nameof(AsyncKeyedLock<TKey>), "Keyed Lock has been disposed of");
				}
			}

			// Were we added (ie: did we take the lock)?
			if (NewValue.IsEmpty)
				return Task.FromResult(new Instance(new AsyncKeyedLockInstance(this, key)));

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

		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<Instance>)state;
			//****************************************

			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}

		private void Release(TKey key)
		{	//****************************************
			ImmutableQueue<TaskCompletionSource<Instance>> NewQueue;
			//****************************************

				// Retrieve the current lock state
			while (_Locks.TryGetValue(key, out var OldQueue))
			{
				NewQueue = OldQueue;

				for (; ; )
				{
					// If it's empty, there's no waiters or they've all cancelled
					if (NewQueue.IsEmpty)
					{
						// Release the lock
						if (((IDictionary<TKey, ImmutableQueue<TaskCompletionSource<Instance>>>)_Locks).Remove(new KeyValuePair<TKey, ImmutableQueue<TaskCompletionSource<Instance>>>(key, OldQueue)))
							return;

						// Someone modified the lock queue, probably a new waiter, so retry
						break;
					}

					// Remove the next waiter from the queue
					NewQueue = NewQueue.Dequeue(out var NextWaiter);

					// Check if we can release this Waiter
					if (NextWaiter.Task.IsCompleted)
						// Waiter has cancelled. Try and pull another off the queue
						continue;

					// Is this a disposal?
					if (NextWaiter.Task.AsyncState == null)
					{
						// Yes, try and release the lock
						if (!((IDictionary<TKey, ImmutableQueue<TaskCompletionSource<Instance>>>)_Locks).Remove(new KeyValuePair<TKey, ImmutableQueue<TaskCompletionSource<Instance>>>(key, OldQueue)))
							break;

						NextWaiter.SetException(new ObjectDisposedException(nameof(AsyncKeyedLock<TKey>), "Keyed Lock has been disposed of"));

						return;
					}

					// Apply the changes to the lock queue
					if (!_Locks.TryUpdate(key, NewQueue, OldQueue))
						// Someone modified the lock queue, probably a new waiter, so retry
						break;

					// Don't want to activate waiters on the calling thread, since it can cause a stack overflow if Wait gets called from a Release continuation
					ThreadPool.QueueUserWorkItem(ReleaseWaiter, NextWaiter);

					return;
				}
			}

			throw new InvalidOperationException("No lock is currently held for key");
		}

		private void ReleaseWaiter(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<Instance>)state;
			var MyKey = (TKey)MyWaiter.Task.AsyncState;
			//****************************************

			// Activate our Waiter
			if (!MyWaiter.TrySetResult(new Instance(new AsyncKeyedLockInstance(this, MyKey))))
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

					var MyDisposeWaiter = new TaskCompletionSource<Instance>();

					// Try and replace the pending waiters
					if (_Locks.TryUpdate(MyPair.Key, ImmutableQueue<TaskCompletionSource<Instance>>.Empty.Enqueue(MyDisposeWaiter), MyPair.Value))
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
			_Dispose!.SetResult(default);
		}

		//****************************************

		/// <summary>
		/// Gets an enumeration of the keys that are currently locked
		/// </summary>
		public IReadOnlyCollection<TKey> KeysHeld => (IReadOnlyCollection<TKey>)_Locks.Keys;

		//****************************************

		/// <summary>
		/// Represents the Keyed Lock currently held
		/// </summary>
		public readonly struct Instance : IDisposable
		{ //****************************************
			private readonly AsyncKeyedLockInstance _Instance;
			//****************************************

			internal Instance(AsyncKeyedLockInstance instance)
			{
				_Instance = instance;
			}

			//****************************************

			/// <summary>
			/// Releases the lock currently held
			/// </summary>
			public void Dispose() => _Instance.Dispose();
		}
	}
}
