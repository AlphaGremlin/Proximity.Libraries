using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq
{
	/// <summary>
	/// Provides useful Linq-style methods
	/// </summary>
	public static class JoinExtensions
	{
		/// <summary>
		/// Joins an enumeration with a dictionary
		/// </summary>
		/// <typeparam name="TKey">The input value type to join on</typeparam>
		/// <typeparam name="TValue">The value type that results from the join</typeparam>
		/// <typeparam name="TResult">The final result of the join</typeparam>
		/// <param name="input">The input enumeration</param>
		/// <param name="inner">The dictionary to join</param>
		/// <param name="keySelector">A selector to determine the key from the input</param>
		/// <param name="resultSelector">A selector to generate the final result</param>
		/// <returns>An enumeration of join matches</returns>
		public static IEnumerable<TResult> Join<TKey, TValue, TResult>(this IEnumerable<TKey> input, IReadOnlyDictionary<TKey, TValue> inner, Func<TKey, TValue, TResult> resultSelector)
		{
			foreach (var Key in input)
			{
				if (!inner.TryGetValue(Key, out var Value))
					continue;

				yield return resultSelector(Key, Value);
			}
		}

		/// <summary>
		/// Joins an enumeration with a dictionary
		/// </summary>
		/// <typeparam name="TInput">The input value type</typeparam>
		/// <typeparam name="TKey">The key type to join on</typeparam>
		/// <typeparam name="TValue">The value type that results from the join</typeparam>
		/// <typeparam name="TResult">The final result of the join</typeparam>
		/// <param name="input">The input enumeration</param>
		/// <param name="inner">The dictionary to join</param>
		/// <param name="keySelector">A selector to determine the key from the input</param>
		/// <param name="resultSelector">A selector to generate the final result</param>
		/// <returns>An enumeration of join matches</returns>
		public static IEnumerable<TResult> Join<TInput, TKey, TValue, TResult>(this IEnumerable<TInput> input, IReadOnlyDictionary<TKey, TValue> inner, Func<TInput, TKey> keySelector, Func<TInput, TKey, TValue, TResult> resultSelector)
		{
			foreach (var Input in input)
			{
				var Key = keySelector(Input);

				if (!inner.TryGetValue(Key, out var Value))
					continue;

				yield return resultSelector(Input, Key, Value);
			}
		}

		/// <summary>
		/// Performs a full outer join on two collections
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="rightKeySelect">A selector returning the key to join on for the right item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <returns>An enumeration returning the result of the full outer join</returns>
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector) where TKey : notnull
		{
			return FullOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default!, default!, null);
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
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
		{
			return FullOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default!, default!, keyComparer);
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
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TLeft defaultLeft, TRight defaultRight) where TKey : notnull
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
		public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TLeft defaultLeft, TRight defaultRight, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
		{ //****************************************
			JoinItem<TLeft>? LeftValue;
			var LeftLookup = BuildLookup(left, leftKeySelect, keyComparer);
			//****************************************

			// Perform the lookups - O(m * n) at worst (in the case where both lists contain items with the same key, resulting in a cross-join).
			// This is unfortunately unavoidable. In the ideal, it will be O(m) when keys are not duplicated
			foreach (var RightItem in right)
			{
				// Can we find a left item with the key of the right?
				if (LeftLookup.TryGetValue(rightKeySelect(RightItem), out var LeftRoot))
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
		public static IEnumerable<TResult> FullOuterGroupJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<IEnumerable<TLeft>, IEnumerable<TRight>, TKey, TResult> resultSelector, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
		{ //****************************************
			var LeftLookup = BuildLookup(left, leftKeySelect, keyComparer);
			var RightLookup = BuildLookup(right, rightKeySelect, keyComparer);
			//****************************************

			// Perform the lookups - O(n)
			foreach (var LeftPair in LeftLookup)
			{
				if (RightLookup.TryGetValue(LeftPair.Key, out var RightRoot))
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
		/// <returns>An enumeration returning the result of the left outer join</returns>
		public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector) where TKey : notnull
		{
			return LeftOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default!, null);
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
		/// <returns>An enumeration returning the result of the left outer join</returns>
		public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TRight defaultRight, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
		{ //****************************************
			var RightLookup = BuildLookup(right, rightKeySelect, keyComparer);
			//****************************************

			// Perform the lookups - O(m * n) at worst (in the case where both lists contain items with the same key, resulting in a cross-join).
			// This is unfortunately unavoidable. In the ideal, it will be O(m) when keys are not duplicated
			foreach (var LeftItem in left)
			{
				// Can we find a right item with the key of the left?
				if (RightLookup.TryGetValue(leftKeySelect(LeftItem), out var RightRoot))
				{
					// Yes, return the combination
					JoinItem<TRight>? RightValue = RightRoot.Root;

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
		/// Performs a left outer join between an enumeration and a dictionary
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <returns>An enumeration returning the result of the left outer join</returns>
		public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IReadOnlyDictionary<TKey, TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TLeft, TRight?, TResult> resultSelector) where TKey : notnull
		{
			return left.LeftOuterJoin(right, leftKeySelect, resultSelector, default!);
		}

		/// <summary>
		/// Performs a left outer join between an enumeration and a dictionary
		/// </summary>
		/// <param name="left">The left collection to join</param>
		/// <param name="right">The right collection to join</param>
		/// <param name="leftKeySelect">A selector returning the key to join on for the left item</param>
		/// <param name="resultSelector">A selector combining the two items into a single result</param>
		/// <param name="defaultRight">The default for the right item when a match is not found</param>
		/// <returns>An enumeration returning the result of the left outer join</returns>
		public static IEnumerable<TResult> LeftOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IReadOnlyDictionary<TKey, TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TLeft, TRight?, TResult> resultSelector, TRight defaultRight) where TKey : notnull
		{
			foreach (var LeftItem in left)
			{
				// Can we find a right item with the key of the left?
				if (right.TryGetValue(leftKeySelect(LeftItem), out var RightValue))
				{
					yield return resultSelector(LeftItem, RightValue);
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
		public static IEnumerable<TResult> RightOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector) where TKey : notnull
		{
			return RightOuterJoin(left, right, leftKeySelect, rightKeySelect, resultSelector, default!, null);
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
		public static IEnumerable<TResult> RightOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TKey> leftKeySelect, Func<TRight, TKey> rightKeySelect, Func<TLeft, TRight, TResult> resultSelector, TLeft defaultLeft, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
		{ //****************************************
			var LeftLookup = BuildLookup(left, leftKeySelect, keyComparer);
			//****************************************

			// Perform the lookups - O(m * n) at worst (in the case where both lists contain items with the same key, resulting in a cross-join).
			// This is unfortunately unavoidable. In the ideal, it will be O(m) when keys are not duplicated
			foreach (var RightItem in right)
			{
				// Can we find a left item with the key of the right?
				if (LeftLookup.TryGetValue(rightKeySelect(RightItem), out var LeftRoot))
				{
					// Yes, return the combination
					JoinItem<TLeft>? LeftValue = LeftRoot.Root;

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

		private static Dictionary<TKey, JoinRoot<TItem>> BuildLookup<TKey, TItem>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
		{ //****************************************
			var Lookup = new Dictionary<TKey, JoinRoot<TItem>>(keyComparer ?? EqualityComparer<TKey>.Default);
			//****************************************

			// Build our set of items - O(n)
			foreach (var Item in items)
			{
				var Key = keySelector(Item);

				// If it exists, extend the linked list
				if (Lookup.TryGetValue(Key, out var Root))
					Root.Root = new JoinItem<TItem>(Item, Root.Root);
				else
					Lookup.Add(Key, new JoinRoot<TItem>(Item));
			}

			//****************************************

			return Lookup;
		}

		//****************************************

		private sealed class JoinItem<T>
		{ //****************************************
			public readonly T Item;
			public readonly JoinItem<T>? Next;
			//****************************************

			public JoinItem(T item)
			{
				Item = item;
				Next = null;
			}

			public JoinItem(T item, JoinItem<T> next)
			{
				Item = item;
				Next = next;
			}
		}

		private sealed class JoinRoot<T>
		{ //****************************************
			public JoinItem<T> Root;
			public bool IsRemoved;
			//****************************************

			public JoinRoot(T item)
			{
				Root = new JoinItem<T>(item);
			}

			//****************************************

			public IEnumerable<T> Enumerate()
			{
				JoinItem<T>? MyItem = Root;

				do
				{
					yield return MyItem.Item;
				} while ((MyItem = MyItem.Next) != null);
			}
		}
	}
}
