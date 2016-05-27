/****************************************\
 ObservableDictionaryCollection.cs
 Created: 2015-02-26
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides an observable view of the dictionary's keys and values
	/// </summary>
	public abstract class ObservableDictionaryCollection<TSource> : IList<TSource>, ICollection<TSource>, IList, INotifyCollectionChanged, INotifyPropertyChanged
#if !NET40
, IReadOnlyCollection<TSource>
#endif
	{	//****************************************
		private const string CountString = "Count";
		//****************************************

		internal ObservableDictionaryCollection()
		{
		}

		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public abstract bool Contains(TSource item);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public abstract void CopyTo(TSource[] array, int arrayIndex);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TSource> GetEnumerator()
		{
			return InternalGetEnumerator();
		}

		/// <summary>
		/// Searches for the specified value and returns the zero-based index of the first occurrence within the ObservableDictionaryCollection
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>True if a matching item was found, otherwise -1</returns>
		public abstract int IndexOf(TSource item);

		//****************************************

		internal void OnCollectionChanged()
		{
			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, TSource changedItem)
		{
			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, TSource changedItem, int index)
		{
			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, TSource newItem, TSource oldItem, int index)
		{
			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<TSource> newItems)
		{
			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItems.ToArray()));
		}

		//****************************************

		internal abstract void InternalCopyTo(Array array, int arrayIndex);

		internal abstract IEnumerator<TSource> InternalGetEnumerator();

		//****************************************

		void ICollection<TSource>.Add(TSource item)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		int IList.Add(object value)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void ICollection<TSource>.Clear()
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void IList.Clear()
		{
			throw new NotSupportedException("Collection is read-only");
		}

		bool IList.Contains(object value)
		{
			return value is TSource && Contains((TSource)value);
		}

		void ICollection.CopyTo(Array array, int arrayIndex)
		{
			InternalCopyTo(array, arrayIndex);
		}

		int IList.IndexOf(object value)
		{
			if (value is TSource)
				return IndexOf((TSource)value);

			return -1;
		}

		void IList.Insert(int index, object value)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void IList<TSource>.Insert(int index, TSource item)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		bool ICollection<TSource>.Remove(TSource item)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void IList.Remove(object value)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void IList.RemoveAt(int index)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void IList<TSource>.RemoveAt(int index)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return InternalGetEnumerator();
		}

		IEnumerator<TSource> IEnumerable<TSource>.GetEnumerator()
		{
			return InternalGetEnumerator();
		}

		//****************************************

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
		/// Gets the number of items in the collection
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Gets whether this collection is read-only. Always true
		/// </summary>
		public bool IsReadOnly
		{
			get { return true; }
		}

		/// <summary>
		/// Gets the item at the requested index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public abstract TSource this[int index] { get; }

		TSource IList<TSource>.this[int index]
		{
			get { return this[index]; }
			set { throw new NotSupportedException("List is read-only"); }
		}

		object IList.this[int index]
		{
			get { return this[index]; }
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
			get { return this; }
		}
	}
}
