using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq
{
	/// <summary>
	/// Extension methods for specialised binary searches
	/// </summary>
	public static class BinarySearchExtensions
	{
		/// <summary>
		/// Searches a sorted list, returning the index of the given value, or the bitwise complement of the first larger index
		/// </summary>
		/// <typeparam name="T">The type of element in the list</typeparam>
		/// <param name="list">The list to search through. Must be sorted according to the default comparer</param>
		/// <param name="item">The item to search for</param>
		/// <returns>A zero or positive integer index into the list, or a negative index representing the bitwise complement of the first larger index</returns>
		public static int BinarySearch<T>(this IList<T> list, T item) => list.BinarySearch(0, list.Count, item, null);

		/// <summary>
		/// Searches a sorted list, returning the index of the given value, or the bitwise complement of the first larger index
		/// </summary>
		/// <typeparam name="T">The type of element in the list</typeparam>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The item to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>A zero or positive integer index into the list, or a negative index representing the bitwise complement of the first larger index</returns>
		public static int BinarySearch<T>(this IList<T> list, T item, IComparer<T>? comparer) => list.BinarySearch(0, list.Count, item, comparer);

		/// <summary>
		/// Searches a sorted list, returning the index of the given value, or the bitwise complement of the first larger index
		/// </summary>
		/// <typeparam name="T">The type of element in the list</typeparam>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="index">The starting index to search from</param>
		/// <param name="count">The number of items following the index to search</param>
		/// <param name="item">The item to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>A zero or positive integer index into the list, or a negative index representing the bitwise complement of the first larger index</returns>
		public static int BinarySearch<T>(this IList<T> list, int index, int count, T item, IComparer<T>? comparer)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (list.Count - index < count)
				throw new ArgumentException("Invalid range");

			if (count == 0)
				return -1;

			comparer ??= Comparer<T>.Default;

			int Low = index, High = index + count - 1;

			while (Low <= High)
			{
				var Middle = Low + ((High - Low) >> 1);
				var Result = comparer.Compare(list[Middle], item);

				if (Result < 0) // Index is less than the target. The target is somewhere above
					Low = Middle + 1;
				else if (Result > 0) // Index is greater than the target. The target is somewhere below
					High = Middle - 1;
				else // Found the target!
					return Middle;
			}

			return ~Low;
		}

		/// <summary>
		/// Searches a sorted list, returning the index of the given value, or the bitwise complement of the first larger index
		/// </summary>
		/// <typeparam name="T">The type of element in the list</typeparam>
		/// <param name="list">The list to search through. Must be sorted according to the default comparer</param>
		/// <param name="item">The item to search for</param>
		/// <returns>A zero or positive integer index into the list, or a negative index representing the bitwise complement of the first larger index</returns>
		public static int BinarySearch<T>(this IReadOnlyList<T> list, T item) => list.BinarySearch(0, list.Count, item, null);

		/// <summary>
		/// Searches a sorted list, returning the index of the given value, or the bitwise complement of the first larger index
		/// </summary>
		/// <typeparam name="T">The type of element in the list</typeparam>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The item to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>A zero or positive integer index into the list, or a negative index representing the bitwise complement of the first larger index</returns>
		public static int BinarySearch<T>(this IReadOnlyList<T> list, T item, IComparer<T>? comparer) => list.BinarySearch(0, list.Count, item, comparer);

		/// <summary>
		/// Searches a sorted list, returning the index of the given value, or the bitwise complement of the first larger index
		/// </summary>
		/// <typeparam name="T">The type of element in the list</typeparam>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="index">The starting index to search from</param>
		/// <param name="count">The number of items following the index to search</param>
		/// <param name="item">The item to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>A zero or positive integer index into the list, or a negative index representing the bitwise complement of the first larger index</returns>
		public static int BinarySearch<T>(this IReadOnlyList<T> list, int index, int count, T item, IComparer<T>? comparer)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (list.Count - index < count)
				throw new ArgumentException("Invalid range");

			if (count == 0)
				return -1;

			comparer ??= Comparer<T>.Default;

			int Low = index, High = index + count - 1;

			while (Low <= High)
			{
				var Middle = Low + ((High - Low) >> 1);
				var Result = comparer.Compare(list[Middle], item);

				if (Result < 0) // Index is less than the target. The target is somewhere above
					Low = Middle + 1;
				else if (Result > 0) // Index is greater than the target. The target is somewhere below
					High = Middle - 1;
				else // Found the target!
					return Middle;
			}

			return ~Low;
		}

		/// <summary>
		/// Searches a sorted list, returning the index of the given value, or the bitwise complement of the first larger index
		/// </summary>
		/// <typeparam name="TOuter">The type of element in the list</typeparam>
		/// <typeparam name="TInner">The type to sort on</typeparam>
		/// <param name="list">The list to search through. Must be sorted according to the given selector and comparer</param>
		/// <param name="index">The starting index to search from</param>
		/// <param name="count">The number of items following the index to search</param>
		/// <param name="item">The item to search for</param>
		/// <param name="selector">A selector to retrieve the value to sort by</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>A zero or positive integer index into the list, or a negative index representing the bitwise complement of the first larger index</returns>
		public static int BinarySearch<TOuter, TInner>(this IReadOnlyList<TOuter> list, int index, int count, TInner item, Func<TOuter, TInner> selector, IComparer<TInner>? comparer)
		{
			if (list is null)
				throw new ArgumentNullException(nameof(list));

			if (selector is null)
				throw new ArgumentNullException(nameof(selector));

			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (list.Count - index < count)
				throw new ArgumentException("Invalid range");

			if (count == 0)
				return -1;

			comparer ??= Comparer<TInner>.Default;

			int Low = index, High = index + count - 1;

			while (Low <= High)
			{
				var Middle = Low + ((High - Low) >> 1);
				var Result = comparer.Compare(selector(list[Middle]), item);

				if (Result < 0) // Index is less than the target. The target is somewhere above
					Low = Middle + 1;
				else if (Result > 0) // Index is greater than the target. The target is somewhere below
					High = Middle - 1;
				else // Found the target!
					return Middle;
			}

			return ~Low;
		}

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAbove<T>(this IList<T> list, T item) => list.NearestAbove(0, list.Count, item, null);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAbove<T>(this IList<T> list, T item, IComparer<T>? comparer) => list.NearestAbove(0, list.Count, item, comparer);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="index">The starting index to search from</param>
		/// <param name="count">The number of items following the index to search</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAbove<T>(this IList<T> list, int index, int count, T item, IComparer<T>? comparer)
		{
			var Position = list.BinarySearch(index, count, item, comparer ??= Comparer<T>.Default);

			if (Position < 0)
			{
				Position = ~Position;

				if (Position == index + count && comparer.Compare(list[index + count - 1], item) < 0)
					return -1; // The last item in the list is less than our target, so no match
			}

			return Position;
		}

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAbove<T>(this IReadOnlyList<T> list, T item) => list.NearestAbove(0, list.Count, item, null);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAbove<T>(this IReadOnlyList<T> list, T item, IComparer<T>? comparer) => list.NearestAbove(0, list.Count, item, comparer);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="index">The starting index to search from</param>
		/// <param name="count">The number of items following the index to search</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAbove<T>(this IReadOnlyList<T> list, int index, int count, T item, IComparer<T>? comparer)
		{
			var Position = list.BinarySearch(index, count, item, comparer ??= Comparer<T>.Default);

			if (Position < 0)
			{
				Position = ~Position;

				if (Position == index + count && comparer.Compare(list[index + count - 1], item) < 0)
					return -1; // The last item in the list is less than our target, so no match
			}

			return Position;
		}

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelow<T>(this IList<T> list, T item) => list.NearestBelow(0, list.Count, item, null);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelow<T>(this IList<T> list, T item, IComparer<T>? comparer) => list.NearestBelow(0, list.Count, item, comparer);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="index">The starting index to search from</param>
		/// <param name="count">The number of items following the index to search</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelow<T>(this IList<T> list, int index, int count, T item, IComparer<T>? comparer)
		{
			var Position = list.BinarySearch(index, count, item, comparer ??= Comparer<T>.Default);

			if (Position < 0)
			{
				Position = ~Position;

				if (Position == index && comparer.Compare(list[index], item) > 0)
					return -1; // The first item in the list is greater than our target, so no match
			}

			return Position;
		}

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelow<T>(this IReadOnlyList<T> list, T item) => list.NearestBelow(0, list.Count, item, null);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelow<T>(this IReadOnlyList<T> list, T item, IComparer<T>? comparer) => list.NearestBelow(0, list.Count, item, comparer);

		/// <summary>
		/// Searches a sorted list, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted according to the given comparer</param>
		/// <param name="index">The starting index to search from</param>
		/// <param name="count">The number of items following the index to search</param>
		/// <param name="item">The target value to search for</param>
		/// <param name="comparer">A comparer that describes the sorting of the list. If null, represents the default comparer</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelow<T>(this IReadOnlyList<T> list, int index, int count, T item, IComparer<T>? comparer)
		{
			var Position = list.BinarySearch(index, count, item, comparer ??= Comparer<T>.Default);

			if (Position < 0)
			{
				Position = ~Position;

				if (Position == index && comparer.Compare(list[index], item) > 0)
					return -1; // The first item in the list is greater than our target, so no match
			}

			return Position;
		}

	}
}
