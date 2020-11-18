using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace System.Collections.Observable
{
	/// <summary>
	/// Provides a base class for Observable Sets
	/// </summary>
	/// <typeparam name="T">The type of value within the set</typeparam>
	public abstract class ObservableBaseSet<T> : ObservableBase<T>
	{
		/// <inheritdoc />
		public override void Add(T item) => InternalAdd(item);

		/// <summary>
		/// Removes all items that are in the given collection from the current set
		/// </summary>
		/// <param name="other">The collection of items to remove</param>
		public void ExceptWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			var BitMask = new BitArray(Count);

			// Mark each item we want to remove
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				// If we find it in the set, mark it for removal
				if (Index != -1)
					BitMask.Set(Index, true);
			}

			// Remove everything we marked
			InternalRemoveAll(BitMask.Get);
		}

		/// <summary>
		/// Modifies the current set so it only contains items present in the given collection
		/// </summary>
		/// <param name="other">The collection to compare against</param>
		public void IntersectWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			var BitMask = new BitArray(Count, true);

			// Mark each item we want to keep
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				// If we find it in the set, mark it for keeping
				if (Index != -1)
					BitMask.Set(Index, false);
			}

			// Remove everything we didn't clear
			InternalRemoveAll(BitMask.Get);
		}

		/// <summary>
		/// Checks whether this set is a strict subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict subset of the collection, otherwise False</returns>
		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			if (Count == 0)
				return other.Any(); // For a proper subset, there has to be at least one item in Other that isn't in us

			// If there are less items in the other collection than in us, we cannot be a subset
			if (other is ICollection<T> && ((ICollection<T>)other).Count < Count)
				return false;

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;
			var FoundExtra = false;

			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index == -1)
					FoundExtra = true; // We found an item that isn't in our set
				else if (!BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// The set cannot have duplicates, so ensure our number of unique items matches
			return FoundExtra && Count == UniqueCount;
		}

		/// <summary>
		/// Checks whether this set is a strict superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a strict superset of the collection, otherwise False</returns>
		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			// If we're empty, we cannot be a proper superset
			if (Count == 0)
				return false;

			// If the other collection is empty, we're not, so we're automatically a superset
			if (other is ICollection<T> OtherCollection)
			{
				if (OtherCollection.Count == 0)
					return true;

				// There could be more items in the enumeration, but they might also be duplicates, so we can't rely on comparing sizes
			}
			else if (!other.Any())
			{
				return true;
			}

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;

			// Check that we have every item in the enumeration
			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index == -1)
					return false; // Doesn't match, so we're not a superset
				else if (!BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// Ensure we have at least one more item than in other
			return Count > UniqueCount;
		}

		/// <summary>
		/// Checks whether this set is a subset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a subset of the collection, otherwise False</returns>
		public bool IsSubsetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			if (Count == 0)
				return true;

			// If there are less items in the other collection than in us, we cannot be a subset
			if (other is ICollection<T> OtherCollection && OtherCollection.Count < Count)
				return false;

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;

			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index != -1 && !BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// Ensure every item was accounted for
			return Count == UniqueCount;
		}

		/// <summary>
		/// Checks whether this set is a superset of the collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if this set is a superset of the collection, otherwise False</returns>
		public bool IsSupersetOf(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			// If the other collection is empty, we're automatically a superset
			if (!other.Any())
				return true;

			// If we're empty, we cannot be a superset, since the other collection has items
			if (Count == 0)
				return false;

			// There could be more items in the enumeration, but they might also be duplicates, so we can't rely on comparing sizes

			// Check that we have every item in the enumeration
			foreach (var MyItem in other)
			{
				if (!Contains(MyItem))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Checks whether the current set overlaps with the specified collection
		/// </summary>
		/// <param name="other">The collection to check</param>
		/// <returns>True if at least one element is common between this set and the collection, otherwise False</returns>
		public bool Overlaps(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			// If there's no items, we can't overlap
			if (Count == 0)
				return false;

			foreach (var MyItem in other)
			{
				if (Contains(MyItem))
					return true;
			}

			return false;
		}

		/// <inheritdoc />
		public override int RemoveAll(Predicate<T> predicate)
		{
			return InternalRemoveAll((index) => predicate(InternalGet(index)));
		}

		/// <summary>
		/// Checks whether this set and the given collection contain the same elements
		/// </summary>
		/// <param name="other">The collection to check against</param>
		/// <returns>True if they contain the same elements, otherwise FAlse</returns>
		public bool SetEquals(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			// If we've no items, ensure the other collection is also empty
			if (Count == 0)
				return other.Any();

			var BitMask = new BitArray(Count, false);
			var UniqueCount = 0;

			foreach (var MyItem in other)
			{
				var Index = IndexOf(MyItem);

				if (Index == -1)
					return false; // Doesn't match, so we're not equal
				else if (!BitMask.Get(Index))
				{
					// We found an item that is in our set, that we haven't marked yet
					BitMask.Set(Index, true);
					UniqueCount++;
				}
			}

			// Ensure our number of unique items matches
			return Count == UniqueCount;
		}

		/// <summary>
		/// Modifies the set so it only contains items that are present in the set, or in the given collection, but not both
		/// </summary>
		/// <param name="other">The collection to compare against</param>
		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			if (other == null)
				throw new ArgumentException("other");

			try
			{
				BeginUpdate();

				var BitMask = new BitArray(Count);
				var NewItems = new List<T>();

				foreach (var MyItem in other)
				{
					var Index = IndexOf(MyItem);

					if (Index == -1)
						NewItems.Add(MyItem); // Item does not exist in our set, so add it later
					else
						BitMask.Set(Index, true); // Item is in our set, so mark it for removal
				}

				// Remove everything we marked
				InternalRemoveAll(BitMask.Get);

				// Add the items we didn't find. Duplicates will be ignored
				AddRange(NewItems);
			}
			finally
			{
				EndUpdate();
			}
		}

		/// <summary>
		/// Ensures the current set contains all the items in the given collection
		/// </summary>
		/// <param name="other">The items to add to the set</param>
		public void UnionWith(IEnumerable<T> other)
		{
			AddRange(other);
		}

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		protected abstract void InternalAdd(T item);

		/// <summary>
		/// Removes all items that match the given predicate
		/// </summary>
		/// <param name="predicate">A predicate that returns True for items to remove</param>
		/// <returns>The number of items removed</returns>
		protected abstract int InternalRemoveAll(Func<int, bool> predicate);
	}
}
