/****************************************\
 ObservableSet.cs
 Created: 2015-06-10
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
	/// Provides an Observable HashSet supporting BeginUpdate and EndUpdate for batch changes as well as indexed access for optimal data-binding
	/// </summary>
	/// <typeparam name="TValue">The type of value within the set</typeparam>
	public class ObservableSet<TValue> : ISet<TValue>, IList<TValue>, IList, INotifyCollectionChanged, INotifyPropertyChanged
	{	//****************************************
		private const string CountString = "Count";
		//****************************************
		private int[] _Keys;
		private TValue[] _Values;

		private readonly IEqualityComparer<TValue> _Comparer;

		private int _UpdateCount = 0;
		private int _Size = 0;
		//****************************************

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		public ObservableSet() : this(EqualityComparer<TValue>.Default)
		{

		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		public ObservableSet(IEnumerable<TValue> collection) : this(collection, EqualityComparer<TValue>.Default)
		{
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(IEqualityComparer<TValue> comparer)
		{
			_Keys = new int[0];
			_Values = new TValue[0];
			_Comparer = comparer;
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(IEnumerable<TValue> collection, IEqualityComparer<TValue> comparer)
		{
			_Comparer = comparer;

			// Ensure we add all the values in the order sorted by hash code
#if PORTABLE
			_Keys = new int[0];
			_Values = new TValue[0];

			// Can't use Array.Sort(key, value, comparer) since it doesn't exist on portable
			foreach (var MyPair in collection.Select(value => new KeyValuePair<int, TValue>(comparer.GetHashCode(value), value)).OrderBy(pair => pair.Key))
			{
				if (_Size == _Keys.Length)
					EnsureCapacity(_Size + 1);

				_Keys[_Size] = MyPair.Key;
				_Values[_Size] = MyPair.Value;

				_Size++;
			}
#else
			_Values = collection.ToArray();
			_Size = _Values.Length;
			_Keys = new int[_Size];

			for (int Index = 0; Index < _Values.Length; Index++)
			{
				_Keys[Index] = comparer.GetHashCode(_Values[Index]);
			}

			Array.Sort<int, TValue>(_Keys, _Values, Comparer<int>.Default);
#endif
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

			var NewItems = new List<TValue>(items);

			// If there's no items in the collection, don't do anything
			if (NewItems.Count == 0)
				return;
			
			// Try and add the new items
			foreach (var MyItem in NewItems)
			{
				if (TryAdd(MyItem, out Index))
				{
					NewItem = MyItem;
					Count++;
				}
			}

			// If we only got one, just do a single add
			if (Count == 1)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, Index);
			// If there's more than one, the indexes could be anywhere, so reset the collection
			else if (Count > 1)
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
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			if (_Size > 0)
			{
				Array.Clear(_Values, 0, _Size);

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
			_Values.CopyTo(array, arrayIndex);
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
			int Key = _Comparer.GetHashCode(item);
			int Index = Array.BinarySearch<int>(_Keys, 0, _Size, Key);
			//****************************************

			// Is there a matching hash code?
			if (Index < 0)
				return -1;

			for (; ; )
			{
				// Do we match this item?
				if (_Comparer.Equals(_Values[Index], item))
					return Index; // Yes, so return the Index

				Index++;

				// Are we at the end of the list?
				if (Index == _Size)
					return -1; // Yes, so we didn't find the item

				// Is there another item with the same key?
				if (_Keys[Index] != Key)
					return -1; // Nope, so we didn't find the item

				// Yes, so loop back and check that
			}
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

			var Item = _Values[index];

			_Size--;

			// If this is in the middle, move the values down
			if (index < _Size)
			{
				Array.Copy(_Keys, index + 1, _Keys, index, _Size - index);
				Array.Copy(_Values, index + 1, _Values, index, _Size - index);
			}

			// Ensure we don't hold a reference to the value
			_Values[_Size] = default(TValue);

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
			_Values.CopyTo(array, index);
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
			int num = (_Keys.Length == 0 ? 4 : _Keys.Length * 2);
			
			if (num > 0x7FEFFFFF)
				num = 0x7FEFFFFF;

			if (num < capacity)
				num = capacity;

			Capacity = capacity;
		}

		private bool TryAdd(TValue item, out int insertIndex)
		{	//****************************************
			int Key = _Comparer.GetHashCode(item);
			int Index = Array.BinarySearch<int>(_Keys, 0, _Size, Key);
			//****************************************

			// Is there a matching hash code?
			if (Index >= 0)
			{
				for (; ; )
				{
					// Do we match this item?
					if (_Comparer.Equals(_Values[Index], item))
					{
						insertIndex = -1;

						return false; // Yes, so return False
					}

					// Are we at the end of the list?
					if (Index == _Size - 1)
						break; // Yes, so we need to insert the new item at the end

					// Is there another item with the same key?
					if (_Keys[Index + 1] != Key)
						break; // Nope, so we can insert the new item at the current Index

					// Yes, so loop back and check that
					Index++;
				}

				Insert(Index, Key, item);

				insertIndex = Index;

				return true;
			}

			// No matching item, insert at the nearest spot above
			Insert(~Index, Key, item);

			insertIndex = ~Index;

			return true;
		}

		private void Insert(int index, int key, TValue value)
		{
			if (_Size == _Keys.Length)
				EnsureCapacity(_Size + 1);

			// Are we inserting before the end item?
			if (index < _Size)
			{
				// Yes, so move things up
				Array.Copy(_Keys, index, _Keys, index + 1, _Size - index);
				Array.Copy(_Values, index, _Values, index + 1, _Size - index);
			}

			_Keys[index] = key;
			_Values[index] = value;

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
		/// Gets the IEqualityComparer{TValue} that is used to compare items in the set
		/// </summary>
		public IEqualityComparer<TValue> Comparer
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
		/// Gets or Sets the number of elements that the Observable Set can contain.
		/// </summary>
		public int Capacity
		{
			get { return _Keys.Length; }
			set
			{
				if (value == _Keys.Length)
					return;

				if (value < _Size)
					throw new ArgumentException("value");

				if (value == 0)
				{
					_Keys = new int[0];
					_Values = new TValue[0];

					return;
				}
				var NewKeys = new int[value];
				var NewValues = new TValue[value];

				if (_Size > 0)
				{
						Array.Copy(_Keys, 0, NewKeys, 0, _Size);
						Array.Copy(_Values, 0, NewValues, 0, _Size);
				}

				_Keys = NewKeys;
				_Values = NewValues;
			}
		}

		/// <summary>
		/// Gets the value corresponding to the provided index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[int index]
		{
			get { return _Values[index]; }
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
			get { return _Values[index]; }
			set { throw new NotSupportedException(); }
		}

		object IList.this[int index]
		{
			get { return _Values[index]; }
			set { throw new NotSupportedException(); }
		}

		//****************************************

		private sealed class ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{	//****************************************
			private readonly ObservableSet<TValue> _Parent;

			private int _Index;
			private TValue _Current;
			//****************************************

			public ValueEnumerator(ObservableSet<TValue> parent)
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

				_Current = _Parent._Values[_Index++];

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
	}
}
