using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.ReadOnly;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Proximity.Collections;
//****************************************

namespace System.Collections.Generic
{
	/// <summary>
	/// Represents a set that holds only weak references to its contents
	/// </summary>
	public sealed class WeakHashSet<T> : IEnumerable<T>, IDisposable where T : class
	{ //****************************************
		private readonly GCHandleType _HandleType;
		
		private SetItem[] _Values;

		private int _Size;
		//****************************************

		/// <summary>
		/// Creates a new WeakHashSet
		/// </summary>
		public WeakHashSet() : this(EqualityComparer<T>.Default, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new WeakHashSet
		/// </summary>
		/// <param name="comparer">The comparer to use for items</param>
		public WeakHashSet(IEqualityComparer<T>? comparer) : this(comparer, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new WeakHashSet
		/// </summary>
		/// <param name="comparer">The comparer to use for items</param>
		/// <param name="handleType">The type of GCHandle to use</param>
		public WeakHashSet(IEqualityComparer<T>? comparer, GCHandleType handleType)
		{
			Comparer = comparer ?? EqualityComparer<T>.Default;
			_HandleType = handleType;
			_Size = 0;
			_Values = Empty.Array<WeakHashSet<T>.SetItem>();
		}

		/// <summary>
		/// Creates a new WeakHashSet of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the items to weakly reference</param>
		public WeakHashSet(IEnumerable<T> collection) : this(collection, EqualityComparer<T>.Default, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new WeakHashSet of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the items to weakly reference</param>
		/// <param name="comparer">The comparer to use for items</param>
		public WeakHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer) : this(collection, comparer, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new WeakHashSet of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the items to reference</param>
		/// <param name="comparer">The comparer to use for items</param>
		/// <param name="handleType">The type of GCHandle to use</param>
		public WeakHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer, GCHandleType handleType)
		{
			if (collection is null)
				throw new ArgumentNullException(nameof(collection), "Collection cannot be null");

			Comparer = comparer ?? EqualityComparer<T>.Default;
			_HandleType = handleType;

			_Values = collection.Select(item => new SetItem(item, this)).ToArray();
			_Size = _Values.Length;
			
			Array.Sort(_Values, Comparer<SetItem>.Default);
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <returns>True if the element was added, False if it was already in the set</returns>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public bool Add(T item)
		{ //****************************************
			int Key, Index;
			T? Current;
			//****************************************

			if (item is null)
				throw new ArgumentNullException(nameof(item), "Cannot add null to a Weak Hash Set");

			Key = Comparer.GetHashCode(item);
			Index = Array.BinarySearch<SetItem>(_Values, 0, _Size, new SetItem(Key));

			// Is there a matching hash code?
			if (Index >= 0)
			{
				// BinarySearch is not guaranteed to return the first matching value, so we may need to move back
				while (Index > 0 && _Values[Index - 1].HashCode == Key)
					Index--;

				for (;;)
				{
					try
					{
						Current = (T?)_Values[Index].Item.Target;

						// Do we match this item?
						if (Current != null && Comparer.Equals(Current, item))
							return false; // Yes, so return False
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed
					}

					// Are we at the end of the list?
					if (Index == _Size - 1)
						break; // Yes, so we need to insert the new item at the end

					// Is there another item with the same key?
					if (_Values[Index + 1].HashCode != Key)
						break; // Nope, so we can insert the new item at the current Index

					// Yes, so loop back and check that
					Index++;
				}

				Insert(Index, Key, item);

				return true;
			}

			// No matching item, insert at the nearest spot above
			Insert(~Index, Key, item);

			return true;
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the collection, otherwise false</returns>
		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			if (_Size > 0)
			{
				Dispose();

				Array.Clear(_Values, 0, _Size);

				_Size = 0;
			}
		}

		/// <summary>
		/// Disposes of the Weak Dictionary, cleaning up any weak references
		/// </summary>
		public void Dispose()
		{
			for (var Index = 0; Index < _Size; Index++)
			{
				_Values[Index].Item.Dispose();
			}
		}

		/// <summary>
		/// Removes all items that are in the given collection from the current set
		/// </summary>
		/// <param name="other">The collection of items to remove</param>
		public void ExceptWith(IEnumerable<T> other)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));

			var BitMask = new BitArray(_Size);

			// Mark each item we want to remove
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				// If we find it in the set, mark it for removal
				if (Index != -1)
					BitMask.Set(Index, true);
			}

			// Remove everything we marked
			RemoveAll(BitMask.Get);
		}

		/// <summary>
		/// Modifies the current set so it only contains items present in the given collection
		/// </summary>
		/// <param name="other">The collection to compare against</param>
		public void IntersectWith(IEnumerable<T> other)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));

			var BitMask = new BitArray(_Size, true);

			// Mark each item we want to keep
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				// If we find it in the set, mark it for keeping
				if (Index != -1)
					BitMask.Set(Index, false);
			}

			// Remove everything we didn't clear
			RemoveAll(BitMask.Get);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		/// <remarks>Will perform a compaction. May not be identical between enumerations</remarks>
		public Enumerator GetEnumerator() => new(this);

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		/// <remarks>Will perform a partial compaction, up to the point the target item is found</remarks>
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
		/// Modifies the set so it only contains items that are present in the set, or in the given collection, but not both
		/// </summary>
		/// <param name="other">The collection to compare against</param>
		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			
			var BitMask = new BitArray(_Size);
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
			RemoveAll(BitMask.Get);

			// Add the items we didn't find. Duplicates will be ignored
			foreach (var MyItem in NewItems)
			{
				Add(MyItem);
			}
		}

		/// <summary>
		/// Creates a set of strong references to the contents of the collection
		/// </summary>
		/// <returns>A set of strong references to the collection</returns>
		/// <remarks>Will perform a compaction.</remarks>
#if NETSTANDARD
		public ISet<T> ToStrong()
#else
		public IReadOnlySet<T> ToStrong()
#endif
		{ //****************************************
			var MyList = new HashSet<T>();
			//****************************************

			foreach (var MyItem in this)
			{
				MyList.Add(MyItem);
			}

#if NET40
			return new ReadOnlySet<T>(MyList);
#else
			return MyList;
#endif
		}

		/// <summary>
		/// Searches the set for a given value and returns the equal value, if any
		/// </summary>
		/// <param name="equalValue">The value to search for</param>
		/// <param name="actualValue">The value already in the set, or the given value if not found</param>
		/// <returns>True if an equal value was found, otherwise False</returns>
		public bool TryGetValue(T equalValue, out T actualValue)
		{
			var Index = IndexOf(equalValue);

			if (Index >= 0)
			{
				try
				{
					var CurrentValue = (T?)_Values[Index].Item.Target;

					if (CurrentValue != null)
					{
						actualValue = CurrentValue;

						return true;
					}

					// Value is null, so it was collected between when we called IndexOf and now
				}
				catch (InvalidOperationException)
				{
					// The GCHandle was disposed
				}
			}

			actualValue = equalValue;

			return false;
		}

		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="items">The elements to add</param>
		/// <remarks>Ignores any null items, rather than throwing an exception</remarks>
		public void UnionWith(IEnumerable<T> items)
		{
			if (items is null)
				throw new ArgumentNullException(nameof(items));

			foreach (var MyItem in items)
			{
				if (MyItem != null)
					Add(MyItem);
			}
		}

		//****************************************

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new Enumerator(this);
		}

		//****************************************

		private void EnsureCapacity(int capacity)
		{
			var num = (_Values.Length == 0 ? 4 : _Values.Length * 2);

			if (num > 0x7FEFFFFF)
				num = 0x7FEFFFFF;

			if (num < capacity)
				num = capacity;

			Capacity = num;
		}

		private void Insert(int index, int key, T value)
		{
			if (_Size == _Values.Length)
				EnsureCapacity(_Size + 1);

			// Are we inserting before the end item?
			if (index < _Size)
			{
				// Yes, so move things up
				Array.Copy(_Values, index, _Values, index + 1, _Size - index);
			}
			
			_Values[index] = new SetItem(key, new GCReference(value, _HandleType));

			_Size++;
		}

		private int IndexOf(T item)
		{
			if (item is null)
				return -1;

			var Key = Comparer.GetHashCode(item);
			var Index = Array.BinarySearch(_Values, 0, _Size, new SetItem(Key));
			T? Current;

			// Is there a matching hash code?
			if (Index < 0)
				return -1;

			// BinarySearch is not guaranteed to return the first matching value, so we may need to move back
			while (Index > 0 && _Values[Index - 1].HashCode == Key)
				Index--;

			for (;;)
			{
				try
				{
					Current = (T?)_Values[Index].Item.Target;

					// Do we match this item?
					if (Current != null && Comparer.Equals(Current, item))
						return Index; // Yes, so return the Index
				}
				catch (InvalidOperationException)
				{
					// The GCHandle was disposed
				}
				
				Index++;

				// Are we at the end of the list?
				if (Index == _Size)
					return -1; // Yes, so we didn't find the item

				// Is there another item with the same key?
				if (_Values[Index].HashCode != Key)
					return -1; // Nope, so we didn't find the item

				// Yes, so loop back and check that
			}
		}

		private void RemoveAt(int index)
		{
			var Item = _Values[index];

			_Size--;

			// If this is in the middle, move the values down
			if (index < _Size)
			{
				Array.Copy(_Values, index + 1, _Values, index, _Size - index);
			}

			// Ensure we don't hold a reference to the value
			_Values[_Size] = default!;

			Item.Item.Dispose();
		}

		private void RemoveAll(Func<int, bool> predicate)
		{
			var Index = 0;

			// Find the first item we need to remove (we also do a compaction here)
			while (Index < _Size && !predicate(Index) && _Values[Index].Item.IsAlive)
				Index++;

			// Did we find anything?
			if (Index >= _Size)
				return;
			
			var InnerIndex = Index + 1;

			while (InnerIndex < _Size)
			{
				// Skip the items we need to remove (including expired items)
				while (InnerIndex < _Size && (predicate(InnerIndex) || !_Values[InnerIndex].Item.IsAlive))
				{
					InnerIndex++;
				}

				// If we reached the end, abort
				if (InnerIndex >= _Size)
					break;

				// We found one we're not removing, so move it up
				_Values[Index] = _Values[InnerIndex];

				Index++;
				InnerIndex++;
			}

			// Clear the removed items
			Array.Clear(_Values, Index, _Size - Index);

			_Size = Index;
		}

		//****************************************

		/// <summary>
		/// Gets the IEqualityComparer{TItem} that is used to compare items in the set
		/// </summary>
		public IEqualityComparer<T> Comparer { get; }

		/// <summary>
		/// Gets or Sets the number of elements that the Hash Set can contain.
		/// </summary>
		public int Capacity
		{
			get => _Values.Length;
			set
			{
				if (value == _Values.Length)
					return;

				if (value < _Size)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (value == 0)
				{
					_Values = Empty.Array<WeakHashSet<T>.SetItem>();

					return;
				}

				var NewValues = new SetItem[value];

				if (_Size > 0)
					Array.Copy(_Values, 0, NewValues, 0, _Size);

				_Values = NewValues;
			}
		}

		//****************************************

		/// <summary>
		/// Enumerates the dictionary while avoiding memory allocations
		/// </summary>
		public struct Enumerator : IEnumerator<T>
		{ //****************************************
			private readonly WeakHashSet<T> _Set;

			private int _Index;
			//****************************************

			internal Enumerator(WeakHashSet<T> parent)
			{
				_Set = parent;
				_Index = 0;
				Current = null!;
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			[SecuritySafeCritical]
			public void Dispose()
			{
				Current = null!;
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				for (;;)
				{
					if (_Index >= _Set._Size)
					{
						Current = null!;

						return false;
					}

					var Handle = _Set._Values[_Index];

					try
					{
						var Value = (T?)Handle.Item.Target;

						if (Value != null)
						{
							Current = Value;
							_Index++;

							return true;
						}
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed
					}

					_Set.RemoveAt(_Index);

					Handle.Item.Dispose();
				}
			}

			void IEnumerator.Reset()
			{
				_Index = 0;
				Current = null!;
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public T Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		private readonly struct SetItem : IComparable<SetItem>
		{ //****************************************
			internal readonly int HashCode;
			internal readonly GCReference Item;
			//****************************************

			internal SetItem(int hashCode)
			{
				HashCode = hashCode;
				Item = null!;
			}

			internal SetItem(int hashCode, GCReference item)
			{
				HashCode = hashCode;
				Item = item;
			}

			internal SetItem(T item, WeakHashSet<T> hashSet)
			{
				Item = new GCReference(item, hashSet._HandleType);
				HashCode = hashSet.Comparer.GetHashCode(item);
			}

			//****************************************

			public int CompareTo(SetItem other) => HashCode.CompareTo(other.HashCode);
		}
	}
}
