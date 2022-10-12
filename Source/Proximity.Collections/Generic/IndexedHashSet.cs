using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
	/// <summary>
	/// Provides a Hash Set implementation with efficient index-based access
	/// </summary>
	/// <typeparam name="T">The type of value</typeparam>
	public class IndexedHashSet<T> : ISet<T>, IList<T>, IList, IReadOnlyList<T>
#if !NETSTANDARD
		, IReadOnlySet<T>
#endif
	{ //****************************************
		private int[] _Keys;
		private T[] _Values;
		private int _Size;
		//****************************************

		/// <summary>
		/// Creates a new Indexed Hash Set
		/// </summary>
		public IndexedHashSet() : this(0, EqualityComparer<T>.Default)
		{

		}

		/// <summary>
		/// Creates a new Indexed Hash Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		public IndexedHashSet(IEnumerable<T> collection) : this(collection, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Creates a new Indexed Hash Set
		/// </summary>
		/// <param name="capacity">The starting capacity for the obserable set</param>
		public IndexedHashSet(int capacity) : this(capacity, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Creates a new Indexed Hash Set
		/// </summary>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public IndexedHashSet(IEqualityComparer<T> comparer) : this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new Indexed Hash Set
		/// </summary>
		/// <param name="capacity">The starting capacity for the obserable set</param>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public IndexedHashSet(int capacity, IEqualityComparer<T> comparer)
		{
			_Size = 0;
			_Keys = new int[capacity];
			_Values = new T[capacity];
			Comparer = comparer;
		}

		/// <summary>
		/// Creates a new Indexed Hash Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public IndexedHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
		{
			Comparer = comparer;

			// Ensure we add all the values in the order sorted by hash code
			_Values = collection.ToArray();
			_Size = _Values.Length;
			_Keys = new int[_Size];

			for (var Index = 0; Index < _Values.Length; Index++)
			{
				var Value = _Values[Index];

				_Keys[Index] = Value is not null ? comparer.GetHashCode(Value) : 0;
			}

			Array.Sort(_Keys, _Values, Comparer<int>.Default);
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <returns>A zero or positive index if the element was added, -1 if it was already in the set</returns>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public int Add(T item)
		{
			if (TryAdd(item, out var Index))
				return Index;

			return -1;
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
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(T item) => IndexOf(item) >= 0;

		/// <summary>
		/// Copies the contents of the collection to an array
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The offset to start copying to</param>
		public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_Values, 0, array, arrayIndex, _Size);

		/// <summary>
		/// Removes all items that are in the given collection from the current set
		/// </summary>
		/// <param name="other">The collection of items to remove</param>
		public void ExceptWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			var BitMask = new BitArray(Count);

			// Mark each item we want to remove
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				// If we find it in the set, mark it for removal
				if (Index != -1)
					BitMask.Set(Index, true);
			}

			// Remove everything we marked
			InternalRemoveAll(BitMask.Get);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public Enumerator GetEnumerator() => new(this);

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(T item)
		{ //****************************************
			var Key = item is not null ? Comparer.GetHashCode(item) : 0;
			var Index = Array.BinarySearch(_Keys, 0, _Size, Key);
			//****************************************

			// Is there a matching hash code?
			if (Index < 0)
				return -1;

			// BinarySearch is not guaranteed to return the first matching value, so we may need to move back
			while (Index > 0 && _Keys[Index - 1] == Key)
				Index--;

			for (; ; )
			{
				// Do we match this item?
				if (Comparer.Equals(_Values[Index], item))
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
		/// Modifies the current set so it only contains items present in the given collection
		/// </summary>
		/// <param name="other">The collection to compare against</param>
		public void IntersectWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			var BitMask = new BitArray(Count, true);

			// Mark each item we want to keep
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				// If we find it in the set, mark it for keeping
				if (Index != -1)
					BitMask.Set(Index, false);
			}

			// Remove everything we didn't clear
			InternalRemoveAll(BitMask.Get);
		}

		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			if (Count == 0)
				return other.Any(); // For a proper subset, there has to be at least one item in Other that isn't in us

			// If there are less items in the other collection than in us, we cannot be a subset
			if (other is ICollection<T> OtherCollection && OtherCollection.Count < Count)
				return false;

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;
			var FoundExtra = false;

			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index == -1)
					FoundExtra = true; // We found an item that isn't in our set
				else if (!BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// The set cannot have duplicates, so ensure our number of unique items matches
			return FoundExtra && Count == UniqueCount;
		}

		/// <summary>
		/// Checks whether this set is a strict superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict superset of the collection, otherwise False</returns>
		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			// If we're empty, we cannot be a proper superset
			if (Count == 0)
				return false;

			// If the other collection is empty, we're not, so we're automatically a superset
			if (other is ICollection<T> OtherCollection)
			{
				if (OtherCollection.Count == 0)
					return true;

				// There could be more items in the enumeration, but they might also be duplicates, so we can't rely on comparing sizes
			}
			else if (!other.Any())
			{
				return true;
			}

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;

			// Check that we have every item in the enumeration
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index == -1)
					return false; // Doesn't match, so we're not a superset
				else if (!BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// Ensure we have at least one more item than in other
			return Count > UniqueCount;
		}

		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			if (Count == 0)
				return true;

			// If there are less items in the other collection than in us, we cannot be a subset
			if (other is ICollection<T> OtherCollection && OtherCollection.Count < Count)
				return false;

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;

			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index != -1 && !BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// Ensure every item was accounted for
			return Count == UniqueCount;
		}

		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			// If the other collection is empty, we're automatically a superset
			if (!other.Any())
				return true;

			// If we're empty, we cannot be a superset, since the other collection has items
			if (Count == 0)
				return false;

			// There could be more items in the enumeration, but they might also be duplicates, so we can't rely on comparing sizes

			// Check that we have every item in the enumeration
			foreach (var MyItem in other)
			{
				if (!Contains(MyItem))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			// If there's no items, we can't overlap
			if (Count == 0)
				return false;

			foreach (var MyItem in other)
			{
				if (Contains(MyItem))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		public bool Remove(T item)
		{ //****************************************
			var Index = IndexOf(item);
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
				throw new ArgumentOutOfRangeException(nameof(index));

			_Size--;

			// If this is in the middle, move the values down
			if (index < _Size)
			{
				Array.Copy(_Keys, index + 1, _Keys, index, _Size - index);
				Array.Copy(_Values, index + 1, _Values, index, _Size - index);
			}

			// Ensure we don't hold a reference to the value
			_Values[_Size] = default!;
		}

		/// <summary>
		/// Checks whether this set and the given collection contain the same elements
		/// </summary>
		/// <param name="other">The collection to check against</param>
		/// <returns>True if they contain the same elements, otherwise FAlse</returns>
		public bool SetEquals(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			// If we've no items, ensure the other collection is also empty
			if (Count == 0)
				return other.Any();

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;

			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index == -1)
					return false; // Doesn't match, so we're not equal
				else if (!BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// Ensure our number of unique items matches
			return Count == UniqueCount;
		}

		/// <summary>
		/// Modifies the set so it only contains items that are present in the set, or in the given collection, but not both
		/// </summary>
		/// <param name="other">The collection to compare against</param>
		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			var BitMask = new BitArray(Count);
			var NewItems = new List<T>();

			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index == -1)
					NewItems.Add(MyItem); // Item does not exist in our set, so add it later
				else
					BitMask.Set(Index, true); // Item is in our set, so mark it for removal
			}

			// Remove everything we marked
			InternalRemoveAll(BitMask.Get);

			// Add the items we didn't find. Duplicates will be ignored
			UnionWith(NewItems);
		}

		/// <summary>
		/// Ensures the current set contains all the items in the given collection
		/// </summary>
		/// <param name="other">The items to add to the set</param>
		public void UnionWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			var NewItems = new List<T>(other);

			// If there's no items in the collection, don't do anything
			if (NewItems.Count == 0)
				return;

			// Try and add the new items
			foreach (var MyItem in NewItems)
			{
				TryAdd(MyItem, out _);
			}
		}

		//****************************************

		bool ISet<T>.Add(T item) => TryAdd(item, out _);

		int IList.Add(object? value)
		{
			if (value is T NewValue)
				return Add(NewValue);

			throw new ArgumentException("Not a supported value", nameof(value));
		}

		void ICollection<T>.Add(T item) => TryAdd(item, out _);

		bool IList.Contains(object? value)
		{
			if (value is T Value)
				return Contains(Value);

			return false;
		}

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

		int IList.IndexOf(object? value)
		{
			if (value is T Value)
				return IndexOf(Value);

			return -1;
		}

		void IList.Insert(int index, object? value) => throw new NotSupportedException();

		void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

		void IList.Remove(object? value)
		{
			if (value is T Value)
				Remove(Value);
		}

		void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

		//****************************************

		private void EnsureCapacity(int capacity)
		{
			var num = (_Keys.Length == 0 ? 4 : _Keys.Length * 2);

			if (num > 0x7FEFFFFF)
				num = 0x7FEFFFFF;

			if (num < capacity)
				num = capacity;

			Capacity = num;
		}

		private bool TryAdd(T item, out int insertIndex)
		{ //****************************************
			var Key = item is not null ? Comparer.GetHashCode(item) : 0;
			var Index = Array.BinarySearch(_Keys, 0, _Size, Key);
			//****************************************

			// Is there a matching hash code?
			if (Index >= 0)
			{
				// BinarySearch is not guaranteed to return the first matching value, so we may need to move back
				while (Index > 0 && _Keys[Index - 1] == Key)
					Index--;

				for (; ; )
				{
					// Do we match this item?
					if (Comparer.Equals(_Values[Index], item))
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

		private void Insert(int index, int key, T value)
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

		private T InternalGet(int index)
		{
			if (index < 0 || index >= _Size)
				throw new ArgumentOutOfRangeException(nameof(index));

			return _Values[index];
		}

		private int InternalRemoveAll(Func<int, bool> predicate)
		{
			var Index = 0;

			// Find the first item we need to remove
			while (Index < _Size && !predicate(Index))
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return 0;

			var InnerIndex = Index + 1;

			while (InnerIndex < _Size)
			{
				// Skip the items we need to remove
				while (InnerIndex < _Size && predicate(InnerIndex))
					InnerIndex++;

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

			return InnerIndex - Index;
		}

		//****************************************

		/// <summary>
		/// Gets the IEqualityComparer{TValue} that is used to compare items in the set
		/// </summary>
		public IEqualityComparer<T> Comparer { get; }

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => _Size;

		/// <summary>
		/// Gets or Sets the number of elements that the Observable Set can contain.
		/// </summary>
		public int Capacity
		{
			get => _Keys.Length;
			set
			{
				if (value == _Keys.Length)
					return;

				if (value < _Size)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (value == 0)
				{
					_Keys = Empty.Array<int>();
					_Values = Empty.Array<T>();

					return;
				}
				var NewKeys = new int[value];
				var NewValues = new T[value];

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
		public T this[int index] => _Values[index];

		T IReadOnlyList<T>.this[int index] => InternalGet(index);

		T IList<T>.this[int index]
		{
			get => InternalGet(index);
			set => throw new NotSupportedException();
		}

		object? IList.this[int index]
		{
			get => InternalGet(index);
			set => throw new NotSupportedException();
		}

		bool ICollection<T>.IsReadOnly => false;

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		object ICollection.SyncRoot => this;

		bool ICollection.IsSynchronized => false;

		//****************************************

		/// <summary>
		/// Provides a key/value pair enumerator
		/// </summary>
		public struct Enumerator : IEnumerator<T>, IEnumerator
		{ //****************************************
			private readonly IndexedHashSet<T> _Parent;

			private int _Index;
			//****************************************

			internal Enumerator(IndexedHashSet<T> parent)
			{
				_Parent = parent;
				_Index = 0;
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				Current = default!;
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
					Current = default!;

					return false;
				}

				Current = _Parent._Values[_Index++];

				return true;
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				Current = default!;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public T Current { get; private set; }

			object IEnumerator.Current => Current!;
		}
	}
}
