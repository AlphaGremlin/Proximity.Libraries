﻿/****************************************\
 AsyncCollection.cs
 Created: 2014-02-20
\****************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides a BlockingCollection that supports async/await
	/// </summary>
	/// <remarks>Producers should not complete the underlying IProducerConsumerCollection, or add a limit to it</remarks>
	public class AsyncCollection<TItem> : IEnumerable<TItem>, IReadOnlyCollection<TItem>
	{	//****************************************
		private readonly IProducerConsumerCollection<TItem> _Collection;
		
		private readonly AsyncCounter _FreeSlots, _UsedSlots;
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
		/// <returns>A task representing the addition operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for free space</exception>
		public Task Add(TItem item) => Add(item, CancellationToken.None);

		/// <summary>
		/// Attempts to add an Item to the collection
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="timeout">The amount of time to wait if the collection is at full capacity</param>
		/// <returns>A task representing the addition operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for free space</exception>
		public Task Add(TItem item, TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource();
			var MyTask = Add(item, MySource.Token);
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
		/// Attempts to add an Item to the collection
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task representing the addition operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for free space</exception>
		public Task Add(TItem item, CancellationToken token)
		{
			if (_IsCompleted)
				return Task.FromException(new InvalidOperationException("Adding has been completed"));

			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				var MyTask = _FreeSlots.Decrement(token);

				if (!MyTask.IsCompleted)
				{
					// No slot, wait for it
					// Do not pass the cancellation token, since if the counter is disposed when token cancels,
					// an ObjectDisposedException could be thrown but never be observed and cause an Unobserved Task Exception
					return MyTask.ContinueWith(InternalAdd, item, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
				}
			}

			// Increment the slots counter, releasing a Taker. They'll wait until the item becomes available
			if (!_UsedSlots.TryIncrement())
				return Task.FromException(new InvalidOperationException("Adding has been completed"));

			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
			{
				// Collection rejected our item. We now have promised to add an item that we can't add.
				_IsFailed = true;
				return Task.FromException(new InvalidOperationException("Item was rejected by the underlying collection"));
			}
			
			return Task.CompletedTask;
		}

		/// <summary>
		/// Attempts to add and complete a collection in one operation
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <returns>A task representing the addition and completion operation</returns>
		/// <remarks>Ensures that in a one-to-one situation, the subscriber enumeration completes without throwing a cancellation</remarks>
		public Task AddComplete(TItem item) => AddComplete(item, CancellationToken.None);

		/// <summary>
		/// Attempts to add and complete a collection in one operation
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="token">A cancellation token to abort the addition operation</param>
		/// <returns>A task representing the addition and completion operation</returns>
		/// <remarks>Ensures that in a one-to-one situation, the subscriber enumeration completes without throwing a cancellation</remarks>
		public Task AddComplete(TItem item, CancellationToken token)
		{
			if (_IsCompleted)
				return Task.FromException(new InvalidOperationException("Adding has already been completed"));
			
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				var MyTask = _FreeSlots.Decrement(token);

				if (!MyTask.IsCompleted)
				{
					// No slot, wait for it
					// Do not pass the cancellation token, since if the counter is disposed when token cancels,
					// an ObjectDisposedException could be thrown but never be observed and cause an Unobserved Task Exception
					return MyTask.ContinueWith(InternalAddComplete, item, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
				}
				
				// Free slot, now cancel anyone else trying to add
				_FreeSlots.Dispose();
			}

			// Flag as completed first, so any Takers will handle the cleanup
			_IsCompleted = true;

			// Increment the slots counter, releasing a Taker. They'll wait until the item becomes available
			if (!_UsedSlots.TryIncrement())
				return Task.FromException(new InvalidOperationException("Adding has been completed"));

			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
			{
				// Collection rejected our item. We now have promised to add an item that we can't add.
				_IsFailed = true;
				return Task.FromException(new InvalidOperationException("Item was rejected by the underlying collection. AsyncCollection is corrupted"));
			}
			
			return Task.CompletedTask;
		}

		/// <summary>
		/// Waits until it's possible to remove an item from this collection
		/// </summary>
		/// <returns>A task that completes when data is available in the collection</returns>
		public Task Peek() => _UsedSlots.PeekDecrement();

		/// <summary>
		/// Waits until it's possible to remove an item from this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort the wait operation</param>
		/// <returns>A task that completes when data is available in the collection</returns>
		public Task Peek(CancellationToken token) => _UsedSlots.PeekDecrement(token);

		/// <summary>
		/// Peeks at the collection to see if there's an item to take without waiting
		/// </summary>
		/// <returns>True if there's an item to take, otherwise False</returns>
		public bool TryPeek() => _UsedSlots.TryPeekDecrement();

		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="InvalidOperationException">The collection was completed and emptied while we were waiting for an item</exception>
		public Task<TItem> Take() => Take(CancellationToken.None);

		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <exception cref="InvalidOperationException">The collection was completed and emptied while we were waiting for an item</exception>
		public Task<TItem> Take(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource();
			var MyTask = Take(MySource.Token);
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
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		/// <exception cref="InvalidOperationException">The collection was completed and emptied while we were waiting for an item</exception>
		public Task<TItem> Take(CancellationToken token)
		{
			if (IsCompleted)
				return Task.FromException<TItem>(new InvalidOperationException("Collection is empty and adding has been completed"));

			// Is there an item to take?
			var MyTask = _UsedSlots.Decrement(token);

			if (!MyTask.IsCompleted)
			{
				// No, wait for it to arrive
				// Do not pass the cancellation token, since if the counter is disposed when token cancels,
				// an ObjectDisposedException could be thrown but never be observed and cause an Unobserved Task Exception
				return MyTask.ContinueWith(InternalTake, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
			}

			// Try and remove an item from the collection
			if (!GetNextItem(out var MyItem))
				return Task.FromException<TItem>(new InvalidOperationException("Collection failed to add to the underlying collection. AsyncCollection is corrupted"));
			
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// We've removed an item, so release any Adders
				// Use TryIncrement, so we ignore if we're disposed and there are no more adders
				_FreeSlots.TryIncrement();
			}
			
			// If the collection is empty, cancel anyone waiting for items
			if (IsCompleted)
				_UsedSlots.Dispose();
			
			return Task.FromResult(MyItem);
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
		public bool TryTake(out TItem item)
		{
			return TryTake(out item, TimeSpan.Zero);
		}
		
		/// <summary>
		/// Attempts to take an item from the collection without waiting
		/// </summary>
		/// <param name="item">The item that was removed from the collection</param>
		/// <param name="timeout">The amount of time to block before returning False</param>
		/// <returns>True if an item was removed without waiting, otherwise False</returns>
		public bool TryTake(out TItem item, TimeSpan timeout)
		{
			// Is there an item to take?
			if (!_UsedSlots.TryDecrement(timeout))
			{
				item = default;
				return false;
			}

			// Yes, remove it from the collection
			if (!GetNextItem(out item))
				throw new InvalidOperationException("Collection failed to add to the underlying collection. AsyncCollection is corrupted");

			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// We've removed an item, so release any Adders
				// Use TryIncrement, so we ignore if we're disposed and there are no more adders
				_FreeSlots.TryIncrement();
			}
			
			// If the collection is now empty, cancel anyone waiting for items
			if (IsCompleted)
				_UsedSlots.Dispose();
			
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
			if (_FreeSlots!= null)
				_FreeSlots.Dispose();
			
			// If the collection is empty, cancel anyone waiting for items
			_UsedSlots.DisposeIfZero();
		}

		/// <summary>
		/// Copies a snapshot of the collection to an array
		/// </summary>
		/// <returns>The current contents of the collection</returns>
		public TItem[] ToArray()
		{
			return _Collection.ToArray();
		}

		/// <summary>
		/// Creates an enumerable that can be used to consume items as they are added to this collection
		/// </summary>
		/// <returns>An enumerable returning tasks that asynchronously wait for items to be added</returns>
		/// <remarks>
		/// <para>If the provider calls <see cref="CompleteAdding" /> while wating, the returned task will throw <see cref="InvalidOperationException"/>.</para>
		/// <para>If there are still items in the collection at that point, the enumeration will complete successfully.</para>
		/// </remarks>
		public IEnumerable<Task<TItem>> GetConsumingEnumerable()
		{
			return GetConsumingEnumerable(CancellationToken.None);
		}
		
		/// <summary>
		/// Creates an enumerable that can be used to consume items as they are added to this collection
		/// </summary>
		/// <param name="token">A cancellation token to abort consuming</param>
		/// <returns>An enumerable returning tasks that asynchronously wait for items to be added</returns>
		/// <remarks>
		/// <para>If the provider calls <see cref="CompleteAdding" /> while wating, the returned task will throw <see cref="InvalidOperationException"/>.</para>
		/// <para>If there are still items in the collection at that point, the enumeration will complete successfully.</para>
		/// </remarks>
		public IEnumerable<Task<TItem>> GetConsumingEnumerable(CancellationToken token)
		{	//****************************************
			Task MyTask;
			//****************************************
			
			while (!IsCompleted)
			{
				try
				{
					// Is there an item to take?
					MyTask = _UsedSlots.Decrement(token);
				}
				catch (ObjectDisposedException) // Adding has completed while we were requesting
				{
					yield break;
				}
				catch (OperationCanceledException) // Cancellation token was raised
				{
					yield break;
				}
				
				if (!MyTask.IsCompleted)
				{
					// No, wait for it to arrive
					// Do not pass the cancellation token, since if the counter is disposed when token cancels,
					// an ObjectDisposedException could be thrown but never be observed and cause an Unobserved Task Exception
					yield return MyTask.ContinueWith(InternalTake, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
					
					continue;
				}

				// Try and remove an item from the collection
				if (!GetNextItem(out var MyItem))
				{
					yield return Task.FromException<TItem>(new InvalidOperationException("Item was not returned by the underlying collection"));

					yield break;
				}

				// Is there a maximum size?
				if (_FreeSlots != null)
				{
					// We've removed an item, so release any Adders
					// Use TryIncrement, so we ignore if we're disposed and there are no more adders
					_FreeSlots.TryIncrement();
				}

				// If the collection is empty, cancel anyone waiting for items
				if (IsCompleted)
					_UsedSlots.Dispose();

				yield return Task.FromResult(MyItem);

				// Remember to clear the reference once we're back, so we don't hold onto it
				MyItem = default;
			}
		}
		
		//****************************************
		
		private void InternalAdd(Task task, object state)
		{	//****************************************
			var MyItem = (TItem)state;
			//****************************************
			
			if (task.IsFaulted)
			{
				if (task.Exception.InnerException is ObjectDisposedException)
					throw new InvalidOperationException("Adding was completed while waiting for a slot");
				
				throw new Exception("Failed to decrement free counter", task.Exception);
			}
			
			// Try and add our item to the collection
			if (!_Collection.TryAdd(MyItem))
				throw new InvalidOperationException("Item was rejected by the underlying collection");
			
			// Increment the slots counter
			_UsedSlots.Increment();
		}
		
		private void InternalAddComplete(Task task, object state)
		{	//****************************************
			var MyItem = (TItem)state;
			//****************************************
			
			if (task.IsFaulted)
			{
				if (task.Exception.InnerException is ObjectDisposedException)
					throw new InvalidOperationException("Adding was completed while waiting for a slot");

				throw new Exception("Failed to decrement free counter", task.Exception);
			}
			
			// Free slot, now cancel anyone else trying to add
			_FreeSlots.Dispose();
			
			// Try and add our item to the collection
			if (!_Collection.TryAdd(MyItem))
				throw new InvalidOperationException("Item was rejected by the underlying collection");
			
			// Flag as completed first, so any Takers will handle the cleanup
			_IsCompleted = true;
			
			// Increment the slots counter
			_UsedSlots.Increment();
		}
		
		private TItem InternalTake(Task task)
		{
			if (task.IsFaulted)
			{
				if (task.Exception.InnerException is ObjectDisposedException)
					throw new InvalidOperationException("Adding was completed and there are no more items in the collection");

				throw new Exception("Failed to decrement used counter", task.Exception.InnerException);
			}

			// Try and remove an item from the collection
			if (!GetNextItem(out var MyItem))
				throw new InvalidOperationException("Item was not returned by the underlying collection");
			
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// We've removed an item, so release any Adders
				// Use TryIncrement, so we ignore if we're disposed and there are no more adders
				_FreeSlots.TryIncrement();
			}
			
			// If the collection is now empty, cancel anyone waiting for items
			if (_IsCompleted)
				_UsedSlots.DisposeIfZero();
			
			return MyItem;
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

		private static void CleanupCancelSource(Task task, object state)
		{
			((CancellationTokenSource)state).Dispose();
		}
		
		//****************************************
		
		IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
		{
			return _Collection.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _Collection.GetEnumerator();
		}

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

		//****************************************

		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <returns>A task returning the result of the take operation</returns>
		public static Task<TakeResult> TakeFromAny(params AsyncCollection<TItem>[] collections)
		{
			return TakeFromAny(collections, CancellationToken.None);
		}
		
		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <returns>A task returning the result of the take operation</returns>
		public static Task<TakeResult> TakeFromAny(IEnumerable<AsyncCollection<TItem>> collections)
		{
			return TakeFromAny(collections, CancellationToken.None);
		}
		
		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The timeout elapsed while we were waiting for an item</exception>
		public static Task<TakeResult> TakeFromAny(IEnumerable<AsyncCollection<TItem>> collections, TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource();
			var MyTask = TakeFromAny(collections, MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}

			MySource.CancelAfter(timeout);
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((Action<Task<TakeResult>, object>)CleanupCancelSource, MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <param name="token">A cancellation token that can abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		public static Task<TakeResult> TakeFromAny(IEnumerable<AsyncCollection<TItem>> collections, CancellationToken token)
		{
			var MyResult = TryTakeFromAny(collections);
			
			if (MyResult.HasItem)
				return Task.FromResult(MyResult);
			
			var MyOperation = new AsyncCollectionTakeFromAny<TItem>(token);
			
			foreach (var MyCollection in collections)
			{
				MyOperation.Attach(MyCollection._UsedSlots, MyCollection);
			}
			
			return MyOperation.Task;
		}
		
		//public static Task<AsyncCollection<TItem>> PeekAtAny(IEnumerable<AsyncCollection<TItem>> collections, CancellationToken token)
		//{
		//	
		//}
		
		/// <summary>
		/// Peeks at a set of collections and finds the first with data available
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>The result of the operation</returns>
		public static AsyncCollection<TItem> TryPeekAtAny(params AsyncCollection<TItem>[] collections)
		{
			return TryPeekAtAny((IEnumerable<AsyncCollection<TItem>>)collections);
		}
		
		/// <summary>
		/// Peeks at a set of collections and finds the first with data available
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>The result of the operation</returns>
		public static AsyncCollection<TItem> TryPeekAtAny(IEnumerable<AsyncCollection<TItem>> collections)
		{
			foreach (var MyCollection in collections)
			{
				if (MyCollection.TryPeek())
					return MyCollection;
			}
			
			return null;
		}
		
		/// <summary>
		/// Attempts to take an Item from a set of collections without waiting
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>The result of the operation</returns>
		public static TakeResult TryTakeFromAny(params AsyncCollection<TItem>[] collections)
		{
			return TryTakeFromAny((IEnumerable<AsyncCollection<TItem>>)collections);
		}
		
		/// <summary>
		/// Attempts to take an Item from a set of collections without waiting
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>The result of the operation</returns>
		public static TakeResult TryTakeFromAny(IEnumerable<AsyncCollection<TItem>> collections)
		{
			foreach (var MyCollection in collections)
			{
				if (MyCollection.TryTake(out TItem MyResult))
					return new TakeResult(MyCollection, MyResult);
			}

			return default;
		}
		
		//****************************************
		
		/// <summary>
		/// Describes the result of a TakeFromAny operation
		/// </summary>
		public struct TakeResult
		{
			internal TakeResult(AsyncCollection<TItem> source, TItem item)
			{
				Source = source;
				Item = item;
			}

			//****************************************

			/// <summary>
			/// Gets whether this result has an item
			/// </summary>
			public bool HasItem => Source != null;

			/// <summary>
			/// Gets the collection the item was retrieved from
			/// </summary>
			/// <remarks>Null if no item was retrieved due</remarks>
			public AsyncCollection<TItem> Source { get; }

			/// <summary>
			/// Gets the item in this result
			/// </summary>
			public TItem Item { get; }
		}
	}
}
