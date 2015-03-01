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
	public sealed class ObservableDictionaryCollection<TSource> : ICollection<TSource>, INotifyCollectionChanged, INotifyPropertyChanged
	{	//****************************************
		private const string CountString = "Count";
		//****************************************
		private readonly ICollection<TSource> _Source;
		//****************************************

		internal ObservableDictionaryCollection(ICollection<TSource> source)
		{
			_Source = source;
		}

		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TSource item)
		{
			return _Source.Contains(item);
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TSource[] array, int arrayIndex)
		{
			_Source.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TSource> GetEnumerator()
		{
			return _Source.GetEnumerator();
		}

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

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, ICollection<TSource> newItems)
		{
			OnPropertyChanged(CountString);

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItems.ToArray()));
		}

		//****************************************

		void ICollection<TSource>.Add(TSource item)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		void ICollection<TSource>.Clear()
		{
			throw new NotSupportedException("Collection is read-only");
		}

		bool ICollection<TSource>.Remove(TSource item)
		{
			throw new NotSupportedException("Collection is read-only");
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_Source).GetEnumerator();
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
		public int Count
		{
			get { return _Source.Count; }
		}

		/// <summary>
		/// Gets whether this collection is read-only. Always true
		/// </summary>
		public bool IsReadOnly
		{
			get { return true; }
		}
	}
}
