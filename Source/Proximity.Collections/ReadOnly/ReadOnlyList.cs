using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only wrapper around a list
	/// </summary>
	public class ReadOnlyList<T> : IList<T>, IReadOnlyList<T>, IList
	{	//****************************************
		private readonly IList<T> _Source;
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a list
		/// </summary>
		/// <param name="source">The list to wrap</param>
		public ReadOnlyList(IList<T> source) => _Source = source;

		//****************************************

		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(T item) => _Source.IndexOf(item);

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(T item) => _Source.Contains(item);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(T[] array, int arrayIndex) => _Source.CopyTo(array, arrayIndex);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<T> GetEnumerator() => _Source.GetEnumerator();

		//****************************************

		void ICollection<T>.Add(T item) => throw new NotSupportedException("List is read-only");

		int IList.Add(object? value) => throw new NotSupportedException("List is read-only");

		void ICollection<T>.Clear() => throw new NotSupportedException("List is read-only");

		void IList.Clear() => throw new NotSupportedException("List is read-only");

		bool IList.Contains(object? value) => value is T Item && Contains(Item);

		void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

		IEnumerator IEnumerable.GetEnumerator() => _Source.GetEnumerator();

		int IList.IndexOf(object? value) => value is T Item ? IndexOf(Item) : -1;

		void IList.Insert(int index, object? value) => throw new NotSupportedException("List is read-only");

		void IList<T>.Insert(int index, T item) => throw new NotSupportedException("List is read-only");

		bool ICollection<T>.Remove(T item) => throw new NotSupportedException("List is read-only");

		void IList.Remove(object? value) => throw new NotSupportedException("List is read-only");

		void IList.RemoveAt(int index) => throw new NotSupportedException("List is read-only");

		void IList<T>.RemoveAt(int index) => throw new NotSupportedException("List is read-only");

		//****************************************

		/// <summary>
		/// Gets the item at the requested index
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public T this[int index] => _Source[index];

		/// <summary>
		/// Gets the number of items in this list
		/// </summary>
		public int Count => _Source.Count;

		object? IList.this[int index]
		{
			get => _Source[index]!;
			set => throw new NotSupportedException("List is read-only");
		}

		T IList<T>.this[int index]
		{
			get => _Source[index];
			set => throw new NotSupportedException("List is read-only");
		}

		bool ICollection<T>.IsReadOnly => true;

		bool IList.IsFixedSize => true;

		bool IList.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;
	}
}
