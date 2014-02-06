/****************************************\
 ReadOnlySet.cs
 Created: 2014-02-05
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a Set
	/// </summary>
	public class ReadOnlySet<TItem> : ISet<TItem>, IReadOnlyCollection<TItem>
	{	//****************************************
		private readonly ISet<TItem> _Set;
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a Set
		/// </summary>
		/// <param name="set">The set to wrap as read-only</param>
		public ReadOnlySet(ISet<TItem> @set)
		{
			_Set = @set;
		}
		
		//****************************************
		
		bool ISet<TItem>.Add(TItem item)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ICollection<TItem>.Add(TItem item)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ICollection<TItem>.Clear()
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		/// <summary>
		/// Determines whether the set contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TItem item)
		{
			return _Set.Contains(item);
		}
		
		/// <summary>
		/// Copies the elements of the set to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			_Set.CopyTo(array, arrayIndex);
		}
		
		void ISet<TItem>.ExceptWith(IEnumerable<TItem> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ISet<TItem>.IntersectWith(IEnumerable<TItem> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		public bool IsSubsetOf(IEnumerable<TItem> other)
		{
			return _Set.IsSubsetOf(other);
		}
		
		public bool IsSupersetOf(IEnumerable<TItem> other)
		{
			return _Set.IsSupersetOf(other);
		}
		
		public bool IsProperSupersetOf(IEnumerable<TItem> other)
		{
			return _Set.IsProperSupersetOf(other);
		}
		
		public bool IsProperSubsetOf(IEnumerable<TItem> other)
		{
			return _Set.IsProperSubsetOf(other);
		}
		
		public bool Overlaps(IEnumerable<TItem> other)
		{
			return _Set.Overlaps(other);
		}
		
		bool ICollection<TItem>.Remove(TItem item)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		public bool SetEquals(IEnumerable<TItem> other)
		{
			return _Set.SetEquals(other);
		}
		
		void ISet<TItem>.SymmetricExceptWith(IEnumerable<TItem> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ISet<TItem>.UnionWith(IEnumerable<TItem> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the set
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the set</returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			return _Set.GetEnumerator();
		}
		
		//****************************************
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_Set).GetEnumerator();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of items in the set
		/// </summary>
		public int Count
		{
			get { return _Set.Count; }
		}
		
		/// <summary>
		/// Gets whether this set is read-only. Always true
		/// </summary>
		public bool IsReadOnly
		{
			get { return true; }
		}
	}
}
