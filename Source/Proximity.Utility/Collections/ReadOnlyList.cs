/****************************************\
 ReadOnlyList.cs
 Created: 2011-08-17
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a list
	/// </summary>
	public class ReadOnlyList<TItem> : IList<TItem>
#if !NET40
		, IReadOnlyList<TItem>
#endif
	{	//****************************************
		private readonly IList<TItem> _Source;
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a list
		/// </summary>
		/// <param name="source">The list to wrap</param>
		public ReadOnlyList(IList<TItem> source)
		{
			_Source = source;
		}
		
		//****************************************
		
		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TItem item)
		{
			return _Source.IndexOf(item);
		}
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TItem item)
		{
			return _Source.Contains(item);
		}
		
		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			_Source.CopyTo(array, arrayIndex);
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			return _Source.GetEnumerator();
		}
		
		//****************************************
		
		void IList<TItem>.Insert(int index, TItem item)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		void IList<TItem>.RemoveAt(int index)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		void ICollection<TItem>.Add(TItem item)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		void ICollection<TItem>.Clear()
		{
			throw new NotSupportedException("List is read-only");
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _Source.GetEnumerator();
		}
		
		bool ICollection<TItem>.Remove(TItem item)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the item at the requested index
		/// </summary>
		public TItem this[int index]
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
		
		TItem IList<TItem>.this[int index]
		{
			get { return _Source[index]; }
			set { throw new NotSupportedException("List is read-only"); }
		}
		
		bool ICollection<TItem>.IsReadOnly
		{
			get { return true; }
		}
	}
}
