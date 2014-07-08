/****************************************\
 AsyncReadWriteLock.cs
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
//****************************************

namespace Proximity.Utility.Threading
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
			_Collection = collection;
			_Capacity = capacity;
			
			_UsedSlots = new AsyncCounter(collection.Count);
			_FreeSlots = new AsyncCounter(capacity);
		}
		
		//****************************************
		
		public Task Add(TItem item)
		{
			return Add(item, CancellationToken.None);
		}
		
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
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource);
			
			return MyTask;
		}
		
		public Task Add(TItem item, CancellationToken token)
		{
			if (_IsCompleted)
				throw new OperationCanceledException("Adding has been completed");
			
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
		
		public Task<TItem> Take()
		{
			return Take(CancellationToken.None);
		}
		
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
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource);
			
			return MyTask;
		}
		
		public Task<TItem> Take(CancellationToken token)
		{	//****************************************
			TItem MyItem;
			//****************************************
			
			if (IsCompleted)
				throw new OperationCanceledException("Collection is empty and adding has been completed");
			
			// Is there an item to take?
			var MyTask = _UsedSlots.Decrement(token);
			
			if (!MyTask.IsCompleted)
				// No, wait for it to arrive
				return MyTask.ContinueWith((Func<Task, TItem>)InternalTake, token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
			
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
			
			return Task.FromResult(MyItem);
		}
		
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
		
		public IEnumerable<Task<TItem>> GetConsumingEnumerable()
		{
			return GetConsumingEnumerable(CancellationToken.None);
		}
		
		public IEnumerable<Task<TItem>> GetConsumingEnumerable(CancellationToken token)
		{
			Task MyTask;
			
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
				throw new InvalidOperationException("Failed to decrement free counter", task.Exception);
			
			if (_IsCompleted)
				throw new InvalidOperationException("Adding has been completed");
			
			// Try and add our item to the collection
			if (!_Collection.TryAdd(MyItem))
				throw new InvalidOperationException("Item was rejected by the underlying collection");
			
			// Increment the slots counter
			_UsedSlots.Increment();
		}
		
		private TItem InternalTake(Task task)
		{	//****************************************
			TItem MyItem;
			//****************************************
			
			if (task.IsFaulted)
				throw new InvalidOperationException("Failed to decrement used counter", task.Exception);
			
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
		
		public bool IsAddingCompleted
		{
			get { return _IsCompleted; }
		}
		
		public bool IsCompleted
		{
			get { return _IsCompleted && _UsedSlots.CurrentCount == 0; }
		}
	}
}
