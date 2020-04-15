using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
//****************************************

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Provides a BlockingCollection that supports async/await
	/// </summary>
	/// <remarks>Producers should not complete the underlying IProducerConsumerCollection, or add a limit to it</remarks>
	public sealed partial class AsyncCollection<TItem> : IEnumerable<TItem>, IReadOnlyCollection<TItem>
	{ //****************************************
		private readonly IProducerConsumerCollection<TItem> _Collection;

		private readonly AsyncCounter? _FreeSlots;
		private readonly AsyncCounter _UsedSlots;
		private bool _IsCompleted, _IsFailed;
		//****************************************

		/// <summary>
		/// Creates a new unlimited collection over a concurrent queue
		/// </summary>
		public AsyncCollection() : this(new ConcurrentQueue<TItem>())
		{
		}

		/// <summary>
		/// Creates a new unlimited collection over the given producer consumer collection
		/// </summary>
		/// <param name="collection">The collection to block over</param>
		public AsyncCollection(IProducerConsumerCollection<TItem> collection)
		{
			_Collection = collection ?? throw new ArgumentNullException(nameof(collection));
			Capacity = int.MaxValue;

			_UsedSlots = new AsyncCounter(collection.Count);
		}

		/// <summary>
		/// Creates a new collection over a concurrent queue
		/// </summary>
		/// <param name="capacity">The maximum number of items in the queue</param>
		public AsyncCollection(int capacity) : this(new ConcurrentQueue<TItem>(), capacity)
		{
		}

		/// <summary>
		/// Creates a new collection over the given producer consumer collection
		/// </summary>
		/// <param name="collection">The collection to block over</param>
		/// <param name="capacity">The maximum number of items in the queue</param>
		public AsyncCollection(IProducerConsumerCollection<TItem> collection, int capacity)
		{
			if (capacity <= 0)
				throw new ArgumentException("Capacity is invalid");

			_Collection = collection ?? throw new ArgumentNullException(nameof(collection));
			Capacity = capacity;

			_UsedSlots = new AsyncCounter(collection.Count);
			_FreeSlots = new AsyncCounter(capacity);
		}

		//****************************************

		/// <summary>
		/// Attempts to add an Item to the collection
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <returns>A task representing the addition operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for free space</exception>
		public ValueTask Add(TItem item, TimeSpan timeout) => InternalAdd(item, false, timeout);

		/// <summary>
		/// Attempts to add an Item to the collection
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task representing the addition operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for free space</exception>
		public ValueTask Add(TItem item, CancellationToken token = default) => InternalAdd(item, false, token);

		/// <summary>
		/// Attempts to add and complete a collection in one operation
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <returns>A task representing the addition and completion operation</returns>
		public ValueTask AddComplete(TItem item, TimeSpan timeout) => InternalAdd(item, true, timeout);

		/// <summary>
		/// Attempts to add and complete a collection in one operation
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task representing the addition and completion operation</returns>
		public ValueTask AddComplete(TItem item, CancellationToken token = default) => InternalAdd(item, true, token);

		/// <summary>
		/// Waits until it's possible to remove an item from this collection
		/// </summary>
		/// <returns>A task that completes when data is available in the collection</returns>
		public ValueTask Peek() => _UsedSlots.PeekDecrement();

		/// <summary>
		/// Waits until it's possible to remove an item from this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort the wait operation</param>
		/// <returns>A task that completes when data is available in the collection</returns>
		public ValueTask Peek(CancellationToken token) => _UsedSlots.PeekDecrement(token);

		/// <summary>
		/// Peeks at the collection to see if there's an item to take without waiting
		/// </summary>
		/// <returns>True if there's an item to take, otherwise False</returns>
		public bool TryPeek() => _UsedSlots.TryPeekDecrement();

		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <exception cref="InvalidOperationException">The collection was completed and emptied while we were waiting for an item</exception>
		public ValueTask<TItem> Take(TimeSpan timeout)
		{
			if (IsCompleted)
				throw new InvalidOperationException("Collection is empty and adding has been completed");

			// Is there an item to take?
			var TakeItem = _UsedSlots.Decrement(timeout);

			if (TakeItem.IsCompleted)
				return new ValueTask<TItem>(CompleteTake());

			// No, wait for one to arrive
			var Instance = CollectionTakeInstance.GetOrCreateFor(this, TakeItem);

			return new ValueTask<TItem>(Instance, Instance.Version);
		}

		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <exception cref="InvalidOperationException">The collection was completed and emptied while we were waiting for an item</exception>
		public ValueTask<TItem> Take(CancellationToken token = default)
		{
			if (IsCompleted)
				throw new InvalidOperationException("Collection is empty and adding has been completed");

			// Is there an item to take?
			var TakeItem = _UsedSlots.Decrement(token);

			if (TakeItem.IsCompleted)
				return new ValueTask<TItem>(CompleteTake());

			// No, wait for one to arrive
			var Instance = CollectionTakeInstance.GetOrCreateFor(this, TakeItem);

			return new ValueTask<TItem>(Instance, Instance.Version);
		}

		/// <summary>
		/// Attempts to add an item to the collection without waiting
		/// </summary>
		/// <param name="item">The item to try and add</param>
		/// <returns>True if the item was added without waiting, otherwise False</returns>
		/// <remarks>Also returns False if the collection has completed adding</remarks>
		public bool TryAdd(TItem item)
		{
			if (_IsCompleted)
				return false;

			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				if (!_FreeSlots.TryDecrement())
					return false;
			}

			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
			{
				// Collection rejected our item. We now have promised to add an item that we can't add.
				_IsFailed = true;
				return false;
			}

			// Increment the slots counter, releasing any Takers
			return _UsedSlots.TryIncrement();
		}

		/// <summary>
		/// Attempts to take an item from the collection without waiting
		/// </summary>
		/// <param name="item">The item that was removed from the collection</param>
		/// <returns>True if an item was removed without waiting, otherwise False</returns>
		public bool TryTake(out TItem item) => TryTake(out item, TimeSpan.Zero);

		/// <summary>
		/// Attempts to take an item from the collection without waiting
		/// </summary>
		/// <param name="item">The item that was removed from the collection</param>
		/// <param name="timeout">The amount of time to block before returning False</param>
		/// <returns>True if an item was removed without waiting, otherwise False</returns>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out TItem item, TimeSpan timeout)
		{
			// Is there an item to take?
			if (!_UsedSlots.TryDecrement(timeout))
			{
				item = default!;
				return false;
			}

			item = CompleteTake();

			return true;
		}

		/// <summary>
		/// Completes the collection
		/// </summary>
		/// <remarks>
		/// <para>Any pending <see cref="O:Add" /> calls will complete successfully. Any future <see cref="O:Add" /> calls will throw <see cref="InvalidOperationException"/>.</para>
		/// <para>Any pending or future <see cref="O:Take" /> calls will return the contents of the collection until it is empty, before also throwing <see cref="InvalidOperationException"/>.</para>
		/// <para>If the collection is empty at this point, pending <see cref="O:Take" /> calls will immediately throw <see cref="InvalidOperationException"/>.</para>
		/// <para>If a consumer is currently waiting on a task retrieved via <see cref="O:GetConsumingEnumerable" /> and the collection is empty, the task will throw <see cref="InvalidOperationException"/>.</para>
		/// </remarks>
		public void CompleteAdding()
		{
			// Any pending Adds may throw, any future adds -will- throw
			_IsCompleted = true;

			// Cancel any adds that are waiting for a free slot
			if (_FreeSlots != null)
				_FreeSlots.Dispose();

			// If the collection is empty, cancel anyone waiting for items
			_UsedSlots.DisposeIfZero();
		}

		/// <summary>
		/// Copies a snapshot of the collection to an array
		/// </summary>
		/// <returns>The current contents of the collection</returns>
		public TItem[] ToArray() => _Collection.ToArray();

		/// <summary>
		/// Creates an enumerable that can be used to consume items as they are added to this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort consuming</param>
		/// <returns>An async enumerable that waits for items to be added</returns>
		/// <remarks>
		/// <para>If the provider calls <see cref="CompleteAdding" /> while wating, the returned enumerable will complete.</para>
		/// </remarks>
		public async IAsyncEnumerable<TItem> GetConsumingAsyncEnumerable([EnumeratorCancellation] CancellationToken token = default)
		{
			while (!IsCompleted)
			{
				try
				{
					// Is there an item to take?
					await _UsedSlots.Decrement(token);
				}
				catch (ObjectDisposedException) // Adding has completed while we were requesting
				{
					break;
				}

				// Try and remove an item from the collection
				if (!GetNextItem(out var MyItem))
					throw new InvalidOperationException("Item was not returned by the underlying collection");

				// Is there a maximum size?
				if (_FreeSlots != null)
					// We've removed an item, so release any Adders
					// Use TryIncrement, so we ignore if we're disposed and there are no more adders
					_FreeSlots.TryIncrement();

				yield return MyItem;
			}

			// Cancel anyone waiting for items
			_UsedSlots.Dispose();
		}

		internal bool TryPeekTake(CancellationToken token, out AsyncCounterDecrement decrement) => _UsedSlots.TryPeekDecrement(Timeout.InfiniteTimeSpan, token, out decrement);

		internal bool TryReserveTake() => _UsedSlots.TryDecrement();

		internal void ReleaseTake() => _UsedSlots.ForceIncrement();

		internal TItem CompleteTake()
		{
			// Remove the item reserved by TryReserveTake from the collection
			if (!GetNextItem(out var Item))
				throw new InvalidOperationException("Collection failed to retrieve from the underlying collection. AsyncCollection is corrupted");

			// Is there a maximum size?
			if (_FreeSlots != null)
				// We've removed an item, so release any Adders
				// Use TryIncrement, so we ignore if we're disposed and there are no more adders
				_FreeSlots.TryIncrement();

			// If the collection is now empty, cancel anyone waiting for items
			if (IsCompleted)
				_UsedSlots.DisposeIfZero();

			return Item;
		}

		//****************************************

		private ValueTask InternalAdd(TItem item, bool complete, TimeSpan timeout)
		{
			if (_IsCompleted)
				return Task.FromException(new InvalidOperationException("Adding has been completed")).AsValueTask();

			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				var TakeSlot = _FreeSlots.Decrement(timeout);

				if (!TakeSlot.IsCompleted)
				{
					// No slot, wait for it
					var Instance = CollectionAddInstance.GetOrCreateFor(this, item, TakeSlot, complete);

					return new ValueTask(Instance, Instance.Version);
				}
			}

			if (complete)
				// Flag as completed first, so any Takers will handle the cleanup
				_IsCompleted = true;

			// Increment the slots counter, releasing a Taker. They'll wait until the item becomes available
			if (!_UsedSlots.TryIncrement())
				return Task.FromException(new InvalidOperationException("Adding has been completed")).AsValueTask();

			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
			{
				// Collection rejected our item. We now have promised to add an item that we can't add.
				_IsFailed = true;
				return Task.FromException(new InvalidOperationException("Item was rejected by the underlying collection")).AsValueTask();
			}

			return default;
		}

		private ValueTask InternalAdd(TItem item, bool complete, CancellationToken token)
		{
			if (_IsCompleted)
				return Task.FromException(new InvalidOperationException("Adding has been completed")).AsValueTask();

			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				var TakeSlot = _FreeSlots.Decrement(token);

				if (!TakeSlot.IsCompleted)
				{
					// No slot, wait for it
					var Instance = CollectionAddInstance.GetOrCreateFor(this, item, TakeSlot, complete);

					return new ValueTask(Instance, Instance.Version);
				}
			}

			if (complete)
				// Flag as completed first, so any Takers will handle the cleanup
				_IsCompleted = true;

			// Increment the slots counter, releasing a Taker. They'll wait until the item becomes available
			if (!_UsedSlots.TryIncrement())
				return Task.FromException(new InvalidOperationException("Adding has been completed")).AsValueTask();

			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
			{
				// Collection rejected our item. We now have promised to add an item that we can't add.
				_IsFailed = true;
				return Task.FromException(new InvalidOperationException("Item was rejected by the underlying collection")).AsValueTask();
			}

			return default;
		}

		private void InternalAdd(TItem item, bool complete)
		{
			if (complete)
				// Flag as completed first, so any Takers will handle the cleanup
				_IsCompleted = true;

			// Increment the slots counter, releasing a Taker. They'll wait until the item becomes available
			if (!_UsedSlots.TryIncrement())
				throw new InvalidOperationException("Adding has been completed");

			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
			{
				// Collection rejected our item. We now have promised to add an item that we can't add.
				_IsFailed = true;
				throw new InvalidOperationException("Item was rejected by the underlying collection");
			}
		}

		private bool GetNextItem(out TItem item)
		{
			if (!_Collection.TryTake(out item))
			{
				// We may need to wait a short moment for the item to become available
				var SpinWait = new SpinWait();

				do
				{
					if (_IsFailed)
						return false;

					SpinWait.SpinOnce();
				}
				while (!_Collection.TryTake(out item));
			}

			return true;
		}

		//****************************************

		IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => _Collection.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _Collection.GetEnumerator();

		//****************************************

		/// <summary>
		/// Gets the maximum number of items in the collection
		/// </summary>
		public int Capacity { get; }

		/// <summary>
		/// Gets the approximate number of items in the collection
		/// </summary>
		public int Count => _Collection.Count;

		/// <summary>
		/// Gets the number of operations waiting to take from the collection
		/// </summary>
		public int WaitingToTake => _UsedSlots.WaitingCount;

		/// <summary>
		/// Gets the number of operations waiting to add to the collection
		/// </summary>
		public int WaitingToAdd => _FreeSlots != null ? _FreeSlots.WaitingCount : 0;

		/// <summary>
		/// Gets whether adding has been completed
		/// </summary>
		public bool IsAddingCompleted => _IsCompleted;

		/// <summary>
		/// Gets whether adding has been completed and the collection is empty
		/// </summary>
		public bool IsCompleted => _IsCompleted && _UsedSlots.CurrentCount == 0;
	}
}
