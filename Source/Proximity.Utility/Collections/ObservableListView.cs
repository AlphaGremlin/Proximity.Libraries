/****************************************\
 ObservableListView.cs
 Created: 2015-09-09
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides an Observable View over a list that performs sorting based on a comparer
	/// </summary>
	/// <typeparam name="TValue">The type of the values in the list</typeparam>
	/// <remarks>
	/// <para>This does not support dynamic sorting, so if values are modified outside of this collection and it changes their positioning, data corruption will result</para>
	/// </remarks>
	public class ObservableListView<TValue> : IList<TValue>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
#if !NET40
, IReadOnlyList<TValue>
#endif
	{	//****************************************
		private const string CountString = "Count";
		private const string IndexerName = "Item[]";
		//****************************************
		private readonly IList<TValue> _Source;
		private readonly List<TValue> _Items;

		private readonly IComparer<TValue> _Comparer;
		private readonly Predicate<TValue> _Filter;
		private readonly int? _Maximum;

		private int _VisibleSize;
		//****************************************
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		public ObservableListView(IList<TValue> source) : this(source, (IComparer<TValue>)null, null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListView(IList<TValue> source, int? maximum) : this(source, (IComparer<TValue>)null, null, maximum)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		public ObservableListView(IList<TValue> source, Comparison<TValue> comparison) : this(source, new ComparisonComparer<TValue>(comparison), null, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		public ObservableListView(IList<TValue> source, IComparer<TValue> comparer) : this(source, comparer, null, null)
		{
		}
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListView(IList<TValue> source, IComparer<TValue> comparer, int? maximum) : this(source, comparer, null, maximum)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListView(IList<TValue> source, Predicate<TValue> filter) : this(source, (IComparer<TValue>)null, filter, null)
		{
		}
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="filter">A filter to apply to the source list</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListView(IList<TValue> source, Predicate<TValue> filter, int? maximum) : this(source, (IComparer<TValue>)null, filter, maximum)
		{
		}
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListView(IList<TValue> source, Comparison<TValue> comparison, Predicate<TValue> filter) : this(source, new ComparisonComparer<TValue>(comparison), filter, null)
		{
		}
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="filter">A filter to apply to the source list</param>
		public ObservableListView(IList<TValue> source, IComparer<TValue> comparer, Predicate<TValue> filter) : this(source, comparer, filter, null)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		/// <param name="filter">A filter to apply to the source list</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListView(IList<TValue> source, Comparison<TValue> comparison, Predicate<TValue> filter, int? maximum) : this(source, new ComparisonComparer<TValue>(comparison), filter, maximum)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="filter">A filter to apply to the source list</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListView(IList<TValue> source, IComparer<TValue> comparer, Predicate<TValue> filter, int? maximum)
		{
			_Source = source;
			_Filter = filter;
			_Maximum = maximum;
			_Items = new List<TValue>(source.Count);
			_Comparer = comparer ?? GetDefaultComparer();

			// If we're filtering, use the filter comparer
			if (filter != null)
				_Comparer = new FilterComparer(_Comparer, filter);

			// Bring the collection up to date
			if (source.Count > 0)
			{
				_Items.AddRange(source);
				_Items.Sort(_Comparer);

				_VisibleSize = GetVisibleCount();
			}
			
			if (source is INotifyCollectionChanged)
				((INotifyCollectionChanged)source).CollectionChanged += OnSourceChanged;
		}

		//****************************************

		/// <summary>
		/// Performs a Binary Search for the given item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>The index of the item, or the one's complement of the index where it should be inserted</returns>
		/// <remarks>Only searches the visible items</remarks>
		public int BinarySearch(TValue item)
		{
			return _Items.BinarySearch(0, Count, item, _Comparer);
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		/// <remarks>Only searches the visible items</remarks>
		public bool Contains(TValue item)
		{
			return _Items.BinarySearch(0, Count, item, _Comparer) >= 0;
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		/// <remarks>Only copies the visible items</remarks>
		public void CopyTo(TValue[] array, int arrayIndex)
		{
			_Items.CopyTo(0, array, arrayIndex, Count);
		}

		/// <summary>
		/// Releases this Observable List View
		/// </summary>
		/// <remarks>Detaches event handlers</remarks>
		public virtual void Dispose()
		{
			if (_Source is INotifyCollectionChanged)
				((INotifyCollectionChanged)_Source).CollectionChanged -= OnSourceChanged;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public ValueEnumerator GetEnumerator()
		{
			return new ValueEnumerator(this);
		}

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TValue item)
		{
			var MyIndex = _Items.BinarySearch(0, Count, item, _Comparer);

			if (MyIndex < 0)
				MyIndex = -1;

			return MyIndex;
		}

		//****************************************

		void IList<TValue>.Insert(int index, TValue item)
		{
			throw new NotSupportedException("List is read-only");
		}

		void IList<TValue>.RemoveAt(int index)
		{
			throw new NotSupportedException("List is read-only");
		}

		int IList.Add(object item)
		{
			throw new NotSupportedException("List is read-only");
		}

		void IList.Clear()
		{
			throw new NotSupportedException("List is read-only");
		}

		bool IList.Contains(object value)
		{
			return value is TValue && Contains((TValue)value);
		}

		int IList.IndexOf(object value)
		{
			if (value is TValue)
				return IndexOf((TValue)value);

			return -1;
		}

		void IList.Insert(int index, object item)
		{
			throw new NotSupportedException("List is read-only");
		}

		void IList.Remove(object item)
		{
			throw new NotSupportedException("List is read-only");
		}

		void IList.RemoveAt(int index)
		{
			throw new NotSupportedException("List is read-only");
		}

		void ICollection<TValue>.Add(TValue item)
		{
			throw new NotSupportedException("List is read-only");
		}

		void ICollection<TValue>.Clear()
		{
			throw new NotSupportedException("List is read-only");
		}

		void ICollection.CopyTo(Array array, int index)
		{
			((IList)_Items).CopyTo(array, index);
		}

		bool ICollection<TValue>.Remove(TValue item)
		{
			throw new NotSupportedException("List is read-only");
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ValueEnumerator(this);
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return new ValueEnumerator(this);
		}

		/// <summary>
		/// Raises the PropertyChanged event
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Occurs when the collection has been reset
		/// </summary>
		/// <param name="oldItems">A list of the old items in the collection, including hidden items</param>
		protected virtual void OnItemsReset(TValue[] oldItems)
		{
		}

		/// <summary>
		/// Occurs when the collection has an item added
		/// </summary>
		/// <param name="newItem">The new item</param>
		/// <param name="index">The index of the item</param>
		protected virtual void OnItemAdded(TValue newItem, int index)
		{
		}

		/// <summary>
		/// Occurs when the collection has an item removed
		/// </summary>
		/// <param name="oldItem">The new item</param>
		/// <param name="index">The index of the item</param>
		protected virtual void OnItemRemoved(TValue oldItem, int index)
		{
		}

		/// <summary>
		/// Occurs when the collection has an item replaced
		/// </summary>
		/// <param name="newItem">The new item</param>
		/// <param name="oldItem">The old item</param>
		/// <param name="index">The index of the item that was replaced</param>
		protected virtual void OnItemReplaced(TValue newItem, TValue oldItem, int index)
		{
		}

		/// <summary>
		/// Re-evaluates the sorting of an item
		/// </summary>
		/// <param name="newItem">The new state of the item to sort on</param>
		/// <param name="oldIndex">The old index of the item</param>
		/// <param name="newIndex">The new index of the item</param>
		/// <param name="hasChanged">True if the value has also changed, otherwise false (if it's an object where the sorting parameter has changed)</param>
		protected virtual void ResortItem(TValue newItem, int oldIndex, int newIndex, bool hasChanged)
		{
			// Where should the item be now?
			var OldItem = _Items[oldIndex];

			if (oldIndex >= _VisibleSize)
			{
				// Old location is hidden. Is the new item visible?
				if ((newIndex < _VisibleSize && (_Filter == null || _Filter(newItem)))
					|| (newIndex == _VisibleSize && (!_Maximum.HasValue || _Maximum.Value != _VisibleSize) && (_Filter == null || _Filter(newItem)))
					)
				{
					// Old Index is hidden, New Index is visible
					// Does this push an item off the list?
					if (_Maximum.HasValue && _Maximum.Value == _VisibleSize)
					{
						// Yes, so first we need to 'remove' the item at the very top of the list
						OldItem = _Items[--_VisibleSize];

						OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, _VisibleSize);
					}

					_VisibleSize++; // Restore visible size

					// Update the internal list and notify observers
					_Items.RemoveAt(oldIndex);
					_Items.Insert(newIndex, newItem); // New Index must be less than Old Index, so we don't need to adjust

					OnCollectionChanged(NotifyCollectionChangedAction.Add, newItem, newIndex);

					return;
				}

				// New item is not visible
				// We don't need to raise any events because the old and new locations are hidden
				if (oldIndex == newIndex || oldIndex + 1 == newIndex)
				{
					// Index hasn't changed at all
					_Items[oldIndex] = newItem;

					return;
				}

				_Items.RemoveAt(oldIndex);

				// If the target index is after where removed the old item, we need to correct the index
				if (newIndex > oldIndex)
					newIndex--;

				_Items.Insert(newIndex, newItem);

				return;
			}

			// Our old location is visible. Is the new one also visible?
			// Make sure we account for the fact that we'll be removing one item from 'below'
			if (newIndex <= _VisibleSize)
			{
				if (_Filter == null || _Filter(newItem))
				{
					// Our new location is also visible, so we can process a move
					// Is the item moving at all?
					if (oldIndex == newIndex || oldIndex + 1 == newIndex)
					{
						// Nope, so just swap in-place
						_Items[oldIndex] = newItem;

						if (hasChanged)
							OnCollectionChanged(NotifyCollectionChangedAction.Replace, newItem, OldItem, oldIndex);

						return;
					}

					_Items.RemoveAt(oldIndex);

					// Yes, so move it and notify observers
					if (hasChanged)
					{
						// Item has changed value too, so we need to process as a Remove/Add
						_VisibleSize--;

						OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, oldIndex);

						if (newIndex > oldIndex)
							newIndex--; // Adjust the index if it's moving up in the list

						_Items.Insert(newIndex, newItem);
						_VisibleSize++;

						OnCollectionChanged(NotifyCollectionChangedAction.Add, newItem, newIndex);
					}
					else
					{
						if (newIndex > oldIndex)
							newIndex--; // Adjust the index if it's moving up in the list

						_Items.Insert(newIndex, newItem);

						OnCollectionChanged(NotifyCollectionChangedAction.Move, newItem, oldIndex, newIndex);
					}

					return;
				}

				// Item is being moved to the top of the visible range, but it's hidden
			}

			// This item is disappearing
			// Get the next item that might potentially appear
			var VisibleItem = _Items[_VisibleSize];
			
			// Adjust our display items
			_Items.RemoveAt(oldIndex);
			_Items.Insert(newIndex - 1, newItem); // Adjust since old index must be less

			// Looks like a remove and size decrease
			_VisibleSize--;
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, oldIndex);

			// Is there an item for us to show to replace it?
			if (_Filter == null || _Filter(VisibleItem))
			{
				// New item is visible
				_VisibleSize++;

				OnCollectionChanged(NotifyCollectionChangedAction.Add, VisibleItem, _VisibleSize - 1);
			}
		}

		//****************************************

		private void OnAddCollectionItem(TValue newItem)
		{
			// Where should this item go based on the comparer?
			var InsertIndex = _Items.BinarySearch(newItem, _Comparer);
			
			// If this is a new item, get the final index to insert at
			if (InsertIndex < 0)
				InsertIndex = ~InsertIndex;

			// Could this change our visible size?
			if (InsertIndex == _VisibleSize)
			{
				// We've been inserted at the very top of the visible set. Is the item filtered?
				if (_Filter == null || _Filter(newItem))
				{
					// Item should be visible. Are we below the maximum (if any)?
					if (!_Maximum.HasValue || _Maximum.Value > InsertIndex)
						_VisibleSize++; // We can be shown. Go for it
				}
			}
			else if (InsertIndex < _VisibleSize)
			{
				// We're inserting below the visible size, which means we have to be unfiltered
				// Does this push an item off the list?
				if (_Maximum.HasValue)
				{
					if (_Maximum.Value == _VisibleSize)
					{
						// Yes, so first we need to 'remove' the item at the very top of the list
						var OldItem = _Items[--_VisibleSize];

						// This affects observers, but not subclasses
						OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, _VisibleSize);
					}
				}

				_VisibleSize++;
			}
			// Otherwise, we've inserted above the visible size, so we must be either filtered or above the maximum

			_Items.Insert(InsertIndex, newItem);

			// Notify subclasses
			OnItemAdded(newItem, InsertIndex);

			// Only notify observers if we're visible
			if (InsertIndex < _VisibleSize)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, newItem, InsertIndex);
		}

		private void OnRemoveCollectionItem(TValue oldItem)
		{
			// Where is this item currently?
			var OldIndex = _Items.BinarySearch(oldItem, _Comparer);

			if (OldIndex < 0)
				throw new InvalidOperationException("Unknown Item being removed"); // Remove for an item we don't know?

			_Items.RemoveAt(OldIndex);

			// Notify subclasses
			OnItemRemoved(oldItem, OldIndex);

			if (OldIndex >= _VisibleSize)
				return; // No need to do anything more, since we're removing a hidden item

			// We're removing a visible item.
			// Are there hidden items that might get shown?
			if (_Items.Count < _VisibleSize)
			{
				// No, so just fix the Visible Size
				_VisibleSize--;
				OnCollectionChanged(NotifyCollectionChangedAction.Remove, oldItem, OldIndex);

				return;
			}

			// We have hidden items that might potentially appear
			var NewItem = _Items[_VisibleSize - 1];

			// Is the new item filtered?
			if (_Filter != null && !_Filter(NewItem))
			{
				// Can't show this item, so the Visible Size decreases
				_VisibleSize--;
				OnCollectionChanged(NotifyCollectionChangedAction.Remove, oldItem, OldIndex);

				return; 
			}

			// The next item in the list can appear!
			if (OldIndex == _VisibleSize - 1)
			{
				// We're removing the top item on the list, so optimise to a Replace
				OnCollectionChanged(NotifyCollectionChangedAction.Replace, NewItem, oldItem, OldIndex);

				return;
			}

			// Remove the old item first, so we never risk going above the maximum
			_VisibleSize--;
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, oldItem, OldIndex);

			// 'Add' the new item
			_VisibleSize++;
			OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, _VisibleSize - 1);
		}

		private void OnReplaceCollectionItem(TValue oldItem, TValue newItem)
		{
			// Where is the old item currently?
			var OldIndex = _Items.BinarySearch(oldItem, _Comparer);

			if (OldIndex < 0)
				throw new InvalidOperationException("Unknown Item being replaced"); // Replace for an item we don't know?

			var NewIndex = _Items.BinarySearch(newItem, _Comparer);

			// If this is a new item, get the final index to insert at
			if (NewIndex < 0)
				NewIndex = ~NewIndex;

			// Relocate the item
			ResortItem(newItem, OldIndex, NewIndex, true);
		}

		//****************************************

		private void OnSourceChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Reset:
				var OldItems = _Items.ToArray();

				_Items.Clear();

				_Items.AddRange(_Source);
				_Items.Sort(_Comparer);
				
				_VisibleSize = GetVisibleCount();

				OnItemsReset(OldItems);
				OnCollectionChanged();
				break;

			case NotifyCollectionChangedAction.Add:
				for (int Index = 0; Index < e.NewItems.Count; Index++)
				{
					OnAddCollectionItem((TValue)e.NewItems[Index]);
				}
				break;

			case NotifyCollectionChangedAction.Remove:
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					OnRemoveCollectionItem((TValue)e.OldItems[Index]);
				}
				break;

			case NotifyCollectionChangedAction.Replace:
				var MinCount = Math.Min(e.OldItems.Count, e.NewItems.Count);

				for (int Index = 0; Index < MinCount; Index++)
				{
					TValue OldItem = (TValue)e.OldItems[Index], NewItem = (TValue)e.NewItems[Index];

					// Has the item changed?
					if (_Comparer.Compare(OldItem, NewItem) == 0)
						return; // Item hasn't changed, so its sorting position won't either

					OnReplaceCollectionItem(OldItem, NewItem);
				}

				// Replace may involve removing items
				for (int Index = MinCount; MinCount < e.OldItems.Count; Index++)
				{
					OnRemoveCollectionItem((TValue)e.OldItems[Index]);
				}

				// Alternatively, it may involve adding new items
				for (int Index = MinCount; MinCount < e.NewItems.Count; Index++)
				{
					OnAddCollectionItem((TValue)e.NewItems[Index]);
				}
				break;

			case NotifyCollectionChangedAction.Move:
				// Do nothing, the source moving things around won't affect our sorting
				break;
			}
		}

		private void OnPropertyChanged()
		{
			OnPropertyChanged(CountString);
			OnPropertyChanged(IndexerName);
		}

		private void OnCollectionChanged()
		{
			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int index)
		{
			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int oldIndex, int newIndex)
		{
			OnPropertyChanged(IndexerName); // Only the indexer has changed, the count is the same

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, newIndex, oldIndex));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue newItem, TValue oldItem, int index)
		{
			OnPropertyChanged(IndexerName); // Only the indexer has changed, the count is the same

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		private bool FilterItem(TValue item)
		{
			return _Filter(item); // Enumerable.Where requires an Action, but this is a Predicate, so we wrap it
		}
		
		private void CheckIndex(int index)
		{
			if (index >= _VisibleSize)
				throw new ArgumentOutOfRangeException("index", "Index is outside the visible range of items");
		}

		private int GetVisibleCount()
		{
			if (Items.Count == 0)
				return 0;

			if (_Filter == null)
			{
				if (_Maximum.HasValue)
					return Math.Min(_Maximum.Value, Items.Count); // Trim if we're above the max

				return Items.Count; // All visible
			}

			// Find the lowest item that's visible
			int LowIndex = 0, HighIndex = Items.Count - 1;

			if (_Maximum.HasValue) // If there's a maximum, our high cannot be above that.
				HighIndex = _Maximum.Value;

			while (LowIndex <= HighIndex)
			{
				int MiddleIndex = LowIndex + ((HighIndex - LowIndex) >> 1);

				if (_Filter(Items[MiddleIndex]))
					LowIndex = MiddleIndex + 1; // Visible, high must be above this
				else
					HighIndex = MiddleIndex - 1; // Hidden, high must be below
			}

			if (LowIndex < Items.Count && _Filter(Items[LowIndex]))
				return LowIndex + 1;

			return LowIndex;
		}
#if DEBUG
		protected void VerifyList()
		{
			var LastItem = _Items[0];

			for (int SubIndex = 1; SubIndex < _Items.Count; SubIndex++)
			{
				var NextItem = _Items[SubIndex];

				if (_Comparer.Compare(LastItem, NextItem) > 0)
					throw new InvalidOperationException("Out of order");

				LastItem = NextItem;
			}
		}
#endif
		//****************************************

			/// <summary>
			/// Raised when the collection changes
			/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Raised when a property of the collection changes
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets the item at the requested index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[int index]
		{
			get { CheckIndex(index); return _Items[index]; }
		}

		/// <summary>
		/// Gets the number of items in this list
		/// </summary>
		public int Count
		{
			get { return _VisibleSize; }
		}

		/// <summary>
		/// Gets whether this collection is read-only
		/// </summary>
		public bool IsReadOnly
		{
			get { return true; }
		}

		/// <summary>
		/// Gets the comparer being used by this Observable View
		/// </summary>
		public IComparer<TValue> Comparer
		{
			get { return _Comparer; }
		}

		/// <summary>
		/// Gets the maximum number of items that will be displayed in this List View
		/// </summary>
		/// <remarks>Will be null if there is no limit</remarks>
		public int? Maximum
		{
			get { return _Maximum; }
		}

		/// <summary>
		/// Gets the predicate that is filtering this List View
		/// </summary>
		public Predicate<TValue> Filter
		{
			get { return _Filter; }
		}

		TValue IList<TValue>.this[int index]
		{
			get { CheckIndex(index); return _Items[index]; }
			set { throw new NotSupportedException("List is read-only"); }
		}

		object IList.this[int index]
		{
			get { CheckIndex(index); return _Items[index]; }
			set { throw new NotSupportedException("List is read-only"); }
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { return _Source; }
		}

		/// <summary>
		/// Gets the internal list of items, including filtered and trimmed ones
		/// </summary>
		protected List<TValue> Items
		{
			get { return _Items; }
		}

		/// <summary>
		/// Gets the internal list of items, including filtered and trimmed ones
		/// </summary>
		public List<TValue> AllItems
		{
			get { return _Items; }
		}

		//****************************************

		private static IComparer<TValue> GetDefaultComparer()
		{
#if NETSTANDARD1_3
			var MyInfo = typeof(TValue).GetTypeInfo();

			if (!typeof(IComparable<TValue>).GetTypeInfo().IsAssignableFrom(MyInfo) && !typeof(IComparable).GetTypeInfo().IsAssignableFrom(MyInfo))
#else
			if (!typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) && !typeof(IComparable).IsAssignableFrom(typeof(TValue)))
#endif
				throw new ArgumentException(string.Format("{0} does not implement IComparable or IComparable<>", typeof(TValue).FullName));

			return Comparer<TValue>.Default;
		}

		//****************************************
		
		/// <summary>
		/// Enumerates the sorted set while avoiding memory allocations
		/// </summary>
		public struct ValueEnumerator : IEnumerator<TValue>, IEnumerator
		{ //****************************************
			private readonly ObservableListView<TValue> _Parent;

			private int _Index;
			private TValue _Current;
			//****************************************

			internal ValueEnumerator(ObservableListView<TValue> parent)
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
				if (_Index >= _Parent._VisibleSize)
				{
					_Index = _Parent._VisibleSize + 1;
					_Current = default(TValue);

					return false;
				}

				_Current = _Parent._Items[_Index++];

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

		private sealed class FilterComparer : IComparer<TValue>
		{ //****************************************
			private readonly IComparer<TValue> _Comparer;
			private readonly Predicate<TValue> _Filter;
			//****************************************

			internal FilterComparer(IComparer<TValue> comparer, Predicate<TValue> filter)
			{
				_Comparer = comparer;
				_Filter = filter;
			}

			//****************************************

			int IComparer<TValue>.Compare(TValue x, TValue y)
			{
				// Figure out if our values are filtered
				var XResult = _Filter(x);
				var YResult = _Filter(y);

				// Are their filtering statuses the same?
				if (XResult != YResult)
					// No. Make sure True objects sort before False
					return XResult ? -1 : 1;
				
				// Filtering status is the same. Use the normal comparer
				return _Comparer.Compare(x, y);
			}
		}
	}
}
