/****************************************\
 ObservableDictionary.cs
 Created: 2014-03-21
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Implements an Observable Sorted List for WPF binding
	/// </summary>
	public class ObservableSortedList<TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, IList, INotifyCollectionChanged, INotifyPropertyChanged
	{	//****************************************
		private const string CountString = "Count";
		private const string IndexerName = "Item[]";
		private const string KeysName = "Keys";
		private const string ValuesName = "Values";
		//****************************************
		private TKey[] _Keys;
		private TValue[] _Values;
		private readonly IComparer<TKey> _Comparer;

		private readonly ObservableDictionaryCollection<TKey> _KeyCollection;
		private readonly ObservableDictionaryCollection<TValue> _ValueCollection;

		private int _UpdateCount = 0;
		private int _Size = 0;
		//****************************************

		/// <summary>
		/// Creates a new, empty observable sorted list
		/// </summary>
		public ObservableSortedList() : this(0, Comparer<TKey>.Default)
		{
		}

		/// <summary>
		/// Creates a new pre-filled observable sorted list
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		public ObservableSortedList(IDictionary<TKey, TValue> dictionary) : this(dictionary, Comparer<TKey>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty observable sorted list with the specified default capacity
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		public ObservableSortedList(int capacity) : this(capacity, Comparer<TKey>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty observable sorted list with the specified comparer
		/// </summary>
		/// <param name="comparer">The comparer to use</param>
		public ObservableSortedList(IComparer<TKey> comparer) : this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new pre-filled observable sorted list with the specified comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="comparer">The comparer to use</param>
		public ObservableSortedList(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
		{
			_Comparer = comparer;

			// Ensure we add all the values in the order sorted by hash code
#if PORTABLE
			_Keys = new TKey[0];
			_Values = new TValue[0];

			// Can't use Array.Sort(key, value, comparer) since it doesn't exist on portable
			foreach (var MyPair in dictionary.OrderBy(pair => pair.Key, _Comparer))
			{
				if (_Size == _Keys.Length)
					EnsureCapacity(_Size + 1);

				_Keys[_Size] = MyPair.Key;
				_Values[_Size] = MyPair.Value;

				_Size++;
			}
#else
			_Size = dictionary.Count;
			_Keys = new TKey[_Size];
			_Values = new TValue[_Size];

			dictionary.Keys.CopyTo(_Keys, 0);
			dictionary.Values.CopyTo(_Values, 0);

			Array.Sort<TKey, TValue>(_Keys, _Values, _Comparer);
#endif

			_KeyCollection = new ObservableDictionaryCollection<TKey>(new KeyCollection(this));
			_ValueCollection = new ObservableDictionaryCollection<TValue>(new ValueCollection(this));
		}

		/// <summary>
		/// Creates a new empty observable sorted list with the specified comparer
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableSortedList(int capacity, IComparer<TKey> comparer)
		{
			_Keys = new TKey[capacity];
			_Values = new TValue[capacity];
			_Comparer = comparer;

			_KeyCollection = new ObservableDictionaryCollection<TKey>(new KeyCollection(this));
			_ValueCollection = new ObservableDictionaryCollection<TValue>(new ValueCollection(this));
		}

		//****************************************

		/// <summary>
		/// Adds a new element the list
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		public void Add(TKey key, TValue value)
		{
			Add(new KeyValuePair<TKey, TValue>(key, value));
		}

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public void Add(KeyValuePair<TKey, TValue> item)
		{	//****************************************
			int Index = Insert(item);
			//****************************************

			if (Index == -1)
				throw new ArgumentException("An item with the same key has already been added.");

			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, Index);
		}

		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="items">The elements to add</param>
		public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			var NewItems = new List<KeyValuePair<TKey, TValue>>(items);

			if (NewItems.Count == 0)
				return;

			if (items.Any(pair => IndexOfKey(pair.Key) >= 0))
				throw new ArgumentException("An item with the same key has already been added.");

			foreach (var NewItem in NewItems)
				Insert(NewItem);

			OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItems);
		}

		/// <summary>
		/// Begins a major update operation, suspending change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void BeginUpdate()
		{
			_UpdateCount++;
		}

		/// <summary>
		/// Performs a Binary Search for the given key
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>The index of the item, or the one's complement of the index where it will be inserted</returns>
		public int BinarySearch(TKey item)
		{
			return Array.BinarySearch<TKey>(_Keys, 0, _Size, item, _Comparer);
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
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return IndexOf(item) >= 0;
		}

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if there is an element with this key, otherwise false</returns>
		public bool ContainsKey(TKey key)
		{
			return IndexOfKey(key) >= 0;
		}

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified value
		/// </summary>
		/// <param name="value">The value to search for</param>
		/// <returns>True if there is at least one element with this value, otherwise false</returns>
		public bool ContainsValue(TValue value)
		{
			return IndexOfValue(value) >= 0;
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			Array.Copy(_Values, 0, array, arrayIndex, _Size);
		}

		/// <summary>
		/// Ends a major update operation, resuming change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void EndUpdate()
		{
			if (_UpdateCount == 0)
				return;

			_UpdateCount--;

			// Raise changes (only if we're zero)
			OnCollectionChanged();
		}

		/// <summary>
		/// Searches for the specified key/value pair and returns the zero-based index within the ObservableSortedList
		/// </summary>
		/// <param name="item">The key/value pair to search for</param>
		/// <returns>True if a matching key/value pair was found, otherwise -1</returns>
		public int IndexOf(KeyValuePair<TKey, TValue> item)
		{
			var MyIndex = IndexOfKey(item.Key);

			// If we found the Key, make sure the Item matches
			if (MyIndex != -1 && !Equals(_Values[MyIndex], item.Value))
				MyIndex = -1;

			return MyIndex;
		}

		/// <summary>
		/// Searches for the specified key and returns the zero-based index within the ObservableSortedList
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if a matching key was found, otherwise -1</returns>
		public int IndexOfKey(TKey key)
		{	//****************************************
			int Index = Array.BinarySearch<TKey>(_Keys, 0, _Size, key, _Comparer);
			//****************************************

			// Is there a matching hash code?
			if (Index < 0)
				return -1;

			return Index;
		}

		/// <summary>
		/// Searches for the specified value and returns the zero-based index of the first occurrence within the ObservableDictionary
		/// </summary>
		/// <param name="value">The value to search for</param>
		/// <returns>True if a matching value was found, otherwise -1</returns>
		public int IndexOfValue(TValue value)
		{
			for (int Index = 0; Index < _Size; Index++)
			{
				if (Equals(_Values[Index], value))
					return Index;
			}

			return -1;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="key">The key of the element to remove</param>
		public bool Remove(TKey key)
		{
			if (key == null) throw new ArgumentNullException("key");

			var Index = IndexOfKey(key);

			if (Index == -1)
				return false;

			var Value = _Values[Index];

			RemoveAt(Index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, Value), Index);

			return true;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (item.Key == null) throw new ArgumentNullException("key");

			var Index = IndexOf(item);

			if (Index == -1)
				return false;

			var Value = _Values[Index];

			RemoveAt(Index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, Index);

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

			var Key = _Keys[index];
			var Value = _Values[index];

			_Size--;

			// If this is in the middle, move the values down
			if (index < _Size)
			{
				Array.Copy(_Keys, index + 1, _Keys, index, _Size - index);
				Array.Copy(_Values, index + 1, _Values, index, _Size - index);
			}

			// Ensure we don't hold a reference to the value
			_Values[_Size] = default(TValue);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(Key, Value), index);
		}

		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key == null) throw new ArgumentNullException("key");

			var Index = IndexOfKey(key);

			if (Index == -1)
			{
				value = default(TValue);
				return false;
			}

			value = _Values[Index];

			return true;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return new KeyValueEnumerator(this);
		}

		//****************************************

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new KeyValueEnumerator(this);
		}

		int IList.Add(object value)
		{	//****************************************
			int Index;
			KeyValuePair<TKey, TValue> NewValue;
			//****************************************

			if (!(value is KeyValuePair<TKey, TValue>))
				throw new ArgumentException("Value is not of the required type");

			NewValue = (KeyValuePair<TKey, TValue>)value;

			Index = Insert(NewValue);

			if (Index != -1)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, NewValue, Index);

			return Index;
		}

		bool IList.Contains(object value)
		{
			return value is KeyValuePair<TKey, TValue> && Contains((KeyValuePair<TKey, TValue>)value);
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			Array.Copy(_Values, 0, array, arrayIndex, _Size);
		}

		int IList.IndexOf(object value)
		{
			if (value is KeyValuePair<TKey, TValue>)
				return IndexOf((KeyValuePair<TKey, TValue>)value);

			return -1;
		}

		void IList<KeyValuePair<TKey, TValue>>.Insert(int index, KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException("Cannot insert into a dictionary");
		}

		void IList.Insert(int index, object value)
		{
			throw new NotSupportedException();
		}

		void IList.Remove(object value)
		{
			if (value is KeyValuePair<TKey, TValue>)
				Remove((KeyValuePair<TKey, TValue>)value);
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

			Capacity = num;
		}

		private int Insert(KeyValuePair<TKey, TValue> item)
		{	//****************************************
			int Index = Array.BinarySearch<TKey>(_Keys, 0, _Size, item.Key, _Comparer);
			//****************************************

			// Is there a matching key?
			if (Index >= 0)
				return -1; // Yes, so reject

			// No matching item, insert at the nearest spot above
			Insert(~Index, item);

			return ~Index;
		}

		private void SetKey(TKey key, TValue value)
		{	//****************************************
			int Index = Array.BinarySearch<TKey>(_Keys, 0, _Size, key, _Comparer);
			var NewValue = new KeyValuePair<TKey, TValue>(key, value);
			//****************************************

			// Is there a matching key?
			if (Index >= 0)
			{
				var OldValue = _Values[Index];

				// Key already exists. Replace the value if it's different
				if (!Equals(OldValue, value))
				{
					_Values[Index] = value;

					OnCollectionChanged(NotifyCollectionChangedAction.Replace, NewValue, new KeyValuePair<TKey, TValue>(key, OldValue), Index);
				}

				return;
			}

			// No matching item, insert at the nearest spot above
			Index = ~Index;

			Insert(Index, NewValue);

			OnCollectionChanged(NotifyCollectionChangedAction.Add, NewValue, Index);
		}

		private void Insert(int index, KeyValuePair<TKey, TValue> item)
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

			_Keys[index] = item.Key;
			_Values[index] = item.Value;

			_Size++;
		}

		private void OnPropertyChanged()
		{
			OnPropertyChanged(CountString);
			OnPropertyChanged(IndexerName);
			OnPropertyChanged(KeysName);
			OnPropertyChanged(ValuesName);
		}

		private void OnCollectionChanged()
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			_KeyCollection.OnCollectionChanged();
			_ValueCollection.OnCollectionChanged();
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem));

			_KeyCollection.OnCollectionChanged(action, changedItem.Key);
			_ValueCollection.OnCollectionChanged(action, changedItem.Value);
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem, int index)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));

			_KeyCollection.OnCollectionChanged(action, changedItem.Key, index);
			_ValueCollection.OnCollectionChanged(action, changedItem.Value, index);
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem, int index)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));

			_KeyCollection.OnCollectionChanged(action, newItem.Key, oldItem.Key, index);
			_ValueCollection.OnCollectionChanged(action, newItem.Value, oldItem.Value, index);
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> newItems)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItems.ToArray()));

			_KeyCollection.OnCollectionChanged(action, newItems.Select(pair => pair.Key));
			_ValueCollection.OnCollectionChanged(action, newItems.Select(pair => pair.Value));
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
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public ObservableDictionaryCollection<TKey> Keys
		{
			get { return _KeyCollection; }
		}

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public ObservableDictionaryCollection<TValue> Values
		{
			get { return _ValueCollection; }
		}

		/// <summary>
		/// Gets/Sets the value corresponding to the provided key
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[TKey key]
		{
			get
			{
				TValue ResultValue;

				if (TryGetValue(key, out ResultValue))
					return ResultValue;

				throw new KeyNotFoundException();
			}
			set { SetKey(key, value); }
		}

		/// <summary>
		/// Gets whether an update is in progress via <see cref="BeginUpdate" /> and <see cref="EndUpdate" />
		/// </summary>
		public bool IsUpdating
		{
			get { return _UpdateCount != 0; }
		}

		/// <summary>
		/// Gets/Sets the number of elements that the Observable Dictionary can contain.
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
					_Keys = new TKey[0];
					_Values = new TValue[0];

					return;
				}
				var NewKeys = new TKey[value];
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
		/// Gets the comparer being used for the Key
		/// </summary>
		public IComparer<TKey> Comparer
		{
			get { return _Comparer; }
		}

		ICollection<TKey> IDictionary<TKey, TValue>.Keys
		{
			get { return _KeyCollection; }
		}

		ICollection<TValue> IDictionary<TKey, TValue>.Values
		{
			get { return _ValueCollection; }
		}

		KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index]
		{
			get { return new KeyValuePair<TKey, TValue>(_Keys[index], _Values[index]); }
			set
			{
				if (index < 0 || index >= _Size)
					throw new ArgumentOutOfRangeException("index");

				// Are we changing the key?
				var OldKey = _Keys[index];

				if (!Equals(OldKey, value.Key))
					throw new InvalidOperationException();

				var OldValue = _Values[index];

				if (Equals(OldValue, value.Value))
					return;

				_Values[index] = value.Value;

				OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, new KeyValuePair<TKey, TValue>(OldKey, OldValue), index);
			}
		}

		object IList.this[int index]
		{
			get { return _Values[index]; }
			set { throw new NotSupportedException("List is read-only"); }
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

		//****************************************

		private class KeyCollection : IList<TKey>
		{	//****************************************
			private readonly ObservableSortedList<TKey, TValue> _Parent;
			//****************************************

			public KeyCollection(ObservableSortedList<TKey, TValue> parent)
			{
				_Parent = parent;
			}

			//****************************************

			int IList<TKey>.IndexOf(TKey item)
			{
				return _Parent.IndexOfKey(item);
			}

			void IList<TKey>.Insert(int index, TKey item)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			void IList<TKey>.RemoveAt(int index)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			void ICollection<TKey>.Add(TKey item)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			void ICollection<TKey>.Clear()
			{
				throw new NotSupportedException("Collection is read-only");
			}

			bool ICollection<TKey>.Contains(TKey item)
			{
				return _Parent.ContainsKey(item);
			}

			void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
			{
				var MyKeys = _Parent._Keys;
				var MySize = _Parent._Size;

				for (int Index = 0; Index < MySize; Index++)
				{
					array[arrayIndex++] = MyKeys[Index];
				}
			}

			bool ICollection<TKey>.Remove(TKey item)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
			{
				return new KeyEnumerator(_Parent);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return new KeyEnumerator(_Parent);
			}

			TKey IList<TKey>.this[int index]
			{
				get { return _Parent._Keys[index]; }
				set { throw new NotSupportedException("Collection is read-only"); }
			}

			int ICollection<TKey>.Count
			{
				get { return _Parent._Size; }
			}

			bool ICollection<TKey>.IsReadOnly
			{
				get { return true; }
			}
		}

		private class ValueCollection : IList<TValue>
		{	//****************************************
			private readonly ObservableSortedList<TKey, TValue> _Parent;
			//****************************************

			public ValueCollection(ObservableSortedList<TKey, TValue> parent)
			{
				_Parent = parent;
			}

			//****************************************

			int IList<TValue>.IndexOf(TValue item)
			{
				return _Parent.IndexOfValue(item);
			}

			void IList<TValue>.Insert(int index, TValue item)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			void IList<TValue>.RemoveAt(int index)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			void ICollection<TValue>.Add(TValue item)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			void ICollection<TValue>.Clear()
			{
				throw new NotSupportedException("Collection is read-only");
			}

			bool ICollection<TValue>.Contains(TValue item)
			{
				return _Parent.ContainsValue(item);
			}

			void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
			{
				var MyValues = _Parent._Values;
				var MySize = _Parent._Size;

				for (int Index = 0; Index < MySize; Index++)
				{
					array[arrayIndex++] = MyValues[Index];
				}
			}

			bool ICollection<TValue>.Remove(TValue item)
			{
				throw new NotSupportedException("Collection is read-only");
			}

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
			{
				return new ValueEnumerator(_Parent);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return new ValueEnumerator(_Parent);
			}

			TValue IList<TValue>.this[int index]
			{
				get { return _Parent._Values[index]; }
				set { throw new NotSupportedException("Collection is read-only"); }
			}

			int ICollection<TValue>.Count
			{
				get { return _Parent._Size; }
			}

			bool ICollection<TValue>.IsReadOnly
			{
				get { return true; }
			}
		}

		private sealed class KeyValueEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator
		{	//****************************************
			private readonly ObservableSortedList<TKey, TValue> _Parent;

			private int _Index;
			private KeyValuePair<TKey, TValue> _Current;
			//****************************************

			public KeyValueEnumerator(ObservableSortedList<TKey, TValue> parent)
			{
				_Parent = parent;
			}

			//****************************************

			public void Dispose()
			{
				_Current = default(KeyValuePair<TKey, TValue>);
			}

			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					_Current = default(KeyValuePair<TKey, TValue>);

					return false;
				}

				_Current = new KeyValuePair<TKey,TValue>(_Parent._Keys[_Index], _Parent._Values[_Index]);

				_Index++;

				return true;
			}

			public void Reset()
			{
				_Index = 0;
				_Current = default(KeyValuePair<TKey, TValue>);
			}

			//****************************************

			public KeyValuePair<TKey, TValue> Current
			{
				get { return _Current; }
			}

			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}

		private sealed class KeyEnumerator : IEnumerator<TKey>, IEnumerator
		{	//****************************************
			private readonly ObservableSortedList<TKey, TValue> _Parent;

			private int _Index;
			private TKey _Current;
			//****************************************

			public KeyEnumerator(ObservableSortedList<TKey, TValue> parent)
			{
				_Parent = parent;
			}

			//****************************************

			public void Dispose()
			{
				_Current = default(TKey);
			}

			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					_Current = default(TKey);

					return false;
				}

				_Current = _Parent._Keys[_Index++];

				return true;
			}

			public void Reset()
			{
				_Index = 0;
				_Current = default(TKey);
			}

			//****************************************

			public TKey Current
			{
				get { return _Current; }
			}

			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}

		private sealed class ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{	//****************************************
			private readonly ObservableSortedList<TKey, TValue> _Parent;

			private int _Index;
			private TValue _Current;
			//****************************************

			public ValueEnumerator(ObservableSortedList<TKey, TValue> parent)
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