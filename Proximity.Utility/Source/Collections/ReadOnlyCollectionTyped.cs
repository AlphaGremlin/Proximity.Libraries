﻿/****************************************\
 ReadOnlyCollectionTyped.cs
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
	/// Represents a read-only wrapper around a Collection that also converts to a base type
	/// </summary>
	public class ReadOnlyCollectionTyped<TSource, TTarget> : ICollection<TTarget>, IReadOnlyCollection<TTarget> where TSource : class, TTarget where TTarget : class
	{	//****************************************
		private readonly IReadOnlyCollection<TSource> _Collection;
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a collection that converts to a base type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollectionTyped(ICollection<TSource> collection)
		{
			_Collection = new ReadOnlyCollection<TSource>(collection);
		}
		
		/// <summary>
		/// Creates a new read-only wrapper around a collection that converts to a base type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollectionTyped(IReadOnlyCollection<TSource> collection)
		{
			_Collection = collection;
		}
		
		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TTarget item)
		{
			if (!(item is TSource))
				return false;
			
			if (_Collection is ICollection<TSource>)
				return ((ICollection<TSource>)_Collection).Contains((TSource)item);

			return System.Linq.Enumerable.Contains(_Collection, (TSource)item);
		}

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TTarget[] array, int arrayIndex)
		{
			if (_Collection is ICollection<TSource>)
			{
				((ICollection<TSource>)_Collection).CopyTo((TSource[])array, arrayIndex);

				return;
			}

			int Index = arrayIndex;

			foreach (var MyItem in _Collection)
			{
				array[Index++] = (TTarget)MyItem;
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TTarget> GetEnumerator()
		{
			return new TypedEnumerator<TSource, TTarget>(_Collection.GetEnumerator());
		}
		
		//****************************************
		
		void ICollection<TTarget>.Add(TTarget item)
		{
			throw new NotSupportedException("Collection is read-only");
		}
		
		void ICollection<TTarget>.Clear()
		{
			throw new NotSupportedException("Collection is read-only");
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_Collection).GetEnumerator();
		}
		
		bool ICollection<TTarget>.Remove(TTarget item)
		{
			throw new NotSupportedException("Collection is read-only");
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Collection.Count; }
		}
		
		bool ICollection<TTarget>.IsReadOnly
		{
			get { return true; }
		}
	}
}
