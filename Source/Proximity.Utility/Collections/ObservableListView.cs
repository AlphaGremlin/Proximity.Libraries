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
		private readonly List<TValue> _Items, _HiddenItems;

		private readonly IComparer<TValue> _Comparer;
		private readonly Predicate<TValue> _Filter;
		private readonly int? _Maximum;
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
		/// <param name="comparer">The comparer to use for sorting</param>
		/// <param name="filter">A filter to apply to the source list</param>
		/// <param name="maximum">The maximum number of items to show</param>
		public ObservableListView(IList<TValue> source, IComparer<TValue> comparer, Predicate<TValue> filter, int? maximum)
		{
			_Source = source;
			_Comparer = comparer ?? GetDefaultComparer();
			_Filter = filter;
			_Maximum = maximum;
			_Items = new List<TValue>(source.Count);

			if (maximum.HasValue)
				_HiddenItems = new List<TValue>();

			// Bring the collection up to date
			if (source.Count > 0)
			{
				if (filter != null)
					_Items.AddRange(source.Where(FilterItem));
				else
					_Items.AddRange(source);

				_Items.Sort(comparer);

				if (maximum.HasValue && _Items.Count > maximum.Value)
					_Items.RemoveRange(maximum.Value, _Items.Count - maximum.Value);
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
		public int BinarySearch(TValue item)
		{
			return _Items.BinarySearch(item, _Comparer);
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TValue item)
		{
			return _Items.BinarySearch(item, _Comparer) >= 0;
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TValue[] array, int arrayIndex)
		{
			_Items.CopyTo(array, arrayIndex);
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
		public List<TValue>.Enumerator GetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TValue item)
		{
			var MyIndex = _Items.BinarySearch(item, _Comparer);

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
			return _Items.GetEnumerator();
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return _Items.GetEnumerator();
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
		/// <param name="oldItems">A list of the old items in the collection</param>
		protected virtual void OnCollectionChanged(TValue[] oldItems)
		{
			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary>
		/// Occurs when the collection has an item added or removed
		/// </summary>
		/// <param name="action">The action that occurred</param>
		/// <param name="changedItem">The old or new item</param>
		/// <param name="index">The index of the item</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem, int index)
		{
			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));
		}
		
		/// <summary>
		/// Occurs when the collection has an item replaced
		/// </summary>
		/// <param name="action">The action that occurred</param>
		/// <param name="newItem">The new item</param>
		/// <param name="oldItem">The old item</param>
		/// <param name="index">The index of the item that was replaced</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, TValue newItem, TValue oldItem, int index)
		{
			OnPropertyChanged(IndexerName); // Only the indexer has changed, the count is the same

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		//****************************************

		private void OnSourceChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Reset:
				var OldItems = _Items.ToArray();

				_Items.Clear();

				if (_Filter != null)
					_Items.AddRange(_Source.Where(FilterItem));
				else
					_Items.AddRange(_Source);

				_Items.Sort(_Comparer);

				// Trim the bottom items if there's a limit
				if (_Maximum.HasValue)
				{
					_HiddenItems.Clear();

					if (_Items.Count > _Maximum.Value)
					{
						// Copy the already sorted items to the hidden items
						for (int Index = _Maximum.Value; Index < _Items.Count; Index++)
						{
							_HiddenItems.Add(_Items[Index]);
						}

						_Items.RemoveRange(_Maximum.Value, _Items.Count - _Maximum.Value);
					}
				}

				OnCollectionChanged(OldItems);
				break;

			case NotifyCollectionChangedAction.Add:
				for (int Index = 0; Index < e.NewItems.Count; Index++)
				{
					var NewItem = (TValue)e.NewItems[Index];

					if (_Filter != null && !_Filter(NewItem))
						continue;

					// Where should this item go based on the comparer?
					var InsertIndex = _Items.BinarySearch(NewItem, _Comparer);

					// Is there a maximum?
					if (_Maximum.HasValue)
					{
						// Are we inserting a new item?
						if (InsertIndex < 0)
						{
							// Get the final index to insert at
							InsertIndex = ~InsertIndex;

							// If we're inserting at the very end past the limit, skip this item
							if (InsertIndex == _Maximum.Value)
							{
								AddToHidden(NewItem);

								continue;
							}
						}

						// If this pushes us over the maximum, move the last visible item
						if (_Items.Count == _Maximum.Value)
							MoveLastVisibleToHidden();
					}
					else
					{
						// If this is a new item, get the final index to insert at
						if (InsertIndex < 0)
							InsertIndex = ~InsertIndex;
					}

					// Insert our new item
					_Items.Insert(InsertIndex, NewItem);

					OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, InsertIndex);
				}
				break;

			case NotifyCollectionChangedAction.Remove:
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					var OldItem = (TValue)e.OldItems[Index];

					var OldIndex = _Items.BinarySearch(OldItem, _Comparer);

					if (OldIndex < 0)
					{
						// If there's a maximum and we didn't find the item, ensure we remove it from the hidden list
						if (_Maximum.HasValue)
						{
							OldIndex = _HiddenItems.BinarySearch(OldItem, _Comparer);

							if (OldIndex >= 0)
								_HiddenItems.RemoveAt(OldIndex);
						}

						continue;
					}

					_Items.RemoveAt(OldIndex);

					OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, OldIndex);
				}

				// If there's a maximum, can we add more items from the hidden list?
				if (_Maximum.HasValue)
				{
					var MaxItems = Math.Min(_Maximum.Value - _Items.Count, _HiddenItems.Count);

					if (MaxItems != 0)
					{
						// Copy the already sorted items from the hidden list to the end of our visible list
						for (int Index = 0; Index < MaxItems; Index++)
						{
							var NewItem = _HiddenItems[Index];

							_Items.Add(NewItem);

							OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, _Items.Count - 1);
						}

						_HiddenItems.RemoveRange(0, MaxItems);
					}
				}
				break;

			case NotifyCollectionChangedAction.Replace:
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					// Retrieve the old and new items
					TValue OldItem = (TValue)e.OldItems[Index], NewItem = (TValue)e.NewItems[Index];

					// Has the item changed?
					if (_Comparer.Compare(OldItem, NewItem) == 0)
						continue; // Item hasn't changed, so its sorting position won't either

					// Is the old Item in the visible list?
					int OldIndex = _Items.BinarySearch(OldItem, _Comparer), NewIndex;

					if (OldIndex < 0)
					{
						if (_Maximum.HasValue)
						{
							// It's not in the visible list, but it might be in the hidden one
							OldIndex = _HiddenItems.BinarySearch(OldItem, _Comparer);

							// If it's in the hidden list, remove it
							if (OldIndex >= 0)
								_HiddenItems.RemoveAt(OldIndex);
						}

						// It's not in the visible list or hidden list.

						// Does the new item meet the filter?
						if (_Filter != null && !_Filter(NewItem))
							continue; // No, so ignore it

						// Yes, so we might need to insert it somewhere

						// Where should it go in the visible list?
						NewIndex = _Items.BinarySearch(NewItem, _Comparer);

						if (NewIndex < 0)
							NewIndex = ~NewIndex;

						// Is there a limit on the visible list?
						if (_Maximum.HasValue)
						{
							// If we're inserting over the maximum, add to the hidden list instead
							if (NewIndex == _Maximum.Value)
							{
								AddToHidden(NewItem);

								continue;
							}

							// We're not adding to the end. Does this push us over the maximum?
							if (_Items.Count == _Maximum.Value)
								MoveLastVisibleToHidden();
						}

						// We're inserting somewhere into the visible list and we're guaranteed space
						_Items.Insert(NewIndex, NewItem);
						OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, NewIndex);

						continue;
					}
					
					// The old item is visible and has changed

					// Does it still meet the filter?
					if (_Filter != null && !_Filter(NewItem))
					{
						// No, so remove it
						_Items.RemoveAt(OldIndex);
						OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, OldIndex);

						// If there's a maximum and we've hidden items, move the first item to replace the one we removed
						if (_Maximum.HasValue && _HiddenItems.Count > 0)
							MoveFirstHiddenToVisible();

						continue;
					}

					// It meets the filter, what is its new Index?
					NewIndex = _Items.BinarySearch(NewItem, _Comparer);

					if (NewIndex < 0)
						NewIndex = ~NewIndex;
					
					// We're moving the new item. Is there a maximum?
					if (_Maximum.HasValue)
					{
						// Are we moving it to the end and there are hidden items?
						if (NewIndex == _Maximum.Value && _HiddenItems.Count > 0)
						{
							// Is it greater than the top-most hidden item?
							if (_Comparer.Compare(NewItem, _HiddenItems[0]) > 0)
							{
								// Yes, so we need to remove ourselves from the visible list, then add the top-most hidden item
								_Items.RemoveAt(OldIndex);
								OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, OldIndex);

								MoveFirstHiddenToVisible();

								// Add the new item to the hidden list
								AddToHidden(NewItem);

								continue;
							}

							// Less than the top-most hidden item, so we'll just add it to the end
						}
					}

					// Would we place this immediately before or after the old item?
					if (NewIndex == OldIndex || NewIndex == OldIndex + 1)
					{
						// Yes, so just replace the old item instead
						_Items[OldIndex] = NewItem;

						OnCollectionChanged(NotifyCollectionChangedAction.Replace, NewItem, OldItem, OldIndex);

						continue;
					}

					// It's going elsewhere, so first remove the old item
					_Items.RemoveAt(OldIndex);
					OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, OldIndex);

					// If the target index is after where removed the old item, we need to correct the index
					if (NewIndex > OldIndex)
						NewIndex--;

					// Now we can insert the new item where it belongs
					_Items.Insert(NewIndex, NewItem);
					OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, NewIndex);
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

		private bool FilterItem(TValue item)
		{
			return _Filter(item); // Enumerable.Where requires an Action, but this is a Predicate, so we wrap it
		}

		private void AddToHidden(TValue item)
		{
			var InsertIndex = _HiddenItems.BinarySearch(item, _Comparer);

			if (InsertIndex < 0)
				InsertIndex = ~InsertIndex;

			_HiddenItems.Insert(InsertIndex, item);
		}

		private void MoveLastVisibleToHidden()
		{
			var OldIndex = _Items.Count - 1;
			var OldItem = _Items[OldIndex];

			_Items.RemoveAt(OldIndex);
			_HiddenItems.Insert(0, OldItem); // The bottom of the visible list is always the top of the hidden

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, OldItem, OldIndex);
		}

		private void MoveFirstHiddenToVisible()
		{
			var NewItem = _HiddenItems[0];
			var NewIndex = _Items.Count;

			_HiddenItems.RemoveAt(0);
			_Items.Add(NewItem); // The top of the hidden list is always the bottom of the visible

			OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, NewIndex);
		}

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
			get { return _Items[index]; }
		}

		/// <summary>
		/// Gets the number of items in this list
		/// </summary>
		public int Count
		{
			get { return _Items.Count; }
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
			get { return _Items[index]; }
			set { throw new NotSupportedException("List is read-only"); }
		}

		object IList.this[int index]
		{
			get { return _Items[index]; }
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
		/// Gets the internal list of items
		/// </summary>
		protected List<TValue> Items
		{
			get { return _Items; }
		}

		//****************************************

		private static IComparer<TValue> GetDefaultComparer()
		{
#if PORTABLE
			var MyInfo = typeof(TValue).GetTypeInfo();

			if (!typeof(IComparable<TValue>).GetTypeInfo().IsAssignableFrom(MyInfo) && !typeof(IComparable).GetTypeInfo().IsAssignableFrom(MyInfo))
#else
			if (!typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)) && !typeof(IComparable).IsAssignableFrom(typeof(TValue)))
#endif
				throw new ArgumentException(string.Format("{0} does not implement IComparable or IComparable<>", typeof(TValue).FullName));

			return Comparer<TValue>.Default;
		}
	}
}
