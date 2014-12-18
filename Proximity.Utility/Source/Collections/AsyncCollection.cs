/****************************************\
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
	public class AsyncCollection<TItem> : IEnumerable<TItem>
	{	//****************************************
		private static readonly Task _Complete = Task.FromResult<VoidStruct>(VoidStruct.Empty);
		//****************************************
		private readonly IProducerConsumerCollection<TItem> _Collection;
		
		private readonly AsyncCounter _FreeSlots, _UsedSlots;

		private readonly int _Capacity;
		private bool _IsCompleted;
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
			_Collection = collection;
			_Capacity = int.MaxValue;
			
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
			
			_Collection = collection;
			_Capacity = capacity;
			
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
		public Task Add(TItem item)
		{
			return Add(item, CancellationToken.None);
		}
		
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
				throw new InvalidOperationException("Adding has been completed");
			
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				var MyTask = _FreeSlots.Decrement(token);
				
				if (!MyTask.IsCompleted)
					// No slot, wait for it
					return MyTask.ContinueWith(InternalAdd, item, token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
			}
			
			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
				throw new InvalidOperationException("Item was rejected by the underlying collection");
			
			// Increment the slots counter, releasing any Takers
			_UsedSlots.Increment();
			
			return _Complete;
		}
		
				/// <summary>
		/// Attempts to add and complete a collection in one operation
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <returns>A task representing the addition and completion operation</returns>
		/// <remarks>Ensures that in a one-to-one situation, the subscriber enumeration completes without throwing a cancellation</remarks>
		public Task AddComplete(TItem item)
		{
			return AddComplete(item, CancellationToken.None);
		}
		
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
				throw new InvalidOperationException("Adding has already been completed");
			
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// Try and find a free slot
				var MyTask = _FreeSlots.Decrement(token);
				
				if (!MyTask.IsCompleted)
					// No slot, wait for it
					return MyTask.ContinueWith(InternalAddComplete, item, token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
				
				// Free slot, now cancel anyone else trying to add
				_FreeSlots.Dispose();
			}
			
			// Try and add our item to the collection
			if (!_Collection.TryAdd(item))
				throw new InvalidOperationException("Item was rejected by the underlying collection");
			
			// Flag as completed first, so any Takers will handle the cleanup
			_IsCompleted = true;
			
			// Increment the slots counter, releasing any Takers
			_UsedSlots.Increment();
			
			return _Complete;
		}
		
		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for an item</exception>
		public Task<TItem> Take()
		{
			return Take(CancellationToken.None);
		}
		
		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for an item</exception>
		public Task<TItem> Take(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = Take(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith(CleanupCancelSource, MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Attempts to take an Item to the collection
		/// </summary>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The collection was completed while we were waiting for an item</exception>
		public Task<TItem> Take(CancellationToken token)
		{	//****************************************
			TItem MyItem;
			//****************************************
			
			if (IsCompleted)
				throw new InvalidOperationException("Collection is empty and adding has been completed");
			
			// Is there an item to take?
			var MyTask = _UsedSlots.Decrement(token);
			
			if (!MyTask.IsCompleted)
				// No, wait for it to arrive
				return MyTask.ContinueWith((Func<Task, TItem>)InternalTake, token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
			
			// Try and remove an item from the collection
			if (!_Collection.TryTake(out MyItem))
				throw new InvalidOperationException("Item was not returned by the underlying collection");
			
			// Is there a maximum size?
			if (_FreeSlots != null && !_IsCompleted)
			{
				// We've removed an item, so release any Adders
				_FreeSlots.Increment();
			}
			
			// If the collection is empty, cancel anyone waiting for items
			if (IsCompleted)
				_UsedSlots.Dispose();
			
			return Task.FromResult(MyItem);
		}
		
		/// <summary>
		/// Completes the collection
		/// </summary>
		/// <remarks>
		/// <para>Any pending <see cref="O:Add" /> calls will complete successfully. Any future <see cref="O:Add" /> calls will throw <see cref="OperationCanceledException"/>.</para>
		/// <para>Any pending or future <see cref="O:Take" /> calls will return the contents of the collection until it is empty, before also throwing <see cref="OperationCanceledException"/>.</para>
		/// <para>If the collection is empty at this point, pending <see cref="O:Take" /> calls will immediately throw <see cref="OperationCanceledException"/>.</para>
		/// <para>If a consumer is currently waiting on a task retrieved via <see cref="O:GetConsumingEnumerable" /> and the collection is empty, the task will throw <see cref="OperationCanceledException"/>.</para>
		/// </remarks>
		public void CompleteAdding()
		{
			// Any pending Adds may throw, any future adds -will- throw
			_IsCompleted = true;
			
			// Cancel any adds that are waiting for a free slot
			if (_FreeSlots!= null)
				_FreeSlots.Dispose();
			
			// If the collection is empty, cancel anyone waiting for items
			if (_Collection.Count == 0)
				_UsedSlots.Dispose();
		}
		
		/// <summary>
		/// Creates an enumerable that can be used to consume items as they are added to this collection
		/// </summary>
		/// <returns>An enumerable returning tasks that asynchronously wait for items to be added</returns>
		/// <remarks>
		/// <para>If the provider calls <see cref="CompleteAdding" /> while wating, the returned task will throw <see cref="OperationCanceledException"/>.</para>
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
		/// <para>If the provider calls <see cref="CompleteAdding" /> while wating, the returned task will throw <see cref="OperationCanceledException"/>.</para>
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
				catch (OperationCanceledException) // Adding has completed while we were requesting
				{
					yield break;
				}
				
				if (!MyTask.IsCompleted)
				{
					// No, wait for it to arrive
					yield return MyTask.ContinueWith((Func<Task, TItem>)InternalTake, token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
					
					continue;
				}
				
				TItem MyItem;
				
				// Try and remove an item from the collection
				if (!_Collection.TryTake(out MyItem))
					throw new InvalidOperationException("Item was not returned by the underlying collection");
				
				// Is there a maximum size?
				if (_FreeSlots != null)
				{
					// We've removed an item, so release any Adders
					_FreeSlots.Increment();
				}
				
				// If the collection is empty, cancel anyone waiting for items
				if (IsCompleted)
					_UsedSlots.Dispose();
				
				yield return Task.FromResult(MyItem);
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
				
				throw new ApplicationException("Failed to decrement free counter", task.Exception);
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
				
				throw new ApplicationException("Failed to decrement free counter", task.Exception);
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
		{	//****************************************
			TItem MyItem;
			//****************************************
			
			if (task.IsFaulted)
			{
				if (task.Exception.InnerException is ObjectDisposedException)
					throw new InvalidOperationException("Adding was completed and there are no more items in the collection");
				
				throw new ApplicationException("Failed to decrement used counter", task.Exception.InnerException);
			}
			
			// Try and remove an item from the collection
			if (!_Collection.TryTake(out MyItem))
				throw new InvalidOperationException("Item was not returned by the underlying collection");
			
			// Is there a maximum size?
			if (_FreeSlots != null)
			{
				// We've removed an item, so release any Adders
				_FreeSlots.Increment();
			}
			
			// If the collection is now empty, cancel anyone waiting for items
			if (IsCompleted)
				_UsedSlots.Dispose();
			
			return MyItem;
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
		public int Capacity
		{
			get { return _Capacity; }
		}
		
		/// <summary>
		/// Gets the approximate number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Collection.Count; }
		}
		
		/// <summary>
		/// Gets the number of operations waiting to take from the collection
		/// </summary>
		public int WaitingToTake
		{
			get { return _UsedSlots.WaitingCount; }
		}
		
		/// <summary>
		/// Gets the number of operations waiting to add to the collection
		/// </summary>
		public int WaitingToAdd
		{
			get { return _FreeSlots != null ? _FreeSlots.WaitingCount : 0; }
		}
		
		/// <summary>
		/// Gets whether adding has been completed
		/// </summary>
		public bool IsAddingCompleted
		{
			get { return _IsCompleted; }
		}
		
		/// <summary>
		/// Gets whether adding has been completed and the collection is empty
		/// </summary>
		public bool IsCompleted
		{
			get { return _IsCompleted && _UsedSlots.CurrentCount == 0; }
		}
	}
}
