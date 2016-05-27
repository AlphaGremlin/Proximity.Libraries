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
	public class ObservableSet<TValue> : ObservableBaseSet<TValue>, ISet<TValue>, IList<TValue>
	{	//****************************************
		private int[] _Keys;
		private TValue[] _Values;

		private readonly IEqualityComparer<TValue> _Comparer;

		private int _Size;
		//****************************************

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		public ObservableSet() : this(0, EqualityComparer<TValue>.Default)
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
		/// <param name="capacity">The starting capacity for the obserable set</param>
		public ObservableSet(int capacity) : this(capacity, EqualityComparer<TValue>.Default)
		{
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(IEqualityComparer<TValue> comparer) : this(0, comparer)
		{
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="capacity">The starting capacity for the obserable set</param>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(int capacity, IEqualityComparer<TValue> comparer)
		{
			_Size = 0;
			_Keys = new int[capacity];
			_Values = new TValue[capacity];
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
			foreach (var MyPair in collection.Select(value => new KeyValuePair<int, TValue>(_Comparer.GetHashCode(value), value)).OrderBy(pair => pair.Key))
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
		public new bool Add(TValue item)
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

		/// <inheritdoc />
		public override void AddRange(IEnumerable<TValue> items)
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
		public override bool Contains(TValue item)
		{
			return IndexOf(item) >= 0;
		}

		/// <inheritdoc />
		public override void CopyTo(TValue[] array, int arrayIndex)
		{
			Array.Copy(_Values, 0, array, arrayIndex, _Size);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public ValueEnumerator GetEnumerator()
		{
			return new ValueEnumerator(this);
		}

		/// <inheritdoc />
		public override int IndexOf(TValue item)
		{	//****************************************
			int Key = _Comparer.GetHashCode(item);
			int Index = Array.BinarySearch<int>(_Keys, 0, _Size, Key);
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

		/// <inheritdoc />
		public override bool Remove(TValue item)
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

		//****************************************

		/// <inheritdoc />
		protected override void InternalAdd(TValue item)
		{
			Add(item);
		}

		/// <inheritdoc />
		protected override IEnumerator<TValue> InternalGetEnumerator()
		{
			return new ValueEnumerator(this);
		}

		/// <inheritdoc />
		protected override TValue InternalGet(int index)
		{
			return _Values[index];
		}

		/// <inheritdoc />
		protected override void InternalInsert(int index, TValue item)
		{
			throw new NotSupportedException("Cannot insert into a set");
		}

		/// <inheritdoc />
		protected override int InternalRemoveAll(Func<int, bool> predicate)
		{
			int Index = 0;

			// Find the first item we need to remove
			while (Index < _Size && !predicate(Index))
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return 0;

			var RemovedItems = new List<TValue>();

			RemovedItems.Add(_Values[Index]);

			int InnerIndex = Index + 1;

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
		protected override void InternalRemoveAt(int index)
		{
			RemoveAt(index);
		}

		/// <inheritdoc />
		protected override void InternalSet(int index, TValue value)
		{
			throw new NotSupportedException("Cannot set by index");
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

		private bool TryAdd(TValue item, out int insertIndex)
		{	//****************************************
			int Key = _Comparer.GetHashCode(item);
			int Index = Array.BinarySearch<int>(_Keys, 0, _Size, Key);
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

		//****************************************

		/// <summary>
		/// Gets the IEqualityComparer{TValue} that is used to compare items in the set
		/// </summary>
		public IEqualityComparer<TValue> Comparer
		{
			get { return _Comparer; }
		}

		/// <inheritdoc />
		public override int Count
		{
			get { return _Size; }
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

		TValue IList<TValue>.this[int index]
		{
			get { return _Values[index]; }
			set { throw new NotSupportedException("Cannot set by index"); }
		}

		//****************************************

		/// <summary>
		/// Enumerates the set while avoiding memory allocations
		/// </summary>
		public struct ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{	//****************************************
			private readonly ObservableSet<TValue> _Parent;

			private int _Index;
			private TValue _Current;
			//****************************************

			internal ValueEnumerator(ObservableSet<TValue> parent)
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

				_Current = _Parent._Values[_Index++];

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
