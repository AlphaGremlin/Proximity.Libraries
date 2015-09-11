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
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides an Observable View over a list that performs sorting based on a comparer
	/// </summary>
	/// <typeparam name="TValue">The type of the values in the list</typeparam>
	/// <remarks>This does not support dynamic sorting, so if values are modified outside of this collection and it changes their positioning, data corruption will result</remarks>
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
		//****************************************
		
		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		public ObservableListView(IList<TValue> source) : this(source, Comparer<TValue>.Default)
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparison">A delegate to perform the comparison with</param>
		public ObservableListView(IList<TValue> source, Comparison<TValue> comparison) : this(source, new ComparisonComparer<TValue>(comparison))
		{
		}

		/// <summary>
		/// Creates a new Observable List View
		/// </summary>
		/// <param name="source">The source list to wrap</param>
		/// <param name="comparer">The comparer to use for sorting</param>
		public ObservableListView(IList<TValue> source, IComparer<TValue> comparer)
		{
			_Source = source;
			_Comparer = comparer;
			_Items = new List<TValue>(source.Count);

			OnSourceChanged(_Source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			
			if (source is INotifyCollectionChanged)
				((INotifyCollectionChanged)source).CollectionChanged += OnSourceChanged;
		}

		//****************************************

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
		public void Dispose()
		{
			if (_Source is INotifyCollectionChanged)
				((INotifyCollectionChanged)_Source).CollectionChanged -= OnSourceChanged;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TValue> GetEnumerator()
		{
			return _Source.GetEnumerator();
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

		/// <summary>
		/// Raises the PropertyChanged event
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		//****************************************

		private void OnSourceChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Reset:
				_Items.Clear();
				_Items.AddRange(_Source);
				_Items.Sort(_Comparer);

				OnCollectionChanged();
				break;

			case NotifyCollectionChangedAction.Add:
				for (int Index = 0; Index < e.NewItems.Count; Index++)
				{
					var NewItem = (TValue)e.NewItems[Index];

					// Where should this item go based on the comparer?
					var InsertIndex = _Items.BinarySearch(NewItem, _Comparer);

					if (InsertIndex >= 0)
					{
						// Identical item is already in the list, so insert there
						_Items.Insert(InsertIndex, NewItem);

						OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, InsertIndex);
					}
					else
					{
						// New item, insert based on the sort order
						_Items.Insert(~InsertIndex, NewItem);

						OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, ~InsertIndex);
					}
				}
				break;

			case NotifyCollectionChangedAction.Remove:
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					var OldItem = (TValue)e.OldItems[Index];

					var OldIndex = _Items.BinarySearch(OldItem, _Comparer);

					if (OldIndex < 0)
						continue;

					_Items.RemoveAt(OldIndex);

					OnCollectionChanged(NotifyCollectionChangedAction.Add, OldItem, OldIndex);
				}
				break;

			case NotifyCollectionChangedAction.Replace:
				for (int Index = 0; Index < e.OldItems.Count; Index++)
				{
					var OldItem = (TValue)e.OldItems[Index];

					var OldIndex = _Items.BinarySearch(OldItem, _Comparer);

					// Is this item even in the list?
					if (OldIndex < 0)
						continue;

					var NewItem = (TValue)e.NewItems[Index];

					// Has the item changed?
					if (_Comparer.Compare(OldItem, NewItem) == 0)
						continue; // Item hasn't changed, so its sorting position won't either

					// Yes, what is its new Index?
					var NewIndex = _Items.BinarySearch(NewItem, _Comparer);
					var RealIndex = NewIndex < 0 ? ~NewIndex : NewIndex;
					
					// Would we place this immediately before or after the old item?
					if (RealIndex == OldIndex || RealIndex == OldIndex + 1)
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
					if (RealIndex > OldIndex)
						RealIndex -= 1;

					// Now we can insert the new item where it belongs
					_Items.Insert(RealIndex, NewItem);
					OnCollectionChanged(NotifyCollectionChangedAction.Add, NewItem, RealIndex);
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

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue newItem, TValue oldItem, int index)
		{
			OnPropertyChanged(IndexerName); // Only the indexer has changed, the count is the same

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
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
			get { return _Source[index]; }
		}

		/// <summary>
		/// Gets the number of items in this list
		/// </summary>
		public int Count
		{
			get { return _Source.Count; }
		}

		/// <summary>
		/// Gets whether this collection is read-only
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		TValue IList<TValue>.this[int index]
		{
			get { return _Source[index]; }
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
	}
}
