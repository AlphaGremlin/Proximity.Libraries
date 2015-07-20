/****************************************\
 ImmutableInterlockedEx.cs
 Created: 2014-05-01
\****************************************/
#if !NET40
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides extra methods for manipulating immutable collections in a thread-safe manner
	/// </summary>
	public static class ImmutableInterlockedEx
	{
		/// <summary>
		/// Performs an atomic add into an Immutable Hash Set
		/// </summary>
		/// <param name="hashSet">The location of the hash set to add to</param>
		/// <param name="item">The item to add</param>
		/// <returns>True if the item was added, or False if it was already in the set</returns>
		public static bool Add<TItem>(ref ImmutableHashSet<TItem> hashSet, TItem item)
		{	//****************************************
			ImmutableHashSet<TItem> OldSet, NewSet;
			//****************************************
			
			do
			{
				OldSet = hashSet;
				NewSet = OldSet.Add(item);
				
				if (object.ReferenceEquals(OldSet, NewSet))
					return false;
				
			} while (Interlocked.CompareExchange(ref hashSet, NewSet, OldSet) != OldSet);
			
			return true;
		}
		
		/// <summary>
		/// Performs an atomic pop from an Immutable Hash Set
		/// </summary>
		/// <param name="hashSet">The location of the hash set to remove from</param>
		/// <param name="item">The item to remove</param>
		/// <returns>True if an item was removed, or False if the item was not found</returns>
		public static bool Remove<TItem>(ref ImmutableHashSet<TItem> hashSet, TItem item)
		{	//****************************************
			ImmutableHashSet<TItem> OldSet, NewSet;
			//****************************************
			
			do
			{
				OldSet = hashSet;
				NewSet = OldSet.Remove(item);
				
				if (object.ReferenceEquals(OldSet, NewSet))
					return false;
				
			} while (Interlocked.CompareExchange(ref hashSet, NewSet, OldSet) != OldSet);
			
			return true;
		}
	}
}
#endif