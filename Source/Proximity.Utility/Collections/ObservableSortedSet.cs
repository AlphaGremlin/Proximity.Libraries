/****************************************\
 ObservableSortedSet.cs
 Created: 2015-10-05
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides an Observable Sorted Set supporting BeginUpdate and EndUpdate for batch changes as well as indexed access for optimal data-binding
	/// </summary>
	/// <typeparam name="TValue">The type of value within the set</typeparam>
	public class ObservableSortedSet<TValue> : ISet<TValue>, IList<TValue>, IList, INotifyPropertyChanged, INotifyCollectionChanged
	{	//****************************************
		private const string CountString = "Count";
		//****************************************
		private readonly IComparer<TValue> _Comparer;

		private TValue[] _Items;

		private int _Size;
		private int _UpdateCount;
		//****************************************

		/// <summary>
		/// Creates a new Observable Set with the default capacity and comparer
		/// </summary>
		public ObservableSortedSet() : this(0, GetDefaultComparer())
		{
		}

		/// <summary>
		/// Creates a new Observable Set with the default comparer
		/// </summary>
		/// <param name="capacity">The starting capacity for the set</param>
		public ObservableSortedSet(int capacity) : this(capacity, GetDefaultComparer())
		{
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="comparer">The comparer to use for this set</param>
		public ObservableSortedSet(IComparer<TValue> comparer) : this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new Observable Sorted Set
		/// </summary>
		/// <param name="collection">The items to populate the set with</param>
		public ObservableSortedSet(IEnumerable<TValue> collection) : this(collection, GetDefaultComparer())
		{
		}

		/// <summary>
		/// Creates a new Observable Sorted Set
		/// </summary>
		/// <param name="collection">The items to populate the set with</param>
		/// <param name="comparer">The comparer to use with this set</param>
		public ObservableSortedSet(IEnumerable<TValue> collection, IComparer<TValue> comparer)
		{
			_Comparer = comparer;

			_Items = collection.ToArray();

			Array.Sort<TValue>(_Items, comparer);
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="capacity">The starting capacity for the set</param>
		/// <param name="comparer">The comparer to use for this set</param>
		public ObservableSortedSet(int capacity, IComparer<TValue> comparer)
		{
			_Comparer = comparer;
			_Size = 0;

			_Items = new TValue[capacity];
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <returns>True if the element was added, False if it was already in the set</returns>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public bool Add(TValue item)
		{	//****************************************
			int Index;
			//****************************************

			if (TryAdd(item, out Index))
			{
				OnCollectionChanged(NotifyCollectionChangedAction.Add, item, Index);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="items">The elements to add</param>
		public void AddRange(IEnumerable<TValue> items)
		{	//****************************************
			int Index = 0;
			int Count = 0;
			TValue NewItem = default(TValue);
			//****************************************

			if (items == null)
				throw new ArgumentNullException("items");

			var NewItems = items.ToArray();

			// If there's no items in the collection, don't do anything
			if (NewItems.Length == 0)
				return;

			if (_Size == 0)
			{
				// Since we're empty, it's faster to load the contents by sorting and then copying
				Array.Sort<TValue>(NewItems, _Comparer);

				EnsureCapacity(NewItems.Length);

				Array.Copy(NewItems, _Items, NewItems.Length);

				NewItem = _Items[0];
				_Size = NewItems.Length;
			}
			else
			{
				// Try and add the new items
				foreach (var MyItem in NewItems)
				{
					if (TryAdd(MyItem, out Index))
					{
						NewItem = MyItem;
						Count++;
					}
				}
			}

			// If we only got one, just do a single add
			if (_Size == 1)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, Index);
			// If there's more than one, the indexes could be anywhere, so reset the collection
			else if (_Size > 1)
				OnCollectionChanged();
		}

		/// <summary>
		/// Begins a major update operation, suspending change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void BeginUpdate()
		{
			if (_UpdateCount++ == 0)
				OnPropertyChanged("IsUpdating");
		}

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		public int BinarySearch(TValue item)
		{
			return Array.BinarySearch<TValue>(_Items, 0, _Size, item, _Comparer);
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			if (_Size > 0)
			{
				Array.Clear(_Items, 0, _Size);

				_Size = 0;

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
			return IndexOf(item) >= 0;
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TValue[] array, int arrayIndex)
		{
			Array.Copy(_Items, 0, array, arrayIndex, _Size);
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
				OnPropertyChanged("IsUpdating");

			OnCollectionChanged();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TValue> GetEnumerator()
		{
			return new ValueEnumerator(this);
		}

		/// <summary>
		/// Determines the index of a specific item in the set
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TValue item)
		{	//****************************************
			int Index = Array.BinarySearch<TValue>(_Items, 0, _Size, item, _Comparer);
			//****************************************

			// Is there a matching hash code?
			if (Index < 0)
				return -1;

			return Index;
		}

		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<TValue> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			throw new NotSupportedException();
		}

		/// <summary>
		/// Checks whether this set is a strict superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict superset of the collection, otherwise False</returns>
		public bool IsProperSupersetOf(IEnumerable<TValue> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			throw new NotSupportedException();
		}

		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<TValue> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			throw new NotSupportedException();
		}

		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<TValue> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			throw new NotSupportedException();
		}

		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<TValue> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was in the collection and removed, otherwise False</returns>
		public bool Remove(TValue item)
		{	//****************************************
			int Index = IndexOf(item);
			//****************************************

			if (Index < 0)
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <summary>
		/// Removes the element at the specified index
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= _Size)
				throw new ArgumentOutOfRangeException("index");

			var Item = _Items[index];

			_Size--;

			// If this is in the middle, move the values down
			if (index < _Size)
			{
				Array.Copy(_Items, index + 1, _Items, index, _Size - index);
			}

			// Ensure we don't hold a reference to the value
			_Items[_Size] = default(TValue);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, Item, index);

		}

		/// <summary>
		/// Checks whether this set and the given collection contain the same elements
		/// </summary>
		/// <param name="other">The collection to check against</param>
		/// <returns>True if they contain the same elements, otherwise FAlse</returns>
		public bool SetEquals(IEnumerable<TValue> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			throw new NotSupportedException();
		}

		//****************************************

		void ICollection<TValue>.Add(TValue item)
		{
			this.Add(item);
		}

		int IList.Add(object value)
		{	//****************************************
			int Index;
			TValue NewValue;
			//****************************************

			if (!(value is TValue))
				throw new ArgumentException("Value is not of the required type");

			NewValue = (TValue)value;

			if (TryAdd(NewValue, out Index))
			{
				OnCollectionChanged(NotifyCollectionChangedAction.Add, NewValue, Index);

				return Index;
			}

			return -1;
		}

		bool IList.Contains(object value)
		{
			return value is TValue && Contains((TValue)value);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			Array.Copy(_Items, 0, array, index, _Size);
		}

		void ISet<TValue>.ExceptWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ValueEnumerator(this);
		}

		int IList.IndexOf(object value)
		{
			if (!(value is TValue))
				return -1;

			return IndexOf((TValue)value);
		}

		void IList.Insert(int index, object value)
		{
			throw new NotSupportedException();
		}

		void IList<TValue>.Insert(int index, TValue item)
		{
			throw new NotSupportedException();
		}

		void ISet<TValue>.IntersectWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
		}

		void IList.Remove(object value)
		{
			if (value is TValue)
				Remove((TValue)value);
		}

		void ISet<TValue>.SymmetricExceptWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
		}

		void ISet<TValue>.UnionWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
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

		private void EnsureCapacity(int capacity)
		{
			int num = (_Items.Length == 0 ? 4 : _Items.Length * 2);

			if (num > 0x7FEFFFFF)
				num = 0x7FEFFFFF;

			if (num < capacity)
				num = capacity;

			Capacity = capacity;
		}

		private bool TryAdd(TValue item, out int insertIndex)
		{	//****************************************
			int Index = Array.BinarySearch<TValue>(_Items, 0, _Size, item, _Comparer);
			//****************************************

			// Is there a matching item?
			if (Index >= 0)
			{
				insertIndex = -1;

				return false; // Yes, so return False
			}

			// No matching item, insert at the nearest spot above
			Insert(~Index, item);

			insertIndex = ~Index;

			return true;
		}

		private void Insert(int index, TValue value)
		{
			if (_Size == _Items.Length)
				EnsureCapacity(_Size + 1);

			// Are we inserting before the end item?
			if (index < _Size)
			{
				// Yes, so move things up
				Array.Copy(_Items, index, _Items, index + 1, _Size - index);
			}

			_Items[index] = value;

			_Size++;
		}

		private void OnCollectionChanged()
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int index)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<TValue> changedItems)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItems.ToArray()));
		}

		//****************************************

		/// <summary>
		/// Raised when the collection changes
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Raised when a property of the dictionary changes
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets the IComparer{TValue} that is used to compare items in the set
		/// </summary>
		public IComparer<TValue> Comparer
		{
			get { return _Comparer; }
		}

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Size; }
		}

		/// <summary>
		/// Gets the minimum value in the sorted set
		/// </summary>
		public TValue Min
		{
			get
			{
				if (_Size == 0)
					return default(TValue);

				return _Items[0];
			}
		}

		/// <summary>
		/// Gets the maximum value in the sorted set
		/// </summary>
		public TValue Max
		{
			get
			{
				if (_Size == 0)
					return default(TValue);

				return _Items[_Size - 1];
			}
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
		/// Gets/Sets the number of elements that the Observable Sorted Set can contain.
		/// </summary>
		public int Capacity
		{
			get { return _Items.Length; }
			set
			{
				if (value == _Items.Length)
					return;

				if (value < _Size)
					throw new ArgumentException("value");

				if (value == 0)
				{
					_Items = new TValue[0];

					return;
				}
				var NewItems = new TValue[value];

				if (_Size > 0)
				{
					Array.Copy(_Items, 0, NewItems, 0, _Size);
				}

				_Items = NewItems;
			}
		}

		/// <summary>
		/// Gets the value corresponding to the provided index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[int index]
		{
			get { return _Items[index]; }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { return this; }
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		TValue IList<TValue>.this[int index]
		{
			get { return _Items[index]; }
			set { throw new NotSupportedException(); }
		}

		object IList.this[int index]
		{
			get { return _Items[index]; }
			set { throw new NotSupportedException(); }
		}

		//****************************************

		private sealed class ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{	//****************************************
			private readonly ObservableSortedSet<TValue> _Parent;

			private int _Index;
			private TValue _Current;
			//****************************************

			public ValueEnumerator(ObservableSortedSet<TValue> parent)
			{
				_Parent = parent;
			}

			//****************************************

			public void Dispose()
			{
				_Current = default(TValue);
			}

			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					_Current = default(TValue);

					return false;
				}

				_Current = _Parent._Items[_Index++];

				return true;
			}

			public void Reset()
			{
				_Index = 0;
				_Current = default(TValue);
			}

			//****************************************

			public TValue Current
			{
				get { return _Current; }
			}

			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}

		//****************************************

		private static IComparer<TValue> GetDefaultComparer()
		{
#if PORTABLE
			var MyInfo = typeof(TValue).GetTypeInfo();

			if (!typeof(IComparable<TValue>).GetTypeInfo().IsAssignableFrom(MyInfo) && !typeof(IComparable).GetTypeInfo().IsAssignableFrom(MyInfo))
#else
			if (!typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) && !typeof(IComparable).IsAssignableFrom(typeof(TValue)))
#endif
				throw new ArgumentException(string.Format("{0} does not implement IComparable or IComparable<>", typeof(TValue).FullName));

			return Comparer<TValue>.Default;
		}
	}
}
