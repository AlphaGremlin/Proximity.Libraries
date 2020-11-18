using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Proximity.Collections;
//****************************************

namespace System.Collections.Generic
{
	/// <summary>
	/// Provides construction methods for the bi-directional Dictionary
	/// </summary>
	public static class BiDictionary
	{
		/// <summary>
		/// Creates a new pre-filled bi-directional dictionary with the specified comparers
		/// </summary>
		/// <typeparam name="TLeft">The left type (Key for normal, Value for inverse)</typeparam>
		/// <typeparam name="TRight">The right type (Value for normal, Key for inverse)</typeparam>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="leftComparer">The equality comparer to use for <typeparamref name="TLeft"/></param>
		/// <param name="rightComparer">The equality comparer to use for <typeparamref name="TRight"/></param>
		/// <returns>The new populated bi-directional dictionary</returns>
		/// <remarks>The items are not guaranteed to be stored in the provided order</remarks>
		public static BiDictionary<TLeft, TRight> From<TLeft, TRight>(IEnumerable<KeyValuePair<TLeft, TRight>> dictionary, IEqualityComparer<TLeft>? leftComparer = null, IEqualityComparer<TRight>? rightComparer = null) where TLeft : notnull where TRight : notnull
		{
			return new BiDictionary<TLeft, TRight>(dictionary, leftComparer ?? EqualityComparer<TLeft>.Default, rightComparer ?? EqualityComparer<TRight>.Default);
		}

		/// <summary>
		/// Creates a new pre-filled bi-directional dictionary with the specified comparers
		/// </summary>
		/// <typeparam name="TLeft">The left type (Key for normal, Value for inverse)</typeparam>
		/// <typeparam name="TRight">The right type (Value for normal, Key for inverse)</typeparam>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="leftComparer">The equality comparer to use for <typeparamref name="TLeft"/></param>
		/// <param name="rightComparer">The equality comparer to use for <typeparamref name="TRight"/></param>
		/// <returns>The new populated bi-directional dictionary</returns>
		/// <remarks>The items are not guaranteed to be stored in the provided order</remarks>
		public static BiDictionary<TLeft, TRight> FromInverse<TLeft, TRight>(IEnumerable<KeyValuePair<TRight, TLeft>> dictionary, IEqualityComparer<TLeft>? leftComparer = null, IEqualityComparer<TRight>? rightComparer = null) where TLeft : notnull where TRight : notnull
		{
			return new BiDictionary<TLeft, TRight>(dictionary.Select(pair => new KeyValuePair<TLeft, TRight>(pair.Value, pair.Key)), leftComparer ?? EqualityComparer<TLeft>.Default, rightComparer ?? EqualityComparer<TRight>.Default);
		}
	}

	/// <summary>
	/// Provides a bi-directional Dictionary that can perform lookups on either TFirst or TSecond
	/// </summary>
	/// <typeparam name="TLeft">The left type (Key for normal, Value for inverse)</typeparam>
	/// <typeparam name="TRight">The right type (Value for normal, Key for inverse)</typeparam>
	public partial class BiDictionary<TLeft, TRight> : IDictionary<TLeft, TRight>, IReadOnlyDictionary<TLeft, TRight>, IList<KeyValuePair<TLeft, TRight>>, IReadOnlyList<KeyValuePair<TLeft, TRight>>, IList, IDictionary where TLeft : notnull where TRight : notnull
	{	//****************************************
		private const int HashCodeMask = 0x7FFFFFFF;
		//****************************************
		private int[] _LeftBuckets;
		private int[] _RightBuckets;
		private Entry[] _Entries;

		private int _Size;
		//****************************************

		/// <summary>
		/// Creates a new, empty bi-directional dictionary
		/// </summary>
		public BiDictionary() : this(0, EqualityComparer<TLeft>.Default, EqualityComparer<TRight>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty bi-directional dictionary with the specified default capacity
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		public BiDictionary(int capacity) : this(capacity, EqualityComparer<TLeft>.Default, EqualityComparer<TRight>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty bi-directional dictionary with the specified Left comparer
		/// </summary>
		/// <param name="leftComparer">The equality comparer to use for <typeparamref name="TLeft"/></param>
		public BiDictionary(IEqualityComparer<TLeft> leftComparer) : this(0, leftComparer, EqualityComparer<TRight>.Default)
		{
		}

		/// <summary>
		/// Creates a new empty bi-directional dictionary with the specified Right comparer
		/// </summary>
		/// <param name="rightComparer">The equality comparer to use for <typeparamref name="TRight"/></param>
		public BiDictionary(IEqualityComparer<TRight> rightComparer) : this(0, EqualityComparer<TLeft>.Default, rightComparer)
		{
		}

		/// <summary>
		/// Creates a new empty observable dictionary with the specified comparers
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		/// <param name="leftComparer">The equality comparer to use for <typeparamref name="TLeft"/></param>
		/// <param name="rightComparer">The equality comparer to use for <typeparamref name="TRight"/></param>
		public BiDictionary(int capacity, IEqualityComparer<TLeft> leftComparer, IEqualityComparer<TRight> rightComparer)
		{
			capacity = HashUtil.GetPrime(capacity);

			_LeftBuckets = new int[capacity];
			_RightBuckets = new int[capacity];
			_Entries = new Entry[capacity];
			LeftComparer = leftComparer;
			RightComparer = rightComparer;

			Lefts = new LeftCollection(this);
			Rights = new RightCollection(this);
			Inverse = new InverseDictionary(this);
		}

		/// <summary>
		/// Creates a new pre-filled bi-directional dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <remarks>The items are not guaranteed to be stored in the provided index order</remarks>
		public BiDictionary(IEnumerable<KeyValuePair<TLeft, TRight>> dictionary) : this(dictionary, EqualityComparer<TLeft>.Default, EqualityComparer<TRight>.Default)
		{
		}

		/// <summary>
		/// Creates a new pre-filled bi-directional dictionary with the specified Left comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="leftComparer">The equality comparer to use for <typeparamref name="TLeft"/></param>
		/// <remarks>The items are not guaranteed to be stored in the provided index order</remarks>
		public BiDictionary(IEnumerable<KeyValuePair<TLeft, TRight>> dictionary, IEqualityComparer<TLeft> leftComparer) : this(dictionary, leftComparer, EqualityComparer<TRight>.Default)
		{
		}

		/// <summary>
		/// Creates a new pre-filled bi-directional dictionary with the specified Right comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="rightComparer">The equality comparer to use for <typeparamref name="TRight"/></param>
		/// <remarks>The items are not guaranteed to be stored in the provided index order</remarks>
		public BiDictionary(IEnumerable<KeyValuePair<TLeft, TRight>> dictionary, IEqualityComparer<TRight> rightComparer) : this(dictionary, EqualityComparer<TLeft>.Default, rightComparer)
		{
		}

		/// <summary>
		/// Creates a new pre-filled bi-directional dictionary with the specified comparers
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="leftComparer">The equality comparer to use for <typeparamref name="TLeft"/></param>
		/// <param name="rightComparer">The equality comparer to use for <typeparamref name="TRight"/></param>
		/// <remarks>The items are not guaranteed to be stored in the provided index order</remarks>
		public BiDictionary(IEnumerable<KeyValuePair<TLeft, TRight>> dictionary, IEqualityComparer<TLeft> leftComparer, IEqualityComparer<TRight> rightComparer)
		{
			LeftComparer = leftComparer;
			RightComparer = rightComparer;

			_Size = dictionary.Count();
			var Capacity = HashUtil.GetPrime(_Size);

			_LeftBuckets = new int[Capacity];
			_RightBuckets = new int[Capacity];
			_Entries = new Entry[Capacity];

			{
				var Index = 0;

				foreach (var MyPair in dictionary)
				{
					ref var Entry = ref _Entries[Index++];

					Entry.Item = MyPair;
					Entry.LeftHashCode = leftComparer.GetHashCode(MyPair.Key) & HashCodeMask;
					Entry.RightHashCode = rightComparer.GetHashCode(MyPair.Value) & HashCodeMask;
				}
			}

			// Check the new items don't have any duplicates
			VerifyDistinct(_Entries, _Size, leftComparer, rightComparer);

			Reindex(_LeftBuckets, _RightBuckets, _Entries, 0, _Size);

			Lefts = new LeftCollection(this);
			Rights = new RightCollection(this);
			Inverse = new InverseDictionary(this);
		}

		//****************************************

		/// <summary>
		/// Adds a new element to the Dictionary
		/// </summary>
		/// <param name="left">The key of the item to add</param>
		/// <param name="right">The value of the item to add</param>
		public void Add(TLeft left, TRight right)
		{
			if (!TryAdd(new KeyValuePair<TLeft, TRight>(left, right), false, out _))
				throw new ArgumentException("Left or right already exist in the dictionary.");
		}

		/// <summary>
		/// Adds a new element to the Dictionary
		/// </summary>
		/// <param name="item">The element to add</param>
		public void Add(KeyValuePair<TLeft, TRight> item)
		{
			if (!TryAdd(item, false, out _))
				throw new ArgumentException("Left or right already exist in the dictionary.");
		}

		/// <summary>
		/// Adds a range of elements to the dictionary
		/// </summary>
		/// <param name="items">The elements to add</param>
		/// <exception cref="ArgumentException">The input elements have duplicated keys, or the key already exists in the dictionary</exception>
		public void AddRange(IEnumerable<KeyValuePair<TLeft, TRight>> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			// Gather all the items to add, calculating their keys as we go
			var NewItems = items.Select(pair => new Entry
			{
				LeftHashCode = LeftComparer.GetHashCode(pair.Key) & HashCodeMask,
				RightHashCode = RightComparer.GetHashCode(pair.Value) & HashCodeMask,
				Item = pair
			}).ToArray();

			if (NewItems.Length == 0)
				return;

			//****************************************

			var InsertIndex = _Size;
			var Entries = _Entries;

			// Check the new items don't have any duplicates
			VerifyDistinct(NewItems, NewItems.Length, LeftComparer, RightComparer);

			// No duplicates in the new items. Check the keys aren't already in the Dictionary
			if (_Size > 0)
			{
				for (var Index = 0; Index < NewItems.Length; Index++)
				{
					var TotalCollisions = 0;
					var Item = NewItems[Index];

					ref var Bucket = ref _LeftBuckets[Item.LeftHashCode % _LeftBuckets.Length];
					var EntryIndex = Bucket - 1;

					// Check for collisions on the left
					while (EntryIndex >= 0)
					{
						if (Entries[EntryIndex].LeftHashCode == Item.LeftHashCode && LeftComparer.Equals(Entries[EntryIndex].Left, Item.Left))
							throw new ArgumentException("An item with the same left value has already been added.");

						EntryIndex = Entries[EntryIndex].NextLeftIndex;

						if (TotalCollisions++ >= InsertIndex)
							throw new InvalidOperationException("State invalid");
					}

					TotalCollisions = 0;
					Bucket = ref _RightBuckets[Item.RightHashCode % _RightBuckets.Length];
					EntryIndex = Bucket - 1;

					// Check for collisions on the right
					while (EntryIndex >= 0)
					{
						if (Entries[EntryIndex].RightHashCode == Item.RightHashCode && RightComparer.Equals(Entries[EntryIndex].Right, Item.Right))
							throw new ArgumentException("An item with the same right value has already been added.");

						EntryIndex = Entries[EntryIndex].NextRightIndex;

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

			Reindex(_LeftBuckets, _RightBuckets, Entries, InsertIndex, InsertIndex + NewItems.Length);
		}

		/// <summary>
		/// Clears all elements from the Dictionary
		/// </summary>
		public void Clear()
		{
			if (_Size > 0)
			{
				Array.Clear(_LeftBuckets, 0, _LeftBuckets.Length);
				Array.Clear(_RightBuckets, 0, _RightBuckets.Length);
				Array.Clear(_Entries, 0, _Size);

				_Size = 0;
			}
		}

		/// <summary>
		/// Determines if the Dictionary contains a specific Left and Right pair
		/// </summary>
		/// <param name="item">The element to check for</param>
		/// <returns>True if the pair exists, otherwise False</returns>
		public bool Contains(KeyValuePair<TLeft, TRight> item)
		{
			if (_Size == 0)
				return false;

			var HashCode = LeftComparer.GetHashCode(item.Key) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			ref var Bucket = ref _LeftBuckets[HashCode % _LeftBuckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].LeftHashCode == HashCode && LeftComparer.Equals(Entries[Index].Left, item.Key))
					return RightComparer.Equals(Entries[Index].Right, item.Value); // Matched on Key, check the Value as well

				Index = Entries[Index].NextLeftIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			return false;
		}

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified Left
		/// </summary>
		/// <param name="left">The Left to search for</param>
		/// <returns>True if there is an element with this Left, otherwise false</returns>
		public bool ContainsLeft(TLeft left) => IndexOfLeft(left) >= 0;

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified Right
		/// </summary>
		/// <param name="right">The Right to search for</param>
		/// <returns>True if there is an element with this Right, otherwise false</returns>
		public bool ContainsRight(TRight right) => IndexOfRight(right) >= 0;

		/// <summary>
		/// Copies the contents of the Dictionary to an array
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The offset to start copying to</param>
		public void CopyTo(KeyValuePair<TLeft, TRight>[] array, int arrayIndex)
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
		public KeyValuePair<TLeft, TRight> Get(int index) => GetByIndex(index);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public Enumerator GetEnumerator() => new Enumerator(this);

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="left">The key of the item to lookup</param>
		/// <param name="right">The value of the item to lookup</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TLeft left, TRight right) => IndexOf(new KeyValuePair<TLeft, TRight>(left, right));

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(KeyValuePair<TLeft, TRight> item)
		{
			if (_Size == 0)
				return -1;

			var HashCode = LeftComparer.GetHashCode(item.Key) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			ref var Bucket = ref _LeftBuckets[HashCode % _LeftBuckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].LeftHashCode == HashCode && LeftComparer.Equals(Entries[Index].Left, item.Key))
				{
					// Matched on Left, check Right as well
					if (RightComparer.Equals(Entries[Index].Right, item.Value))
						break;

					return -1; // Right value doesn't match
				}

				Index = Entries[Index].NextLeftIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			return Index;
		}

		/// <summary>
		/// Finds the index of a particular left value
		/// </summary>
		/// <param name="left">The left value to lookup</param>
		/// <returns>The index of the left value, if found, otherwise -1</returns>
		public int IndexOfLeft(TLeft left)
		{
			if (left == null)
				throw new ArgumentNullException(nameof(left));

			if (_Size == 0)
				return -1;

			var HashCode = LeftComparer.GetHashCode(left) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			ref var Bucket = ref _LeftBuckets[HashCode % _LeftBuckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].LeftHashCode == HashCode && LeftComparer.Equals(Entries[Index].Left, left))
					break;

				Index = Entries[Index].NextLeftIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			return Index;
		}

		/// <summary>
		/// Finds the index of a particular right value
		/// </summary>
		/// <param name="right">The right value to lookup</param>
		/// <returns></returns>
		public int IndexOfRight(TRight right)
		{
			if (right == null)
				throw new ArgumentNullException(nameof(right));

			if (_Size == 0)
				return -1;

			var HashCode = RightComparer.GetHashCode(right) & HashCodeMask;
			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			ref var Bucket = ref _RightBuckets[HashCode % _RightBuckets.Length];
			var Index = Bucket - 1;

			// Check for collisions
			while (Index >= 0)
			{
				if (Entries[Index].RightHashCode == HashCode && RightComparer.Equals(Entries[Index].Right, right))
					return Index;

				Index = Entries[Index].NextRightIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			return Index;
		}

		/// <summary>
		/// Removes an element from the dictionary
		/// </summary>
		/// <param name="left">The Left of the element to remove</param>
		public bool Remove(TLeft left)
		{
			if (left == null) throw new ArgumentNullException(nameof(left));

			var Index = IndexOfLeft(left);

			if (Index == -1)
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <summary>
		/// Removes a specific Left and Right pair from the dictionary
		/// </summary>
		/// <param name="item">The element to remove</param>
		public bool Remove(KeyValuePair<TLeft, TRight> item)
		{
			if (item.Key == null || item.Value == null) throw new ArgumentNullException("The left or right values are null", nameof(item));

			var Index = IndexOf(item);

			if (Index == -1)
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <summary>
		/// Removes all elements that match a predicate
		/// </summary>
		/// <param name="predicate">A predicate that returns true for each item to remove</param>
		/// <returns>The total number of items removed</returns>
		public int RemoveAll(Predicate<KeyValuePair<TLeft, TRight>> predicate)
		{
			var Entries = _Entries;
			var LeftBuckets = _LeftBuckets;
			var RightBuckets = _RightBuckets;
			var Index = 0;

			// Find the Left item we need to remove
			while (Index < _Size && !predicate(Entries[Index].Item))
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return 0;

			var RemovedItems = new List<KeyValuePair<TLeft, TRight>>() { Entries[Index].Item };

			var InnerIndex = Index + 1;

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
				ref var LeftBucket = ref LeftBuckets[Entry.LeftHashCode % LeftBuckets.Length];
				ref var RightBucket = ref RightBuckets[Entry.RightHashCode % RightBuckets.Length];
				var NextLeftIndex = LeftBucket - 1;
				var NextRightIndex = RightBucket - 1;

				Entry.NextLeftIndex = NextLeftIndex;
				Entry.NextRightIndex = NextRightIndex;
				Entry.PreviousLeftIndex = -1;
				Entry.PreviousRightIndex = -1;

				if (NextLeftIndex >= 0)
					Entries[NextLeftIndex].PreviousLeftIndex = Index;

				if (NextRightIndex >= 0)
					Entries[NextRightIndex].PreviousRightIndex = Index;

				LeftBucket = RightBucket = Index + 1;

				Index++;
				InnerIndex++;
			}

			// Clear the removed item(s)
			Array.Clear(_Entries, Index, _Size - Index);
			_Size = Index;

			return InnerIndex - Index;
		}

		/// <summary>
		/// Removes the element at the specified index
		/// </summary>
		/// <param name="index">The index of the element to remove</param>
		public void RemoveAt(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			// So with our Left Remove, we reduce the Count and (if it's the last item in the list) we can just notify and be done with it
			// Otherwise, we Left set ShiftIndex to the Entry we just removed. This allows the event handler to view us as if there's no gap in the Entry list

			// Once that's done, we can then clear the ShiftIndex, relocate the end item to the gap, and raise a Move event
			// The list is thus consistent at every notification. The only restriction is that you cannot (currently) edit the dictionary in the event handler

			var Entries = _Entries;

			ref var Entry = ref Entries[index];

			// We're removing an entry, so fix up the left linked list
			if (Entry.NextLeftIndex >= 0)
			{
				// There's someone after us. Adjust them to point to the entry before us. If there's nobody, they will become the new tail
				Entries[Entry.NextLeftIndex].PreviousLeftIndex = Entry.PreviousLeftIndex;
			}

			if (Entry.PreviousLeftIndex >= 0)
			{
				// There's someone before us. Adjust them to point to the entry after us. If there's nobody, they will become the new head
				Entries[Entry.PreviousLeftIndex].NextLeftIndex = Entry.NextLeftIndex;
			}
			else
			{
				// We're either the tail or a solitary entry (with no one before or after us)
				// Either way, we need to update the index stored in the Bucket
				_LeftBuckets[Entry.LeftHashCode % _LeftBuckets.Length] = Entry.NextLeftIndex + 1;
			}

			// Fix up the right linked list
			if (Entry.NextRightIndex >= 0)
			{
				// There's someone after us. Adjust them to point to the entry before us. If there's nobody, they will become the new tail
				Entries[Entry.NextRightIndex].PreviousRightIndex = Entry.PreviousRightIndex;
			}

			if (Entry.PreviousRightIndex >= 0)
			{
				// There's someone before us. Adjust them to point to the entry after us. If there's nobody, they will become the new head
				Entries[Entry.PreviousRightIndex].NextRightIndex = Entry.NextRightIndex;
			}
			else
			{
				// We're either the tail or a solitary entry (with no one before or after us)
				// Either way, we need to update the index stored in the Bucket
				_RightBuckets[Entry.RightHashCode % _RightBuckets.Length] = Entry.NextRightIndex + 1;
			}

			if (index == _Size - 1)
			{
				// We're removing the last entry. Clear it and we're done
				Entries[--_Size] = default;

				return;
			}

			// We're not removing the last entry, so we need to copy that entry to our previous position
			// This ensures there are no gaps in the Entry table, allowing us to use direct array indexing at the cost of items moving around after a remove
			_Size--;
			Entry.Item = default;
			Entry = Entries[_Size];

			// Since this entry has been relocated, we need to fix up the linked lists here too
			if (Entry.NextLeftIndex >= 0)
			{
				// There's at least one entry ahead of us, correct it to point to our new location
				Entries[Entry.NextLeftIndex].PreviousLeftIndex = index;
			}

			if (Entry.PreviousLeftIndex >= 0)
			{
				// There's at least one entry behind us, correct it to point to us
				Entries[Entry.PreviousLeftIndex].NextLeftIndex = index;
			}
			else
			{
				// There's no entry behind us, so we're the tail or a solitary item. Correct the bucket index
				_LeftBuckets[Entry.LeftHashCode % _LeftBuckets.Length] = index + 1;
			}

			if (Entry.NextRightIndex >= 0)
			{
				// There's at least one entry ahead of us, correct it to point to our new location
				Entries[Entry.NextRightIndex].PreviousRightIndex = index;
			}

			if (Entry.PreviousRightIndex >= 0)
			{
				// There's at least one entry behind us, correct it to point to us
				Entries[Entry.PreviousRightIndex].NextRightIndex = index;
			}
			else
			{
				// There's no entry behind us, so we're the tail or a solitary item. Correct the bucket index
				_RightBuckets[Entry.RightHashCode % _RightBuckets.Length] = index + 1;
			}

			// Clear the final entry
			Entries[_Size] = default;
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

			// Since a remove does not slide down the values above, and simply relocates the last item,
			// we need to remove in reverse so we don't accidentally move one of the items we plan to remove
			for (var Index = LastIndex; Index >= index; Index--)
				RemoveAt(Index);
		}

		/// <summary>
		/// Converts the contents of the Dictionary to an array
		/// </summary>
		/// <returns>The resulting array</returns>
		public KeyValuePair<TLeft, TRight>[] ToArray()
		{
			var Copy = new KeyValuePair<TLeft, TRight>[_Size];

			CopyTo(Copy, 0);

			return Copy;
		}

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="left">The key of the item</param>
		/// <param name="right">The value to associate with the key</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(TLeft left, TRight right) => TryAdd(new KeyValuePair<TLeft, TRight>(left, right), false, out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="left">The key of the item</param>
		/// <param name="right">The value to associate with the key</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(TLeft left, TRight right, out int index) => TryAdd(new KeyValuePair<TLeft, TRight>(left, right), false, out index);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<TLeft, TRight> item) => TryAdd(item, false, out _);

		/// <summary>
		/// Tries to add an item to the Dictionary
		/// </summary>
		/// <param name="item">The key/value pair to add</param>
		/// <param name="index">Receives the index of the new key/value pair, if added</param>
		/// <returns>True if the item was added, otherwise False</returns>
		public bool TryAdd(KeyValuePair<TLeft, TRight> item, out int index) => TryAdd(item, false, out index);

		/// <summary>
		/// Gets the Left associated with the specified Right
		/// </summary>
		/// <param name="right">The Right whose Left to get</param>
		/// <param name="left">When complete, contains the Left associated with the given Right, otherwise the default value for the type</param>
		/// <returns>True if the Right was found, otherwise false</returns>
		public bool TryGetLeft(TRight right,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TLeft left)
		{
			if (right == null)
				throw new ArgumentNullException(nameof(right));

			var Index = IndexOfRight(right);

			if (Index == -1)
			{
				left = default!;
				return false;
			}

			left = _Entries[Index].Left;

			return true;
		}

		/// <summary>
		/// Gets the Right associated with the specified Left
		/// </summary>
		/// <param name="left">The Left whose Right to get</param>
		/// <param name="right">When complete, contains the Right associated with the given Left, otherwise the default value for the type</param>
		/// <returns>True if the Left was found, otherwise false</returns>
		public bool TryGetRight(TLeft left,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TRight right)
		{
			if (left == null) throw new ArgumentNullException(nameof(left));

			var Index = IndexOfLeft(left);

			if (Index == -1)
			{
				right = default!;
				return false;
			}

			right = _Entries[Index].Right;

			return true;
		}

		/// <summary>
		/// Tries to remove an item from the dictionary
		/// </summary>
		/// <param name="left">The key of the item to remove</param>
		/// <param name="right">Receives the value of the removed item</param>
		/// <returns>True if the item was removed, otherwise False</returns>
		public bool TryRemove(TLeft left,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TRight right)
		{
			var Index = IndexOfLeft(left);

			if (Index == -1)
			{
				right = default!;
				return false;
			}

			right = _Entries[Index].Right;

			RemoveAt(Index);

			return true;
		}

		//****************************************

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
			var LeftBuckets = new int[size];
			var RightBuckets = new int[size];
			var NewEntries = new Entry[size];

			Array.Copy(_Entries, 0, NewEntries, 0, _Size);

			Reindex(LeftBuckets, RightBuckets, NewEntries, 0, _Size);

			_LeftBuckets = LeftBuckets;
			_RightBuckets = RightBuckets;
			_Entries = NewEntries;
		}

		private ref KeyValuePair<TLeft, TRight> GetByIndex(int index)
		{
			if (index >= _Size || index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref _Entries[index].Item;
		}

		private bool TryAdd(KeyValuePair<TLeft, TRight> item, bool replace, out int index)
		{
			if (item.Key == null || item.Value == null)
				throw new ArgumentNullException("The key or value are null", nameof(item));

			if (_LeftBuckets.Length == 0)
				EnsureCapacity(0);

			var Entries = _Entries;
			var TotalCollisions = 0;

			// Find the bucket we belong to
			var LeftHashCode = LeftComparer.GetHashCode(item.Key) & HashCodeMask;
			ref var LeftBucket = ref _LeftBuckets[LeftHashCode % _LeftBuckets.Length];
			var LeftIndex = LeftBucket - 1;

			index = -1;

			// Check for Left collisions
			while (LeftIndex >= 0)
			{
				if (Entries[LeftIndex].LeftHashCode == LeftHashCode && LeftComparer.Equals(Entries[LeftIndex].Left, item.Key))
				{
					if (!replace)
						return false;

					break;
				}

				LeftIndex = Entries[LeftIndex].NextLeftIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			TotalCollisions = 0;

			var RightHashCode = RightComparer.GetHashCode(item.Value) & HashCodeMask;
			ref var RightBucket = ref _RightBuckets[RightHashCode % _RightBuckets.Length];
			var RightIndex = RightBucket - 1;

			// Check for Right collisions
			while (RightIndex >= 0)
			{
				if (Entries[RightIndex].RightHashCode == RightHashCode && RightComparer.Equals(Entries[RightIndex].Right, item.Value))
				{
					if (!replace || (LeftIndex != -1 && LeftIndex != RightIndex))
						return false;

					break;
				}

				RightIndex = Entries[RightIndex].NextRightIndex;

				if (TotalCollisions++ >= _Size)
					throw new InvalidOperationException("State invalid");
			}

			if (LeftIndex != -1)
			{
				// We're replacing the value on the right
				index = LeftIndex;

				ref var Entry = ref Entries[LeftIndex];

				// Fix up the right linked list
				if (Entry.NextRightIndex >= 0)
				{
					// There's someone after us. Adjust them to point to the entry before us. If there's nobody, they will become the new tail
					Entries[Entry.NextRightIndex].PreviousRightIndex = Entry.PreviousRightIndex;
				}

				if (Entry.PreviousRightIndex >= 0)
				{
					// There's someone before us. Adjust them to point to the entry after us. If there's nobody, they will become the new head
					Entries[Entry.PreviousRightIndex].NextRightIndex = Entry.NextRightIndex;
				}
				else
				{
					// We're either the tail or a solitary entry (with no one before or after us)
					// Either way, we need to update the index stored in the Bucket
					_RightBuckets[Entry.RightHashCode % _RightBuckets.Length] = Entry.NextRightIndex + 1;
				}

				var NextRightIndex = RightBucket - 1;
				Entry.RightHashCode = RightHashCode;
				Entry.Item = item;

				Entry.NextRightIndex = NextRightIndex; // We take over as the tail of the linked list
				Entry.PreviousRightIndex = -1; // We're the tail, so there's nobody behind us

				// Double-link the list, so we can quickly resort
				if (NextRightIndex >= 0)
					Entries[NextRightIndex].PreviousRightIndex = LeftIndex;

				RightBucket = LeftIndex + 1;
			}
			else if (RightIndex != -1)
			{
				// We're replacing the value on the left
				index = RightIndex;

				ref var Entry = ref Entries[RightIndex];

				// We're removing an entry, so fix up the left linked list
				if (Entry.NextLeftIndex >= 0)
				{
					// There's someone after us. Adjust them to point to the entry before us. If there's nobody, they will become the new tail
					Entries[Entry.NextLeftIndex].PreviousLeftIndex = Entry.PreviousLeftIndex;
				}

				if (Entry.PreviousLeftIndex >= 0)
				{
					// There's someone before us. Adjust them to point to the entry after us. If there's nobody, they will become the new head
					Entries[Entry.PreviousLeftIndex].NextLeftIndex = Entry.NextLeftIndex;
				}
				else
				{
					// We're either the tail or a solitary entry (with no one before or after us)
					// Either way, we need to update the index stored in the Bucket
					_LeftBuckets[Entry.LeftHashCode % _LeftBuckets.Length] = Entry.NextLeftIndex + 1;
				}

				var NextLeftIndex = LeftBucket - 1;
				Entry.LeftHashCode = LeftHashCode;
				Entry.Item = item;

				Entry.NextLeftIndex = NextLeftIndex; // We take over as the tail of the linked list
				Entry.PreviousLeftIndex = -1; // We're the tail, so there's nobody behind us

				// Double-link the list, so we can quickly resort
				if (NextLeftIndex >= 0)
					Entries[NextLeftIndex].PreviousLeftIndex = RightIndex;

				LeftBucket = RightIndex + 1;
			}
			else
			{
				// No collisions found, are there enough slots for this item?
				if (_Size >= Entries.Length)
				{
					// No, so resize our entries table
					EnsureCapacity(_Size + 1);
					LeftBucket = ref _LeftBuckets[LeftHashCode % _LeftBuckets.Length];
					RightBucket = ref _RightBuckets[RightHashCode % _RightBuckets.Length];
					Entries = _Entries;
				}

				// Store our item at the end
				index = LeftIndex = _Size++;

				ref var Entry = ref Entries[LeftIndex];
				var NextLeftIndex = LeftBucket - 1;
				var NextRightIndex = RightBucket - 1;

				Entry.LeftHashCode = LeftHashCode;
				Entry.RightHashCode = RightHashCode;
				Entry.Item = item;

				Entry.NextLeftIndex = NextLeftIndex; // We take over as the tail of the linked list
				Entry.PreviousLeftIndex = -1; // We're the tail, so there's nobody behind us

				Entry.NextRightIndex = NextRightIndex; // We take over as the tail of the linked list
				Entry.PreviousRightIndex = -1; // We're the tail, so there's nobody behind us

				// Double-link the list, so we can quickly resort
				if (NextLeftIndex >= 0)
					Entries[NextLeftIndex].PreviousLeftIndex = LeftIndex;
				if (NextRightIndex >= 0)
					Entries[NextRightIndex].PreviousRightIndex = LeftIndex;

				LeftBucket = RightBucket = LeftIndex + 1;
			}

			return true;
		}

		//****************************************

		bool IDictionary<TLeft, TRight>.ContainsKey(TLeft key) => ContainsLeft(key);

		bool IReadOnlyDictionary<TLeft, TRight>.ContainsKey(TLeft key) => ContainsLeft(key);

		IEnumerator<KeyValuePair<TLeft, TRight>> IEnumerable<KeyValuePair<TLeft, TRight>>.GetEnumerator() => new Enumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

		bool IDictionary<TLeft, TRight>.TryGetValue(TLeft key, out TRight value) => TryGetRight(key, out value!);

		bool IReadOnlyDictionary<TLeft, TRight>.TryGetValue(TLeft key, out TRight value) => TryGetRight(key, out value!);

		void IList.Insert(int index, object value) => throw new NotSupportedException();

		void IList<KeyValuePair<TLeft, TRight>>.Insert(int index, KeyValuePair<TLeft, TRight> item) => throw new NotSupportedException();

		void ICollection.CopyTo(Array array, int index) => CopyTo((KeyValuePair<TLeft, TRight>[])array, index);

		void IDictionary.Add(object key, object value)
		{
			if (!(key is TLeft Left))
				throw new ArgumentException("Not a supported key", nameof(key));

			if (!(value is TRight Right))
				throw new ArgumentException("Not a supported value", nameof(value));

			Add(Left, Right);
		}

		bool IDictionary.Contains(object value)
		{
			if (value is KeyValuePair<TLeft, TRight> Pair)
				return Contains(Pair);

			if (value is DictionaryEntry Entry && Entry.Key is TLeft Left && Entry.Value is TRight Right)
				return Contains(new KeyValuePair<TLeft, TRight>(Left, Right));

			return false;
		}

		void IDictionary.Remove(object value)
		{
			if (value is KeyValuePair<TLeft, TRight> Pair)
				Remove(Pair);
			else if (value is DictionaryEntry Entry && Entry.Key is TLeft Left && Entry.Value is TRight Right)
				Remove(new KeyValuePair<TLeft, TRight>(Left, Right));
		}

		int IList.Add(object value)
		{
			if (value is KeyValuePair<TLeft, TRight> Pair)
			{
				if (TryAdd(Pair, out var Index))
					return Index;

				return -1;
			}

			if (value is DictionaryEntry Entry && Entry.Key is TLeft Left && Entry.Value is TRight Right)
			{
				if (TryAdd(new KeyValuePair<TLeft, TRight>(Left, Right), out var Index))
					return Index;

				return -1;
			}

			throw new ArgumentException("Not a supported value", nameof(value));
		}

		bool IList.Contains(object value)
		{
			if (value is KeyValuePair<TLeft, TRight> Pair)
				return Contains(Pair);

			if (value is DictionaryEntry Entry && Entry.Key is TLeft Left && Entry.Value is TRight Right)
				return Contains(new KeyValuePair<TLeft, TRight>(Left, Right));

			return false;
		}

		int IList.IndexOf(object value)
		{
			if (value is KeyValuePair<TLeft, TRight> Pair)
				return IndexOf(Pair);

			if (value is DictionaryEntry Entry && Entry.Key is TLeft Left && Entry.Value is TRight Right)
				return IndexOf(new KeyValuePair<TLeft, TRight>(Left, Right));

			return -1;
		}

		void IList.Remove(object value)
		{
			if (value is KeyValuePair<TLeft, TRight> Pair)
				Remove(Pair);
			else if (value is DictionaryEntry Entry && Entry.Key is TLeft Left && Entry.Value is TRight Right)
				Remove(new KeyValuePair<TLeft, TRight>(Left, Right));
		}

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => _Size;

		/// <summary>
		/// Gets/Sets the value corresponding to the provided key
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TRight this[TLeft key]
		{
			get
			{
				if (TryGetRight(key, out var ResultValue))
					return ResultValue;

				throw new KeyNotFoundException();
			}
			set
			{
				var Item = new KeyValuePair<TLeft, TRight>(key, value);

				if (!TryAdd(Item, true, out _))
					throw new ArgumentException("Left and Right already exist in the dictionary");
			}
		}

		/// <summary>
		/// Gets/Sets the number of elements that the Observable Dictionary can contain.
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
		/// Gets the equality comparer being used for the First
		/// </summary>
		public IEqualityComparer<TLeft> LeftComparer { get; }

		/// <summary>
		/// Gets the equality comparer being used for the First
		/// </summary>
		public IEqualityComparer<TRight> RightComparer { get; }

		ICollection IDictionary.Keys => Lefts;

		ICollection IDictionary.Values => Rights;

		ICollection<TLeft> IDictionary<TLeft, TRight>.Keys => Lefts;

		ICollection<TRight> IDictionary<TLeft, TRight>.Values => Rights;

		IEnumerable<TLeft> IReadOnlyDictionary<TLeft, TRight>.Keys => Lefts;

		IEnumerable<TRight> IReadOnlyDictionary<TLeft, TRight>.Values => Rights;

		bool ICollection<KeyValuePair<TLeft, TRight>>.IsReadOnly => false;

		bool IDictionary.IsFixedSize => false;

		bool IDictionary.IsReadOnly => false;

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		KeyValuePair<TLeft, TRight> IReadOnlyList<KeyValuePair<TLeft, TRight>>.this[int index] => GetByIndex(index);

		KeyValuePair<TLeft, TRight> IList<KeyValuePair<TLeft, TRight>>.this[int index]
		{
			get => GetByIndex(index);
			set => throw new NotSupportedException();
		}

		object? IDictionary.this[object key]
		{
			get
			{
				if (key is TLeft Left)
					return this[Left];

				throw new KeyNotFoundException();
			}
			set
			{
				if (!(key is TLeft Left))
					throw new ArgumentException("Not a supported key", nameof(key));

				if (!(value is TRight Right))
					throw new ArgumentException("Not a supported value", nameof(value));

				if (!TryAdd(new KeyValuePair<TLeft, TRight>(Left, Right), true, out _))
					throw new ArgumentException("Left and Right already exist in the dictionary");
			}
		}

		object IList.this[int index]
		{
			get => GetByIndex(index);
			set => throw new NotSupportedException();
		}

		object ICollection.SyncRoot => this;

		bool ICollection.IsSynchronized => false;

		//****************************************

		private static void Reindex(int[] leftBuckets, int[] rightBuckets, Entry[] entries, int startIndex, int size)
		{
			// Reindex a range of entries
			for (var Index = startIndex; Index < size; Index++)
			{
				ref var Entry = ref entries[Index];

				ref var LeftBucket = ref leftBuckets[Entry.LeftHashCode % leftBuckets.Length];
				var NextLeftIndex = LeftBucket - 1;

				Entry.NextLeftIndex = NextLeftIndex;
				Entry.PreviousLeftIndex = -1;

				if (NextLeftIndex >= 0)
					entries[NextLeftIndex].PreviousLeftIndex = Index;

				ref var RightBucket = ref rightBuckets[Entry.RightHashCode % rightBuckets.Length];
				var NextRightIndex = RightBucket - 1;

				Entry.NextRightIndex = NextRightIndex;
				Entry.PreviousRightIndex = -1;

				if (NextRightIndex >= 0)
					entries[NextRightIndex].PreviousRightIndex = Index;

				LeftBucket = RightBucket = Index + 1;
			}
		}

		private static void VerifyDistinct(Entry[] entries, int size, IEqualityComparer<TLeft> leftComparer, IEqualityComparer<TRight> rightComparer)
		{
			// Get everything into Left HashCode order
			Array.Sort(entries, 0, size, EntryLeftComparer.Default);

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
				while (Index < size && entries[Index].LeftHashCode == CurrentItem.LeftHashCode);

				// Is there more than one item with the same Hash?
				while (Index - StartIndex > 1)
				{
					// Compare the Left item to the others
					for (var SubIndex = StartIndex + 1; SubIndex < Index; SubIndex++)
					{
						if (leftComparer.Equals(CurrentItem.Left, entries[SubIndex].Left))
							throw new ArgumentException("Input collection has duplicates");
					}

					// Move up the Left item
					StartIndex++;
				}
			}

			// Now repeat in Right HashCode order
			Array.Sort(entries, 0, size, EntryRightComparer.Default);

			Index = 0;

			while (Index < size)
			{
				var StartIndex = Index;
				var CurrentItem = entries[Index];

				// Find all the items that have the same Hash
				do
				{
					Index++;
				}
				while (Index < size && entries[Index].RightHashCode == CurrentItem.RightHashCode);

				// Is there more than one item with the same Hash?
				while (Index - StartIndex > 1)
				{
					// Compare the Left item to the others
					for (var SubIndex = StartIndex + 1; SubIndex < Index; SubIndex++)
					{
						if (rightComparer.Equals(CurrentItem.Right, entries[SubIndex].Right))
							throw new ArgumentException("Input collection has duplicates");
					}

					// Move up the Left item
					StartIndex++;
				}
			}
		}

		//****************************************

		private struct Entry
		{
			public int NextLeftIndex, NextRightIndex;
			public int PreviousLeftIndex, PreviousRightIndex;
			public int LeftHashCode, RightHashCode;
			public KeyValuePair<TLeft, TRight> Item;
			public KeyValuePair<TRight, TLeft> InverseItem => new KeyValuePair<TRight, TLeft>(Right, Left);
			public TLeft Left => Item.Key;
			public TRight Right => Item.Value;
		}

		private sealed class EntryLeftComparer : IComparer<Entry>
		{
			private EntryLeftComparer()
			{
			}

			public int Compare(Entry x, Entry y) => x.LeftHashCode.CompareTo(y.LeftHashCode);

			public static EntryLeftComparer Default { get; } = new EntryLeftComparer();
		}

		private sealed class EntryRightComparer : IComparer<Entry>
		{
			private EntryRightComparer()
			{
			}

			public int Compare(Entry x, Entry y) => x.RightHashCode.CompareTo(y.RightHashCode);

			public static EntryRightComparer Default { get; } = new EntryRightComparer();
		}

		/// <summary>
		/// Enumerates the dictionary with First as the Key while avoiding memory allocations
		/// </summary>
		public struct Enumerator : IEnumerator<KeyValuePair<TLeft, TRight>>, IEnumerator
		{	//****************************************
			private readonly BiDictionary<TLeft, TRight> _Parent;

			private int _Index;
			//****************************************

			internal Enumerator(BiDictionary<TLeft, TRight> parent)
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
			public KeyValuePair<TLeft, TRight> Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		private sealed class DictionaryEnumerator : IDictionaryEnumerator
		{ //****************************************
			private Enumerator _Parent;
			//****************************************

			internal DictionaryEnumerator(BiDictionary<TLeft, TRight> parent)
			{
				_Parent = new Enumerator(parent);
			}

			//****************************************

			public bool MoveNext() => _Parent.MoveNext();

			public void Reset() => _Parent.Reset();

			//****************************************

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

			object? IDictionaryEnumerator.Key => _Parent.Current.Key;

			object? IDictionaryEnumerator.Value => _Parent.Current.Value;
		}

	}
}
