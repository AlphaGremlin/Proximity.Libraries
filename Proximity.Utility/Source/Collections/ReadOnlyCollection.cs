/****************************************\
 ReadOnlyCollection.cs
 Created: 2011-04-03
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a Collection
	/// </summary>
	/// <remarks>Unlike System.Collections.ObjectModel.ReadOnlyCollection, this requires just a Collection, and not a List</remarks>
	public class ReadOnlyCollection<TItem> : ICollection<TItem>, IReadOnlyCollection<TItem>
	{	//****************************************
		private readonly ICollection<TItem> _Collection;
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a collection
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollection(ICollection<TItem> collection)
		{
			_Collection = collection;
		}
		
		//****************************************
		
		void ICollection<TItem>.Add(TItem item)
		{
			throw new NotSupportedException("Collection is read-only");
		}
		
		void ICollection<TItem>.Clear()
		{
			throw new NotSupportedException("Collection is read-only");
		}
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TItem item)
		{
			return _Collection.Contains(item);
		}
		
		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			_Collection.CopyTo(array, arrayIndex);
		}
		
		bool ICollection<TItem>.Remove(TItem item)
		{
			throw new NotSupportedException("Collection is read-only");
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			return _Collection.GetEnumerator();
		}
		
		//****************************************
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_Collection).GetEnumerator();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Collection.Count; }
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
