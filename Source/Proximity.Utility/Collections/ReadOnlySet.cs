﻿using System;
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
		public ReadOnlySet(ISet<TItem> @set) => _Set = @set;

		//****************************************

		/// <summary>
		/// Determines whether the set contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TItem item) => _Set.Contains(item);

		/// <summary>
		/// Copies the elements of the set to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TItem[] array, int arrayIndex) => _Set.CopyTo(array, arrayIndex);

		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<TItem> other) => _Set.IsSubsetOf(other);

		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<TItem> other) => _Set.IsSupersetOf(other);

		/// <summary>
		/// Checks whether this set is a strict superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict superset of the collection, otherwise False</returns>
		public bool IsProperSupersetOf(IEnumerable<TItem> other) => _Set.IsProperSupersetOf(other);

		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<TItem> other) => _Set.IsProperSubsetOf(other);

		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<TItem> other) => _Set.Overlaps(other);

		/// <summary>
		/// Checks whether this set and the given collection contain the same elements
		/// </summary>
		/// <param name="other">The collection to check against</param>
		/// <returns>True if they contain the same elements, otherwise FAlse</returns>
		public bool SetEquals(IEnumerable<TItem> other) => _Set.SetEquals(other);

		/// <summary>
		/// Returns an enumerator that iterates through the set
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the set</returns>
		public IEnumerator<TItem> GetEnumerator() => _Set.GetEnumerator();

		//****************************************

		bool ISet<TItem>.Add(TItem item) => throw new NotSupportedException("Set is read-only");

		void ICollection<TItem>.Add(TItem item) => throw new NotSupportedException("Set is read-only");

		void ICollection<TItem>.Clear() => throw new NotSupportedException("Set is read-only");

		void ISet<TItem>.ExceptWith(IEnumerable<TItem> other) => throw new NotSupportedException("Set is read-only");

		void ISet<TItem>.IntersectWith(IEnumerable<TItem> other) => throw new NotSupportedException("Set is read-only");

		bool ICollection<TItem>.Remove(TItem item) => throw new NotSupportedException("Set is read-only");

		void ISet<TItem>.SymmetricExceptWith(IEnumerable<TItem> other) => throw new NotSupportedException("Set is read-only");

		void ISet<TItem>.UnionWith(IEnumerable<TItem> other) => throw new NotSupportedException("Set is read-only");

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Set).GetEnumerator();

		//****************************************

		/// <summary>
		/// Gets the number of items in the set
		/// </summary>
		public int Count => _Set.Count;

		/// <summary>
		/// Gets whether this set is read-only. Always true
		/// </summary>
		public bool IsReadOnly => true;
	}
}
