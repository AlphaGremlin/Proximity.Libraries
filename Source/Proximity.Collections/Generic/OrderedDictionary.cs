using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Proximity.Collections;

namespace System.Collections.Generic
{
	/// <summary>
	/// Provides a Dictionary with O(1) key lookups maintained in value-sorted order for O(LogN) value lookups and O(1) indexing
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public sealed partial class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, IReadOnlyList<KeyValuePair<TKey, TValue>>, IList, IDictionary where TKey : notnull
	{ //****************************************
		private const int HashCodeMask = 0x7FFFFFFF;
		//****************************************
		private int[] _Buckets;
		private int[] _Sorting;

		private Entry[] _Entries;
		private int _Size;
		//****************************************

		/// <summary>
		/// Creates a new, empty ordered dictionary
		/// </summary>
		public OrderedDictionary() : this(0, EqualityComparer<TKey>.Default, Comparer<TValue>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty ordered dictionary with the specified default capacity
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		public OrderedDictionary(int capacity) : this(capacity, EqualityComparer<TKey>.Default, Comparer<TValue>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty ordered dictionary with the specified comparer
		/// </summary>
		/// <param name="keyComparer">The equality comparer to use for keys</param>
		/// <param name="valueComparer">The comparer to use for values</param>
		public OrderedDictionary(IEqualityComparer<TKey> keyComparer, IComparer<TValue> valueComparer) : this(0, keyComparer, valueComparer)
		{
		}

		/// <summary>
		/// Creates a new pre-filled ordered dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary) : this(dictionary, EqualityComparer<TKey>.Default, Comparer<TValue>.Default)
		{
		}

		/// <summary>
		/// Creates a new pre-filled ordered dictionary with the specified comparers
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="keyComparer">The equality comparer to use for keys</param>
		/// <param name="valueComparer">The comparer to use for values</param>
		public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, IEqualityComparer<TKey> keyComparer, IComparer<TValue> valueComparer)
		{
			KeyComparer = keyComparer;
			ValueComparer = valueComparer;

			_Size = dictionary.Count();
			var Capacity = HashUtil.GetPrime(_Size);

			_Buckets = new int[Capacity];
			_Sorting = new int[Capacity];
			_Entries = new Entry[Capacity];

			{
				var Index = 0;

				foreach (var MyPair in dictionary)
				{
					ref var Entry = ref _Entries[Index++];

					Entry.Item = MyPair;
					Entry.HashCode = keyComparer.GetHashCode(MyPair.Key) & HashCodeMask;
				}
			}

			// Get everything into HashCode order
			Array.Sort(_Entries, 0, _Size);

			// Check the new items don't have any duplicates
			VerifyDistinct(_Entries, _Size, keyComparer);

			// Sort into value order
			Array.Sort(_Entries, 0, _Size, new EntryValueComparer(valueComparer));

			// Index by hashcode
			Reindex(_Buckets, _Entries, 0, _Size);

			// Sorting is now easy - it's one-to-one
			for (var Index = 0; Index < _Size; Index++)
				_Sorting[Index] = Index;

			Keys = new KeyCollection(this);
			Values = new ValueCollection(this);
		}

		/// <summary>
		/// Creates a new empty ordered dictionary with the specified comparers
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		/// <param name="keyComparer">The equality comparer to use for keys</param>
		/// <param name="valueComparer">The comparer to use for values</param>
		public OrderedDictionary(int capacity, IEqualityComparer<TKey> keyComparer, IComparer<TValue> valueComparer)
		{
			capacity = HashUtil.GetPrime(capacity);

			_Buckets = new int[capacity];
			_Sorting = new int[capacity];
			_Entries = new Entry[capacity];
			KeyComparer = keyComparer;
			ValueComparer = valueComparer;

			Keys = new KeyCollection(this);
			Values = new ValueCollection(this);
		}

		//****************************************

		/// <summary>
		/// Adds a new element the Dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		/// <returns>The index at which the element was inserted</returns>
		public int Add(TKey key, TValue value) => Add(new KeyValuePair<TKey, TValue>(key, value));

		/// <summary>
		/// Adds a new element to the Dictionary
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <returns>The index at which the element was inserted</returns>
		public int Add(KeyValuePair<TKey, TValue> item)
		{
			if (!TryAdd(item, ReplaceMode.AddOnly, out var Index, out _))
				throw new ArgumentException("An item with the same key has already been added.");

			return Index;
		}

		/// <summary>
		/// Adds a range of elements to the dictionary
		/// </summary>
		/// <param name="items">The elements to add</param>
		/// <exception cref="ArgumentException">The input elements have duplicated keys, or the key already exists in the dictionary</exception>
		public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			// Gather all the items to add, calculating their keys as we go
			var NewItems = items.Select(pair => new Entry { HashCode = KeyComparer.GetHashCode(pair.Key) & HashCodeMask, Item = pair }).ToArray();

			if (NewItems.Length == 0)
				return;

			// Get everything into HashCode order
			Array.Sort(NewItems);

			//****************************************

			var InsertIndex = _Size;
			var Entries = _Entries;

			// Check the new items don't have any duplicates
			VerifyDistinct(NewItems, NewItems.Length, KeyComparer);

			// No duplicates in the new items. Check the keys aren't already in the Dictionary
			if (_Size > 0)
			{
				for (var Index = 0; Index < NewItems.Length; Index++)
				{
					var TotalCollisions = 0;
					var Item = NewItems[Index];

					// Find the bucket we belong to
					ref var Bucket = ref _Buckets[Item.HashCode % _Buckets.Length];
					var EntryIndex = Bucket - 1;

					// Check for collisions
					while (EntryIndex >= 0)
					{
						if (Entries[EntryIndex].HashCode == Item.HashCode && KeyComparer.Equals(Entries[EntryIndex].Key, Item.Key))
							throw new ArgumentException("An item with the same key has already been added.");

						EntryIndex = Entries[EntryIndex].NextIndex;

						if (TotalCollisions++ >= InsertIndex)
							throw new InvalidOperationException("State invalid");
					}
				}
			}

			//****************************************

			// Ensure we have enough space for the new items
			EnsureCapacity(_Size + NewItems.Length);
			Entries = _Entries;

			// Add the new items
			for (var Index = 0; Index < NewItems.Length; Index++)
			{
				ref var Entry = ref NewItems[Index];

				Entries[_Size] = Entry;

				// Store the sorting value
				var SortIndex = BinarySearch(_Sorting, _Entries, _Size, Entry.Value, ValueComparer);

				if (SortIndex < 0)
					SortIndex = ~SortIndex;

				Insert(_Sorting, _Size, SortIndex, _Size);

				_Size++;
			}

			Reindex(_Buckets, Entries, InsertIndex, InsertIndex + NewItems.Length);
		}

		/// <summary>
		/// Clears all elements from the Dictionary
		/// </summary>
		public void Clear()
		{
			if (_Size > 0)
			{
				Array.Clear(_Buckets, 0, _Buckets.Length);
				Array.Clear(_Sorting, 0, _Sorting.Length);
				Array.Clear(_Entries, 0, _Size);

				_Size = 0;
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TValue> item) => IndexOf(item) != -1;

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if there is an element with this key, otherwise false</returns>
		public bool ContainsKey(TKey key) => EntryIndexOfKey(key) >= 0;

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified value
		/// </summary>
		/// <param name="value">The value to search for</param>
		/// <returns>True if there is an element with this value, otherwise false</returns>
		public bool ContainsValue(TValue value) => IndexOfValue(value) >= 0;

		/// <summary>
		/// Copies the contents of the Dictionary to an array
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The offset to start copying to</param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (arrayIndex + _Size > array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			for (var Index = 0; Index < _Size; Index++)
			{
				array[arrayIndex + Index] = _Entries[Index].Item;
			}
		}

		/// <summary>
		/// Gets the key/value pair at the given index
		/// </summary>
		/// <param name="index">The index to retrieve</param>
		/// <returns>The key/value pair at the requested index</returns>
		public KeyValuePair<TKey, TValue> Get(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			// Lookup by Sorting index
			return _Entries[_Sorting[index]].Item;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection in value-sorted order
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public Enumerator GetEnumerator() => new(this);

		/// <summary>
		/// Gets the key/value pair at the given index by reference
		/// </summary>
		/// <param name="index">The index to retrieve</param>
		/// <returns>They key/value pair at the requested index</returns>
		public ref readonly KeyValuePair<TKey, TValue> GetRef(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			// Lookup by Sorting index
			return ref _Entries[_Sorting[index]].Item;
		}

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="key">The key of the item to lookup</param>
		/// <param name="value">The value of the item to lookup</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TKey key, TValue value) => IndexOf(new(key, value));

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(KeyValuePair<TKey, TValue> item)
		{
			if (_Size == 0)
				return -1;

			// Lookup by Entry Index
			var EntryIndex = EntryIndexOfKey(item.Key);

			if (EntryIndex == -1)
				return -1;

			if (ValueComparer.Compare(_Entries[EntryIndex].Value, item.Value) != 0)
				return -1;

			// Translate to Sort index
			return SortIndexOfEntry(EntryIndex, item.Value);
		}

		/// <summary>
		/// Finds the index of a particular key
		/// </summary>
		/// <param name="key">The key to lookup</param>
		/// <returns>The index of the key, if found, otherwise -1</returns>
		public int IndexOfKey(TKey key)
		{
			if (_Size == 0)
				return -1;

			// Lookup by Entry Index
			var EntryIndex = EntryIndexOfKey(key);

			if (EntryIndex == -1)
				return -1;

			// Translate to Sort index
			return SortIndexOfEntry(EntryIndex, _Entries[EntryIndex].Value);
		}

		/// <summary>
		/// Finds the index of a particular value
		/// </summary>
		/// <param name="value">The value to lookup</param>
		/// <returns>The index of the value, if found, otherwise -1</returns>
		public int IndexOfValue(TValue value)
		{
			if (_Size == 0)
				return -1;

			// Lookup by Sorting index
			var Index = BinarySearch(_Sorting, _Entries, _Size, value, ValueComparer);

			if (Index < 0)
				return -1;

			return Index;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="key">The key of the element to remove</param>
		public bool Remove(TKey key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var Index = EntryIndexOfKey(key);

			if (Index == -1)
				return false;

			RemoveAtEntry(Index);

			return true;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			var Index = EntryIndexOfKey(item.Key);

			if (Index == -1 || ValueComparer.Compare(_Entries[Index].Value, item.Value) != 0)
				return false;

			RemoveAtEntry(Index);

			return true;
		}

		/// <summary>
		/// Removes the element at the specified index
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		public void RemoveAt(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			RemoveAt(_Sorting[index], index);
		}

		/// <summary>
		/// Removes the elements within the specified range
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		/// <param name="count">The number of items to remove after the index</param>
		public void RemoveRange(int index, int count)
		{
			var LastIndex = index + count - 1;

			if (index < 0 || LastIndex >= _Size)
				throw new ArgumentOutOfRangeException(nameof(index));

			// TODO: Could be optimised
			for (var Index = LastIndex; Index >= index; Index--)
				RemoveAt(_Sorting[Index], Index);
		}

		/// <inheritdoc />
		public KeyValuePair<TKey, TValue>[] ToArray()
		{
			var Copy = new KeyValuePair<TKey, TValue>[_Size];

			CopyTo(Copy, 0);

			return Copy;
		}

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="key">The key of the item</param>
		/// <param name="value">The value to associate with the key</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(TKey key, TValue value) => TryAdd(new KeyValuePair<TKey, TValue>(key, value), ReplaceMode.AddOnly, out _, out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<TKey, TValue> item) => TryAdd(item, ReplaceMode.AddOnly, out _, out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="key">The key of the item</param>
		/// <param name="value">The value to associate with the key</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(TKey key, TValue value, out int index) => TryAdd(new KeyValuePair<TKey, TValue>(key, value), ReplaceMode.AddOnly, out index, out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<TKey, TValue> item, out int index) => TryAdd(item, ReplaceMode.AddOnly, out index, out _);

		/// <summary>
		/// Searches the dictionary for a given key and returns the equal key, if any
		/// </summary>
		/// <param name="equalKey">The key to search for</param>
		/// <param name="actualKey">The key already in the collection, or the given key if not found</param>
		/// <returns>True if an equal key was found, otherwise False</returns>
		public bool TryGetKey(TKey equalKey,
#if !NETSTANDARD2_0 && !NET40
			[MaybeNullWhen(false)]
#endif
			out TKey actualKey)
		{
			var Index = EntryIndexOfKey(equalKey);

			if (Index > 0)
			{
				actualKey = _Entries[Index].Key;

				return true;
			}

			actualKey = equalKey;

			return false;
		}

		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key,
#if !NETSTANDARD && !NET40
			[MaybeNullWhen(false)]
#endif
			out TValue value)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var Index = EntryIndexOfKey(key);

			if (Index == -1)
			{
				value = default!;
				return false;
			}

			value = _Entries[Index].Value;

			return true;
		}

		/// <summary>
		/// Tries to remove an item from the dictionary
		/// </summary>
		/// <param name="key">The key of the item to remove</param>
		/// <param name="value">Receives the value of the removed item</param>
		/// <returns>True if the item was removed, otherwise False</returns>
		public bool TryRemove(TKey key,
#if !NETSTANDARD2_0 && !NET40
			[MaybeNullWhen(false)]
#endif
			out TValue value)
		{
			var Index = EntryIndexOfKey(key);

			if (Index == -1)
			{
				value = default!;
				return false;
			}

			value = _Entries[Index].Value;

			RemoveAtEntry(Index);

			return true;
		}

		/// <summary>
		/// Tries to update an item's value in the dictionary
		/// </summary>
		/// <param name="key">The key whose value to update</param>
		/// <param name="value">The new value to associate with the key</param>
		/// <returns>True if the key was found and the value was updated, otherwise False</returns>
		public bool TryUpdate(TKey key, TValue value) => TryAdd(new KeyValuePair<TKey, TValue>(key, value), ReplaceMode.ReplaceOnly, out _, out _);

		/// <summary>
		/// Tries to update an item's value in the dictionary
		/// </summary>
		/// <param name="item">The key/value pair to update</param>
		/// <returns>True if the key was found and the value was updated, otherwise False</returns>
		public bool TryUpdate(KeyValuePair<TKey, TValue> item) => TryAdd(item, ReplaceMode.ReplaceOnly, out _, out _);

		/// <summary>
		/// Tries to update an item's value in the dictionary
		/// </summary>
		/// <param name="key">The key whose value to update</param>
		/// <param name="value">The new value to associate with the key</param>
		/// <param name="index">Receives the new index of the item</param>
		/// <param name="previous">Receives the previous index of the item</param>
		/// <returns>True if the key was found and the value was updated, otherwise False</returns>
		public bool TryUpdate(TKey key, TValue value, out int index, out int previous) => TryAdd(new KeyValuePair<TKey, TValue>(key, value), ReplaceMode.ReplaceOnly, out index, out previous);

		/// <summary>
		/// Tries to update an item's value in the dictionary
		/// </summary>
		/// <param name="item">The key/value pair to update</param>
		/// <param name="index">Receives the new index of the item</param>
		/// <param name="previous">Receives the previous index of the item</param>
		/// <returns>True if the key was found and the value was updated, otherwise False</returns>
		public bool TryUpdate(KeyValuePair<TKey, TValue> item, out int index, out int previous) => TryAdd(item, ReplaceMode.ReplaceOnly, out index, out previous);

		//****************************************

		void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
		{
			if (!TryAdd(new KeyValuePair<TKey, TValue>(key, value), ReplaceMode.AddOnly, out _, out _))
				throw new ArgumentException("Key already exists", nameof(key));
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item);

		void IDictionary.Add(object? key, object? value)
		{
			if (key is not TKey MyKey)
				throw new ArgumentException("Not a supported key", nameof(key));

			if (value is not TValue MyValue)
				throw new ArgumentException("Not a supported value", nameof(value));

			Add(new KeyValuePair<TKey, TValue>(MyKey, MyValue));
		}

		bool IDictionary.Contains(object? value)
		{
			if (value is KeyValuePair<TKey, TValue> MyPair)
				return Contains(MyPair);

			if (value is DictionaryEntry MyEntry && MyEntry.Key is TKey MyKey && MyEntry.Value is TValue MyValue)
				return Contains(new KeyValuePair<TKey, TValue>(MyKey, MyValue));

			return false;
		}

		void IDictionary.Remove(object? value)
		{
			if (value is KeyValuePair<TKey, TValue> MyPair)
			{
				Remove(MyPair);
				return;
			}

			if (value is DictionaryEntry MyEntry && MyEntry.Key is TKey MyKey && MyEntry.Value is TValue MyValue)
			{
				Remove(new KeyValuePair<TKey, TValue>(MyKey, MyValue));
				return;
			}
		}

		int IList.Add(object? value)
		{
			if (value is KeyValuePair<TKey, TValue> MyPair)
			{
				if (TryAdd(MyPair, ReplaceMode.AddOnly, out var Index, out _))
					return Index;

				return -1;
			}

			if (value is DictionaryEntry MyEntry && MyEntry.Key is TKey MyKey && MyEntry.Value is TValue MyValue)
			{
				if (TryAdd(new KeyValuePair<TKey, TValue>(MyKey, MyValue), ReplaceMode.AddOnly, out var Index, out _))
					return Index;

				return -1;
			}

			throw new ArgumentException("Not a supported value", nameof(value));
		}

		bool IList.Contains(object? value)
		{
			if (value is KeyValuePair<TKey, TValue> MyPair)
				return Contains(MyPair);

			if (value is DictionaryEntry MyEntry && MyEntry.Key is TKey MyKey && MyEntry.Value is TValue MyValue)
				return Contains(new KeyValuePair<TKey, TValue>(MyKey, MyValue));

			return false;
		}

		int IList.IndexOf(object? value)
		{
			if (value is KeyValuePair<TKey, TValue> MyPair)
				return IndexOf(MyPair);

			if (value is DictionaryEntry MyEntry && MyEntry.Key is TKey MyKey && MyEntry.Value is TValue MyValue)
				return IndexOf(new KeyValuePair<TKey, TValue>(MyKey, MyValue));

			return -1;
		}

		void IList.Remove(object? value)
		{
			if (value is KeyValuePair<TKey, TValue> MyPair)
			{
				Remove(MyPair);
				return;
			}

			if (value is DictionaryEntry MyEntry && MyEntry.Key is TKey MyKey && MyEntry.Value is TValue MyValue)
			{
				Remove(new KeyValuePair<TKey, TValue>(MyKey, MyValue));
				return;
			}
		}

		void ICollection.CopyTo(Array array, int index) => CopyTo((KeyValuePair<TKey, TValue>[])array, index);

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this);

		IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		void IList.Insert(int index, object? value) => throw new NotSupportedException();

		void IList<KeyValuePair<TKey, TValue>>.Insert(int index, KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

		//****************************************

		private bool TryAdd(KeyValuePair<TKey, TValue> item, ReplaceMode replace, out int index, out int previous)
		{
			if (item.Key == null)
				throw new ArgumentNullException(nameof(item));

			if (_Buckets.Length == 0)
				EnsureCapacity(0);

			var HashCode = KeyComparer.GetHashCode(item.Key) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;
			int SortIndex;

			// Find the bucket we belong to
			ref var Bucket = ref _Buckets[HashCode % _Buckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				ref var TestEntry = ref Entries[Index];

				if (TestEntry.HashCode == HashCode && KeyComparer.Equals(TestEntry.Key, item.Key))
				{
					// Key is the same
					if (replace == ReplaceMode.AddOnly)
					{
						index = -1;
						previous = -1;

						return false;
					}

					if (ValueComparer.Compare(TestEntry.Value, item.Value) == 0)
					{
						// Value is unchanged
						index = previous = SortIndexOfEntry(Index, item.Value);

						return true;
					}

					// Find the previous position in the sorting and remove it
					previous = SortIndexOfEntry(Index, TestEntry.Value);

					RemoveAt(_Sorting, _Size, previous);

					// Update the value
					_Entries[Index].Item = item;

					// Store the new sorting value
					SortIndex = BinarySearch(_Sorting, _Entries, _Size - 1, item.Value, ValueComparer);

					if (SortIndex < 0)
						SortIndex = ~SortIndex;

					Insert(_Sorting, _Size - 1, SortIndex, Index);

					index = SortIndex;

					return true;
				}

				Index = Entries[Index].NextIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			if (replace == ReplaceMode.ReplaceOnly)
			{
				index = -1;
				previous = -1;

				return false;
			}

			// No collision found, are there enough slots for this item?
			if (_Size >= Entries.Length)
			{
				// No, so resize our entries table
				EnsureCapacity(_Size + 1);
				Bucket = ref _Buckets[HashCode % _Buckets.Length];
				Entries = _Entries;
			}

			// Store our item at the end
			Index = _Size;

			ref var Entry = ref Entries[Index];
			var NextIndex = Bucket - 1;

			Entry.HashCode = HashCode;
			Entry.NextIndex = NextIndex; // We take over as the head of the linked list
			Entry.PreviousIndex = -1; // We're the tail, so there's nobody behind us
			Entry.Item = item;

			// Double-link the list, so we can quickly resort
			if (NextIndex >= 0)
				Entries[NextIndex].PreviousIndex = Index;

			Bucket = Index + 1;

			// Store the sorting value
			SortIndex = BinarySearch(_Sorting, _Entries, _Size, item.Value, ValueComparer);

			if (SortIndex < 0)
				SortIndex = ~SortIndex;

			Insert(_Sorting, _Size, SortIndex, Index);

			index = SortIndex;
			previous = -1;
			_Size++;

			return true;
		}

		private int EntryIndexOfKey(TKey key)
		{
			if (_Size == 0)
				return -1;

			var HashCode = KeyComparer.GetHashCode(key) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			ref var Bucket = ref _Buckets[HashCode % _Buckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].HashCode == HashCode && KeyComparer.Equals(Entries[Index].Key, key))
					break;

				Index = Entries[Index].NextIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			return Index;
		}

		private int SortIndexOfEntry(int entryIndex, TValue value)
		{
			if (_Size == 0)
				return -1;

			// For small arrays, just do an IndexOf looking for the index
			if (_Size < 128)
				return Array.IndexOf(_Sorting, entryIndex, 0, _Size);

			// Lookup by Sorting index
			var Index = BinarySearch(_Sorting, _Entries, _Size, value, ValueComparer);

			if (Index < 0)
				return -1;

			// Check that we've found the expected entry
			if (_Sorting[Index] == entryIndex)
				return Index;

			var SubIndex = Index;

			// There may be multiple entries with the same value but different keys, so scan down
			while (SubIndex > 0)
			{
				var EntryIndex = _Sorting[SubIndex - 1];

				if (EntryIndex == entryIndex)
					return SubIndex - 1;

				if (ValueComparer.Compare(_Entries[EntryIndex].Value, value) != 0)
					break; // No luck looking below Index

				SubIndex--;
			}

			SubIndex = Index;

			// No luck, scan upwards
			while (SubIndex < _Size - 1)
			{
				var EntryIndex = _Sorting[SubIndex + 1];

				if (EntryIndex == entryIndex)
					return SubIndex + 1;

				if (ValueComparer.Compare(_Entries[EntryIndex].Value, value) != 0)
					break; // No luck looking above Index

				SubIndex++;
			}

			return -1;
		}

		private void EnsureCapacity(int capacity)
		{
			var NewSize = (_Entries.Length == 0 ? 4 : _Entries.Length * 2);

			if (NewSize < capacity)
				NewSize = capacity;

			NewSize = HashUtil.GetPrime(NewSize);

			SetCapacity(NewSize);
		}

		private void SetCapacity(int size)
		{
			var NewBuckets = new int[size];
			var NewSorting = new int[size];
			var NewEntries = new Entry[size];

			Array.Copy(_Entries, 0, NewEntries, 0, _Size);
			Array.Copy(_Sorting, 0, NewSorting, 0, _Size);

			Reindex(NewBuckets, NewEntries, 0, _Size);

			_Buckets = NewBuckets;
			_Entries = NewEntries;
			_Sorting = NewSorting;
		}

		private void RemoveAt(int entryIndex, int sortIndex)
		{
			if (sortIndex >= _Size || sortIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(sortIndex));

			// So with our first Remove, we reduce the Count and (if it's the last item in the list) we're done
			// Otherwise we relocate the end item to the gap and fix the internal indexes
			// The list is thus consistent and gap-free at the end

			var Entries = _Entries;

			ref var Entry = ref Entries[entryIndex];

			// Remove from the sorted list
			RemoveAt(_Sorting, _Size, sortIndex);

			var NextIndex = Entry.NextIndex;
			var PreviousIndex = Entry.PreviousIndex;

			// We're removing an entry, so fix up the linked list
			if (NextIndex >= 0)
			{
				// There's someone after us. Adjust them to point to the entry before us. If there's nobody, they will become the new tail
				Entries[NextIndex].PreviousIndex = PreviousIndex;
			}

			if (PreviousIndex >= 0)
			{
				// There's someone before us. Adjust them to point to the entry after us. If there's nobody, they will become the new head
				Entries[PreviousIndex].NextIndex = NextIndex;
			}
			else
			{
				// We're either the tail or a solitary entry (with no one before or after us)
				// Either way, we need to update the index stored in the Bucket
				_Buckets[Entry.HashCode % _Buckets.Length] = NextIndex + 1;
			}

			if (entryIndex == _Size - 1)
			{
				// We're removing the last entry
				// Clear it, then raise the event, and we're done
				Entries[--_Size] = default;

				return;
			}

			_Size--;

			// Find the sort index of the last entry in the list, and correct it
			var SortIndex = SortIndexOfEntry(_Size, Entries[_Size].Value);

			if (SortIndex < 0)
				throw new InvalidOperationException("Sorting inconsistent");

			_Sorting[SortIndex] = entryIndex;

			// Now we copy the last entry to our previous index
			Entry = Entries[_Size];
			NextIndex = Entry.NextIndex;
			PreviousIndex = Entry.PreviousIndex;

			// Since this entry has been relocated, we need to fix up the linked list here too
			if (NextIndex >= 0)
			{
				// There's at least one entry ahead of us, correct it to point to our new location
				Entries[NextIndex].PreviousIndex = entryIndex;
			}

			if (PreviousIndex >= 0)
			{
				// There's at least one entry behind us, correct it to point to us
				Entries[PreviousIndex].NextIndex = entryIndex;
			}
			else
			{
				// There's no entry behind us, so we're the tail or a solitary item
				// Correct the bucket index
				_Buckets[Entry.HashCode % _Buckets.Length] = entryIndex + 1;
			}

			// Clear the final entry
			Entries[_Size] = default;
		}

		private void RemoveAtEntry(int entryIndex)
		{
			if (entryIndex >= _Size || entryIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(entryIndex));

			// Remove from the sorted list
			var SortIndex = SortIndexOfEntry(entryIndex, _Entries[entryIndex].Value);

			RemoveAt(entryIndex, SortIndex);
		}

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => _Size;

		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public KeyCollection Keys { get; }

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public ValueCollection Values { get; }

		/// <summary>
		/// Gets/Sets the value corresponding to the provided key
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[TKey key]
		{
			get
			{
				if (TryGetValue(key, out var ResultValue))
					return ResultValue;

				throw new KeyNotFoundException();
			}
			set
			{
				var Item = new KeyValuePair<TKey, TValue>(key, value);

				if (!TryAdd(Item, ReplaceMode.AddReplace, out _, out _))
					throw new ArgumentException("Key already exists in the dictionary");
			}
		}

		/// <summary>
		/// Gets/Sets the number of elements that the Dictionary can contain.
		/// </summary>
		public int Capacity
		{
			get => _Entries.Length;
			set
			{
				value = HashUtil.GetPrime(value);

				if (value < _Size)
					throw new ArgumentOutOfRangeException(nameof(value));

				SetCapacity(value);
			}
		}

		/// <summary>
		/// Gets the equality comparer being used for the keys
		/// </summary>
		public IEqualityComparer<TKey> KeyComparer { get; }

		/// <summary>
		/// Gets the comparer used for sorting the values
		/// </summary>
		public IComparer<TValue> ValueComparer { get; }

		ICollection IDictionary.Keys => Keys;

		ICollection IDictionary.Values => Values;

		ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

		ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

		bool IDictionary.IsFixedSize => false;

		bool IDictionary.IsReadOnly => false;

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		object? IDictionary.this[object? key]
		{
			get
			{
				if (key is not TKey MyKey)
					throw new KeyNotFoundException();

				var Index = IndexOfKey(MyKey);

				if (Index == -1)
					throw new KeyNotFoundException();

				return _Entries[Index].Value;
			}
			set
			{
				if (key is not TKey MyKey)
					throw new ArgumentException("Not a supported key", nameof(key));

				if (value is not TValue MyValue)
					throw new ArgumentException("Not a supported value", nameof(value));

				TryAdd(new KeyValuePair<TKey, TValue>(MyKey, MyValue), ReplaceMode.AddReplace, out _, out _);
			}
		}

		KeyValuePair<TKey, TValue> IReadOnlyList<KeyValuePair<TKey, TValue>>.this[int index] => Get(index);

		KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index]
		{
			get => Get(index);
			set => throw new NotSupportedException();
		}

		object? IList.this[int index]
		{
			get => Get(index);
			set => throw new NotSupportedException();
		}

		object ICollection.SyncRoot => this;

		bool ICollection.IsSynchronized => false;

		//****************************************

		private static void Reindex(int[] buckets, Entry[] entries, int startIndex, int size)
		{
			// Reindex a range of entries
			for (var Index = startIndex; Index < size; Index++)
			{
				ref var Entry = ref entries[Index];

				var Bucket = Entry.HashCode % buckets.Length;
				var NextIndex = buckets[Bucket] - 1;

				Entry.NextIndex = NextIndex;
				Entry.PreviousIndex = -1;

				if (NextIndex >= 0)
					entries[NextIndex].PreviousIndex = Index;

				buckets[Bucket] = Index + 1;
			}
		}

		private static void VerifyDistinct(Entry[] entries, int size, IEqualityComparer<TKey> comparer)
		{
			var Index = 0;

			while (Index < size)
			{
				var StartIndex = Index;
				var CurrentItem = entries[Index];

				// Find all the items that have the same Hash
				do
				{
					Index++;
				}
				while (Index < size && entries[Index].HashCode == CurrentItem.HashCode);

				// Is there more than one item with the same Hash?
				while (Index - StartIndex > 1)
				{
					// Compare the first item to the others
					for (var SubIndex = StartIndex + 1; SubIndex < Index; SubIndex++)
					{
						if (comparer.Equals(CurrentItem.Key, entries[SubIndex].Key))
							throw new ArgumentException("Input collection has duplicates");
					}

					// Move up the first item
					StartIndex++;
				}
			}
		}

		private static int BinarySearch(int[] sorting, Entry[] entries, int size, TValue value, IComparer<TValue> comparer)
		{
			// Locates the Sorted index of an item, not the Entry index
			if (size == 0)
				return -1;

			int Low = 0, High = size - 1;

			while (Low <= High)
			{
				var Middle = Low + ((High - Low) >> 1);
				var Result = comparer.Compare(entries[sorting[Middle]].Value, value);

				if (Result < 0) // Index is less than the target. The target is somewhere above
					Low = Middle + 1;
				else if (Result > 0) // Index is greater than the target. The target is somewhere below
					High = Middle - 1;
				else // Found the target!
					return Middle;
			}

			return ~Low;
		}

		private static void Insert(int[] sorting, int size, int index, int value)
		{
			// Move any existing items up
			if (index < size)
				Array.Copy(sorting, index, sorting, index + 1, size - index);

			sorting[index] = value;
		}

		private static void RemoveAt(int[] sorting, int size, int index)
		{
			// Move any existing items down
			if (index < size - 1)
				Array.Copy(sorting, index + 1, sorting, index, size - index - 1);

			sorting[size - 1] = default;
		}

		//****************************************

		private struct Entry : IComparable<Entry>
		{
			public int NextIndex, PreviousIndex;
			public int HashCode;
			public KeyValuePair<TKey, TValue> Item;
			public TKey Key => Item.Key;
			public TValue Value => Item.Value;

			int IComparable<Entry>.CompareTo(Entry other) => HashCode - other.HashCode;
		}

		private enum ReplaceMode
		{
			AddOnly,
			ReplaceOnly,
			AddReplace
		}

		private sealed class EntryValueComparer : IComparer<Entry>
		{
			private readonly IComparer<TValue> _Comparer;

			public EntryValueComparer(IComparer<TValue> comparer)
			{
				_Comparer = comparer;
			}

			public int Compare(Entry x, Entry y) => _Comparer.Compare(x.Value, y.Value);
		}

		/// <summary>
		/// Enumerates the dictionary while avoiding memory allocations
		/// </summary>
		public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator
		{ //****************************************
			private readonly OrderedDictionary<TKey, TValue> _Parent;

			private int _Index;
			//****************************************

			internal Enumerator(OrderedDictionary<TKey, TValue> parent)
			{
				_Parent = parent;
				_Index = 0;
				Current = default;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				Current = default;
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
					Current = default;

					return false;
				}

				Current = _Parent.Get(_Index++);

				return true;
			}

			/// <inheritdoc />
			public void Reset()
			{
				_Index = 0;
				Current = default;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public KeyValuePair<TKey, TValue> Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		/// <summary>
		/// Provides a DictionaryEntry enumerator
		/// </summary>
		public struct DictionaryEnumerator : IDictionaryEnumerator
		{ //****************************************
			private Enumerator _Parent;
			//****************************************

			internal DictionaryEnumerator(OrderedDictionary<TKey, TValue> parent)
			{
				_Parent = new Enumerator(parent);
			}

			//****************************************

			/// <inheritdoc />
			public bool MoveNext() => _Parent.MoveNext();

			/// <inheritdoc />
			public void Reset() => _Parent.Reset();

			//****************************************

			/// <inheritdoc />
			public DictionaryEntry Current
			{
				get
				{
					var Current = _Parent.Current;

					return new DictionaryEntry(Current.Key, Current.Value);
				}
			}

			object IEnumerator.Current => Current;

			DictionaryEntry IDictionaryEnumerator.Entry => Current;

			object IDictionaryEnumerator.Key => _Parent.Current.Key;

			object? IDictionaryEnumerator.Value => _Parent.Current.Value;
		}
	}
}
