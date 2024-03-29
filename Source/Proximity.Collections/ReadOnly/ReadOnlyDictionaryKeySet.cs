using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only <see cref="ISet{TKey}"/> wrapper around the given Dictionary's keys
	/// </summary>
	public static class ReadOnlyDictionaryKeySet
	{
		/// <summary>
		/// Creates a new read-only wrapper around a Dictionary Keys using the default comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only set around</param>
		/// <returns>A read-only Set around the given Dictionary Keys</returns>
		public static ReadOnlyDictionaryKeySet<TKey, TValue> From<TKey, TValue>(IDictionary<TKey, TValue> dictionary) where TKey : notnull => new(dictionary);

		/// <summary>
		/// Creates a new read-only wrapper around a Dictionary Keys
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only set around</param>
		/// <param name="comparer">The comparer to use when performing set comparisons</param>
		/// <returns>A read-only Set around the given Dictionary Keys</returns>
		public static ReadOnlyDictionaryKeySet<TKey, TValue> From<TKey, TValue>(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) where TKey : notnull => new(dictionary, comparer);

		/// <summary>
		/// Creates a new read-only wrapper around a Dictionary Keys using the default comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only set around</param>
		/// <returns>A read-only Set around the given Dictionary Keys</returns>
		public static ReadOnlyDictionaryKeySet<TKey, TValue> From<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull => new(dictionary);
	}

	/// <summary>
	/// Represents a read-only <see cref="ISet{TKey}"/> wrapper around the given Dictionary's keys
	/// </summary>
	public class ReadOnlyDictionaryKeySet<TKey, TValue> : ISet<TKey>, IReadOnlyCollection<TKey>, ICollection
#if !NETSTANDARD
		, IReadOnlySet<TKey>
#endif
		where TKey : notnull
	{ //****************************************
		private readonly IDictionary<TKey, TValue> _Set;
		private readonly IEqualityComparer<TKey> _Comparer;
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a Dictionary Keys using the default comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only set around</param>
		public ReadOnlyDictionaryKeySet(IDictionary<TKey, TValue> dictionary)
		{
			_Set = dictionary;
			_Comparer = EqualityComparer<TKey>.Default;
		}

		/// <summary>
		/// Creates a new read-only wrapper around a Dictionary Keys
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only set around</param>
		/// <param name="comparer">The comparer to use when performing set comparisons</param>
		public ReadOnlyDictionaryKeySet(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
		{
			_Set = dictionary;
			_Comparer = comparer;
		}

		/// <summary>
		/// Creates a new read-only wrapper around a Dictionary Keys
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only set around</param>
		public ReadOnlyDictionaryKeySet(Dictionary<TKey, TValue> dictionary)
		{
			_Set = dictionary;
			_Comparer = dictionary.Comparer;
		}

		//****************************************

		/// <summary>
		/// Determines whether the set contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TKey item) => _Set.ContainsKey(item);

		/// <summary>
		/// Copies the elements of the set to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TKey[] array, int arrayIndex) => _Set.Keys.CopyTo(array, arrayIndex);

		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<TKey> other)
		{
			if (_Set.Count == 0)
				return true;

			// If there are less items in the other collection than in us, we cannot be a subset
			if (other is ICollection<TKey> OtherCollection)
			{
				if (OtherCollection.Count < _Set.Count)
					return false;
			}
			else if (other is IReadOnlyCollection<TKey> OtherReadOnlyCollection)
			{
				if (OtherReadOnlyCollection.Count < _Set.Count)
					return false;
			}

			var UniqueItems = new HashSet<TKey>(_Comparer);

			foreach (var MyItem in other)
			{
				// Is this item in the list?
				if (_Set.ContainsKey(MyItem))
					UniqueItems.Add(MyItem); // Yes, so it matches
			}

			// Our dictionary cannot have duplicates, so ensure our number of unique items matches
			return _Set.Count == UniqueItems.Count;
		}
		
		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<TKey> other)
		{
			// If we're empty, we cannot be a superset if the other collection has items
			if (_Set.Count == 0)
				return !other.Any();

			// There could be more items in the enumeration, but they might also be duplicates, so we can't rely on comparing sizes

			// Check that we have every item in the enumeration
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
			// If we're empty, we cannot be a proper superset even if the other collection is also empty
			if (_Set.Count == 0)
				return false;

			// If the other collection is empty, we're not, so we're automatically a superset
			if (other is ICollection<TKey> OtherCollection)
			{
				// If the other collection is empty, we're not, so we're automatically a superset
				if (OtherCollection.Count == 0)
					return true;

				// There could be more items in the enumeration, but they might also be duplicates, so we can't rely on comparing sizes
			}
			else if (other is IReadOnlyCollection<TKey> OtherReadOnlyCollection)
			{
				if (OtherReadOnlyCollection.Count == 0)
					return true;
			}
			else if (!other.Any())
			{
				return true;
			}

			var UniqueItems = new HashSet<TKey>(_Comparer);

			// Check that we have every item in the enumeration
			foreach (var MyItem in other)
			{
				if (_Set.ContainsKey(MyItem))
					UniqueItems.Add(MyItem); // Yes, so add it to our known matches
				else
					return false; // Doesn't match, so we're not a superset
			}

			// Ensure we have at least one more item than in other
			return _Set.Count > UniqueItems.Count;
		}
		
		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<TKey> other)
		{
			if (_Set.Count == 0)
				return other.Any(); // For a proper subset, there has to be at least one item in Other that isn't in us

			// If there are less items in the other collection than in us, we cannot be a subset
			if (other is ICollection<TKey> OtherCollection)
			{
				if (OtherCollection.Count < _Set.Count)
					return false;
			}
			else if (other is IReadOnlyCollection<TKey> OtherReadOnlyCollection)
			{
				if (OtherReadOnlyCollection.Count < _Set.Count)
					return false;
			}
			
			var UniqueItems = new HashSet<TKey>(_Comparer);
			var FoundExtra = false;

			foreach (var MyItem in other)
			{
				// Is this item in the list?
				if (_Set.ContainsKey(MyItem))
					UniqueItems.Add(MyItem); // Yes, so it matches
				else
					FoundExtra = true;
			}

			// Our dictionary cannot have duplicates, so ensure our number of unique items matches
			return FoundExtra && _Set.Count == UniqueItems.Count;
		}
		
		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<TKey> other)
		{
			// If there's no items, we can't overlap
			if (_Set.Count == 0)
				return false;

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
		/// <returns>True if they contain the same elements, otherwise False</returns>
		public bool SetEquals(IEnumerable<TKey> other)
		{
			// If we've no items, ensure the other collection is also empty
			if (_Set.Count == 0)
				return other.Any();

			var UniqueItems = new HashSet<TKey>(_Comparer);

			foreach (var MyItem in other)
			{
				// Is this item in the list?
				if (!_Set.ContainsKey(MyItem))
					return false; // No, so fail

				// Since our source enumeration may contain duplicates, build a list of unique items
				UniqueItems.Add(MyItem);
			}

			// Our dictionary cannot have duplicates, so ensure our number of unique items matches
			return _Set.Count == UniqueItems.Count;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the set
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the set</returns>
		public IEnumerator<TKey> GetEnumerator() => _Set.Keys.GetEnumerator();

		//****************************************

		bool ISet<TKey>.Add(TKey item) => throw new NotSupportedException("Set is read-only");

		void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException("Set is read-only");

		void ICollection<TKey>.Clear() => throw new NotSupportedException("Set is read-only");

		void ICollection.CopyTo(Array array, int index) => CopyTo((TKey[])array, index);

		void ISet<TKey>.ExceptWith(IEnumerable<TKey> other) => throw new NotSupportedException("Set is read-only");

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Set).GetEnumerator();

		void ISet<TKey>.IntersectWith(IEnumerable<TKey> other) => throw new NotSupportedException("Set is read-only");

		bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException("Set is read-only");

		void ISet<TKey>.SymmetricExceptWith(IEnumerable<TKey> other) => throw new NotSupportedException("Set is read-only");

		void ISet<TKey>.UnionWith(IEnumerable<TKey> other) => throw new NotSupportedException("Set is read-only");

		//****************************************

		/// <summary>
		/// Gets the number of items in the set
		/// </summary>
		public int Count => _Set.Count;

		/// <summary>
		/// Gets whether this set is read-only. Always true
		/// </summary>
		bool ICollection<TKey>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;
	}
}
