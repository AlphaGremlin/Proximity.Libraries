/****************************************\
 LinqExtensions.cs
 Created: 2014-11-25
\****************************************/
using System;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides useful Linq-style methods
	/// </summary>
	public static class LinqExtensions
	{
		/// <summary>
		/// Performs a full outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector)
		{
			return FullOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default(TLeft), default(TRight), null);
		}
		
		/// <summary>
		/// Performs a full outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <param name="keyComparer">A comparer to use to compare keys</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, IEqualityComparer<TKey> keyComparer)
		{
			return FullOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default(TLeft), default(TRight), keyComparer);
		}
		
		/// <summary>
		/// Performs a full outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <param name="defaultLeft">The default for the left item when a match is not found</param>
		/// <param name="defaultRight">The default for the right item when a match is not found</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TLeft defaultLeft, TRight defaultRight)
		{
			return FullOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, defaultLeft, defaultRight, null);
		}
		
		/// <summary>
		/// Performs a full outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <param name="defaultLeft">The default for the left item when a match is not found</param>
		/// <param name="defaultRight">The default for the right item when a match is not found</param>
		/// <param name="keyComparer">A comparer to use to compare keys</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TLeft defaultLeft, TRight defaultRight, IEqualityComparer<TKey> keyComparer)
		{	//****************************************
			JoinItem<TLeft> LeftValue;
			var LeftLookup = BuildLookup(left, leftKeySelect, keyComparer);
			//****************************************
			
			// Perform the lookups - O(m * n) at worst (in the case where both lists contain items with the same key, resulting in a cross-join).
			// This is unfortunately unavoidable. In the ideal, it will be O(m) when keys are not duplicated
			foreach (var RightItem in right)
			{
				JoinRoot<TLeft> LeftRoot;
				
				// Can we find a left item with the key of the right?
				if (LeftLookup.TryGetValue(rightKeySelect(RightItem), out LeftRoot))
				{
					// Yes, flag it as removed so we don't return it later
					LeftRoot.IsRemoved = true;
					LeftValue = LeftRoot.Root;
					
					do
					{
						yield return resultSelector(LeftValue.Item, RightItem);
					} while ((LeftValue = LeftValue.Next) != null);
				}
				else
				{
					// No, so return right without a left
					yield return resultSelector(defaultLeft, RightItem);
				}
			}
			
			// All matches processed, return the remining lefts without rights - O(n) at worst
			foreach (var RemainingValue in LeftLookup.Values)
			{
				// Skip if it's marked as removed
				if (RemainingValue.IsRemoved)
					continue;
				
				LeftValue = RemainingValue.Root;
				
				do
				{
					yield return resultSelector(LeftValue.Item, defaultRight);
				} while ((LeftValue = LeftValue.Next) != null);
			}
		}
		
		/// <summary>
		/// Performs a grouped full outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the matching items for a key into a single result</param>
		/// <param name="keyComparer">A comparer to use to compare keys</param>
		/// <returns>An enumeration returning the result of the grouped full outer join</returns>
		public static IEnumerable<TResult> FullOuterGroupJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<IEnumerable<TLeft>, IEnumerable<TRight>, TKey, TResult> resultSelector, IEqualityComparer<TKey> keyComparer)
		{	//****************************************
			var LeftLookup = BuildLookup(left, leftKeySelect, keyComparer);
			var RightLookup = BuildLookup(right, rightKeySelect, keyComparer);
			//****************************************
			
			// Perform the lookups - O(n)
			foreach (var LeftPair in LeftLookup)
			{
				JoinRoot<TRight> RightRoot;
				
				if (RightLookup.TryGetValue(LeftPair.Key, out RightRoot))
				{
					RightRoot.IsRemoved = true;
					
					yield return resultSelector(LeftPair.Value.Enumerate(), RightRoot.Enumerate(), LeftPair.Key);
				}
				else
				{
					yield return resultSelector(LeftPair.Value.Enumerate(), System.Linq.Enumerable.Empty<TRight>(), LeftPair.Key);
				}
			}
			
			// Return the remaining items - O(m)
			foreach (var RightPair in RightLookup)
			{
				// Skip if it's marked as removed
				if (RightPair.Value.IsRemoved)
					continue;
				
				yield return resultSelector(System.Linq.Enumerable.Empty<TLeft>(), RightPair.Value.Enumerate(), RightPair.Key);
			}
		}
		
		/// <summary>
		/// Performs a left outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector)
		{
			return LeftOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default(TRight), null);
		}
		
		/// <summary>
		/// Performs a left outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <param name="defaultRight">The default for the right item when a match is not found</param>
		/// <param name="keyComparer">A comparer to use to compare keys</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TRight defaultRight, IEqualityComparer<TKey> keyComparer)
		{	//****************************************
			var RightLookup = BuildLookup(right, rightKeySelect, keyComparer);
			//****************************************
			
			// Perform the lookups - O(m * n) at worst (in the case where both lists contain items with the same key, resulting in a cross-join).
			// This is unfortunately unavoidable. In the ideal, it will be O(m) when keys are not duplicated
			foreach (var LeftItem in left)
			{
				JoinRoot<TRight> RightRoot;

				// Can we find a right item with the key of the left?
				if (RightLookup.TryGetValue(leftKeySelect(LeftItem), out RightRoot))
				{
					// Yes, return the combination
					var RightValue = RightRoot.Root;
					
					do
					{
						yield return resultSelector(LeftItem, RightValue.Item);
					} while ((RightValue = RightValue.Next) != null);
				}
				else
				{
					// No, so return right without a left
					yield return resultSelector(LeftItem, defaultRight);
				}
			}
		}
		
		/// <summary>
		/// Performs a right outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> RightOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector)
		{
			return RightOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default(TLeft), null);
		}
		
			/// <summary>
		/// Performs a right outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <param name="defaultLeft">The default for the left item when a match is not found</param>
		/// <param name="keyComparer">A comparer to use to compare keys</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> RightOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TLeft defaultLeft, IEqualityComparer<TKey> keyComparer)
		{	//****************************************
			var LeftLookup = BuildLookup(left, leftKeySelect, keyComparer);
			//****************************************
			
			// Perform the lookups - O(m * n) at worst (in the case where both lists contain items with the same key, resulting in a cross-join).
			// This is unfortunately unavoidable. In the ideal, it will be O(m) when keys are not duplicated
			foreach (var RightItem in right)
			{
				JoinRoot<TLeft> LeftRoot;
				
				// Can we find a left item with the key of the right?
				if (LeftLookup.TryGetValue(rightKeySelect(RightItem), out LeftRoot))
				{
					// Yes, return the combination
					var LeftValue = LeftRoot.Root;
					
					do
					{
						yield return resultSelector(LeftValue.Item, RightItem);
					} while ((LeftValue = LeftValue.Next) != null);
				}
				else
				{
					// No, so return right without a left
					yield return resultSelector(defaultLeft, RightItem);
				}
			}
		}
		
		//****************************************
		
		private static Dictionary<TKey, JoinRoot<TItem>> BuildLookup<TKey, TItem>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
		{	//****************************************
			var Lookup = new Dictionary<TKey, JoinRoot<TItem>>(keyComparer ?? EqualityComparer<TKey>.Default);
			//****************************************
			
			// Build our set of items - O(n)
			foreach (var Item in items)
			{
				JoinRoot<TItem> Root;
				var Key = keySelector(Item);
				
				// If it exists, extend the linked list
				if (Lookup.TryGetValue(Key, out Root))
					Root.Root = new JoinItem<TItem>(Item, Root.Root);
				else
					Lookup.Add(Key, new JoinRoot<TItem>(Item));
			}
			
			//****************************************
			
			return Lookup;
		}
		
		//****************************************
		
		private class JoinItem<TItem>
		{	//****************************************
			public readonly TItem Item;
			public readonly JoinItem<TItem> Next;
			//****************************************
			
			public JoinItem(TItem item)
			{
				this.Item = item;
				this.Next = null;
			}
			
			public JoinItem(TItem item, JoinItem<TItem> next)
			{
				this.Item = item;
				this.Next = next;
			}
		}
		
		private class JoinRoot<TItem>
		{	//****************************************
			public JoinItem<TItem> Root;
			public bool IsRemoved;
			//****************************************
			
			public JoinRoot(TItem item)
			{
				this.Root = new JoinItem<TItem>(item);
			}
			
			//****************************************
			
			public IEnumerable<TItem> Enumerate()
			{
				var MyItem = Root;
				
				do
				{
					yield return MyItem.Item;
				} while ((MyItem = MyItem.Next) != null);
			}
		}
	}
}
