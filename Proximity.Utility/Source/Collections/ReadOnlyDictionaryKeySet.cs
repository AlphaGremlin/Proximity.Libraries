/****************************************\
 ReadOnlySet.cs
 Created: 2014-10-29
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only Set wrapper around a Dictionary's Keys
	/// </summary>
	public class ReadOnlyDictionaryKeySet<TKey, TValue> : ISet<TKey>, IReadOnlyCollection<TKey>
	{	//****************************************
		private readonly IDictionary<TKey, TValue> _Set;
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a Dictionary Keys
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only set around</param>
		public ReadOnlyDictionaryKeySet(IDictionary<TKey, TValue> dictionary)
		{
			_Set = dictionary;
		}
		
		//****************************************
		
		/// <summary>
		/// Determines whether the set contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TKey item)
		{
			return _Set.ContainsKey(item);
		}
		
		/// <summary>
		/// Copies the elements of the set to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TKey[] array, int arrayIndex)
		{
			_Set.Keys.CopyTo(array, arrayIndex);
		}
		
		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<TKey> other)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<TKey> other)
		{
			foreach (var MyItem in other)
			{
				if (!_Set.ContainsKey(MyItem))
					return false;
			}
			
			return true;
		}
		
		/// <summary>
		/// Checks whether this set is a strict superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict superset of the collection, otherwise False</returns>
		public bool IsProperSupersetOf(IEnumerable<TKey> other)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<TKey> other)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<TKey> other)
		{
			foreach (var MyItem in other)
			{
				if (_Set.ContainsKey(MyItem))
					return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Checks whether this set and the given collection contain the same elements
		/// </summary>
		/// <param name="other">The collection to check against</param>
		/// <returns>True if they contain the same elements, otherwise FAlse</returns>
		public bool SetEquals(IEnumerable<TKey> other)
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the set
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the set</returns>
		public IEnumerator<TKey> GetEnumerator()
		{
			return _Set.Keys.GetEnumerator();
		}
		
		//****************************************
		
		bool ISet<TKey>.Add(TKey item)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ICollection<TKey>.Add(TKey item)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ICollection<TKey>.Clear()
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ISet<TKey>.ExceptWith(IEnumerable<TKey> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ISet<TKey>.IntersectWith(IEnumerable<TKey> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		bool ICollection<TKey>.Remove(TKey item)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ISet<TKey>.SymmetricExceptWith(IEnumerable<TKey> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
		void ISet<TKey>.UnionWith(IEnumerable<TKey> other)
		{
			throw new NotSupportedException("Set is read-only");
		}
		
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
