/****************************************\
 ReadOnlyListTyped.cs
 Created: 2011-08-08
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a list that also converts to a base type
	/// </summary>
	public class ReadOnlyListTyped<TSource, TTarget> : IList<TTarget>
#if !NET40
		, IReadOnlyList<TTarget>
#endif
		where TSource : class, TTarget where TTarget : class
	{	//****************************************
		private readonly IList<TSource> _Source;
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a list
		/// </summary>
		/// <param name="source">The list to wrap</param>
		public ReadOnlyListTyped(IList<TSource> source)
		{
			_Source = source;
		}
		
		//****************************************
		
		/// <summary>
		/// Determines the index of a specific item in the list
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>The index of the item if found, otherwise -1</returns>
		public int IndexOf(TTarget item)
		{
			if (!(item is TSource))
				return -1;
			
			return _Source.IndexOf((TSource)item);
		}
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TTarget item)
		{
			if (!(item is TSource))
				return false;
			
			return _Source.Contains((TSource)item);
		}
		
		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TTarget[] array, int arrayIndex)
		{
			// Array Contravariance
			_Source.CopyTo((TSource[])array, arrayIndex);
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TTarget> GetEnumerator()
		{
			return new TypedEnumerator<TSource, TTarget>(_Source.GetEnumerator());
		}
		
		//****************************************
		
		void IList<TTarget>.Insert(int index, TTarget item)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		void IList<TTarget>.RemoveAt(int index)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		void ICollection<TTarget>.Add(TTarget item)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		void ICollection<TTarget>.Clear()
		{
			throw new NotSupportedException("List is read-only");
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _Source.GetEnumerator();
		}
		
		bool ICollection<TTarget>.Remove(TTarget item)
		{
			throw new NotSupportedException("List is read-only");
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the item at the requested index
		/// </summary>
		public TTarget this[int index]
		{
			get { return _Source[index]; }
		}
		
		/// <summary>
		/// Gets the number of items in the list
		/// </summary>
		public int Count
		{
			get { return _Source.Count; }
		}
		
		TTarget IList<TTarget>.this[int index]
		{
			get { return _Source[index]; }
			set { throw new NotSupportedException("List is read-only"); }
		}
		
		bool ICollection<TTarget>.IsReadOnly
		{
			get { return true; }
		}
	}
}
