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
using Proximity.Threading;
//****************************************

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Provides a BlockingCollection that supports async/await
	/// </summary>
	/// <remarks>Producers should not complete the underlying IProducerConsumerCollection, or add a limit to it</remarks>
	public sealed partial class AsyncCollection<T> : IEnumerable<T>, IReadOnlyCollection<T>
	{ //****************************************
		private readonly IProducerConsumerCollection<T> _Collection;

		private readonly AsyncCounter? _FreeSlots;
		private readonly AsyncCounter _UsedSlots;
		private bool _IsFailed;
		//****************************************

		/// <summary>
		/// Creates a new unlimited collection over a concurrent queue
		/// </summary>
		public AsyncCollection() : this(new ConcurrentQueue<T>())
		{
		}

		/// <summary>
		/// Creates a new unlimited collection over the given producer consumer collection
		/// </summary>
		/// <param name="collection">The collection to block over</param>
		public AsyncCollection(IProducerConsumerCollection<T> collection)
		{
			_Collection = collection ?? throw new ArgumentNullException(nameof(collection));
			Capacity = int.MaxValue;

			_UsedSlots = new AsyncCounter(collection.Count);
		}

		/// <summary>
		/// Creates a new collection over a concurrent queue
		/// </summary>
		/// <param name="capacity">The maximum number of items in the queue</param>
		public AsyncCollection(int capacity) : this(new ConcurrentQueue<T>(), capacity)
		{
		}

		/// <summary>
		/// Creates a new collection over the given producer consumer collection
		/// </summary>
		/// <param name="collection">The collection to block over</param>
		/// <param name="capacity">The maximum number of items in the queue</param>
		public AsyncCollection(IProducerConsumerCollection<T> collection, int capacity)
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
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task returning True if the item was added, False if the Collection was completed</returns>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<bool> Add(T item, TimeSpan timeout, CancellationToken token = default) => InternalAdd(item, false, timeout, token);

		/// <summary>
		/// Attempts to add an Item to the collection
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task returning True if the item was added, False if the Collection was completed</returns>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		public ValueTask<bool> Add(T item, CancellationToken token = default) => InternalAdd(item, false, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to add a range of Items to the collection
		/// </summary>
		/// <param name="items">The range of items to add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task returning True if the item was added, False if the Collection was completed</returns>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		/// <remarks>
		/// <para>Items will be added sequentially, however they may be interleaved with other concurrent adders</para>
		/// <para>When using a maximum capacity, cancelling this operation may still result in some of the items being added</para>
		/// </remarks>
		public ValueTask<bool> AddMany(IReadOnlyCollection<T> items, CancellationToken token = default) => InternalAddMany(items, false, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to add a range of Items to the collection
		/// </summary>
		/// <param name="items">The range of items to add</param>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task returning True if the item was added, False if the Collection was completed</returns>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		/// <remarks>
		/// <para>Items will be added sequentially, however they may be interleaved with other concurrent adders</para>
		/// <para>When using a maximum capacity, cancelling this operation may still result in some of the items being added</para>
		/// </remarks>
		public ValueTask<bool> AddMany(IReadOnlyCollection<T> items, TimeSpan timeout, CancellationToken token = default) => InternalAddMany(items, false, timeout, token);

		/// <summary>
		/// Attempts to add an Item to the collection and completes it once added
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task returning True if the item was added, False if the Collection was already completed</returns>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<bool> AddComplete(T item, TimeSpan timeout, CancellationToken token = default) => InternalAdd(item, true, timeout, token);

		/// <summary>
		/// Attempts to add an Item to the collection and completes it once added
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task returning True if the item was added, False if the Collection was already completed</returns>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		public ValueTask<bool> AddComplete(T item, CancellationToken token = default) => InternalAdd(item, true, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Completes the collection
		/// </summary>
		/// <returns>A Task that completes when the collection is empty</returns>
		/// <remarks>
		/// <para>This is safe to call multiple times</para>
		/// <para>Any pending <see cref="O:Add" /> calls may complete successfully. Any future <see cref="O:Add" /> calls will return False</para>
		/// <para>Any pending or future <see cref="O:Take" /> calls will return the contents of the collection until it is empty, before throwing <see cref="InvalidOperationException"/>.</para>
		/// <para>If the collection is empty at this point, pending <see cref="O:Take" /> calls will immediately throw <see cref="InvalidOperationException"/>.</para>
		/// <para>If a consumer is currently waiting on a task retrieved via <see cref="O:GetConsumingEnumerable" /> and the collection is empty, the task will throw <see cref="InvalidOperationException"/>.</para>
		/// </remarks>
		public ValueTask CompleteAdding()
		{
			// New Add operations will now fail. Take operations will fail once the collection is empty
			var Used = _UsedSlots.DisposeAsync();

			if (_FreeSlots != null)
				// Pending Add operations will now fail.
				_FreeSlots.DisposeAsync();

			return Used;
		}

		/// <summary>
		/// Copies the current contents of the collection
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="index">The index at which copying begins</param>
		public void CopyTo(T[] array, int index) => _Collection.CopyTo(array, index);

		/// <summary>
		/// Waits until it's possible to remove an item from this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort the wait operation</param>
		/// <returns>A task that returns True if an item is available, False if the collection is completed</returns>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		public ValueTask<bool> Peek(CancellationToken token = default) => _UsedSlots.PeekDecrement(token);

		/// <summary>
		/// Waits until it's possible to remove an item from this collection
		/// </summary>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <param name="token">A cancellation token to abort the wait operation</param>
		/// <returns>A task that returns True if an item is available, False if the collection is completed</returns>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		/// <exception cref="OperationCanceledException">The operation was cancelled</exception>
		public ValueTask<bool> Peek(TimeSpan timeout, CancellationToken token = default) => _UsedSlots.PeekDecrement(timeout, token);

		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <exception cref="InvalidOperationException">The collection was completed and emptied while we were waiting for an item</exception>
		public ValueTask<T> Take(CancellationToken token = default) => Take(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <exception cref="InvalidOperationException">The collection was completed and emptied while we were waiting for an item</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<T> Take(TimeSpan timeout, CancellationToken token = default)
		{
			// Is there an item to take?
			var TakeItem = _UsedSlots.TryDecrementAsync(timeout, token);

			if (TakeItem.IsCompleted)
			{
				if (TakeItem.Result)
					return new ValueTask<T>(CompleteTake());

				throw new InvalidOperationException("Collection is empty and adding has been completed");
			}

			// No, wait for one to arrive
			var Instance = CollectionTakeInstance.GetOrCreateFor(this, TakeItem);

			return new ValueTask<T>(Instance, Instance.Version);
		}

		/// <summary>
		/// Attempts to add an item to the collection without waiting
		/// </summary>
		/// <param name="item">The item to try and add</param>
		/// <returns>True if the item was added without waiting, False if we would have to wait, or if the collection is completed</returns>
		public bool TryAdd(T item)
		{
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				if (!_FreeSlots.TryDecrement())
					return false; // No free items, or we're disposed
			}

			return CompleteAdd(item, false);
		}

		/// <summary>
		/// Blocks while adding an item to the collection
		/// </summary>
		/// <param name="item">The item to try and add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>True if the item was added, False if the collection is completed</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for a free slot</exception>
		public bool TryAdd(T item, CancellationToken token) => TryAdd(item, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks while adding an item to the collection
		/// </summary>
		/// <param name="item">The item to try and add</param>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>True if the item was added, False if the collection is completed</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for a free slot</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryAdd(T item, TimeSpan timeout, CancellationToken token = default)
		{
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				if (!_FreeSlots.TryDecrement(timeout, token))
					return false; // No free items, or we're disposed
			}

			return CompleteAdd(item, false);
		}

		/// <summary>
		/// Attempts to add an item to the collection and complete the collection without waiting
		/// </summary>
		/// <param name="item">The item to try and add</param>
		/// <returns>True if the item was added without waiting, False if we would have to wait, or if the collection is already completed</returns>
		public bool TryAddComplete(T item)
		{
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				if (!_FreeSlots.TryDecrement())
					return false; // No free items, or we're disposed
			}

			return CompleteAdd(item, true);
		}

		/// <summary>
		/// Blocks until we can add an item to the collection and complete the collection
		/// </summary>
		/// <param name="item">The item to try and add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>True if the item was added, False if the collection is already completed</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for a free slot</exception>
		public bool TryAddComplete(T item, CancellationToken token) => TryAddComplete(item, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks until we can add an item to the collection and complete the collection
		/// </summary>
		/// <param name="item">The item to try and add</param>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>True if the item was added, False if the collection is already completed</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for a free slot</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryAddComplete(T item, TimeSpan timeout, CancellationToken token = default)
		{
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				if (!_FreeSlots.TryDecrement(timeout, token))
					return false; // No free items, or we're disposed
			}

			return CompleteAdd(item, true);
		}

		/// <summary>
		/// Peeks at the collection to see if there's an item to take without waiting
		/// </summary>
		/// <returns>True if there's an item to take, otherwise False</returns>
		public bool TryPeek() => _UsedSlots.TryPeekDecrement();

		/// <summary>
		/// Peeks at the collection to see if there's an item to take without waiting
		/// </summary>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>True if there's an item to take, otherwise False</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for a free slot</exception>
		public bool TryPeek(CancellationToken token) => _UsedSlots.TryPeekDecrement(token);

		/// <summary>
		/// Peeks at the collection to see if there's an item to take without waiting
		/// </summary>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>True if there's an item to take, otherwise False</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for a free slot</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryPeek(TimeSpan timeout, CancellationToken token = default) => _UsedSlots.TryPeekDecrement(timeout, token);

		/// <summary>
		/// Attempts to take an item from the collection without waiting
		/// </summary>
		/// <param name="item">The item that was removed from the collection</param>
		/// <returns>True if an item was removed without waiting, False if we would have to wait, or if the collection is completed and empty</returns>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out T item
			)
		{
			// Is there an item to take?
			if (!_UsedSlots.TryDecrement())
			{
				item = default!;
				return false;
			}

			item = CompleteTake();

			return true;
		}

		/// <summary>
		/// Blocks while taking an item from the collection
		/// </summary>
		/// <param name="item">The item that was removed from the collection</param>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>True if an item was removed, False if the collection is completed and empty</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out T item, CancellationToken token
			) => TryTake(out item!, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks while taking an item from the collection
		/// </summary>
		/// <param name="item">The item that was removed from the collection</param>
		/// <param name="timeout">The amount of time to block before returning False</param>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>True if an item was removed, False if the collection is completed and empty</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out T item, TimeSpan timeout, CancellationToken token = default)
		{
			// Is there an item to take?
			if (!_UsedSlots.TryDecrement(timeout, token))
			{
				item = default!;
				return false;
			}

			item = CompleteTake();

			return true;
		}

		/// <summary>
		/// Copies a snapshot of the collection to an array
		/// </summary>
		/// <returns>The current contents of the collection</returns>
		public T[] ToArray() => _Collection.ToArray();

		/// <summary>
		/// Creates a blocking enumerable that can be used to consume items as they are added to this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort consuming</param>
		/// <returns>An enumerable returning items that have been added</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <remarks>
		/// <para>If the provider calls <see cref="CompleteAdding" />, <see cref="O:AddComplete"/>, or <see cref="O:TryAddComplete"/> while waiting, the enumeration will complete once the collection is empty.</para>
		/// </remarks>
		public IEnumerable<T> GetConsumingEnumerable(CancellationToken token = default)
		{
			while (!IsCompleted)
			{
				token.ThrowIfCancellationRequested();

				// Is there an item to take?
				if (!_UsedSlots.TryDecrement(token))
					yield break; // We're completed and empty

				yield return CompleteTake();
			}
		}

		/// <summary>
		/// Creates an enumerable that can be used to consume items as they are added to this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort consuming</param>
		/// <returns>An enumerable returning tasks that asynchronously wait for items to be added</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <remarks>
		/// <para>If the provider calls <see cref="CompleteAdding" />, <see cref="O:AddComplete"/>, or <see cref="O:TryAddComplete"/> while waiting, the returned task will throw <see cref="InvalidOperationException"/>.</para>
		/// <para>If there are still items in the collection at that point, the enumeration will complete successfully.</para>
		/// </remarks>
		public IEnumerable<ValueTask<T>> GetConsumingEnumerableAsync(CancellationToken token = default)
		{
			while (!IsCompleted)
			{
				token.ThrowIfCancellationRequested();

				// Is there an item to take?
				if (_UsedSlots.TryDecrement())
				{
					yield return new ValueTask<T>(CompleteTake());

					continue;
				}

				// Is there an item to take?
				var MyTask = _UsedSlots.TryDecrementAsync(token);

				if (!MyTask.IsCompleted)
					yield return TakeAsync(MyTask); // Wait for the operation to finish
				else if (MyTask.Result)
					yield return new ValueTask<T>(CompleteTake()); // We decremented an item
				else
					yield break; // We disposed
			}

			async ValueTask<T> TakeAsync(ValueTask<bool> decrementTask)
			{
				if (await decrementTask)
					return CompleteTake();
				else // Adding has completed while we were requesting
					throw new InvalidOperationException("Collection is empty and adding has been completed");
			}
		}

		/// <summary>
		/// Creates an enumerable that can be used to consume items as they are added to this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort consuming</param>
		/// <returns>An async enumerable that waits for items to be added</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <remarks>
		/// <para>If the provider calls <see cref="CompleteAdding" />, <see cref="O:AddComplete"/>, or <see cref="O:TryAddComplete"/>, the returned async enumerable will complete.</para>
		/// </remarks>
		public async IAsyncEnumerable<T> GetConsumingAsyncEnumerable([EnumeratorCancellation] CancellationToken token = default)
		{
			while (!IsCompleted)
			{
				token.ThrowIfCancellationRequested();

				// Is there an item to take?
				if (await _UsedSlots.TryDecrementAsync(token))
					yield return CompleteTake();
				else // Adding has completed while we were requesting
					yield break;
			}
		}

		//****************************************

		internal bool TryPeekTake(CancellationToken token, out AsyncCounterDecrement decrement) => _UsedSlots.TryPeekDecrement(AsyncCounterFlags.None, Timeout.InfiniteTimeSpan, token, out decrement);

		internal bool TryReserveTake() => _UsedSlots.TryDecrement();

		internal void ReleaseTake() => _UsedSlots.TryIncrement();

		internal T CompleteTake()
		{
			// Remove the item reserved by TryReserveTake from the collection
			if (!GetNextItem(out var Item))
				throw new InvalidOperationException("Collection failed to retrieve from the underlying collection. AsyncCollection is corrupted");

			// Is there a maximum size?
			if (_FreeSlots != null)
				// We've removed an item, so release any Adders
				// Use TryIncrement, so we ignore if we're disposed and there are no more adders
				_FreeSlots.TryIncrement();

			return Item;
		}

		//****************************************

		private ValueTask<bool> InternalAdd(T item, bool complete, TimeSpan timeout, CancellationToken token = default)
		{
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				var TakeSlot = _FreeSlots.TryDecrementAsync(timeout, token);

				if (!TakeSlot.IsCompleted)
				{
					// No slot, wait for it
					var Instance = CollectionAddInstance.GetOrCreateFor(this, item, TakeSlot, complete);

					return new ValueTask<bool>(Instance, Instance.Version);
				}

				if (!TakeSlot.Result)
					return new ValueTask<bool>(false); // Disposed
			}

			return new ValueTask<bool>(CompleteAdd(item, complete));
		}

		private ValueTask<bool> InternalAddMany(IReadOnlyCollection<T> items, bool complete, TimeSpan timeout, CancellationToken token = default)
		{
			if (items.Count == 0)
				return new ValueTask<bool>(true);

			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find enough free slots
				if (!_FreeSlots.TryDecrement(items.Count, out var Count) || Count < items.Count)
				{
					// Insufficient slots, wait for it
					var Instance = CollectionAddManyInstance.GetOrCreateFor(this, items, Count, complete, timeout, token);

					return new ValueTask<bool>(Instance, Instance.Version);
				}
			}

			using var Enumerator = items.GetEnumerator();

			return new ValueTask<bool>(CompleteAdd(Enumerator, items.Count, complete));
		}

		private bool CompleteAdd(T item, bool complete)
		{
			// Increment the slots counter, releasing a Taker. GetNextItem may wait until the item becomes available
			if (!_UsedSlots.TryIncrement())
				return false;

			if (complete)
				CompleteAdding();

			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
			{
				// Collection rejected our item. We now have promised to add an item that we can't add.
				_IsFailed = true;
				throw new InvalidOperationException("Item was rejected by the underlying collection");
			}

			return true;
		}

		private bool CompleteAdd(IEnumerator<T> items, int total, bool complete)
		{
			// Increment the slots counter, releasing one or more Takers. GetNextItem may wait until the items become available
			if (!_UsedSlots.TryAdd(total))
				return false;

			if (complete)
				CompleteAdding();

			// Try and add our item to the collection
			do
			{
				if (!items.MoveNext())
				{
					// Collection couldn't supply our item. We now have promised to add an item that we can't add.
					_IsFailed = true;
					throw new InvalidOperationException("Item was not available from the source collection");
				}

				if (!_Collection.TryAdd(items.Current))
				{
					// Collection rejected our item. We now have promised to add an item that we can't add.
					_IsFailed = true;
					throw new InvalidOperationException("Item was rejected by the underlying collection");
				}
			}
			while (--total > 0);

			return true;
		}

		private bool GetNextItem(out T item)
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

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => _Collection.GetEnumerator();

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
		public bool IsAddingCompleted => _UsedSlots.IsDisposed;

		/// <summary>
		/// Gets whether adding has been completed and the collection is empty
		/// </summary>
		public bool IsCompleted => _UsedSlots.IsDisposed && _UsedSlots.CurrentCount == 0;
	}
}
