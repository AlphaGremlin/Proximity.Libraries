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
	/// Provides an Observable Set supporting BeginUpdate and EndUpdate for batch changes
	/// </summary>
	/// <typeparam name="TValue">The type of value within the set</typeparam>
	public class ObservableSet<TValue> : ISet<TValue>, INotifyCollectionChanged, INotifyPropertyChanged
	{	//****************************************
		private const string CountString = "Count";
		//****************************************
		private readonly HashSet<TValue> _Items;

		private int _UpdateCount = 0;
		//****************************************

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		public ObservableSet()
		{
			_Items = new HashSet<TValue>();
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		public ObservableSet(IEnumerable<TValue> collection)
		{
			_Items = new HashSet<TValue>(collection);
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(IEqualityComparer<TValue> comparer)
		{
			_Items = new HashSet<TValue>(comparer);
		}

		/// <summary>
		/// Creates a new Observable Set
		/// </summary>
		/// <param name="collection">The items to populate the list with</param>
		/// <param name="comparer">The equality comparer to use for the obserable set</param>
		public ObservableSet(IEnumerable<TValue> collection, IEqualityComparer<TValue> comparer)
		{
			_Items = new HashSet<TValue>(collection, comparer);
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <returns>True if the element was added, False if it was already in the set</returns>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public bool Add(TValue item)
		{	//****************************************
			var WasAdded = _Items.Add(item);
			//****************************************

			if (WasAdded)
			{
				OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
			}

			return WasAdded;
		}

		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="items">The elements to add</param>
		public void AddRange(IEnumerable<TValue> items)
		{
			var NewItems = new List<TValue>();

			if (items == null) throw new ArgumentNullException("items");

			foreach (var MyItem in items)
			{
				if (_Items.Add(MyItem))
					NewItems.Add(MyItem);
			}

			if (NewItems.Count != 0)
				OnCollectionChanged(NotifyCollectionChangedAction.Add, items);
		}

		/// <summary>
		/// Begins a major update operation, suspending change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void BeginUpdate()
		{
			if (_UpdateCount++ == 0)
				OnPropertyChanged("IsUpdating");
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			if (_Items.Count > 0)
			{
				_Items.Clear();

				OnCollectionChanged();
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TValue item)
		{
			return _Items.Contains(item);
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
		/// Ends a major update operation, resuming change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void EndUpdate()
		{
			if (_UpdateCount == 0)
				return;

			if (--_UpdateCount == 0)
				OnPropertyChanged("IsUpdating");

			OnCollectionChanged();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TValue> GetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<TValue> other)
		{
			return _Items.IsProperSubsetOf(other);
		}

		/// <summary>
		/// Checks whether this set is a strict superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict superset of the collection, otherwise False</returns>
		public bool IsProperSupersetOf(IEnumerable<TValue> other)
		{
			return _Items.IsProperSupersetOf(other);
		}

		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<TValue> other)
		{
			return _Items.IsSubsetOf(other);
		}

		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<TValue> other)
		{
			return _Items.IsSupersetOf(other);
		}

		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<TValue> other)
		{
			return _Items.Overlaps(other);
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was in the collection and removed, otherwise False</returns>
		public bool Remove(TValue item)
		{	//****************************************
			var WasRemoved = _Items.Remove(item);
			//****************************************

			if (WasRemoved)
			{
				OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
			}

			return WasRemoved;
		}

		/// <summary>
		/// Checks whether this set and the given collection contain the same elements
		/// </summary>
		/// <param name="other">The collection to check against</param>
		/// <returns>True if they contain the same elements, otherwise FAlse</returns>
		public bool SetEquals(IEnumerable<TValue> other)
		{
			return _Items.SetEquals(other);
		}

		//****************************************

		void ICollection<TValue>.Add(TValue item)
		{
			this.Add(item);
		}

		void ISet<TValue>.ExceptWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_Items).GetEnumerator();
		}

		void ISet<TValue>.IntersectWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
		}

		void ISet<TValue>.SymmetricExceptWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
		}

		void ISet<TValue>.UnionWith(IEnumerable<TValue> other)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Raises the PropertyChanged event
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (_UpdateCount == 0 && PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		//****************************************

		private void OnCollectionChanged()
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue changedItem)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem));
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<TValue> changedItems)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItems.ToArray()));
		}

		//****************************************

		/// <summary>
		/// Raised when the collection changes
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Raised when a property of the dictionary changes
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets the IEqualityComparer{TValue} that is used to compare items in the set
		/// </summary>
		public IEqualityComparer<TValue> Comparer
		{
			get { return _Items.Comparer; }
		}

		/// <summary>
		/// Gets the number of items in the collection
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
			get { return false; }
		}

		/// <summary>
		/// Gets whether an update is in progress via <see cref="BeginUpdate" /> and <see cref="EndUpdate" />
		/// </summary>
		public bool IsUpdating
		{
			get { return _UpdateCount != 0; }
		}

		/// <summary>
		/// Gets the underlying set object
		/// </summary>
		protected ISet<TValue> Items
		{
			get { return _Items; }
		}
	}
}
