using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
//****************************************

namespace System.Collections.Observable
{
	/// <summary>
	/// Provides an Observable Sorted Set supporting BeginUpdate and EndUpdate for batch changes as well as indexed access for optimal data-binding
	/// </summary>
	/// <typeparam name="T">The type of value within the set</typeparam>
	public class ObservableSortedSet<T> : ObservableBaseSet<T>, ISet<T>, IList<T>
	{ //****************************************
		private T[] _Items;

		private int _Size;
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
		public ObservableSortedSet(IComparer<T> comparer) : this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new Observable Sorted Set
		/// </summary>
		/// <param name="collection">The items to populate the set with</param>
		public ObservableSortedSet(IEnumerable<T> collection) : this(collection, GetDefaultComparer())
		{
		}

		/// <summary>
		/// Creates a new Observable Sorted Set
		/// </summary>
		/// <param name="collection">The items to populate the set with</param>
		/// <param name="comparer">The comparer to use with this set</param>
		public ObservableSortedSet(IEnumerable<T> collection, IComparer<T> comparer)
		{
			Comparer = comparer;

			_Items = collection.ToArray();

			Array.Sort<T>(_Items, comparer);
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="capacity">The starting capacity for the set</param>
		/// <param name="comparer">The comparer to use for this set</param>
		public ObservableSortedSet(int capacity, IComparer<T> comparer)
		{
			Comparer = comparer;
			_Size = 0;

			_Items = new T[capacity];
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <returns>True if the element was added, False if it was already in the set</returns>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public new bool Add(T item)
		{
			if (TryAdd(item, out var Index))
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
		/// <remarks>Duplicate items will be automatically filtered out</remarks>
		public override void AddRange(IEnumerable<T> items)
		{	//****************************************
			var Index = 0;
			var Count = 0;
			T NewItem;
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
				Array.Sort(NewItems, Comparer);

				// Remove duplicates
				for (Index = 1; Index < NewItems.Length; Index++)
				{
					// Is the leading item equal to the trailing item?
					if (Comparer.Compare(NewItems[Count], NewItem = NewItems[Index]) != 0)
					{
						// Shift the final item up one
						NewItems[++Count] = NewItem;
					}
				}

				// Add one to the count, to cover the last item
				if (++Count > _Items.Length)
					Capacity = Count;

				Array.Copy(NewItems, _Items, Count);

				NewItem = _Items[0];
				_Size = Count;
				Index = 0;
			}
			else
			{
				NewItem = default!;

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
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		public int BinarySearch(T item) => Array.BinarySearch<T>(_Items, 0, _Size, item, Comparer);

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public override void Clear()
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
		public override bool Contains(T item) => IndexOf(item) >= 0;

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public override void CopyTo(T[] array, int arrayIndex) => Array.Copy(_Items, 0, array, arrayIndex, _Size);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public ValueEnumerator GetEnumerator() => new ValueEnumerator(this);

		/// <summary>
		/// Determines the index of a specific item in the set
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public override int IndexOf(T item)
		{	//****************************************
			var Index = Array.BinarySearch(_Items, 0, _Size, item, Comparer);
			//****************************************

			// Is there a matching hash code?
			if (Index < 0)
				return -1;

			return Index;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was in the collection and removed, otherwise False</returns>
		public override bool Remove(T item)
		{	//****************************************
			var Index = IndexOf(item);
			//****************************************

			if (Index < 0)
				return false;

			RemoveAt(Index);

			return true;
		}

		/// <inheritdoc />
		public override void RemoveAt(int index)
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
			_Items[_Size] = default!;

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, Item, index);
		}

		/// <inheritdoc />
		public override void RemoveRange(int index, int count)
		{
			if (index < 0 || index + count > _Size)
				throw new ArgumentOutOfRangeException("index");

			var OldItems = new T[count];

			Array.Copy(_Items, index, OldItems, 0, count);

			_Size -= count;

			// If this is in the middle, move the values down
			if (index + count <= _Size)
			{
				Array.Copy(_Items, index + count, _Items, index, _Size - index);
			}

			// Ensure we don't hold a reference to the values
			Array.Clear(_Items, _Size, count);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItems, index);
		}

		/// <inheritdoc />
		public override T[] ToArray()
		{
			var Copy = new T[_Size];

			Array.Copy(_Items, 0, Copy, 0, _Size);

			return Copy;
		}

		//****************************************

		/// <inheritdoc />
		protected override void InternalAdd(T item) => Add(item);

		/// <inheritdoc />
		protected override IEnumerator<T> InternalGetEnumerator() => new ValueEnumerator(this);

		/// <inheritdoc />
		protected override T InternalGet(int index)
		{
			if (index < 0 || index >= _Size)
				throw new ArgumentOutOfRangeException("index");

			return _Items[index];
		}

		/// <inheritdoc />
		protected override void InternalInsert(int index, T item) => throw new NotSupportedException("Cannot insert into a set");

		/// <inheritdoc />
		protected override int InternalRemoveAll(Func<int, bool> predicate)
		{
			var Index = 0;

			// Find the first item we need to remove
			while (Index < _Size && !predicate(Index))
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return 0;

			var RemovedItems = new List<T> { _Items[Index] };

			var InnerIndex = Index + 1;

			while (InnerIndex < _Size)
			{
				// Skip the items we need to remove
				while (InnerIndex < _Size && predicate(InnerIndex))
				{
					RemovedItems.Add(_Items[InnerIndex]);

					InnerIndex++;
				}

				// If we reached the end, abort
				if (InnerIndex >= _Size)
					break;

				// We found one we're not removing, so move it up
				_Items[Index] = _Items[InnerIndex];

				Index++;
				InnerIndex++;
			}

			// Clear the removed items
			Array.Clear(_Items, Index, _Size - Index);

			_Size = Index;

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, RemovedItems);

			return InnerIndex - Index;
		}

		/// <inheritdoc />
		protected override void InternalSet(int index, T value) => throw new NotSupportedException("Cannot set by index");

		//****************************************

		private void EnsureCapacity(int capacity)
		{
			var num = (_Items.Length == 0 ? 4 : _Items.Length * 2);

			if (num > 0x7FEFFFFF)
				num = 0x7FEFFFFF;

			if (num < capacity)
				num = capacity;

			Capacity = num;
		}

		private bool TryAdd(T item, out int insertIndex)
		{	//****************************************
			var Index = Array.BinarySearch(_Items, 0, _Size, item, Comparer);
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

		private void Insert(int index, T value)
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

		//****************************************

		/// <summary>
		/// Gets the IComparer{TValue} that is used to compare items in the set
		/// </summary>
		public IComparer<T> Comparer { get; }

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public override int Count => _Size;

		/// <summary>
		/// Gets the minimum value in the sorted set
		/// </summary>
#if !NETSTANDARD2_0
		[MaybeNull]
#endif
		public T Min => (_Size == 0) ? default! : _Items[0];

		/// <summary>
		/// Gets the maximum value in the sorted set
		/// </summary>
#if !NETSTANDARD2_0
		[MaybeNull]
#endif
		public T Max => (_Size == 0) ? default! : _Items[_Size - 1];

		/// <summary>
		/// Gets/Sets the number of elements that the Observable Sorted Set can contain.
		/// </summary>
		public int Capacity
		{
			get => _Items.Length;
			set
			{
				if (value == _Items.Length)
					return;

				if (value < _Size)
					throw new ArgumentException("value");

				if (value == 0)
				{
					_Items = new T[0];

					return;
				}

				var NewItems = new T[value];

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
		public T this[int index] => _Items[index];

		T IList<T>.this[int index]
		{
			get => _Items[index];
			set => throw new NotSupportedException("Cannot set by index");
		}

		//****************************************

		/// <summary>
		/// Enumerates the sorted set while avoiding memory allocations
		/// </summary>
		public struct ValueEnumerator : IEnumerator<T>, IEnumerator
		{	//****************************************
			private readonly ObservableSortedSet<T> _Parent;

			private int _Index;

			//****************************************

			internal ValueEnumerator(ObservableSortedSet<T> parent)
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

				Current = _Parent._Items[_Index++];

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

		//****************************************

		private static IComparer<T> GetDefaultComparer()
		{
			if (!typeof(IComparable<T>).IsAssignableFrom(typeof(T)) && !typeof(IComparable).IsAssignableFrom(typeof(T)))
				throw new ArgumentException(string.Format("{0} does not implement IComparable or IComparable<>", typeof(T).FullName));

			return Comparer<T>.Default;
		}
	}
}
