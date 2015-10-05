/****************************************\
 ObservableList.cs
 Created: 2014-12-11
\****************************************/
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
	/// Provides an Observable List supporting BeginUpdate and EndUpdate for batch changes
	/// </summary>
	public class ObservableList<TValue> : IList<TValue>, IList, INotifyCollectionChanged, INotifyPropertyChanged
	{	//****************************************
		private const string CountString = "Count";
		private const string IndexerName = "Item[]";
		//****************************************
		private readonly List<TValue> _Items;

		private int _UpdateCount = 0;
		private bool _HasChanged = false;
		//****************************************

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		public ObservableList()
		{
			_Items = new List<TValue>();
		}

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		public ObservableList(IEnumerable<TValue> collection)
		{
			_Items = new List<TValue>(collection);
		}

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		/// <param name="capacity">The starting capacity of the list</param>
		public ObservableList(int capacity)
		{
			_Items = new List<TValue>(capacity);
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public void Add(TValue item)
		{
			Insert(_Items.Count, item, true);
		}

		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="items">The elements to add</param>
		public void AddRange(IEnumerable<TValue> items)
		{
			bool AddedItem = false;
			int StartIndex = _Items.Count;

			if (items == null) throw new ArgumentNullException("items");

			foreach (var MyItem in items)
			{
				_Items.Add(MyItem);

				AddedItem = true;
			}

			if (AddedItem)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, items, StartIndex);
		}

		/// <summary>
		/// Begins a major update operation, suspending change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void BeginUpdate()
		{
			if (_UpdateCount++ == 0)
			{
				OnPropertyChanged("IsUpdating");

				_HasChanged = false;
			}
		}

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		/// <remarks>The list must be sorted before calling this method</remarks>
		public int BinarySearch(TValue item)
		{
			return _Items.BinarySearch(item);
		}

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <param name="comparer">The comparer to use when searching</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		/// <remarks>The list must be sorted before calling this method</remarks>
		public int BinarySearch(TValue item, IComparer<TValue> comparer)
		{
			return _Items.BinarySearch(item, comparer);
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			if (_Items.Count > 0)
			{
				_Items.Clear();

				OnCollectionChanged();
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TValue item)
		{
			return _Items.Contains(item);
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TValue[] array, int arrayIndex)
		{
			_Items.CopyTo(array, arrayIndex);
		}

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

				if (_HasChanged)
					OnCollectionChanged();
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TValue> GetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TValue item)
		{
			return _Items.IndexOf(item);
		}

		/// <summary>
		/// Inserts an element at the specified index
		/// </summary>
		/// <param name="index">The index to insert the element</param>
		/// <param name="item">The element to insert</param>
		public void Insert(int index, TValue item)
		{
			Insert(index, item, true);
		}

		/// <summary>
		/// Sorts the contents of this Observable List
		/// </summary>
		public void Sort()
		{
			if (_Items.Count != 0)
			{
				_Items.Sort();

				OnCollectionChanged();
			}
		}

		/// <summary>
		/// Sorts the contents of this Observable List based on a comparer
		/// </summary>
		/// <param name="comparer">The comparer to sort by</param>
		public void Sort(IComparer<TValue> comparer)
		{
			if (_Items.Count != 0)
			{
				_Items.Sort(comparer);

				OnCollectionChanged();
			}
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		public bool Remove(TValue item)
		{
			var MyIndex = _Items.IndexOf(item);

			if (MyIndex == -1)
				return false;

			_Items.RemoveAt(MyIndex);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, MyIndex);

			return true;
		}

		/// <summary>
		/// Removes the element at the specified index
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		public void RemoveAt(int index)
		{
			var OldItem = _Items[index];

			_Items.RemoveAt(index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, index);
		}

		//****************************************

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_Items).GetEnumerator();
		}

		int IList.Add(object value)
		{
			var Count = _Items.Count;

			Insert(Count, (TValue)value, true);

			return Count;
		}

		bool IList.Contains(object value)
		{
			return value is TValue && Contains((TValue)value);
		}

		int IList.IndexOf(object value)
		{
			if (value is TValue)
				return IndexOf((TValue)value);

			return -1;
		}

		void IList.Insert(int index, object value)
		{
			Insert(index, (TValue)value, false);
		}

		void IList.Remove(object value)
		{
			if (value is TValue)
				Remove((TValue)value);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			((IList)_Items).CopyTo(array, index);
		}

		/// <summary>
		/// Raises the PropertyChanged event
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (_UpdateCount == 0 && PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		//****************************************

		private void Insert(int index, TValue item, bool isAdd)
		{
			if (isAdd)
			{
				_Items.Insert(index, item);

				OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
			}
			else
			{
				var OldValue = _Items[index];
				_Items[index] = item;

				OnCollectionChanged(NotifyCollectionChangedAction.Replace, item, OldValue, index);
			}
		}

		private void OnPropertyChanged()
		{
			OnPropertyChanged(CountString);
			OnPropertyChanged(IndexerName);
		}

		private void OnCollectionChanged()
		{
			_HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int index)
		{
			_HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue newItem, TValue oldItem, int index)
		{
			_HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(IndexerName); // Only the indexer has changed, the count is the same

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<TValue> newItems, int startIndex)
		{
			_HasChanged = true;

			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItems.ToArray(), startIndex));
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
		/// Gets/Sets the value at the provided index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[int index]
		{
			get { return _Items[index]; }
			set { Insert(index, value, false); }
		}

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Items.Count; }
		}

		/// <summary>
		/// Gets whether this collection is read-only
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Gets whether an update is in progress via <see cref="BeginUpdate" /> and <see cref="EndUpdate" />
		/// </summary>
		public bool IsUpdating
		{
			get { return _UpdateCount != 0; }
		}

		/// <summary>
		/// Gets the underlying list object
		/// </summary>
		protected List<TValue> Items
		{
			get { return _Items; }
		}

		object IList.this[int index]
		{
			get { return _Items[index]; }
			set { Insert(index, (TValue)value, false); }
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { return this; }
		}
	}
}
