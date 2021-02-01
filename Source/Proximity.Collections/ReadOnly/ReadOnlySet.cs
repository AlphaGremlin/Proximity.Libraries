using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only wrapper around a Set
	/// </summary>
	public class ReadOnlySet<T> : ISet<T>, IReadOnlyCollection<T>, ICollection
#if !NETSTANDARD
		, IReadOnlySet<T>
#endif
	{	//****************************************
		private readonly ISet<T> _Set;
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a Set
		/// </summary>
		/// <param name="set">The set to wrap as read-only</param>
		public ReadOnlySet(ISet<T> @set) => _Set = @set;

		//****************************************

		/// <summary>
		/// Determines whether the set contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(T item) => _Set.Contains(item);

		/// <summary>
		/// Copies the elements of the set to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(T[] array, int arrayIndex) => _Set.CopyTo(array, arrayIndex);

		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<T> other) => _Set.IsSubsetOf(other);

		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<T> other) => _Set.IsSupersetOf(other);

		/// <summary>
		/// Checks whether this set is a strict superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict superset of the collection, otherwise False</returns>
		public bool IsProperSupersetOf(IEnumerable<T> other) => _Set.IsProperSupersetOf(other);

		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<T> other) => _Set.IsProperSubsetOf(other);

		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<T> other) => _Set.Overlaps(other);

		/// <summary>
		/// Checks whether this set and the given collection contain the same elements
		/// </summary>
		/// <param name="other">The collection to check against</param>
		/// <returns>True if they contain the same elements, otherwise FAlse</returns>
		public bool SetEquals(IEnumerable<T> other) => _Set.SetEquals(other);

		/// <summary>
		/// Returns an enumerator that iterates through the set
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the set</returns>
		public IEnumerator<T> GetEnumerator() => _Set.GetEnumerator();

		//****************************************

		bool ISet<T>.Add(T item) => throw new NotSupportedException("Set is read-only");

		void ICollection<T>.Add(T item) => throw new NotSupportedException("Set is read-only");

		void ICollection<T>.Clear() => throw new NotSupportedException("Set is read-only");

		void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

		void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException("Set is read-only");

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Set).GetEnumerator();

		void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException("Set is read-only");

		bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Set is read-only");

		void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException("Set is read-only");

		void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException("Set is read-only");

		//****************************************

		/// <summary>
		/// Gets the number of items in the set
		/// </summary>
		public int Count => _Set.Count;

		bool ICollection<T>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;
	}
}
