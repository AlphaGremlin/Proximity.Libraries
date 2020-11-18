using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only wrapper around a Collection
	/// </summary>
	/// <remarks>Unlike System.Collections.ObjectModel.ReadOnlyCollection, this only requires <see cref="ICollection{T}"/>, rather than <see cref="IList{T}"/></remarks>
	/// <typeparam name="T">The type of item in the collection</typeparam>
	public class ReadOnlyCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
	{	//****************************************
		private readonly ICollection<T> _Collection;
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a collection
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollection(ICollection<T> collection)
		{
			_Collection = collection;
		}

		//****************************************

		void ICollection<T>.Add(T item) => throw new NotSupportedException("Collection is read-only");

		void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only");

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(T item) => _Collection.Contains(item);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(T[] array, int arrayIndex) => _Collection.CopyTo(array, arrayIndex);

		bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only");

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<T> GetEnumerator() => _Collection.GetEnumerator();

		//****************************************

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Collection).GetEnumerator();

		void ICollection.CopyTo(Array array, int index) => _Collection.CopyTo((T[])array, index);

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => _Collection.Count;

		bool ICollection<T>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;
	}
}
