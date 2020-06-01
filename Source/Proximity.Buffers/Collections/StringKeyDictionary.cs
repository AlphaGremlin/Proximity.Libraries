using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Proximity.Buffers.Collections;

namespace System.Collections.Generic
{
	/// <summary>
	/// Provides a Dictionary implementation with character key supporting <see cref="ReadOnlyMemory{Char}"/> and <see cref="ReadOnlySpan{Char}"/>
	/// </summary>
	/// <typeparam name="TValue">The type of value</typeparam>
	/// <remarks>Note that index locations can change in response to Remove operations</remarks>
	public class StringKeyDictionary<TValue> :
		IDictionary<string, TValue>, IDictionary, // We don't implement IDictionary<ReadOnlyMemory<char>>, since it would require copying the key values
		IList<KeyValuePair<string, TValue>>, IList,
		IReadOnlyDictionary<string, TValue>, IReadOnlyDictionary<ReadOnlyMemory<char>, TValue>,
		IReadOnlyList<KeyValuePair<string, TValue>>, IReadOnlyList<KeyValuePair<ReadOnlyMemory<char>, TValue>>
	{ //****************************************
		private const int HashCodeMask = 0x7FFFFFFF;
		//****************************************
		private readonly BlockEqualityComparer<char> _Comparer;

		private int[] _Buckets;
		private Entry[] _Entries;

		private int _Size;
		//****************************************

		/// <summary>
		/// Creates a default Indexed Dictionary
		/// </summary>
		public StringKeyDictionary() : this(0, StringComparison.Ordinal)
		{
		}

		/// <summary>
		/// Creates an Indexed Dictionary
		/// </summary>
		/// <param name="capacity">The starting capacity</param>
		public StringKeyDictionary(int capacity) : this(capacity, StringComparison.Ordinal)
		{
		}

		/// <summary>
		/// Creates an Indexed Dictionary
		/// </summary>
		/// <param name="comparison">The string comparer to use for keys</param>
		public StringKeyDictionary(StringComparison comparison) : this(0, comparison)
		{
		}

		/// <summary>
		/// Creates an Indexed Dictionary
		/// </summary>
		/// <param name="capacity">The starting capacity</param>
		/// <param name="comparison">The string comparer to use for keys</param>
		public StringKeyDictionary(int capacity, StringComparison comparison)
		{
			if (capacity < 0)
				throw new ArgumentOutOfRangeException(nameof(capacity));

			Comparison = comparison;
			_Comparer = BlockEqualityComparer.ForChar(comparison);

			if (capacity == 0)
			{
				_Buckets = Array.Empty<int>();
				_Entries = Array.Empty<Entry>();
			}
			else
			{
				_Buckets = new int[capacity];
				_Entries = new Entry[capacity];
			}

			Keys = new DictionaryKeys(this);
			Values = new DictionaryValues(this);
		}

		/// <summary>
		/// Creates an Indexed Dictionary
		/// </summary>
		/// <param name="collection">The starting contents of the dictionary</param>
		/// <remarks>The items are not guaranteed to be stored in the provided index order</remarks>
		public StringKeyDictionary(IEnumerable<KeyValuePair<string, TValue>> collection) : this(collection, StringComparison.Ordinal)
		{
		}

		/// <summary>
		/// Creates an Indexed Dictionary
		/// </summary>
		/// <param name="collection">The starting contents of the dictionary</param>
		/// <param name="comparison">The string comparer to use for keys</param>
		/// <remarks>The items are not guaranteed to be stored in the provided index order</remarks>
		public StringKeyDictionary(IEnumerable<KeyValuePair<string, TValue>> collection, StringComparison comparison)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			Comparison = comparison;
			_Comparer = BlockEqualityComparer.ForChar(comparison);

			_Size = collection.Count();
			var Capacity = HashUtil.GetPrime(_Size);

			_Buckets = new int[Capacity];
			_Entries = new Entry[Capacity];

			var Index = 0;

			foreach (var MyPair in collection)
			{
				ref var Entry = ref _Entries[Index++];

				Entry.Item = MyPair;
				Entry.HashCode = _Comparer.GetHashCode(MyPair.Key.AsSpan()) & HashCodeMask;
			}

			// Get everything into HashCode order
			Array.Sort(_Entries, 0, _Size);

			// Check the new items don't have any duplicates
			VerifyDistinct(_Entries, _Size, _Comparer);

			Reindex(_Buckets, _Entries, 0, _Size);

			Keys = new DictionaryKeys(this);
			Values = new DictionaryValues(this);
		}

		//****************************************

		/// <summary>
		/// Adds an item to the dictionary
		/// </summary>
		/// <param name="key">The key of the item</param>
		/// <param name="value">The value to associate with the key</param>
		/// <returns>The index of the added key/value pair</returns>
		public int Add(string key, TValue value) => Add(new KeyValuePair<string, TValue>(key, value));

		/// <summary>
		/// Adds an item to the dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <returns>The index of the added key/value pair</returns>
		public int Add(KeyValuePair<string, TValue> item)
		{
			if (!TryAdd(item, false, out var Index))
				throw new ArgumentException(nameof(item));

			return Index;
		}

		/// <summary>
		/// Adds or updates an item in the Dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		/// <param name="index">Receives the index of the key/value pair</param>
		public void AddOrUpdate(string key, TValue value, out int index) => AddOrUpdate(new KeyValuePair<string, TValue>(key, value), out index);

		/// <summary>
		/// Adds or updates an item in the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <param name="index">Receives the index of the key/value pair</param>
		public void AddOrUpdate(KeyValuePair<string, TValue> item, out int index) => TryAdd(item, true, out index);

		/// <summary>
		/// Adds a range of elements to the dictionary
		/// </summary>
		/// <param name="items">The elements to add</param>
		/// <exception cref="ArgumentException">The input elements have duplicated keys, or the key already exists in the dictionary</exception>
		public void AddRange(IEnumerable<KeyValuePair<string, TValue>> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			// Gather all the items to add, calculating their keys as we go
			var NewItems = items.Select(pair => new Entry { HashCode = _Comparer.GetHashCode(pair.Key.AsSpan()) & HashCodeMask, Item = pair }).ToArray();

			if (NewItems.Length == 0)
				return;

			// Get everything into Key order
			Array.Sort(NewItems);

			//****************************************

			var InsertIndex = _Size;
			var Entries = _Entries;

			// Check the new items don't have any duplicates
			VerifyDistinct(NewItems, NewItems.Length, _Comparer);

			// No duplicates in the new items. Check the keys aren't already in the Dictionary
			if (_Size > 0)
			{
				for (var Index = 0; Index < NewItems.Length; Index++)
				{
					var TotalCollisions = 0;
					var Item = NewItems[Index];

					// Find the bucket we belong to
					var Bucket = _Buckets[Item.HashCode % _Buckets.Length];
					var EntryIndex = Bucket - 1;

					var ItemSpan = Item.Key.AsSpan();

					// Check for collisions
					while (EntryIndex >= 0)
					{
						if (Entries[EntryIndex].HashCode == Item.HashCode && _Comparer.Equals(Entries[EntryIndex].Key.AsSpan(), ItemSpan))
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
				Entries[_Size++] = NewItems[Index];
			}

			Reindex(_Buckets, Entries, InsertIndex, InsertIndex + NewItems.Length);
		}

		/// <inheritdoc />
		public void Clear()
		{
			if (_Size == 0)
				return;

			Array.Clear(_Buckets, 0, _Buckets.Length);
			Array.Clear(_Entries, 0, _Size);

			_Size = 0;
		}

		/// <inheritdoc />
		public bool Contains(KeyValuePair<string, TValue> item) => IndexOf(item) != -1;

		/// <inheritdoc />
		public bool ContainsKey(string key) => IndexOfKey(key.AsSpan()) != -1;

		/// <inheritdoc />
		public bool ContainsKey(ReadOnlyMemory<char> key) => IndexOfKey(key.Span) != -1;

		/// <inheritdoc />
		public bool ContainsKey(ReadOnlySpan<char> key) => IndexOfKey(key) != -1;

		/// <inheritdoc />
		public bool ContainsValue(TValue value) => IndexOfValue(value) != -1;

		/// <inheritdoc />
		public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
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
		/// <returns>They key/value pair at the requested index</returns>
		public KeyValuePair<string, TValue> Get(int index) => GetByIndex(index);

		/// <summary>
		/// Gets an enumerator for this Dictionary
		/// </summary>
		/// <returns>An Enumerator to enumerate the contents of the Dictionary</returns>
		public Enumerator GetEnumerator() => new Enumerator(this);

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(KeyValuePair<string, TValue> item)
		{
			var Index = IndexOfKey(item.Key.AsSpan());

			if (Index == -1 || !EqualityComparer<TValue>.Default.Equals(_Entries[Index].Value, item.Value))
				return -1;

			return Index;
		}

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="key">The key to lookup</param>
		/// <param name="value">The value to match</param>
		/// <returns>The index of the item if found and the value matches, otherwise -1</returns>
		public int IndexOf(ReadOnlySpan<char> key, TValue value)
		{
			var Index = IndexOfKey(key);

			if (Index == -1 || !EqualityComparer<TValue>.Default.Equals(_Entries[Index].Value, value))
				return -1;

			return Index;
		}

		/// <summary>
		/// Finds the index of a particular key
		/// </summary>
		/// <param name="key">The key to lookup</param>
		/// <returns>The index of the key, if found, otherwise -1</returns>
		public int IndexOfKey(ReadOnlySpan<char> key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (_Buckets.Length == 0)
				return -1;

			var HashCode = _Comparer.GetHashCode(key) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			var Bucket = _Buckets[HashCode % _Buckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].HashCode == HashCode && _Comparer.Equals(Entries[Index].Key.AsSpan(), key))
					break;

				Index = Entries[Index].NextIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			return Index;
		}

		/// <summary>
		/// Finds the index of a particular key
		/// </summary>
		/// <param name="key">The key to lookup</param>
		/// <returns>The index of the key, if found, otherwise -1</returns>
		public int IndexOfKey(string key) => IndexOfKey(key.AsSpan());

		/// <summary>
		/// Finds the index of a particular value
		/// </summary>
		/// <param name="value">The value to lookup</param>
		/// <returns></returns>
		public int IndexOfValue(TValue value)
		{
			var Comparer = EqualityComparer<TValue>.Default;
			var Entries = _Entries;

			for (var Index = 0; Index < _Size; Index++)
			{
				if (Comparer.Equals(Entries[Index].Value, value))
					return Index;
			}

			return -1;
		}

		/// <inheritdoc />
		public bool Remove(ReadOnlySpan<char> key)
		{
			var Index = IndexOfKey(key);

			if (Index == -1)
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <inheritdoc />
		public bool Remove(string key) => Remove(key.AsSpan());

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was found and removed, otherwise False</returns>
		public bool Remove(KeyValuePair<string, TValue> item)
		{
			var Index = IndexOfKey(item.Key.AsSpan());

			if (Index == -1 || !EqualityComparer<TValue>.Default.Equals(_Entries[Index].Value, item.Value))
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="key">The element to remove</param>
		/// <param name="value">The value to remove</param>
		/// <returns>True if the item was found and removed, otherwise False</returns>
		public bool Remove(ReadOnlySpan<char> key, TValue value)
		{
			var Index = IndexOfKey(key);

			if (Index == -1 || !EqualityComparer<TValue>.Default.Equals(_Entries[Index].Value, value))
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <summary>
		/// Removes all items that match the given predicate
		/// </summary>
		/// <param name="predicate">A predicate that returns True for items to remove</param>
		/// <returns>The number of items removed</returns>
		public int RemoveAll(Predicate<KeyValuePair<string, TValue>> predicate)
		{
			var Entries = _Entries;
			var Buckets = _Buckets;
			var Index = 0;

			// Find the first item we need to remove
			while (Index < _Size && !predicate(Entries[Index].Item))
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return 0;

			Array.Clear(Buckets, 0, Buckets.Length);

			if (Index > 0)
				Reindex(Buckets, Entries, 0, Index);

			var LeadIndex = Index + 1;

			while (LeadIndex < _Size)
			{
				// Skip the items we need to remove
				while (LeadIndex < _Size && predicate(Entries[LeadIndex].Item))
					LeadIndex++;

				// If we reached the end, abort
				if (LeadIndex >= _Size)
					break;

				// We found one we're not removing, so move it up
				ref var Entry = ref Entries[Index];

				Entry = Entries[LeadIndex];

				// Reindex it
				ref var Bucket = ref Buckets[Entry.HashCode % Buckets.Length];
				var NextIndex = Bucket - 1;

				Entry.NextIndex = NextIndex;
				Entry.PreviousIndex = -1;

				if (NextIndex >= 0)
					Entries[NextIndex].PreviousIndex = Index;

				Bucket = Index + 1;

				Index++;
				LeadIndex++;
			}

			// Clear the removed items
			Array.Clear(_Entries, Index, _Size - Index);
			_Size = Index;

			return LeadIndex - Index;
		}

		/// <summary>
		/// Removes the element at the specified index
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		public void RemoveAt(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			var Entries = _Entries;

			ref var Entry = ref Entries[index];

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

			if (index < _Size - 1)
			{
				// We're not removing the last entry, so we need to copy that entry to our previous position
				// This ensures there are no gaps in the Entry table, allowing us to use direct array indexing at the cost of items moving around after a remove
				Entry = Entries[_Size - 1];
				NextIndex = Entry.NextIndex;
				PreviousIndex = Entry.PreviousIndex;

				// Since this entry has been relocated, we need to fix up the linked list here too
				if (NextIndex >= 0)
				{
					// There's at least one entry ahead of us, correct it to point to our new location
					Entries[NextIndex].PreviousIndex = index;
				}

				if (PreviousIndex >= 0)
				{
					// There's at least one entry behind us, correct it to point to us
					Entries[PreviousIndex].NextIndex = index;
				}
				else
				{
					// There's no entry behind us, so we're the tail or a solitary item
					// Correct the bucket index
					_Buckets[Entry.HashCode % _Buckets.Length] = index + 1;
				}
			}

			// Clear the final entry
			Entries[--_Size] = default;
		}

		/// <summary>
		/// Removes the elements within the specified range
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		/// <param name="count">The number of items to remove after the index</param>
		public void RemoveRange(int index, int count)
		{
			var LastIndex = index + count;

			if (index < 0 || LastIndex > _Size)
				throw new ArgumentOutOfRangeException(nameof(index));

			// Since a remove does not slide down the values above, and simply relocates the last item,
			// we need to remove in reverse so we don't accidentally move one of the items we plan to remove
			for (var Index = LastIndex - 1; Index >= index; Index--)
				RemoveAt(Index);
		}

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="key">The key of the item</param>
		/// <param name="value">The value to associate with the key</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(string key, TValue value) => TryAdd(new KeyValuePair<string, TValue>(key, value), out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<string, TValue> item) => TryAdd(item, out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="key">The key of the item</param>
		/// <param name="value">The value to associate with the key</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(string key, TValue value, out int index) => TryAdd(new KeyValuePair<string, TValue>(key, value), out index);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<string, TValue> item, out int index) => TryAdd(item, false, out index);

		/// <inheritdoc />
		public bool TryGetValue(string key,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TValue value) => TryGetValue(key.AsSpan(), out value);

		/// <inheritdoc />
		public bool TryGetValue(ReadOnlyMemory<char> key,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TValue value) => TryGetValue(key.Span, out value);

		/// <inheritdoc />
		public bool TryGetValue(ReadOnlySpan<char> key,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TValue value)
		{
			var Index = IndexOfKey(key);

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
		public bool TryRemove(string key,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TValue value) => TryRemove(key.AsSpan(), out value);

		/// <summary>
		/// Tries to remove an item from the dictionary
		/// </summary>
		/// <param name="key">The key of the item to remove</param>
		/// <param name="value">Receives the value of the removed item</param>
		/// <returns>True if the item was removed, otherwise False</returns>
		public bool TryRemove(ReadOnlySpan<char> key,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TValue value)
		{
			var Index = IndexOfKey(key);

			if (Index == -1)
			{
				value = default!;
				return false;
			}

			value = _Entries[Index].Value;

			RemoveAt(Index);

			return true;
		}

		//****************************************

		void IDictionary<string, TValue>.Add(string key, TValue value)
		{
			if (!TryAdd(new KeyValuePair<string, TValue>(key, value), false, out _))
				throw new ArgumentException(nameof(key));
		}

		void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) => Add(item);

		bool IDictionary<string, TValue>.Remove(string key) => Remove(key.AsSpan());

		void IDictionary.Add(object key, object? value)
		{
			if (!(key is string MyKey))
				throw new ArgumentException("Not a supported key", nameof(key));

			if (!(value is TValue MyValue))
				throw new ArgumentException("Not a supported value", nameof(value));

			Add(new KeyValuePair<string, TValue>(MyKey, MyValue));
		}

		bool IDictionary.Contains(object value)
		{
			if (value is KeyValuePair<string, TValue> MyPair)
				return Contains(MyPair);

			if (value is DictionaryEntry MyEntry && MyEntry.Key is string MyKey && MyEntry.Value is TValue MyValue)
				return Contains(new KeyValuePair<string, TValue>(MyKey, MyValue));

			return false;
		}

		void IDictionary.Remove(object value)
		{
			if (value is KeyValuePair<string, TValue> MyPair)
			{
				Remove(MyPair);
				return;
			}

			if (value is DictionaryEntry MyEntry && MyEntry.Key is string MyKey && MyEntry.Value is TValue MyValue)
			{
				Remove(new KeyValuePair<string, TValue>(MyKey, MyValue));
				return;
			}
		}

		int IList.Add(object? value)
		{
			if (value is KeyValuePair<string, TValue> MyPair)
			{
				if (TryAdd(MyPair, false, out var Index))
					return Index;

				return -1;
			}

			if (value is DictionaryEntry MyEntry && MyEntry.Key is string MyKey && MyEntry.Value is TValue MyValue)
			{
				if (TryAdd(new KeyValuePair<string, TValue>(MyKey, MyValue), false, out var Index))
					return Index;

				return -1;
			}

			throw new ArgumentException("Not a supported value", nameof(value));
		}

		bool IList.Contains(object? value)
		{
			if (value is KeyValuePair<string, TValue> MyPair)
				return Contains(MyPair);

			if (value is DictionaryEntry MyEntry && MyEntry.Key is string MyKey && MyEntry.Value is TValue MyValue)
				return Contains(new KeyValuePair<string, TValue>(MyKey, MyValue));

			return false;
		}

		int IList.IndexOf(object? value)
		{
			if (value is KeyValuePair<string, TValue> MyPair)
				return IndexOf(MyPair);

			if (value is DictionaryEntry MyEntry && MyEntry.Key is string MyKey && MyEntry.Value is TValue MyValue)
				return IndexOf(new KeyValuePair<string, TValue>(MyKey, MyValue));

			return -1;
		}

		void IList.Remove(object? value)
		{
			if (value is KeyValuePair<string, TValue> MyPair)
			{
				Remove(MyPair);
				return;
			}

			if (value is DictionaryEntry MyEntry && MyEntry.Key is string MyKey && MyEntry.Value is TValue MyValue)
			{
				Remove(new KeyValuePair<string, TValue>(MyKey, MyValue));
				return;
			}
		}

		void ICollection.CopyTo(Array array, int index) => CopyTo((KeyValuePair<string, TValue>[])array, index);

		IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => new Enumerator(this);

		IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>> IEnumerable<KeyValuePair<ReadOnlyMemory<char>, TValue>>.GetEnumerator() => new Enumerator(this);

		IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		void IList.Insert(int index, object? value) => throw new NotSupportedException();

		void IList<KeyValuePair<string, TValue>>.Insert(int index, KeyValuePair<string, TValue> item) => throw new NotSupportedException();

		//****************************************

		private bool TryAdd(KeyValuePair<string, TValue> item, bool replace, out int index)
		{
			var HashCode = _Comparer.GetHashCode(item.Key.AsSpan()) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			if (_Buckets.Length == 0)
				EnsureCapacity(0);

			// Find the bucket we belong to
			ref var Bucket = ref _Buckets[HashCode % _Buckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].HashCode == HashCode && _Comparer.Equals(Entries[Index].Key.AsSpan(), item.Key.AsSpan()))
				{
					// Key is the same
					if (replace)
						_Entries[Index].Item = item;

					index = Index;

					return replace;
				}

				Index = Entries[Index].NextIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
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
			Index = _Size++;

			ref var Entry = ref Entries[Index];
			var NextIndex = Bucket - 1;

			Entry.HashCode = HashCode;
			Entry.NextIndex = NextIndex; // We take over as the tail of the linked list
			Entry.PreviousIndex = -1; // We're the tail, so there's nobody behind us
			Entry.Item = item;

			// Double-link the list, so we can quickly resort
			if (NextIndex >= 0)
				Entries[NextIndex].PreviousIndex = Index;

			Bucket = Index + 1;

			index = Index;

			return true;
		}

		private void EnsureCapacity(int capacity)
		{
			var NewSize = (_Entries.Length == 0 ? 4 : _Entries.Length * 2);

			if (NewSize < capacity)
				NewSize = capacity;

			SetCapacity(HashUtil.GetPrime(NewSize));
		}

		private void SetCapacity(int size)
		{
			var NewBuckets = new int[size];
			var NewEntries = new Entry[size];

			Array.Copy(_Entries, 0, NewEntries, 0, _Size);

			Reindex(NewBuckets, NewEntries, 0, _Size);

			_Buckets = NewBuckets;
			_Entries = NewEntries;
		}

		private ref readonly KeyValuePair<string, TValue> GetByIndex(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref _Entries[index].Item;
		}

		private static void Reindex(int[] buckets, Entry[] entries, int startIndex, int size)
		{
			// Reindex a range of entries
			for (var Index = startIndex; Index < size; Index++)
			{
				ref var Entry = ref entries[Index];

				ref var Bucket = ref buckets[Entry.HashCode % buckets.Length];
				var NextIndex = Bucket - 1;

				Entry.NextIndex = NextIndex;
				Entry.PreviousIndex = -1;

				if (NextIndex >= 0)
					entries[NextIndex].PreviousIndex = Index;

				Bucket = Index + 1;
			}
		}

		private static void VerifyDistinct(Entry[] entries, int size, BlockEqualityComparer<char> comparer)
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

				var CurrentSpan = CurrentItem.Key.AsSpan();

				// Is there more than one item with the same Hash?
				while (Index - StartIndex > 1)
				{
					// Compare the first item to the others
					for (var SubIndex = StartIndex + 1; SubIndex < Index; SubIndex++)
					{
						if (comparer.Equals(CurrentSpan, entries[SubIndex].Key.AsSpan()))
							throw new ArgumentException("Input collection has duplicates");
					}

					// Move up the first item
					StartIndex++;
				}
			}
		}

		//****************************************

		/// <inheritdoc />
		public TValue this[ReadOnlySpan<char> key]
		{
			get
			{
				var Index = IndexOfKey(key);

				if (Index == -1)
					throw new KeyNotFoundException();

				return _Entries[Index].Value;
			}
		}

		/// <inheritdoc />
		public TValue this[ReadOnlyMemory<char> key] => this[key.Span];

		/// <inheritdoc />
		public TValue this[string key]
		{
			get => this[key.AsSpan()];
			set => TryAdd(new KeyValuePair<string, TValue>(key, value), true, out _);
		}

		/// <inheritdoc />
		public int Count => _Size;

		/// <summary>
		/// Gets a collection of keys
		/// </summary>
		public DictionaryKeys Keys { get; }

		/// <summary>
		/// Gets a collection of values
		/// </summary>
		public DictionaryValues Values { get; }

		/// <summary>
		/// Gets/Sets the number of elements that the String Key Dictionary can contain.
		/// </summary>
		public int Capacity
		{
			get => _Entries.Length;
			set
			{
				if (value < _Size)
					throw new ArgumentOutOfRangeException(nameof(value));

				SetCapacity(value);
			}
		}

		/// <summary>
		/// Gets the string comparison being used for the Key
		/// </summary>
		public StringComparison Comparison { get; }

		ICollection IDictionary.Keys => Keys;

		ICollection IDictionary.Values => Values;

		ICollection<string> IDictionary<string, TValue>.Keys => Keys;

		ICollection<TValue> IDictionary<string, TValue>.Values => Values;

		IEnumerable<ReadOnlyMemory<char>> IReadOnlyDictionary<ReadOnlyMemory<char>, TValue>.Keys => Keys;

		IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => Keys;

		IEnumerable<TValue> IReadOnlyDictionary<ReadOnlyMemory<char>, TValue>.Values => Values;

		IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => Values;

		bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => false;

		bool IDictionary.IsFixedSize => false;

		bool IDictionary.IsReadOnly => false;

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		object? IDictionary.this[object key]
		{
			get
			{
				ReadOnlySpan<char> Key;

				if (key is string StringKey)
					Key = StringKey.AsSpan();
				else if (key is ReadOnlyMemory<char> ROMKey)
					Key = ROMKey.Span;
				else if (key is char[] ArrayKey)
					Key = ArrayKey;
				else if (key is ArraySegment<char> SegmentKey)
					Key = SegmentKey;
				else
					throw new KeyNotFoundException();

				var Index = IndexOfKey(Key);

				if (Index == -1)
					throw new KeyNotFoundException();

				return _Entries[Index].Value;
			}
			set
			{
				if (!(key is string MyKey))
					throw new ArgumentException("Not a supported key", nameof(key));

				if (!(value is TValue MyValue))
					throw new ArgumentException("Not a supported value", nameof(value));

				TryAdd(new KeyValuePair<string, TValue>(MyKey, MyValue), true, out _);
			}
		}

		KeyValuePair<string, TValue> IReadOnlyList<KeyValuePair<string, TValue>>.this[int index] => GetByIndex(index);

		KeyValuePair<ReadOnlyMemory<char>, TValue> IReadOnlyList<KeyValuePair<ReadOnlyMemory<char>, TValue>>.this[int index]
		{
			get
			{
				ref readonly var Entry = ref GetByIndex(index);

				return new KeyValuePair<ReadOnlyMemory<char>, TValue>(Entry.Key.AsMemory(), Entry.Value);
			}
		}

		KeyValuePair<string, TValue> IList<KeyValuePair<string, TValue>>.this[int index]
		{
			get => GetByIndex(index);
			set => throw new NotSupportedException();
		}

		object? IList.this[int index]
		{
			get => GetByIndex(index);
			set => throw new NotSupportedException();
		}

		object ICollection.SyncRoot => this;

		bool ICollection.IsSynchronized => false;

		//****************************************

		private struct Entry : IComparable<Entry>
		{
			public int NextIndex;
			public int PreviousIndex;
			public int HashCode;
			public KeyValuePair<string, TValue> Item;
			public string Key => Item.Key;
			public TValue Value => Item.Value;

			int IComparable<Entry>.CompareTo(Entry other) => HashCode - other.HashCode;
		}

		/// <summary>
		/// Provides a key/value pair enumerator
		/// </summary>
		public struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>, IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>>
		{ //****************************************
			private readonly StringKeyDictionary<TValue> _Parent;

			private int _Index;
			private KeyValuePair<string, TValue> _Current;
			//****************************************

			internal Enumerator(StringKeyDictionary<TValue> parent)
			{
				_Parent = parent;
				_Index = 0;
				_Current = default;
			}

			//****************************************

			void IDisposable.Dispose()
			{
			}

			/// <inheritdoc />
			public bool MoveNext()
			{
				if (_Index >= _Parent._Size)
					return false;

				_Current = _Parent._Entries[_Index++].Item;

				return true;
			}

			/// <inheritdoc />
			public void Reset()
			{
				_Index = 0;
			}

			//****************************************

			/// <inheritdoc />
			public KeyValuePair<string, TValue> Current => _Current;

			KeyValuePair<ReadOnlyMemory<char>, TValue> IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>>.Current => new KeyValuePair<ReadOnlyMemory<char>, TValue>(_Current.Key.AsMemory(), _Current.Value);

			object IEnumerator.Current => Current;
		}

		/// <summary>
		/// Provides a DictionaryEntry enumerator
		/// </summary>
		public struct DictionaryEnumerator : IDictionaryEnumerator
		{ //****************************************
			private Enumerator _Parent;
			//****************************************

			internal DictionaryEnumerator(StringKeyDictionary<TValue> parent)
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

		/// <summary>
		/// Provides a key enumerator
		/// </summary>
		public struct KeyEnumerator : IEnumerator<string>, IEnumerator<ReadOnlyMemory<char>>
		{ //****************************************
			private readonly Enumerator _Parent;
			//****************************************

			internal KeyEnumerator(StringKeyDictionary<TValue> parent)
			{
				_Parent = new Enumerator(parent);
			}

			//****************************************

			void IDisposable.Dispose()
			{
			}

			/// <inheritdoc />
			public bool MoveNext() => _Parent.MoveNext();

			/// <inheritdoc />
			public void Reset() => _Parent.Reset();

			//****************************************

			/// <inheritdoc />
			public string Current => _Parent.Current.Key;

			ReadOnlyMemory<char> IEnumerator<ReadOnlyMemory<char>>.Current => _Parent.Current.Key.AsMemory();

			object? IEnumerator.Current => Current;
		}

		/// <summary>
		/// Provides a value enumerator
		/// </summary>
		public struct ValueEnumerator : IEnumerator<TValue>
		{ //****************************************
			private readonly Enumerator _Parent;
			//****************************************

			internal ValueEnumerator(StringKeyDictionary<TValue> parent)
			{
				_Parent = new Enumerator(parent);
			}

			//****************************************

			void IDisposable.Dispose()
			{
			}

			/// <inheritdoc />
			public bool MoveNext() => _Parent.MoveNext();

			/// <inheritdoc />
			public void Reset() => _Parent.Reset();

			//****************************************

			/// <inheritdoc />
			public TValue Current => _Parent.Current.Value;

			object? IEnumerator.Current => Current;
		}

		/// <summary>
		/// Provides a base class for the Indexed Dictionary collections
		/// </summary>
		/// <typeparam name="T">The type of item</typeparam>
		public abstract class Collection<T> : IList<T>, IReadOnlyList<T>, IList
		{
			internal Collection(StringKeyDictionary<TValue> parent)
			{
				Parent = parent;
			}

			//****************************************

			/// <inheritdoc />
			public abstract bool Contains(T item);

			/// <inheritdoc />
			public abstract void CopyTo(T[] array, int arrayIndex);

			/// <inheritdoc />
			public IEnumerator<T> GetEnumerator() => InternalGetEnumerator();

			/// <inheritdoc />
			public abstract int IndexOf(T item);

			private protected abstract IEnumerator<T> InternalGetEnumerator();

			/// <summary>
			/// Gets an array that represents this collection
			/// </summary>
			/// <returns>A new array containing the elements of this collection</returns>
			public abstract T[] ToArray();

			void ICollection.CopyTo(Array array, int index)
			{
				if (array is T[] MyArray)
				{
					CopyTo(MyArray, index);

					return;
				}

				throw new ArgumentException("Cannot copy to target array");
			}

			bool IList.Contains(object? value)
			{
				if (value is T Item)
					return Contains(Item);

				return false;
			}

			int IList.IndexOf(object? value)
			{
				if (value is T Item)
					return IndexOf(Item);

				return -1;
			}

			IEnumerator IEnumerable.GetEnumerator() => InternalGetEnumerator();

			void ICollection<T>.Add(T item) => throw new NotSupportedException();

			int IList.Add(object? item) => throw new NotSupportedException();

			void ICollection<T>.Clear() => throw new NotSupportedException();

			void IList.Clear() => throw new NotSupportedException();

			void IList.Insert(int index, object? value) => throw new NotSupportedException();

			void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

			bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

			void IList.Remove(object? item) => throw new NotSupportedException();

			void IList.RemoveAt(int index) => throw new NotSupportedException();

			void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

			//****************************************

			/// <inheritdoc />
			public abstract T this[int index] { get; }

			/// <inheritdoc />
			public int Count => Parent.Count;

			bool ICollection<T>.IsReadOnly => true;

			bool IList.IsFixedSize => false;

			bool IList.IsReadOnly => true;

			T IList<T>.this[int index]
			{
				get => this[index];
				set => throw new NotSupportedException();
			}

			object? IList.this[int index]
			{
				get => this[index];
				set => throw new NotSupportedException();
			}

			object ICollection.SyncRoot => Parent;

			bool ICollection.IsSynchronized => false;

			/// <summary>
			/// Gets the parent Dictionary
			/// </summary>
			protected StringKeyDictionary<TValue> Parent { get; }
		}

		/// <summary>
		/// Provides a read-only keys wrapper
		/// </summary>
		public sealed class DictionaryKeys : Collection<string>, IEnumerable<ReadOnlyMemory<char>>
		{
			internal DictionaryKeys(StringKeyDictionary<TValue> parent) : base(parent)
			{
			}

			//****************************************

			/// <inheritdoc />
			public override bool Contains(string item) => Parent.ContainsKey(item.AsSpan());

			/// <inheritdoc />
			public bool Contains(ReadOnlySpan<char> item) => Parent.ContainsKey(item);

			/// <inheritdoc />
			public override void CopyTo(string[] array, int arrayIndex)
			{
				var Count = Parent._Size;

				if (arrayIndex + Count >= array.Length)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));

				var Entries = Parent._Entries;

				for (var Index = 0; Index < Count; Index++)
				{
					array[arrayIndex + Index] = Entries[Index].Key;
				}
			}

			/// <inheritdoc />
			public new KeyEnumerator GetEnumerator() => new KeyEnumerator(Parent);

			/// <inheritdoc />
			public override int IndexOf(string item) => Parent.IndexOfKey(item.AsSpan());

			/// <inheritdoc />
			public int IndexOf(ReadOnlySpan<char> item) => Parent.IndexOfKey(item);

			/// <inheritdoc />
			public override string this[int index] => Parent.GetByIndex(index).Key;

			/// <inheritdoc />
			public override string[] ToArray()
			{
				var Result = new string[Parent.Count];

				CopyTo(Result, 0);

				return Result;
			}

			//****************************************

			private protected override IEnumerator<string> InternalGetEnumerator() => GetEnumerator();

			IEnumerator<ReadOnlyMemory<char>> IEnumerable<ReadOnlyMemory<char>>.GetEnumerator() => GetEnumerator();
		}

		/// <summary>
		/// Provides a read-only values wrapper
		/// </summary>
		public sealed class DictionaryValues : Collection<TValue>
		{
			internal DictionaryValues(StringKeyDictionary<TValue> parent) : base(parent)
			{
			}

			//****************************************

			/// <inheritdoc />
			public override bool Contains(TValue item) => Parent.ContainsValue(item);

			/// <inheritdoc />
			public override void CopyTo(TValue[] array, int arrayIndex)
			{
				var Count = Parent._Size;

				if (arrayIndex + Count >= array.Length)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));

				var Entries = Parent._Entries;

				for (var Index = 0; Index < Count; Index++)
				{
					array[arrayIndex + Index] = Entries[Index].Value;
				}
			}

			/// <inheritdoc />
			public new ValueEnumerator GetEnumerator() => new ValueEnumerator(Parent);

			/// <inheritdoc />
			public override int IndexOf(TValue item) => Parent.IndexOfValue(item);

			/// <inheritdoc />
			public override TValue this[int index] => Parent.GetByIndex(index).Value;

			/// <inheritdoc />
			public override TValue[] ToArray()
			{
				var Result = new TValue[Parent.Count];

				CopyTo(Result, 0);

				return Result;
			}

			//****************************************

			private protected override IEnumerator<TValue> InternalGetEnumerator() => GetEnumerator();
		}
	}
}
