using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides a base class for observable collections
	/// </summary>
	/// <typeparam name="TValue">The type contained in the collection</typeparam>
	public abstract class ObservableBase<TValue> : IList<TValue>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyList<TValue>
	{
		/// <summary>
		/// The name of the Count field
		/// </summary>
		protected const string CountFieldName = "Count";

		/// <summary>
		/// The name of the Indexer
		/// </summary>
		protected const string IndexerName = "Item[]";

		//****************************************
		private int _UpdateCount = 0;

		//****************************************

		/// <summary>
		/// Creates a new Observable Base Class
		/// </summary>
		protected ObservableBase()
		{
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public abstract void Add(TValue item);

		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="items">The elements to add</param>
		public abstract void AddRange(IEnumerable<TValue> items);

		/// <summary>
		/// Begins a major update operation, suspending change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void BeginUpdate()
		{
			if (_UpdateCount++ == 0)
			{
				OnPropertyChanged("IsUpdating");

				HasChanged = false;
			}
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public abstract bool Contains(TValue item);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public abstract void CopyTo(TValue[] array, int arrayIndex);

		/// <summary>
		/// Ends a major update operation, resuming change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void EndUpdate()
		{
			if (_UpdateCount == 0)
				return;

			if (--_UpdateCount == 0)
			{
				OnPropertyChanged("IsUpdating");

				if (HasChanged)
					OnCollectionChanged();
			}
		}

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public abstract int IndexOf(TValue item);

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		public abstract bool Remove(TValue item);

		/// <summary>
		/// Removes all items that match the given predicate
		/// </summary>
		/// <param name="predicate">A predicate that returns True for items to remove</param>
		/// <returns>The number of items removed</returns>
		public abstract int RemoveAll(Predicate<TValue> predicate);

		/// <summary>
		/// Removes the element at the specified index
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		public abstract void RemoveAt(int index);

		/// <summary>
		/// Removes the elements within the specified range
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		/// <param name="count">The number of items to remove after the index</param>
		public abstract void RemoveRange(int index, int count);

		/// <summary>
		/// Copies a snapshot of the collection to an array
		/// </summary>
		/// <returns>The current contents of the collection</returns>
		public abstract TValue[] ToArray();

		/// <summary>
		/// Searches the collection for a given value and returns the equal value, if any
		/// </summary>
		/// <param name="equalValue">The value to search for</param>
		/// <param name="actualValue">The value already in the collection, or the given value if not found</param>
		/// <returns>True if an equal value was found, otherwise False</returns>
		public bool TryGetValue(TValue equalValue, out TValue actualValue)
		{
			var Index = IndexOf(equalValue);

			if (Index > 0)
			{
				actualValue = InternalGet(Index);

				return true;
			}

			actualValue = equalValue;

			return false;
		}

		//****************************************

		IEnumerator IEnumerable.GetEnumerator() => InternalGetEnumerator();

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => InternalGetEnumerator();

		int IList.Add(object value)
		{
			var MyCount = Count;

			Add((TValue)value);

			return MyCount;
		}

		bool IList.Contains(object value) => value is TValue Value && Contains(Value);

		int IList.IndexOf(object value)
		{
			if (value is TValue Value)
				return IndexOf(Value);

			return -1;
		}

		void IList.Insert(int index, object value) => InternalInsert(index, (TValue)value);

		void IList<TValue>.Insert(int index, TValue item) => InternalInsert(index, item);

		void IList.Remove(object value)
		{
			if (value is TValue Value)
				Remove(Value);
		}

		void ICollection.CopyTo(Array array, int index) => CopyTo((TValue[])array, index);

		//****************************************

		/// <summary>
		/// Raises the PropertyChanged event
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (_UpdateCount == 0)
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		protected abstract IEnumerator<TValue> InternalGetEnumerator();

		/// <summary>
		/// Retrieves an item by index
		/// </summary>
		/// <param name="index">The index of the item to retrieve</param>
		/// <returns>The requested Item</returns>
		protected abstract TValue InternalGet(int index);

		/// <summary>
		/// Inserts an element at the specified index
		/// </summary>
		/// <param name="index">The index to insert the element</param>
		/// <param name="item">The element to insert</param>
		protected abstract void InternalInsert(int index, TValue item);
		
		/// <summary>
		/// Sets an item by index
		/// </summary>
		/// <param name="index">The index to set the item at</param>
		/// <param name="value">The new item</param>
		protected abstract void InternalSet(int index, TValue value);

		/// <summary>
		/// Publishes <see cref="PropertyChanged"/> notifications when an item is added or removed
		/// </summary>
		protected virtual void OnPropertyChanged()
		{
			OnPropertyChanged(CountFieldName);
			OnPropertyChanged(IndexerName);
		}

		/// <summary>
		/// Publishes a <see cref="CollectionChanged"/> Reset notification
		/// </summary>
		protected virtual void OnCollectionChanged()
		{
			HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary>
		/// Publishes a <see cref="CollectionChanged"/> Add or Remove notification
		/// </summary>
		/// <param name="action">Whether this change is an Add or Remove</param>
		/// <param name="changedItem">The item that has been added or removed</param>
		/// <param name="index">The index of the item that was added or removed</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int index)
		{
			HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));
		}

		/// <summary>
		/// Publishes a <see cref="CollectionChanged"/> Move notification
		/// </summary>
		/// <param name="action">The Move action</param>
		/// <param name="changedItem">The item that has been added or removed</param>
		/// <param name="newIndex">The index of the item that is being moved to</param>
		/// <param name="oldIndex">The index of the item that is being moved from</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int newIndex, int oldIndex)
		{
			HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem, newIndex, oldIndex));
		}

		/// <summary>
		/// Publishes a <see cref="CollectionChanged"/> Replace notification
		/// </summary>
		/// <param name="action">A Replace action</param>
		/// <param name="newItem">The new item</param>
		/// <param name="oldItem">The old item</param>
		/// <param name="index">The index the item is at</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, TValue newItem, TValue oldItem, int index)
		{
			HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(IndexerName); // Only the indexer has changed, the count is the same

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		/// <summary>
		/// Publishes a <see cref="CollectionChanged"/> bulk Add or Remove notification without an Index
		/// </summary>
		/// <param name="action">Whether this change is an Add or Remove</param>
		/// <param name="changedItems">The items that were added or removed</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<TValue> changedItems)
		{
			HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems.ToArray()));
		}

		/// <summary>
		/// Publishes a <see cref="CollectionChanged"/> bulk Add or Remove notification  for a range of items
		/// </summary>
		/// <param name="action">Whether this change is an Add or Remove</param>
		/// <param name="changedItems">The items that were added or removed</param>
		/// <param name="startIndex">The index of the first item that was added or removed</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<TValue> changedItems, int startIndex)
		{
			HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems.ToArray(), startIndex));
		}

		//****************************************

		/// <summary>
		/// Raised when the collection changes
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Raised when a property of the collection changes
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets whether an update is in progress via <see cref="BeginUpdate" /> and <see cref="EndUpdate" />
		/// </summary>
		public bool IsUpdating => _UpdateCount != 0;

		/// <summary>
		/// Gets whether this collection is read-only
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public abstract int Count { get; }

		//****************************************

		/// <summary>
		/// Gets
		/// </summary>
		protected bool HasChanged { get; private set; } = false;

		//****************************************

		TValue IList<TValue>.this[int index]
		{
			get => InternalGet(index);
			set => InternalSet(index, value);
		}

		TValue IReadOnlyList<TValue>.this[int index] => InternalGet(index);

		object IList.this[int index]
		{
			get => InternalGet(index);
			set => InternalSet(index, (TValue)value);
		}

		bool IList.IsFixedSize => false;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;
	}
}
