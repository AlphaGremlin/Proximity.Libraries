using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace System.Collections.Observable
{
	/// <summary>
	/// Provides an observable view of the dictionary's keys and values
	/// </summary>
	public abstract class ObservableDictionaryCollection<T> : IList<T>, ICollection<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyList<T>, IReadOnlyCollection<T>
	{
		internal ObservableDictionaryCollection()
		{
		}

		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public abstract bool Contains(T item);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public abstract void CopyTo(T[] array, int arrayIndex);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<T> GetEnumerator()
		{
			return InternalGetEnumerator();
		}

		/// <summary>
		/// Searches for the specified value and returns the zero-based index of the first occurrence within the ObservableDictionaryCollection
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>True if a matching item was found, otherwise -1</returns>
		public abstract int IndexOf(T item);

		//****************************************

		internal void OnCollectionChanged()
		{
			OnPropertyChanged(nameof(Count));

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, T changedItem)
		{
			OnPropertyChanged(nameof(Count));

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, T changedItem, int index)
		{
			OnPropertyChanged(nameof(Count));

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, T changedItem, int newIndex, int oldIndex)
		{
			OnPropertyChanged(nameof(Count));

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem, newIndex, oldIndex));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, T newItem, T oldItem, int index)
		{
			OnPropertyChanged(nameof(Count));

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		internal void OnCollectionChanged(NotifyCollectionChangedAction action, IEnumerable<T> newItems)
		{
			OnPropertyChanged(nameof(Count));

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItems.ToArray()));
		}

		//****************************************

		internal abstract void InternalCopyTo(Array array, int arrayIndex);

		internal abstract IEnumerator<T> InternalGetEnumerator();

		//****************************************

		void ICollection<T>.Add(T item) => throw new NotSupportedException("Collection is read-only");

		int IList.Add(object? value) => throw new NotSupportedException("Collection is read-only");

		void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only");

		void IList.Clear() => throw new NotSupportedException("Collection is read-only");

		bool IList.Contains(object? value) => value is T Value && Contains(Value);

		void ICollection.CopyTo(Array array, int arrayIndex) => InternalCopyTo(array, arrayIndex);

		int IList.IndexOf(object? value)
		{
			if (value is T Value)
				return IndexOf(Value);

			return -1;
		}

		void IList.Insert(int index, object? value) => throw new NotSupportedException("Collection is read-only");

		void IList<T>.Insert(int index, T item) => throw new NotSupportedException("Collection is read-only");

		bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only");

		void IList.Remove(object? value) => throw new NotSupportedException("Collection is read-only");

		void IList.RemoveAt(int index) => throw new NotSupportedException("Collection is read-only");

		void IList<T>.RemoveAt(int index) => throw new NotSupportedException("Collection is read-only");

		IEnumerator IEnumerable.GetEnumerator() => InternalGetEnumerator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => InternalGetEnumerator();

		//****************************************

		private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		//****************************************

		/// <summary>
		/// Raised when the collection changes
		/// </summary>
		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		/// <summary>
		/// Raised when a property of the collection changes
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Gets whether this collection is read-only. Always true
		/// </summary>
		public bool IsReadOnly => true;

		/// <summary>
		/// Gets the item at the requested index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public abstract T this[int index] { get; }

		T IList<T>.this[int index]
		{
			get => this[index];
			set => throw new NotSupportedException("List is read-only");
		}

		object? IList.this[int index]
		{
			get => this[index]!;
			set => throw new NotSupportedException("List is read-only");
		}

		bool IList.IsFixedSize => false;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;
	}
}
