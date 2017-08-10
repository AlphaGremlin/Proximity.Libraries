/****************************************\
 ObservableList.cs
 Created: 2014-12-11
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
	/// Provides an Observable List supporting BeginUpdate and EndUpdate for batch changes
	/// </summary>
	public class ObservableList<TValue> : ObservableBase<TValue>, IList<TValue>
	{	//****************************************
		private readonly List<TValue> _Items;
		//****************************************

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		public ObservableList()
		{
			_Items = new List<TValue>();
		}

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		public ObservableList(IEnumerable<TValue> collection)
		{
			_Items = new List<TValue>(collection);
		}

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		/// <param name="capacity">The starting capacity of the list</param>
		public ObservableList(int capacity)
		{
			_Items = new List<TValue>(capacity);
		}

		//****************************************

		/// <inheritdoc />
		public override void Add(TValue item)
		{
			Insert(_Items.Count, item, true);
		}

		/// <inheritdoc />
		public override void AddRange(IEnumerable<TValue> items)
		{
			bool AddedItem = false;
			int StartIndex = _Items.Count;

			if (items == null) throw new ArgumentNullException("items");

			foreach (var MyItem in items)
			{
				_Items.Add(MyItem);

				AddedItem = true;
			}

			if (AddedItem)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, items, StartIndex);
		}

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		/// <remarks>The list must be sorted before calling this method</remarks>
		public int BinarySearch(TValue item)
		{
			return _Items.BinarySearch(item);
		}

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <param name="comparer">The comparer to use when searching</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		/// <remarks>The list must be sorted before calling this method</remarks>
		public int BinarySearch(TValue item, IComparer<TValue> comparer)
		{
			return _Items.BinarySearch(item, comparer);
		}

		/// <inheritdoc />
		public override void Clear()
		{
			if (_Items.Count > 0)
			{
				_Items.Clear();

				OnCollectionChanged();
			}
		}

		/// <inheritdoc />
		public override bool Contains(TValue item)
		{
			return _Items.Contains(item);
		}

		/// <inheritdoc />
		public override void CopyTo(TValue[] array, int arrayIndex)
		{
			_Items.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public List<TValue>.Enumerator GetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		/// <inheritdoc />
		public override int IndexOf(TValue item)
		{
			return _Items.IndexOf(item);
		}

		/// <summary>
		/// Inserts an element at the specified index
		/// </summary>
		/// <param name="index">The index to insert the element</param>
		/// <param name="item">The element to insert</param>
		public void Insert(int index, TValue item)
		{
			Insert(index, item, true);
		}

		/// <inheritdoc />
		public override bool Remove(TValue item)
		{
			var MyIndex = _Items.IndexOf(item);

			if (MyIndex == -1)
				return false;

			_Items.RemoveAt(MyIndex);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, MyIndex);

			return true;
		}

		/// <inheritdoc />
		public override int RemoveAll(Predicate<TValue> predicate)
		{
			int Index = 0;

			// Find the first item we need to remove
			while (Index < _Items.Count && !predicate(_Items[Index]))
				Index++;

			// Did we find anything?
			if (Index >= _Items.Count)
				return 0;

			var RemovedItems = new List<TValue>();

			RemovedItems.Add(_Items[Index]);

			int InnerIndex = Index + 1;

			while (InnerIndex < _Items.Count)
			{
				// Skip the items we need to remove
				while (InnerIndex < _Items.Count && predicate(_Items[InnerIndex]))
				{
					RemovedItems.Add(_Items[InnerIndex]);

					InnerIndex++;
				}

				// If we reached the end, abort
				if (InnerIndex >= _Items.Count)
					break;

				// We found one we're not removing, so move it up
				_Items[Index] = _Items[InnerIndex];

				Index++;
				InnerIndex++;
			}

			// Clear the removed items
			_Items.RemoveRange(Index, _Items.Count - Index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, RemovedItems);

			return InnerIndex - Index;
		}

		/// <inheritdoc />
		public override void RemoveAt(int index)
		{
			var OldItem = _Items[index];

			_Items.RemoveAt(index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, index);
		}

		/// <inheritdoc />
		public override void RemoveRange(int index, int count)
		{
			var OldItems = new TValue[count];

			_Items.CopyTo(index, OldItems, 0, count);

			_Items.RemoveRange(index, count);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItems, index);
		}

		/// <summary>
		/// Sorts the contents of this Observable List
		/// </summary>
		public void Sort()
		{
			if (_Items.Count != 0)
			{
				_Items.Sort();

				OnCollectionChanged();
			}
		}

		/// <summary>
		/// Sorts the contents of this Observable List based on a comparer
		/// </summary>
		/// <param name="comparer">The comparer to sort by</param>
		public void Sort(IComparer<TValue> comparer)
		{
			if (_Items.Count != 0)
			{
				_Items.Sort(comparer);

				OnCollectionChanged();
			}
		}

		/// <inheritdoc />
		public override TValue[] ToArray()
		{
			return _Items.ToArray();
		}

		//****************************************

		/// <inheritdoc />
		protected override TValue InternalGet(int index)
		{
			return _Items[index];
		}

		/// <inheritdoc />
		protected override IEnumerator<TValue> InternalGetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		/// <inheritdoc />
		protected override void InternalInsert(int index, TValue item)
		{
			_Items.Insert(index, item);
		}
		
		/// <inheritdoc />
		protected override void InternalSet(int index, TValue value)
		{
			_Items[index] = value;
		}

		//****************************************

		private void Insert(int index, TValue item, bool isAdd)
		{
			if (isAdd)
			{
				_Items.Insert(index, item);

				OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
			}
			else
			{
				var OldValue = _Items[index];
				_Items[index] = item;

				OnCollectionChanged(NotifyCollectionChangedAction.Replace, item, OldValue, index);
			}
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the value at the provided index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[int index]
		{
			get { return _Items[index]; }
			set { Insert(index, value, false); }
		}

		/// <inheritdoc />
		public override int Count
		{
			get { return _Items.Count; }
		}

		/// <summary>
		/// Gets the underlying list object
		/// </summary>
		protected List<TValue> Items
		{
			get { return _Items; }
		}

		/// <summary>
		/// Gets/Sets the current item capacity
		/// </summary>
		public int Capacity
		{
			get { return _Items.Capacity; }
			set { _Items.Capacity = value; }
		}
	}
}
