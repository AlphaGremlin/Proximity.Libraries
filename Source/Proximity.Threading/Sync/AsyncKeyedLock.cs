using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Threading;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides an async primitive for ordered locking based on a key
	/// </summary>
	/// <typeparam name="TKey">The type of key to lock on</typeparam>
	public sealed partial class AsyncKeyedLock<TKey> : IAsyncDisposable where TKey : notnull
	{	//****************************************
		private readonly ConcurrentDictionary<TKey, ImmutableList<KeyedLockInstance>> _Locks;
		private LockDisposer? _Disposer;
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
			_Locks = new ConcurrentDictionary<TKey, ImmutableList<KeyedLockInstance>>(comparer);
		}

		//****************************************

		/// <summary>
		/// Disposes of the keyed lock
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public ValueTask DisposeAsync()
		{
			if (_Disposer == null && Interlocked.CompareExchange(ref _Disposer, new LockDisposer(), null) == null)
			{
				// Success, now close any pending waiters
				DisposeWaiters();
			}

			return new ValueTask(_Disposer.Task);
		}

		/// <summary>
		/// Attempts to take a lock
		/// </summary>
		/// <param name="key">The key to lock on</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when the lock is taken, giving an IDisposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<Instance> Lock(TKey key, CancellationToken token = default) => Lock(key, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to take a lock
		/// </summary>
		/// <param name="key">The key to lock on</param>
		/// <param name="timeout">The amount of time to wait for the lock</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when the lock is taken, giving an IDisposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<Instance> Lock(TKey key, TimeSpan timeout, CancellationToken token = default)
		{ //****************************************
			ImmutableList<KeyedLockInstance> NewValue;
			//****************************************

			if (key == null)
				throw new ArgumentNullException(nameof(key), "Key cannot be null");

			// Is this keyed lock disposing?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncKeyedLock<TKey>), "Keyed Lock has been disposed of");

			var NewInstance = KeyedLockInstance.GetOrCreate(this, key);

			// Try and take the lock, or add ourselves to the lock queue
			for (; ; )
			{
				if (_Locks.TryGetValue(key, out var OldValue))
				{
					// Add ourselves to the lock queue
					NewValue = OldValue.Add(NewInstance);

					if (_Locks.TryUpdate(key, NewValue, OldValue))
						break;
				}
				else
				{
					// An empty queue means we hold the lock
					if (_Locks.TryAdd(key, NewValue = ImmutableList<KeyedLockInstance>.Empty))
						break;
				}
			}

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			// Did we dispose along the way
			if (_Disposer == null)
			{
				// If the queue is empty then we hold the lock
				if (NewValue.IsEmpty)
				{
					// If we cancelled between taking the lock and now, we need to release it so someone else can possibly take it
					if (!NewInstance.TrySwitchToCompleted())
						Release(key);
				}

				// We're waiting, so make sure we handle cancellation
				if (!ValueTask.IsCompleted)
					NewInstance.ApplyCancellation(token, timeout);
			}
			else
			{
				if (NewValue.IsEmpty)
				{
					// If we didn't add ourselves to the queue, we need to dispose ourselves
					NewInstance.SwitchToDisposed();

					// Now release the lock we took, to trigger disposal
					Release(key);
				}
				else
				{
					// Dispose of the waiter (if the existing lock doesn't release and do it for us)
					DisposeWaiters();
				}
			}

			return ValueTask;
		}

		//****************************************

		private void Cancel(KeyedLockInstance instance)
		{
			// Retrieve the current lock state
			while (_Locks.TryGetValue(instance.Key!, out var OldQueue))
			{
				// Can get no results (or no queue at all) if
				// - someone releases while we cancel and Release removes us from the queue
				// - we disposed
				if (OldQueue.IsEmpty)
					return;

				var NewQueue = OldQueue.Remove(instance);

				if (ReferenceEquals(OldQueue, NewQueue))
					return;

				// Apply the changes to the lock queue
				if (_Locks.TryUpdate(instance.Key!, NewQueue, OldQueue))
					return;

				// Someone modified the lock queue, probably a new waiter, so retry
			}
		}

		private void Release(TKey key)
		{
			// Retrieve the current lock state
			while (_Locks.TryGetValue(key, out var OldQueue))
			{
				for (; ; )
				{
					// If it's empty, there's no waiters or they've all cancelled
					if (OldQueue.IsEmpty)
					{
						// Release the lock
						if (((IDictionary<TKey, ImmutableList<KeyedLockInstance>>)_Locks).Remove(new KeyValuePair<TKey, ImmutableList<KeyedLockInstance>>(key, OldQueue)))
						{
							// If we're disposing and the final held lock, complete disposal
							if (_Disposer != null && _Locks.IsEmpty)
								_Disposer.SwitchToComplete();

							return;
						}

						// Someone modified the lock queue, probably a new waiter, so retry
						break;
					}

					// Remove the next waiter from the queue
					var NextWaiter = OldQueue[0];

					var NewQueue = OldQueue.RemoveAt(0);

					// Apply the changes to the lock queue
					if (!_Locks.TryUpdate(key, NewQueue, OldQueue))
						// Someone modified the lock queue, probably a new waiter, so retry
						break;

					// Try to activate the waiter
					if (NextWaiter.TrySwitchToCompleted())
						return;

					// Waiter has cancelled. Try and pull another off the queue
					OldQueue = NewQueue;
				}
			}

			// May get here if we dispose
			Debug.Fail("Lock was released but not held");
		}

		private void DisposeWaiters()
		{
			// If there's no locks, we can complete the dispose task
			if (_Locks.IsEmpty)
			{
				_Disposer!.SwitchToComplete();

				return;
			}

			foreach (var Key in _Locks.Keys)
			{
				while (_Locks.TryGetValue(Key, out var Queue) && !Queue.IsEmpty)
				{
					// Try to clear all the waiters
					if (_Locks.TryUpdate(Key, ImmutableList<KeyedLockInstance>.Empty, Queue))
					{
						foreach (var Instance in Queue)
							Instance.SwitchToDisposed();

						break;
					}
				}
			}
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
			private readonly KeyedLockInstance _Instance;
			private readonly short _Token;
			//****************************************

			internal Instance(KeyedLockInstance instance)
			{
				_Instance = instance;
				_Token = instance.Version;
			}

			//****************************************

			/// <summary>
			/// Releases the lock currently held
			/// </summary>
			public void Dispose() => _Instance.Release(_Token);
		}
	}
}
