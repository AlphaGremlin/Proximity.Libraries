using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace System.Collections.Observable
{
	/// <summary>
	/// Provides an Observable HashSet supporting BeginUpdate and EndUpdate for batch changes as well as indexed access for optimal data-binding
	/// </summary>
	/// <typeparam name="T">The type of value within the set</typeparam>
	public class ObservableSet<T> : ObservableBaseSet<T>, ISet<T>, IList<T>
#if !NETSTANDARD
		, IReadOnlySet<T>
#endif
		where T : notnull
	{ //****************************************
		private int[] _Keys;
		private T[] _Values;
		private int _Size;
		//****************************************

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		public ObservableSet() : this(0, EqualityComparer<T>.Default)
		{

		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		public ObservableSet(IEnumerable<T> collection) : this(collection, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="capacity">The starting capacity for the obserable set</param>
		public ObservableSet(int capacity) : this(capacity, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(IEqualityComparer<T> comparer) : this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="capacity">The starting capacity for the obserable set</param>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(int capacity, IEqualityComparer<T> comparer)
		{
			_Size = 0;
			_Keys = new int[capacity];
			_Values = new T[capacity];
			Comparer = comparer;
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
		{
			Comparer = comparer;

			// Ensure we add all the values in the order sorted by hash code
			_Values = collection.ToArray();
			_Size = _Values.Length;
			_Keys = new int[_Size];

			for (var Index = 0; Index < _Values.Length; Index++)
			{
				_Keys[Index] = comparer.GetHashCode(_Values[Index]);
			}

			Array.Sort(_Keys, _Values, Comparer<int>.Default);
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

		/// <inheritdoc />
		public override void AddRange(IEnumerable<T> items)
		{	//****************************************
			var Index = 0;
			var Count = 0;
			T NewItem = default!;
			//****************************************

			if (items == null)
				throw new ArgumentNullException(nameof(items));

			var NewItems = new List<T>(items);

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
		public override bool Contains(T item) => IndexOf(item) >= 0;

		/// <inheritdoc />
		public override void CopyTo(T[] array, int arrayIndex) => Array.Copy(_Values, 0, array, arrayIndex, _Size);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public ValueEnumerator GetEnumerator() => new ValueEnumerator(this);

		/// <inheritdoc />
		public override int IndexOf(T item)
		{	//****************************************
			var Key = Comparer.GetHashCode(item);
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

		/// <inheritdoc />
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
				throw new ArgumentOutOfRangeException(nameof(index));

			var Item = _Values[index];

			_Size--;

			// If this is in the middle, move the values down
			if (index < _Size)
			{
				Array.Copy(_Keys, index + 1, _Keys, index, _Size - index);
				Array.Copy(_Values, index + 1, _Values, index, _Size - index);
			}

			// Ensure we don't hold a reference to the value
			_Values[_Size] = default!;

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, Item, index);
		}

		/// <inheritdoc />
		public override void RemoveRange(int index, int count)
		{
			if (index < 0 || index + count > _Size)
				throw new ArgumentOutOfRangeException(nameof(index));

			var OldItems = new T[count];

			Array.Copy(_Values, index, OldItems, 0, count);
			
			_Size -= count;

			// If this is in the middle, move the values down
			if (index + count <= _Size)
			{
				Array.Copy(_Keys, index + count, _Keys, index, _Size - index);
				Array.Copy(_Values, index + count, _Values, index, _Size - index);
			}

			// Ensure we don't hold a reference to the values
			Array.Clear(_Values, _Size, count);
			
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItems, index);
		}

		/// <inheritdoc />
		public override T[] ToArray()
		{
			var Copy = new T[_Size];

			Array.Copy(_Values, 0, Copy, 0, _Size);

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
				throw new ArgumentOutOfRangeException(nameof(index));

			return _Values[index];
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

			var RemovedItems = new List<T> { _Values[Index] };

			var InnerIndex = Index + 1;

			while (InnerIndex < _Size)
			{
				// Skip the items we need to remove
				while (InnerIndex < _Size && predicate(InnerIndex))
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

		/// <inheritdoc />
		protected override void InternalSet(int index, T value) => throw new NotSupportedException("Cannot set by index");

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
		{	//****************************************
			var Key = Comparer.GetHashCode(item);
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

		//****************************************

		/// <summary>
		/// Gets the IEqualityComparer{TValue} that is used to compare items in the set
		/// </summary>
		public IEqualityComparer<T> Comparer { get; }

		/// <inheritdoc />
		public override int Count => _Size;

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
					_Keys = Array.Empty<int>();
					_Values = Array.Empty<T>();

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

		T IList<T>.this[int index]
		{
			get => _Values[index];
			set => throw new NotSupportedException("Cannot set by index");
		}

		//****************************************

		/// <summary>
		/// Enumerates the set while avoiding memory allocations
		/// </summary>
		public struct ValueEnumerator : IEnumerator<T>, IEnumerator
		{	//****************************************
			private readonly ObservableSet<T> _Parent;

			private int _Index;
			//****************************************

			internal ValueEnumerator(ObservableSet<T> parent)
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
