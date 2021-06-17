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
	/// Provides an Observable List supporting BeginUpdate and EndUpdate for batch changes
	/// </summary>
	public class ObservableList<T> : ObservableBase<T>, IList<T>
	{
		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		public ObservableList() => Items = new List<T>();

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		public ObservableList(IEnumerable<T> collection) => Items = new List<T>(collection);

		/// <summary>
		/// Creates a new Observable List
		/// </summary>
		/// <param name="capacity">The starting capacity of the list</param>
		public ObservableList(int capacity) => Items = new List<T>(capacity);

		//****************************************

		/// <inheritdoc />
		public override void Add(T item) => Insert(Items.Count, item, true);

		/// <inheritdoc />
		public override void AddRange(IEnumerable<T> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			var NewItems = new List<T>(items);

			if (NewItems.Count == 0)
				return;

			var StartIndex = Items.Count;

			Items.AddRange(NewItems);

			OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItems, StartIndex);
		}

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		/// <remarks>The list must be sorted before calling this method</remarks>
		public int BinarySearch(T item) => Items.BinarySearch(item);

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <param name="comparer">The comparer to use when searching</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		/// <remarks>The list must be sorted before calling this method</remarks>
		public int BinarySearch(T item, IComparer<T> comparer) => Items.BinarySearch(item, comparer);

		/// <inheritdoc />
		public override void Clear()
		{
			if (Items.Count > 0)
			{
				Items.Clear();

				OnCollectionChanged();
			}
		}

		/// <inheritdoc />
		public override bool Contains(T item) => Items.Contains(item);

		/// <inheritdoc />
		public override void CopyTo(T[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public List<T>.Enumerator GetEnumerator() => Items.GetEnumerator();

		/// <inheritdoc />
		public override int IndexOf(T item) => Items.IndexOf(item);

		/// <summary>
		/// Inserts an element at the specified index
		/// </summary>
		/// <param name="index">The index to insert the element</param>
		/// <param name="item">The element to insert</param>
		public void Insert(int index, T item) => Insert(index, item, true);

		/// <summary>
		/// Inserts elements at the specified index
		/// </summary>
		/// <param name="index">The index to insert the element</param>
		/// <param name="items">The elements to insert</param>
		public void InsertRange(int index, IEnumerable<T> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			var NewItems = new List<T>(items);

			if (NewItems.Count == 0)
				return;

			Items.InsertRange(index, NewItems);

			OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItems, index);
		}

		/// <summary>
		/// Replaces elements at the specified index
		/// </summary>
		/// <param name="index">The index to start replacing the elements</param>
		/// <param name="items">The elements to replace</param>
		public void ReplaceRange(int index, IEnumerable<T> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			var NewItems = new List<T>(items);

			if (NewItems.Count == 0)
				return;

			if (index < 0 || index + NewItems.Count > Items.Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			var OldItems = new T[NewItems.Count];

			var Index = 0;
			var StartIndex = index;

			foreach (var Item in NewItems)
			{
				OldItems[Index++] = Items[index];

				Items[index++] = Item;
			}

			OnCollectionChanged(NotifyCollectionChangedAction.Replace, OldItems, NewItems, StartIndex);
		}

		/// <inheritdoc />
		public override bool Remove(T item)
		{
			var MyIndex = Items.IndexOf(item);

			if (MyIndex == -1)
				return false;

			Items.RemoveAt(MyIndex);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, MyIndex);

			return true;
		}

		/// <inheritdoc />
		public override int RemoveAll(Predicate<T> predicate)
		{
			var Index = 0;

			// Find the first item we need to remove
			while (Index < Items.Count && !predicate(Items[Index]))
				Index++;

			// Did we find anything?
			if (Index >= Items.Count)
				return 0;

			var RemovedItems = new List<T> { Items[Index] };

			var InnerIndex = Index + 1;

			while (InnerIndex < Items.Count)
			{
				// Skip the items we need to remove
				while (InnerIndex < Items.Count && predicate(Items[InnerIndex]))
				{
					RemovedItems.Add(Items[InnerIndex]);

					InnerIndex++;
				}

				// If we reached the end, abort
				if (InnerIndex >= Items.Count)
					break;

				// We found one we're not removing, so move it up
				Items[Index] = Items[InnerIndex];

				Index++;
				InnerIndex++;
			}

			// Clear the removed items
			Items.RemoveRange(Index, Items.Count - Index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, RemovedItems);

			return InnerIndex - Index;
		}

		/// <inheritdoc />
		public override void RemoveAt(int index)
		{
			var OldItem = Items[index];

			Items.RemoveAt(index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, index);
		}

		/// <inheritdoc />
		public override void RemoveRange(int index, int count)
		{
			if (index < 0 || index >= Items.Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (index < 0 || index + count > Items.Count)
				throw new ArgumentOutOfRangeException(nameof(count));

			var OldItems = new T[count];

			Items.CopyTo(index, OldItems, 0, count);

			Items.RemoveRange(index, count);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItems, index);
		}

		/// <summary>
		/// Sorts the contents of this Observable List
		/// </summary>
		public void Sort()
		{
			if (Items.Count != 0)
			{
				Items.Sort();

				OnCollectionChanged();
			}
		}

		/// <summary>
		/// Sorts the contents of this Observable List based on a comparer
		/// </summary>
		/// <param name="comparer">The comparer to sort by</param>
		public void Sort(IComparer<T> comparer)
		{
			if (Items.Count != 0)
			{
				Items.Sort(comparer);

				OnCollectionChanged();
			}
		}

		/// <inheritdoc />
		public override T[] ToArray() => Items.ToArray();

		//****************************************

		/// <inheritdoc />
		protected override T InternalGet(int index) => Items[index];

		/// <inheritdoc />
		protected override IEnumerator<T> InternalGetEnumerator() => Items.GetEnumerator();

		/// <inheritdoc />
		protected override void InternalInsert(int index, T item) => Items.Insert(index, item);

		/// <inheritdoc />
		protected override void InternalSet(int index, T value) => Items[index] = value;

		//****************************************

		private void Insert(int index, T item, bool isAdd)
		{
			if (isAdd)
			{
				Items.Insert(index, item);

				OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
			}
			else
			{
				var OldValue = Items[index];
				Items[index] = item;

				OnCollectionChanged(NotifyCollectionChangedAction.Replace, item, OldValue, index);
			}
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the value at the provided index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public T this[int index]
		{
			get => Items[index];
			set => Insert(index, value, false);
		}

		/// <inheritdoc />
		public override int Count => Items.Count;

		/// <summary>
		/// Gets the underlying list object
		/// </summary>
		protected List<T> Items { get; private set; }

		/// <summary>
		/// Gets/Sets the current item capacity
		/// </summary>
		public int Capacity
		{
			get => Items.Capacity;
			set => Items.Capacity = value;
		}
	}
}
