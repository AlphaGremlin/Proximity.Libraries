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
	public class ObservableDictionary<TKey, TValue> : ObservableBase<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
	{	//****************************************
		private const string KeysName = "Keys";
		private const string ValuesName = "Values";

		private const int HashCodeMask = 0x7FFFFFFF;
		//****************************************
		private int[] _Buckets;
		private Entry[] _Entries;
		private int? _ShiftIndex;

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
		public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary) : this(dictionary, EqualityComparer<TKey>.Default)
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
		public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> dictionary, IEqualityComparer<TKey> comparer)
		{
			Comparer = comparer;

			_Size = dictionary.Count();
			var Capacity = HashUtil.GetPrime(_Size);

			_Buckets = new int[Capacity];
			_Entries = new Entry[Capacity];

			int Index = 0;

			foreach (var MyPair in dictionary)
			{
				ref var Entry = ref _Entries[Index++];
				
				Entry.Item = MyPair;
				Entry.HashCode = comparer.GetHashCode(MyPair.Key) & HashCodeMask;
			}

			Reindex(_Buckets, _Entries, 0, _Size);

			Keys = new KeyCollection(this);
			Values = new ValueCollection(this);
		}

		/// <summary>
		/// Creates a new empty observable dictionary with the specified comparer
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
		{
			capacity = HashUtil.GetPrime(capacity);

			_Buckets = new int[capacity];
			_Entries = new Entry[capacity];
			Comparer = comparer;

			Keys = new KeyCollection(this);
			Values = new ValueCollection(this);
		}

		//****************************************

		/// <summary>
		/// Adds a new element the Dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		public void Add(TKey key, TValue value) => Add(new KeyValuePair<TKey, TValue>(key, value));

		/// <inheritdoc />
		public override void Add(KeyValuePair<TKey, TValue> item)
		{
			if (!TryAdd(item, false, out var Index, out _))
				throw new ArgumentException("An item with the same key has already been added.");

			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, Index);
		}

		/// <summary>
		/// Adds or updates an item in the Dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		/// <param name="index">Receives the index of the key/value pair</param>
		public void AddOrUpdate(TKey key, TValue value, out int index) => AddOrUpdate(new KeyValuePair<TKey, TValue>(key, value), out index);

		/// <summary>
		/// Adds or updates an item in the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <param name="index">Receives the index of the key/value pair</param>
		public void AddOrUpdate(KeyValuePair<TKey, TValue> item, out int index)
		{
			var OldSize = _Size;

			if (!TryAdd(item, true, out index, out var PreviousItem))
				return;

			if (_Size > OldSize)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
			else if (!EqualityComparer<TValue>.Default.Equals(item.Value, PreviousItem.Value)) // Only raise replace if we actually changed the value
				OnCollectionChanged(NotifyCollectionChangedAction.Replace, item, PreviousItem, index);
		}

		/// <summary>
		/// Adds a range of elements to the dictionary
		/// </summary>
		/// <param name="items">The elements to add</param>
		/// <exception cref="ArgumentException">The input elements have duplicated keys, or the key already exists in the dictionary</exception>
		public override void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			// Gather all the items to add, calculating their keys as we go
			var NewItems = items.Select(pair => new Entry { HashCode = Comparer.GetHashCode(pair.Key) & HashCodeMask, Item = pair }).ToArray();

			if (NewItems.Length == 0)
				return;

			// Get everything into HashCode order
			Array.Sort(NewItems);

			//****************************************

			var InsertIndex = _Size;
			var Entries = _Entries;

			// Check the new items don't have any duplicates
			{
				int Index = 0;

				while (Index < NewItems.Length)
				{
					int StartIndex = Index;
					var CurrentItem = NewItems[Index];

					// Find all the items that have the same Hash
					do
					{
						Index++;
					}
					while (Index < NewItems.Length && NewItems[Index].HashCode == CurrentItem.HashCode);

					// Is there more than one item with the same Hash?
					while (Index - StartIndex > 1)
					{
						// Compare the first item to the others
						for (int SubIndex = StartIndex + 1; SubIndex < Index; SubIndex++)
						{
							if (Comparer.Equals(CurrentItem.Key, NewItems[SubIndex].Key))
								throw new ArgumentException("Input collection has duplicates");
						}

						// Move up the first item
						StartIndex++;
					}
				}
			}

			// No duplicates in the new items. Check the keys aren't already in the Dictionary
			for (int Index = 0; Index < NewItems.Length; Index++)
			{
				var TotalCollisions = 0;
				var Item = NewItems[Index];

				// Find the bucket we belong to
				ref var Bucket = ref _Buckets[Item.HashCode % _Buckets.Length];
				var EntryIndex = Bucket - 1;

				// Check for collisions
				while (EntryIndex >= 0)
				{
					if (Entries[EntryIndex].HashCode == Item.HashCode && Comparer.Equals(Entries[EntryIndex].Key, Item.Key))
						throw new ArgumentException("An item with the same key has already been added.");

					EntryIndex = Entries[EntryIndex].NextIndex;

					if (TotalCollisions++ >= InsertIndex)
						throw new InvalidOperationException("State invalid");
				}
			}

			//****************************************

			// Ensure we have enough space for the new items
			EnsureCapacity(_Size + NewItems.Length);
			Entries = _Entries;

			var ItemsNotify = new KeyValuePair<TKey, TValue>[NewItems.Length];

			// Add the new items
			for (int Index = 0; Index < NewItems.Length; Index++)
			{
				ref var Entry = ref NewItems[Index];

				Entries[_Size++] = Entry;
				ItemsNotify[Index] = Entry.Item;
			}

			Reindex(_Buckets, Entries, InsertIndex, InsertIndex + NewItems.Length);

			OnCollectionChanged(NotifyCollectionChangedAction.Add, ItemsNotify, InsertIndex);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			if (_ShiftIndex.HasValue)
				throw new InvalidOperationException("Cannot modify in the middle of a remove operation");

			if (_Size > 0)
			{
				Array.Clear(_Buckets, 0, _Buckets.Length);
				Array.Clear(_Entries, 0, _Size);

				_Size = 0;

				OnCollectionChanged();
			}
		}

		/// <inheritdoc />
		public override bool Contains(KeyValuePair<TKey, TValue> item) => IndexOf(item) >= 0;

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if there is an element with this key, otherwise false</returns>
		public bool ContainsKey(TKey key) => IndexOfKey(key) >= 0;

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified value
		/// </summary>
		/// <param name="value">The value to search for</param>
		/// <returns>True if there is at least one element with this value, otherwise false</returns>
		public bool ContainsValue(TValue value) => IndexOfValue(value) >= 0;

		/// <inheritdoc />
		public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (arrayIndex + _Size > array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			for (int Index = 0; Index < _Size; Index++)
			{
				array[arrayIndex + Index] = GetByIndex(Index);
			}
		}

		/// <summary>
		/// Gets the key/value pair at the given index
		/// </summary>
		/// <param name="index">The index to retrieve</param>
		/// <returns>They key/value pair at the requested index</returns>
		public KeyValuePair<TKey, TValue> Get(int index) => GetByIndex(index);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public KeyValueEnumerator GetEnumerator() => new KeyValueEnumerator(this);

		/// <inheritdoc />
		public override int IndexOf(KeyValuePair<TKey, TValue> item)
		{
			var MyIndex = IndexOfKey(item.Key);

			// If we found the Key, make sure the Item matches
			if (MyIndex != -1 && !Equals(GetByIndex(MyIndex).Value, item.Value))
				MyIndex = -1;

			return MyIndex;
		}

		/// <summary>
		/// Searches for the specified key and returns the zero-based index within the ObservableDictionary
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if a matching key was found, otherwise -1</returns>
		public int IndexOfKey(TKey key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var HashCode = Comparer.GetHashCode(key) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			ref var Bucket = ref _Buckets[HashCode % _Buckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].HashCode == HashCode && Comparer.Equals(Entries[Index].Key, key))
					break;

				Index = Entries[Index].NextIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			if (_ShiftIndex.HasValue && Index > _ShiftIndex.Value)
				Index--;

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
				if (Equals(GetByIndex(Index).Value, value))
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
			if (_ShiftIndex.HasValue)
				throw new InvalidOperationException("Cannot modify in the middle of a remove operation");

			var Entries = _Entries;
			var Buckets = _Buckets;
			int Index = 0;

			// Find the first item we need to remove
			while (Index < _Size && !predicate(Entries[Index].Item))
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return 0;

			var RemovedItems = new List<KeyValuePair<TKey, TValue>>();

			// We'll reindex the dictionary at the same time
			Array.Clear(Buckets, 0, Buckets.Length);

			RemovedItems.Add(Entries[Index].Item);

			int InnerIndex = Index + 1;

			while (InnerIndex < _Size)
			{
				// Skip the items we need to remove
				while (InnerIndex < _Size && predicate(Entries[InnerIndex].Item))
				{
					RemovedItems.Add(Entries[InnerIndex].Item);

					InnerIndex++;
				}

				// If we reached the end, abort
				if (InnerIndex >= _Size)
					break;

				// We found one we're not removing, so move it up
				ref var Entry = ref Entries[Index];

				Entry = Entries[InnerIndex];

				// Reindex it
				ref var Bucket = ref Buckets[Entry.HashCode % Buckets.Length];
				var NextIndex = Bucket - 1;

				Entry.NextIndex = NextIndex;
				Entry.PreviousIndex = -1;

				if (NextIndex >= 0)
					Entries[NextIndex].PreviousIndex = Index;

				Bucket = Index + 1;

				Index++;
				InnerIndex++;
			}

			// Clear the removed items
			Array.Clear(_Entries, Index, _Size - Index);
			_Size = Index;

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, RemovedItems);

			return InnerIndex - Index;
		}

		/// <inheritdoc />
		public override void RemoveAt(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_ShiftIndex.HasValue)
				throw new InvalidOperationException("Cannot modify in the middle of a remove operation");

			// So with our first Remove, we reduce the Count and (if it's the last item in the list) we can just notify and be done with it
			// Otherwise, we first set ShiftIndex to the Entry we just removed. This allows the event handler to view us as if there's no gap in the Entry list

			// Once that's done, we can then clear the ShiftIndex, relocate the end item to the gap, and raise a Move event
			// The list is thus consistent at every notification. The only restriction is that you cannot (currently) edit the dictionary in the event handler

			var Entries = _Entries;

			ref var Entry = ref Entries[index];

			var Item = Entry.Item; // Copy the item for the notification
			var NextIndex = Entry.NextIndex;
			var PreviousIndex = Entry.PreviousIndex;

			// We're removing an entry, so fix up the linked list
			if (PreviousIndex >= 0)
			{
				if (NextIndex >= 0)
				{
					// We're neither the head or tail, so we can remove ourselves easily
					Entries[NextIndex].PreviousIndex = PreviousIndex;
					Entries[PreviousIndex].NextIndex = NextIndex;
				}
				else
				{
					// We're the head, so we can just update the entry before us
					Entries[PreviousIndex].NextIndex = NextIndex;
				}
			}
			else
			{
				if (NextIndex >= 0)
				{
					// We're the tail, so we need to update the entry after us
					Entries[NextIndex].PreviousIndex = -1;
				}

				// We're either the tail or a solitary entry (with no one before or after us)
				// Either way, we need to update the index stored in the Bucket
				_Buckets[Entry.HashCode % _Buckets.Length] = NextIndex + 1;
			}

			if (index == _Size -1)
			{
				// We're removing the last entry
				// Clear it, then raise the event, and we're done
				Entries[--_Size].Item = default;

				OnCollectionChanged(NotifyCollectionChangedAction.Remove, Item, index);

				return;
			}

			// We're not the last item in the list, so raising a Remove needs to look like a list-style shift down
			// ShiftIndex emulates this, altering all indexed operations to seek one higher when equal or above
			_ShiftIndex = index;
			_Size--;
			Entry.Item = default;

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, Item, index);

			// Now we copy the last entry to our previous index
			_ShiftIndex = null;
			Entry = Entries[_Size];
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

			// Clear the final entry
			Entries[_Size].Item = default;

			// We only need to notify of the move if we didn't just move the last item down one
			if (index != _Size - 1)
				OnCollectionChanged(NotifyCollectionChangedAction.Move, Item, index, _Size - 1);
		}

		/// <inheritdoc />
		public override void RemoveRange(int index, int count)
		{
			var LastIndex = index + count;

			if (index < 0 || LastIndex > _Size)
				throw new ArgumentOutOfRangeException("index");

			// Since a remove does not slide down the values above, and simply relocates the last item,
			// we need to remove in reverse so we don't accidentally move one of the items we plan to remove
			for (int Index = LastIndex - 1; Index >= index; Index--)
				RemoveAt(Index);
		}

		/// <inheritdoc />
		public override KeyValuePair<TKey, TValue>[] ToArray()
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
		public bool TryAdd(TKey key, TValue value) => TryAdd(new KeyValuePair<TKey, TValue>(key, value), out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<TKey, TValue> item) => TryAdd(item, out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="key">The key of the item</param>
		/// <param name="value">The value to associate with the key</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(TKey key, TValue value, out int index) => TryAdd(new KeyValuePair<TKey, TValue>(key, value), out index);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<TKey, TValue> item, out int index)
		{
			if (!TryAdd(item, false, out index, out var PreviousItem))
				return false;

			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);

			return true;
		}

		/// <summary>
		/// Searches the dictionary for a given key and returns the equal key, if any
		/// </summary>
		/// <param name="equalKey">The key to search for</param>
		/// <param name="actualKey">The key already in the collection, or the given key if not found</param>
		/// <returns>True if an equal key was found, otherwise False</returns>
		public bool TryGetKey(TKey equalKey, out TKey actualKey)
		{
			var Index = IndexOfKey(equalKey);

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
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key == null) throw new ArgumentNullException("key");

			var Index = IndexOfKey(key);

			if (Index == -1)
			{
				value = default;
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
		public bool TryRemove(TKey key, out TValue value)
		{
			var Index = IndexOfKey(key);

			if (Index == -1)
			{
				value = default;
				return false;
			}

			value = _Entries[Index].Value;

			RemoveAt(Index);

			return true;
		}

		//****************************************

		private bool TryAdd(KeyValuePair<TKey, TValue> item, bool replace, out int index, out KeyValuePair<TKey, TValue> previous)
		{
			if (item.Key == null)
				throw new ArgumentNullException(nameof(item));

			if (_ShiftIndex.HasValue)
				throw new InvalidOperationException("Cannot modify in the middle of a remove operation");

			if (_Buckets.Length == 0)
				EnsureCapacity(0);

			var HashCode = Comparer.GetHashCode(item.Key) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			ref var Bucket = ref _Buckets[HashCode % _Buckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].HashCode == HashCode && Comparer.Equals(Entries[Index].Key, item.Key))
				{
					// Key is the same
					if (replace)
					{
						previous = _Entries[Index].Item;

						_Entries[Index].Item = item;
					}
					else
					{
						previous = default;
					}

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
			previous = default;

			return true;
		}

		private void EnsureCapacity(int capacity)
		{
			int NewSize = (_Entries.Length == 0 ? 4 : _Entries.Length * 2);

			if (NewSize < capacity)
				NewSize = capacity;

			NewSize = HashUtil.GetPrime(NewSize);

			SetCapacity(NewSize);
		}

		private void SetCapacity(int size)
		{
			var NewBuckets = new int[size];
			var NewEntries = new Entry[size];

			Array.Copy(_Entries, 0, NewEntries, 0, _Size);

			Reindex(NewBuckets, NewEntries, 0 ,_Size);

			_Buckets = NewBuckets;
			_Entries = NewEntries;
		}

		private ref KeyValuePair<TKey, TValue> GetByIndex(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_ShiftIndex.HasValue && index >= _ShiftIndex.Value)
				index++;

			return ref _Entries[index].Item;
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

			Keys.OnCollectionChanged();
			Values.OnCollectionChanged();
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem, int index)
		{
			base.OnCollectionChanged(action, changedItem, index);

			if (IsUpdating)
				return;

			Keys.OnCollectionChanged(action, changedItem.Key, index);
			Values.OnCollectionChanged(action, changedItem.Value, index);
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem, int newIndex, int oldIndex)
		{
			base.OnCollectionChanged(action, changedItem, newIndex, oldIndex);

			if (IsUpdating)
				return;

			Keys.OnCollectionChanged(action, changedItem.Key, newIndex, oldIndex);
			Values.OnCollectionChanged(action, changedItem.Value, newIndex, oldIndex);
		}

		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem, int index)
		{
			base.OnCollectionChanged(action, newItem, oldItem, index);

			if (IsUpdating)
				return;

			Keys.OnCollectionChanged(action, newItem.Key, oldItem.Key, index);
			Values.OnCollectionChanged(action, newItem.Value, oldItem.Value, index);
		}
		
		/// <inheritdoc />
		protected override void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> changedItems)
		{
			base.OnCollectionChanged(action, changedItems);

			if (IsUpdating)
				return;

			Keys.OnCollectionChanged(action, changedItems.Select(pair => pair.Key));
			Values.OnCollectionChanged(action, changedItems.Select(pair => pair.Value));
		}

		/// <inheritdoc />
		protected override KeyValuePair<TKey, TValue> InternalGet(int index) => GetByIndex(index);

		/// <inheritdoc />
		protected override IEnumerator<KeyValuePair<TKey, TValue>> InternalGetEnumerator() => new KeyValueEnumerator(this);

		/// <inheritdoc />
		protected override void InternalInsert(int index, KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Cannot insert into a dictionary");

		/// <inheritdoc />
		protected override void InternalSet(int index, KeyValuePair<TKey, TValue> value)
		{
			ref var Entry = ref GetByIndex(index);
			var OldValue = Entry;
			
			if (!Equals(OldValue.Key, value.Key))
				throw new InvalidOperationException();

			if (Equals(OldValue.Value, value.Value))
				return;

			Entry = value;

			OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, OldValue, index);
		}

		//****************************************

		private static void Reindex(int[] buckets, Entry[] entries, int startIndex, int size)
		{
			// Reindex a range of entries
			for (int Index = startIndex; Index < size; Index++)
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

		//****************************************

		/// <inheritdoc />
		public override int Count => _Size;

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

				if (DefaultIndexer)
					return default;

				throw new KeyNotFoundException();
			}
			set
			{
				var Item = new KeyValuePair<TKey, TValue>(key, value);
				var OldSize = _Size;

				TryAdd(Item, true, out var Index, out var PreviousItem);

				if (_Size > OldSize)
					OnCollectionChanged(NotifyCollectionChangedAction.Add, Item, Index);
				else if (!EqualityComparer<TValue>.Default.Equals(value, PreviousItem.Value)) // Only raise replace if we actually changed the value
					OnCollectionChanged(NotifyCollectionChangedAction.Replace, Item, PreviousItem, Index);
			}
		}

		/// <summary>
		/// Gets/Sets the number of elements that the Observable Dictionary can contain.
		/// </summary>
		public int Capacity
		{
			get { return _Entries.Length; }
			set
			{
				value = HashUtil.GetPrime(value);

				if (value < _Size)
					throw new ArgumentOutOfRangeException(nameof(value));

				SetCapacity(value);
			}
		}

		/// <summary>
		/// Gets the equality comparer being used for the Key
		/// </summary>
		public IEqualityComparer<TKey> Comparer { get; }

		/// <summary>
		/// Gets/Sets whether indexer access will return the default for TValue when the key is not found
		/// </summary>
		/// <remarks>Improves functionality when used in certain data-binding situations. Defaults to False</remarks>
		public bool DefaultIndexer { get; set; }

		ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

		ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

		//****************************************

		private struct Entry : IComparable<Entry>
		{
			public int NextIndex;
			public int PreviousIndex;
			public int HashCode;
			public KeyValuePair<TKey, TValue> Item;
			public TKey Key => Item.Key;
			public TValue Value => Item.Value;

			int IComparable<Entry>.CompareTo(Entry other) => HashCode - other.HashCode;
		}

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
			public override bool Contains(TKey item) => _Parent.ContainsKey(item);

			/// <inheritdoc />
			public override void CopyTo(TKey[] array, int arrayIndex)
			{	//****************************************
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array[arrayIndex++] = _Parent.GetByIndex(Index).Key;
				}
			}

			/// <inheritdoc />
			public new KeyEnumerator GetEnumerator() => new KeyEnumerator(_Parent);

			/// <inheritdoc />
			public override int IndexOf(TKey item) => _Parent.IndexOfKey(item);

			//****************************************

			internal override void InternalCopyTo(Array array, int arrayIndex)
			{	//****************************************
				var MyValues = _Parent._Entries;
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array.SetValue(MyValues[Index].Key, arrayIndex++);
				}
			}

			internal override IEnumerator<TKey> InternalGetEnumerator() => new KeyEnumerator(_Parent);

			//****************************************

			/// <inheritdoc />
			public override TKey this[int index] => _Parent._Entries[index].Key;

			/// <inheritdoc />
			public override int Count => _Parent._Size;
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
			public override bool Contains(TValue item) => _Parent.ContainsValue(item);

			/// <inheritdoc />
			public override void CopyTo(TValue[] array, int arrayIndex)
			{	//****************************************
				var MyValues = _Parent._Entries;
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array[arrayIndex++] = _Parent.GetByIndex(Index).Value;
				}
			}

			/// <inheritdoc />
			public new ValueEnumerator GetEnumerator() => new ValueEnumerator(_Parent);

			/// <inheritdoc />
			public override int IndexOf(TValue item) => _Parent.IndexOfValue(item);

			//****************************************

			internal override void InternalCopyTo(Array array, int arrayIndex)
			{	//****************************************
				var MyValues = _Parent._Entries;
				var MySize = _Parent._Size;
				//****************************************

				for (int Index = 0; Index < MySize; Index++)
				{
					array.SetValue(MyValues[Index].Value, arrayIndex++);
				}
			}

			internal override IEnumerator<TValue> InternalGetEnumerator() => new ValueEnumerator(_Parent);

			//****************************************

			/// <inheritdoc />
			public override TValue this[int index] => _Parent._Entries[index].Value;

			/// <inheritdoc />
			public override int Count => _Parent._Size;
		}

		/// <summary>
		/// Enumerates the dictionary while avoiding memory allocations
		/// </summary>
		public struct KeyValueEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;

			private int _Index;
			//****************************************

			internal KeyValueEnumerator(ObservableDictionary<TKey, TValue> parent)
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

				Current = _Parent._Entries[_Index++].Item;

				return true;
			}

			void IEnumerator.Reset()
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
		/// Enumerates the dictionary keys while avoiding memory allocations
		/// </summary>
		public struct KeyEnumerator : IEnumerator<TKey>, IEnumerator
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;

			private int _Index;
			//****************************************

			internal KeyEnumerator(ObservableDictionary<TKey, TValue> parent)
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

				Current = _Parent._Entries[_Index++].Key;

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				Current = default;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public TKey Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		/// <summary>
		/// Enumerates the dictionary values while avoiding memory allocations
		/// </summary>
		public struct ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{	//****************************************
			private readonly ObservableDictionary<TKey, TValue> _Parent;

			private int _Index;
			//****************************************

			internal ValueEnumerator(ObservableDictionary<TKey, TValue> parent)
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

				Current = _Parent._Entries[_Index++].Value;

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				Current = default;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public TValue Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		private struct RangeKeyValue : IComparable<RangeKeyValue>
		{
			public RangeKeyValue(int keyHash, TKey key, TValue value)
			{
				KeyHash = keyHash;
				Key = key;
				Value = value;
				EstimatedIndex = 0;
			}

			//****************************************

			int IComparable<RangeKeyValue>.CompareTo(RangeKeyValue other) => KeyHash - other.KeyHash;

			//****************************************

			public int KeyHash { get; }

			public TKey Key { get; }

			public TValue Value { get; }

			public int EstimatedIndex { get; set; }
		}
	}
}