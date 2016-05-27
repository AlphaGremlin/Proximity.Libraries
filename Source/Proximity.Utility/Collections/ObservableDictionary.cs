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
	/// Implements an Observable Dictionary for WPF binding
	/// </summary>
	public class ObservableDictionary<TKey, TValue> : ObservableBase<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>
	{	//****************************************
		private const string KeysName = "Keys";
		private const string ValuesName = "Values";
		//****************************************
		private int[] _Keys;
		private KeyValuePair<TKey, TValue>[] _Values;
		private readonly IEqualityComparer<TKey> _Comparer;

		private readonly KeyCollection _KeyCollection;
		private readonly ValueCollection _ValueCollection;
		
		private int _Size = 0;
		//****************************************

		/// <summary>
		/// Creates a new, empty observable dictionary
		/// </summary>
		public ObservableDictionary() : this(0, EqualityComparer<TKey>.Default)
		{
		}

		/// <summary>
		/// Creates a new pre-filled observable dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		public ObservableDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, EqualityComparer<TKey>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty observable dictionary with the specified default capacity
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		public ObservableDictionary(int capacity) : this(capacity, EqualityComparer<TKey>.Default)
		{
		}
		
		/// <summary>
		/// Creates a new empty observable dictionary with the specified comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new pre-filled observable dictionary with the specified comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
		{
			_Comparer = comparer;

			// Ensure we add all the values in the order sorted by hash code
#if PORTABLE
			_Keys = new int[0];
			_Values = new KeyValuePair<TKey, TValue>[0];

			// Can't use Array.Sort(key, value, comparer) since it doesn't exist on portable
			foreach (var MyPair in dictionary.Select(pair => new KeyValuePair<int, KeyValuePair<TKey, TValue>>(_Comparer.GetHashCode(pair.Key), pair)).OrderBy(pair => pair.Key))
			{
				if (_Size == _Keys.Length)
					EnsureCapacity(_Size + 1);

				_Keys[_Size] = MyPair.Key;
				_Values[_Size] = MyPair.Value;

				_Size++;
			}
#else
			_Size = dictionary.Count;
			_Keys = new int[_Size];
			_Values = new KeyValuePair<TKey, TValue>[_Size];

			dictionary.CopyTo(_Values, 0);

			for (int Index = 0; Index < _Values.Length; Index++)
			{
				_Keys[Index] = comparer.GetHashCode(_Values[Index].Key);
			}

			Array.Sort<int, KeyValuePair<TKey, TValue>>(_Keys, _Values, Comparer<int>.Default);
#endif

			_KeyCollection = new KeyCollection(this);
			_ValueCollection = new ValueCollection(this);
		}

		/// <summary>
		/// Creates a new empty observable dictionary with the specified comparer
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
		{
			_Keys = new int[capacity];
			_Values = new KeyValuePair<TKey, TValue>[capacity];
			_Comparer = comparer;

			_KeyCollection = new KeyCollection(this);
			_ValueCollection = new ValueCollection(this);
		}
		
		//****************************************

		/// <summary>
		/// Adds a new element the Dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		public void Add(TKey key, TValue value)
		{
			Add(new KeyValuePair<TKey, TValue>(key, value));
		}

		/// <inheritdoc />
		public override void Add(KeyValuePair<TKey, TValue> item)
		{	//****************************************
			int Index = Insert(item);
			//****************************************

			if (Index == -1)
				throw new ArgumentException("An item with the same key has already been added.");

			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, Index);
		}

		/// <inheritdoc />
		public override void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
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

		/// <inheritdoc />
		public override void Clear()
		{
			if (_Size > 0)
			{
				Array.Clear(_Values, 0, _Size);

				_Size = 0;

				OnCollectionChanged();
			}
		}

		/// <inheritdoc />
		public override bool Contains(KeyValuePair<TKey, TValue> item)
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

		/// <inheritdoc />
		public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			Array.Copy(_Values, 0, array, arrayIndex, _Size);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public KeyValueEnumerator GetEnumerator()
		{
			return new KeyValueEnumerator(this);
		}

		/// <inheritdoc />
		public override int IndexOf(KeyValuePair<TKey, TValue> item)
		{
			var MyIndex = IndexOfKey(item.Key);

			// If we found the Key, make sure the Item matches
			if (MyIndex != -1 && !Equals(_Values[MyIndex].Value, item.Value))
				MyIndex = -1;

			return MyIndex;
		}

		/// <summary>
		/// Searches for the specified key and returns the zero-based index within the ObservableDictionary
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if a matching key was found, otherwise -1</returns>
		public int IndexOfKey(TKey key)
		{	//****************************************
			int KeyHash = _Comparer.GetHashCode(key);
			int Index = Array.BinarySearch<int>(_Keys, 0, _Size, KeyHash);
			//****************************************

			// Is there a matching hash code?
			if (Index < 0)
				return -1;

			// BinarySearch is not guaranteed to return the first matching value, so we may need to move back
			while (Index > 0 && _Keys[Index - 1] == KeyHash)
				Index--;

			for (; ; )
			{
				// Do we match this item?
				if (_Comparer.Equals(_Values[Index].Key, key))
					return Index; // Yes, so return the Index

				Index++;

				// Are we at the end of the list?
				if (Index == _Size)
					return -1; // Yes, so we didn't find the item

				// Is there another item with the same key?
				if (_Keys[Index] != KeyHash)
					return -1; // Nope, so we didn't find the item

				// Yes, so loop back and check that
			}
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
				if (Equals(_Values[Index].Value, value))
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

			RemoveAt(Index);

			return true;
		}

		/// <inheritdoc />
		public override bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (item.Key == null) throw new ArgumentNullException("key");

			var Index = IndexOf(item);

			if (Index == -1)
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <inheritdoc />
		public override int RemoveAll(Predicate<KeyValuePair<TKey, TValue>> predicate)
		{
			int Index = 0;

			// Find the first item we need to remove
			while (Index < _Size && !predicate(_Values[Index]))
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return 0;

			var RemovedItems = new List<KeyValuePair<TKey, TValue>>();

			RemovedItems.Add(_Values[Index]);

			int InnerIndex = Index + 1;

			while (InnerIndex < _Size)
			{
				// Skip the items we need to remove
				while (InnerIndex < _Size && predicate(_Values[InnerIndex]))
				{
					RemovedItems.Add(_Values[InnerIndex]);

					InnerIndex++;
				}

				// If we reached the end, abort
				if (InnerIndex >= _Size)
					break;

				// We found one we're not removing, so move it up
				_Keys[Index] = _Keys[InnerIndex];
				_Values[Index] = _Values[InnerIndex];

				Index++;
				InnerIndex++;
			}

			// Clear the removed items
			Array.Clear(_Keys, Index, _Size - Index);
			Array.Clear(_Values, Index, _Size - Index);

			_Size = Index;

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, RemovedItems);

			return InnerIndex - Index;
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
			_Values[_Size] = default(KeyValuePair<TKey, TValue>);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, Item, index);
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

			value = _Values[Index].Value;

			return true;
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
			int KeyHash = _Comparer.GetHashCode(item.Key);
			int Index = Array.BinarySearch<int>(_Keys, 0, _Size, KeyHash);
			//****************************************

			// Is there a matching hash code?
			if (Index >= 0)
			{
				// BinarySearch is not guaranteed to return the first matching value, so we may need to move back
				while (Index > 0 && _Keys[Index - 1] == KeyHash)
					Index--;

				for (; ; )
				{
					// Do we match this item?
					if (_Comparer.Equals(_Values[Index].Key, item.Key))
						return -1; // Yes, so the key already exists

					// Are we at the end of the list?
					if (Index == _Size - 1)
						break; // Yes, so we need to insert the new item at the end

					// Is there another item with the same key?
					if (_Keys[Index + 1] != KeyHash)
						break; // Nope, so we can insert the new item at the current Index

					// Yes, so loop back and check that
					Index++;
				}

				Insert(Index, KeyHash, item);

				return Index;
			}

			// No matching item, insert at the nearest spot above
			Insert(~Index, KeyHash, item);

			return ~Index;
		}

		private void SetKey(TKey key, TValue value)
		{	//****************************************
			int KeyHash = _Comparer.GetHashCode(key);
			int Index = Array.BinarySearch<int>(_Keys, 0, _Size, KeyHash);
			var NewValue = new KeyValuePair<TKey, TValue>(key, value);
			//****************************************

			// Is there a matching hash code?
			if (Index >= 0)
			{
				// BinarySearch is not guaranteed to return the first matching value, so we may need to move back
				while (Index > 0 && _Keys[Index - 1] == KeyHash)
					Index--;

				for (; ; )
				{
					// Do we match this item?
					if (_Comparer.Equals(_Values[Index].Key, key))
					{
						var OldValue = _Values[Index];

						// Yes, so the key already exists. Replace the value if it's different
						if (!Equals(OldValue.Value, value))
						{
							_Values[Index] = NewValue;

							OnCollectionChanged(NotifyCollectionChangedAction.Replace, NewValue, OldValue, Index);
						}

						return;
					}

					// Are we at the end of the list?
					if (Index == _Size - 1)
						break; // Yes, so we need to insert the new item at the end

					// Is there another item with the same key?
					if (_Keys[Index + 1] != KeyHash)
						break; // Nope, so we can insert the new item at the current Index

					// Yes, so loop back and check that
					Index++;
				}
			}
			else
			{
				// No matching item, insert at the nearest spot above
				Index = ~Index;
			}

			Insert(Index, KeyHash, NewValue);

			OnCollectionChanged(NotifyCollectionChangedAction.Add, NewValue, Index);
		}

		private void Insert(int index, int key, KeyValuePair<TKey, TValue> item)
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
			_Values[index] = item;

			_Size++;
		}

		//****************************************

		/// <inheritdoc />
		protected override void OnPropertyChanged()
		{
			base.OnPropertyChanged();

			OnPropertyChanged(KeysName);
			OnPropertyChanged(ValuesName);
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged()
		{
			base.OnCollectionChanged();

			if (IsUpdating)
				return;

			_KeyCollection.OnCollectionChanged();
			_ValueCollection.OnCollectionChanged();
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem, int index)
		{
			base.OnCollectionChanged(action, changedItem, index);

			if (IsUpdating)
				return;

			_KeyCollection.OnCollectionChanged(action, changedItem.Key, index);
			_ValueCollection.OnCollectionChanged(action, changedItem.Value, index);
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem, int index)
		{
			base.OnCollectionChanged(action, newItem, oldItem, index);

			if (IsUpdating)
				return;

			_KeyCollection.OnCollectionChanged(action, newItem.Key, oldItem.Key, index);
			_ValueCollection.OnCollectionChanged(action, newItem.Value, oldItem.Value, index);
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> changedItems)
		{
			base.OnCollectionChanged(action, changedItems);

			if (IsUpdating)
				return;

			_KeyCollection.OnCollectionChanged(action, changedItems.Select(pair => pair.Key));
			_ValueCollection.OnCollectionChanged(action, changedItems.Select(pair => pair.Value));
		}

		/// <inheritdoc />
		protected override KeyValuePair<TKey, TValue> InternalGet(int index)
		{
			if (index < 0 || index >= _Size)
				throw new ArgumentOutOfRangeException("index");

			return _Values[index];
		}

		/// <inheritdoc />
		protected override IEnumerator<KeyValuePair<TKey, TValue>> InternalGetEnumerator()
		{
			return new KeyValueEnumerator(this);
		}

		/// <inheritdoc />
		protected override void InternalInsert(int index, KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException("Cannot insert into a dictionary");
		}

		/// <inheritdoc />
		protected override void InternalRemoveAt(int index)
		{
			RemoveAt(index);
		}

		/// <inheritdoc />
		protected override void InternalSet(int index, KeyValuePair<TKey, TValue> value)
		{
			if (index < 0 || index >= _Size)
				throw new ArgumentOutOfRangeException("index");

			var OldValue = _Values[index];

			if (!Equals(OldValue.Key, value.Key))
				throw new InvalidOperationException();

			if (Equals(OldValue.Value, value.Value))
				return;

			_Values[index] = value;

			OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, OldValue, index);
		}

		//****************************************

		/// <inheritdoc />
		public override int Count
		{
			get { return _Size; }
		}

		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public KeyCollection Keys
		{
			get { return _KeyCollection; }
		}

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public ValueCollection Values
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
					_Keys = new int[0];
					_Values = new KeyValuePair<TKey, TValue>[0];

					return;
				}
				var NewKeys = new int[value];
				var NewValues = new KeyValuePair<TKey, TValue>[value];

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
		/// Gets the equality comparer being used for the Key
		/// </summary>
		public IEqualityComparer<TKey> Comparer
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

		//****************************************

		/// <summary>
		/// Provides an observable collection over the Dictionary Keys
		/// </summary>
		public sealed class KeyCollection : ObservableDictionaryCollection<TKey>
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;
			//****************************************

			internal KeyCollection(ObservableDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
			}

			//****************************************

			/// <inheritdoc />
			public override bool Contains(TKey item)
			{
				return _Parent.ContainsKey(item);
			}

			/// <inheritdoc />
			public override void CopyTo(TKey[] array, int arrayIndex)
			{	//****************************************
				var MyValues = _Parent._Values;
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array[arrayIndex++] = MyValues[Index].Key;
				}
			}

			/// <inheritdoc />
			public new KeyEnumerator GetEnumerator()
			{
				return new KeyEnumerator(_Parent);
			}

			/// <inheritdoc />
			public override int IndexOf(TKey item)
			{
				return _Parent.IndexOfKey(item);
			}

			//****************************************

			internal override void InternalCopyTo(Array array, int arrayIndex)
			{	//****************************************
				var MyValues = _Parent._Values;
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array.SetValue(MyValues[Index].Key, arrayIndex++);
				}
			}

			internal override IEnumerator<TKey> InternalGetEnumerator()
			{
				return new KeyEnumerator(_Parent);
			}

			//****************************************

			/// <inheritdoc />
			public override TKey this[int index]
			{
				get { return _Parent._Values[index].Key; }
			}

			/// <inheritdoc />
			public override int Count
			{
				get { return _Parent._Size; }
			}
		}

		/// <summary>
		/// Provides an observable collection over the Dictionary Values
		/// </summary>
		public sealed class ValueCollection : ObservableDictionaryCollection<TValue>
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;
			//****************************************

			internal ValueCollection(ObservableDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
			}

			//****************************************

			/// <inheritdoc />
			public override bool Contains(TValue item)
			{
				return _Parent.ContainsValue(item);
			}

			/// <inheritdoc />
			public override void CopyTo(TValue[] array, int arrayIndex)
			{	//****************************************
				var MyValues = _Parent._Values;
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array[arrayIndex++] = MyValues[Index].Value;
				}
			}

			/// <inheritdoc />
			public new ValueEnumerator GetEnumerator()
			{
				return new ValueEnumerator(_Parent);
			}

			/// <inheritdoc />
			public override int IndexOf(TValue item)
			{
				return _Parent.IndexOfValue(item);
			}

			//****************************************

			internal override void InternalCopyTo(Array array, int arrayIndex)
			{	//****************************************
				var MyValues = _Parent._Values;
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array.SetValue(MyValues[Index].Value, arrayIndex++);
				}
			}

			internal override IEnumerator<TValue> InternalGetEnumerator()
			{
				return new ValueEnumerator(_Parent);
			}

			//****************************************

			/// <inheritdoc />
			public override TValue this[int index]
			{
				get { return _Parent._Values[index].Value; }
			}

			/// <inheritdoc />
			public override int Count
			{
				get { return _Parent._Size; }
			}
		}

		/// <summary>
		/// Enumerates the dictionary while avoiding memory allocations
		/// </summary>
		public struct KeyValueEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;

			private int _Index;
			private KeyValuePair<TKey, TValue> _Current;
			//****************************************

			internal KeyValueEnumerator(ObservableDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
				_Index = 0;
				_Current = default(KeyValuePair<TKey, TValue>);
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Current = default(KeyValuePair<TKey, TValue>);
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					_Current = default(KeyValuePair<TKey, TValue>);

					return false;
				}

				_Current = _Parent._Values[_Index++];

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				_Current = default(KeyValuePair<TKey, TValue>);
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public KeyValuePair<TKey, TValue> Current
			{
				get { return _Current; }
			}

			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}

		/// <summary>
		/// Enumerates the dictionary keys while avoiding memory allocations
		/// </summary>
		public struct KeyEnumerator : IEnumerator<TKey>, IEnumerator
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;

			private int _Index;
			private TKey _Current;
			//****************************************

			internal KeyEnumerator(ObservableDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
				_Index = 0;
				_Current = default(TKey);
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Current = default(TKey);
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					_Current = default(TKey);

					return false;
				}

				_Current = _Parent._Values[_Index++].Key;

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				_Current = default(TKey);
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public TKey Current
			{
				get { return _Current; }
			}

			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}

		/// <summary>
		/// Enumerates the dictionary values while avoiding memory allocations
		/// </summary>
		public struct ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;

			private int _Index;
			private TValue _Current;
			//****************************************

			internal ValueEnumerator(ObservableDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
				_Index = 0;
				_Current = default(TValue);
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Current = default(TValue);
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
				{
					_Index = _Parent._Size + 1;
					_Current = default(TValue);

					return false;
				}

				_Current = _Parent._Values[_Index++].Value;

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				_Current = default(TValue);
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
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