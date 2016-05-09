/****************************************\
 ListExtensions.cs
 Created: 2012-07-23
\****************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Extension methods for sorted list searching
	/// </summary>
	public static class ListExtensions
	{
		/// <summary>
		/// Searches a list sorted in ascending order, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted in ascending order</param>
		/// <param name="targetValue">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAboveAscending<TValue>(this IList<TValue> list, TValue targetValue) where TValue : IComparable<TValue>
		{	//****************************************
			int StartIndex = 0, EndIndex = list.Count - 1;
			int MiddleIndex = 0, CompareResult = -1;
			//****************************************

			if (list.Count == 0)
				return -1;

			while (StartIndex < EndIndex)
			{
				MiddleIndex = StartIndex + ((EndIndex - StartIndex) >> 1);
				CompareResult = list[MiddleIndex].CompareTo(targetValue);

				//Log.Debug("Check {0}: {1} ({2})", MiddleIndex, Dates[MiddleIndex].Ticks, CompareResult);

				if (CompareResult < 0) // Index is less than the target. The target is somewhere above
					StartIndex = MiddleIndex + 1;
				else if (CompareResult > 0) // Index is greater than the target. The target is somewhere below
					EndIndex = MiddleIndex - 1;
				else // Found the target!
					return MiddleIndex;
			}

			CompareResult = list[StartIndex].CompareTo(targetValue);

			//Log.Debug("Inexact {0}: {1} ({2})", StartIndex, Dates[StartIndex].Ticks, CompareResult);

			if (CompareResult >= 0) // The current start location is greater than our target, it will do
				return StartIndex;

			// Start location is less than our target, move one up
			StartIndex = StartIndex + 1;

			// Ensure we're still within range
			if (StartIndex < list.Count)
				return StartIndex;

			return -1;
		}

		/// <summary>
		/// Searches a list sorted in descending order, returning the index of the closest value equal to or greater than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted in descending order</param>
		/// <param name="targetValue">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestAboveDescending<TValue>(this IList<TValue> list, TValue targetValue) where TValue : IComparable<TValue>
		{	//****************************************
			int StartIndex = 0, EndIndex = list.Count - 1;
			int MiddleIndex = 0, CompareResult = -1;
			//****************************************

			if (list.Count == 0)
				return -1;

			while (StartIndex < EndIndex)
			{
				MiddleIndex = StartIndex + ((EndIndex - StartIndex) >> 1);
				CompareResult = list[MiddleIndex].CompareTo(targetValue);

				//Log.Debug("Check {0}: {1} ({2})", MiddleIndex, Dates[MiddleIndex].Ticks, CompareResult);

				if (CompareResult > 0) // Index is greater than the target. The target is somewhere above
					StartIndex = MiddleIndex + 1;
				else if (CompareResult < 0) // Index is less than the target. The target is somewhere below
					EndIndex = MiddleIndex - 1;
				else // Found the target!
					return MiddleIndex;
			}

			CompareResult = list[StartIndex].CompareTo(targetValue);

			//Log.Debug("Inexact {0}: {1} ({2})", StartIndex, Dates[StartIndex].Ticks, CompareResult);

			if (CompareResult >= 0) // The current start location is less than our target, it will do
				return StartIndex;

			// Start location is greater than our target, move one down
			StartIndex = StartIndex - 1;

			// Ensure we're still within range
			if (StartIndex >= 0)
				return StartIndex;

			return -1;
		}

		/// <summary>
		/// Searches a list sorted in ascending order, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted in ascending order</param>
		/// <param name="targetValue">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelowAscending<TValue>(this IList<TValue> list, TValue targetValue) where TValue : IComparable<TValue>
		{	//****************************************
			int StartIndex = 0, EndIndex = list.Count - 1;
			int MiddleIndex = 0, CompareResult = -1;
			//****************************************
			
			if (list.Count == 0)
				return -1;
			
			while (StartIndex < EndIndex)
			{
				MiddleIndex = StartIndex + ((EndIndex - StartIndex) >> 1);
				CompareResult = list[MiddleIndex].CompareTo(targetValue);
				
				//Log.Debug("Check {0}: {1} ({2})", MiddleIndex, Dates[MiddleIndex].Ticks, CompareResult);
				
				if (CompareResult < 0) // Index is less than the target. The target is somewhere above
					StartIndex = MiddleIndex + 1;
				else if (CompareResult > 0) // Index is greater than the target. The target is somewhere below
					EndIndex = MiddleIndex - 1;
				else // Found the target!
					return MiddleIndex;
			}
			
			CompareResult = list[StartIndex].CompareTo(targetValue);
			
			//Log.Debug("Inexact {0}: {1} ({2})", StartIndex, Dates[StartIndex].Ticks, CompareResult);
			
			if (CompareResult <= 0) // The current start location is less than our target, it will do
				return StartIndex;
			
			// Start location is greater than our target, move one down
			StartIndex = StartIndex - 1;
			
			// Ensure we're still within range
			if (StartIndex >= 0)
				return StartIndex;
			
			return -1;
		}
		
		/// <summary>
		/// Searches a list sorted in descending order, returning the index of the closest value equal to or less than the target
		/// </summary>
		/// <param name="list">The list to search through. Must be sorted in descending order</param>
		/// <param name="targetValue">The target value to search for</param>
		/// <returns>The index of the nearest value, or -1 if not found</returns>
		public static int NearestBelowDescending<TValue>(this IList<TValue> list, TValue targetValue) where TValue : IComparable<TValue>
		{	//****************************************
			int StartIndex = 0, EndIndex = list.Count - 1;
			int MiddleIndex = 0, CompareResult = -1;
			//****************************************
			
			if (list.Count == 0)
				return -1;
			
			while (StartIndex < EndIndex)
			{
				MiddleIndex = StartIndex + ((EndIndex - StartIndex) >> 1);
				CompareResult = list[MiddleIndex].CompareTo(targetValue);
				
				//Log.Debug("Check {0}: {1} ({2})", MiddleIndex, Dates[MiddleIndex].Ticks, CompareResult);
				
				if (CompareResult > 0) // Index is greater than the target. The target is somewhere above
					StartIndex = MiddleIndex + 1;
				else if (CompareResult < 0) // Index is less than the target. The target is somewhere below
					EndIndex = MiddleIndex - 1;
				else // Found the target!
					return MiddleIndex;
			}
			
			CompareResult = list[StartIndex].CompareTo(targetValue);
			
			//Log.Debug("Inexact {0}: {1} ({2})", StartIndex, Dates[StartIndex].Ticks, CompareResult);
			
			if (CompareResult <= 0) // The current start location is less than our target, it will do
				return StartIndex;
			
			// Start location is greater than our target, move one up
			StartIndex = StartIndex + 1;
			
			// Ensure we're still within range
			if (StartIndex < list.Count)
				return StartIndex;
			
			return -1;
		}

		//****************************************

		// Partial sort only works on things we can call sort on (otherwise we'd need to write a full QuickSort algorithm)

		// Based on https://blogs.msdn.microsoft.com/devdev/2006/01/18/efficient-selection-and-partial-sorting-based-on-quicksort/
		// and http://www.cs.yale.edu/homes/aspnes/pinewiki/QuickSelect.html

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		public static void PartialSort<TValue>(this List<TValue> list, int startIndex, int sortLength)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Count, Comparer<TValue>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<TValue>(this List<TValue> list, int startIndex, int sortLength, IComparer<TValue> comparer)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Count, Comparer<TValue>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		public static void PartialSort<TValue>(this List<TValue> list, int startIndex, int sortLength, int offset, int count)
		{
			list.PartialSort(startIndex, sortLength, offset, count, Comparer<TValue>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<TValue>(this List<TValue> list, int startIndex, int sortLength, int offset, int count, IComparer<TValue> comparer)
		{
			// Partial sort, so everything below the start index is less
			list.Partition(startIndex, offset, count, comparer);

			// Now sort so everything above the end index is greater
			// We can skip everything before startIndex, since everything below that is already sorted
			list.Partition(startIndex + sortLength - 1, startIndex, count - (startIndex - offset), comparer);

			// The items in our sort range are now the ones we expect. Sort what is left
			list.Sort(startIndex, sortLength, comparer);
		}
		
		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		public static void PartialSort<TValue>(this TValue[] list, int startIndex, int sortLength)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Length, Comparer<TValue>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<TValue>(this TValue[] list, int startIndex, int sortLength, IComparer<TValue> comparer)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Length, Comparer<TValue>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		public static void PartialSort<TValue>(this TValue[] list, int startIndex, int sortLength, int offset, int count)
		{
			list.PartialSort(startIndex, sortLength, offset, count, Comparer<TValue>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<TValue>(this TValue[] list, int startIndex, int sortLength, int offset, int count, IComparer<TValue> comparer)
		{
			// Partial sort, so everything below the start index is less
			list.Partition(startIndex, offset, count, comparer);

			// Now sort so everything above the end index is greater
			// We can skip everything before startIndex, since everything below that is already sorted
			list.Partition(startIndex + sortLength - 1, startIndex, count - (startIndex - offset), comparer);

			// The items in our sort range are now the ones we expect. Sort what is left
			Array.Sort(list, startIndex, sortLength, comparer);
		}

		/// <summary>
		/// Partitions a list around the Nth item
		/// </summary>
		/// <typeparam name="TValue">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="index">The index we want to guarantee is in the correct position</param>
		/// <param name="offset">The offset into the list we wish to partition from</param>
		/// <param name="count">The number of items from offset to partition</param>
		/// <param name="comparer">A comparer to compare items</param>
		/// <remarks>When complete, the Nth item in the list will be in its proper sort position. All items before the Nth item will be smaller, and all items after will be greater</remarks>
		public static void Partition<TValue>(this IList<TValue> list, int index, int offset, int count, IComparer<TValue> comparer)
		{	//****************************************
			int Min = offset, Max = offset + count - 1;
			//****************************************
			
			while (Min < Max)
			{
				// Select the middle item as our pivot
				int PivotIndex = Min + ((Max - Min) >> 1);

				// Sort our first and last items, plus the pivot
				list.SwapIfGreater(Min, PivotIndex, comparer);
				list.SwapIfGreater(Min, Max, comparer);
				list.SwapIfGreater(PivotIndex, Max, comparer);

				TValue Pivot = list[PivotIndex];

				int Left = Min, Right = Max;

				// Sort everything so all items less than the pivot are to its left, and all items greater are on its right
				while (Left < Right)
				{
					// Move the left boundary up as long as we're less than the pivot
					while (comparer.Compare(list[Left], Pivot) < 0)
					{
						Left++;
					}

					// Move the right boundary down as long as we're greater than the pivot
					while (comparer.Compare(Pivot, list[Right]) < 0)
					{
						Right--;
					}

					if (Left >= Right)
						break;

					// Swap these two items, since they're on the wrong sides of the pivot
					if (Left < Right)
					{
						var Temp1 = list[Left];
						var Temp2 = list[Right];

						// If our two items are the same, then we have one or more items in the list that are identical to our pivot
						if (comparer.Compare(Temp1, Temp2) == 0)
						{
							// Skip one
							Left++;
							//Right--;
						}
						else
						{
							list[Left] = Temp2;
							list[Right] = Temp1;
						}
					}
				}

				// Now we repeat either on the left or the right of the pivot
				if (Right > index)
				{
					// Pivot is higher, so select on the left
					Max = Left - 1;
				}
				else if (Left < index)
				{
					Min = Right + 1;
				}
				else
				{
					return;
				}
			}
		}

		//****************************************

		private static void SwapIfGreater<TValue>(this IList<TValue> list, int first, int second, IComparer<TValue> comparer)
		{
			if (first != second && comparer.Compare(list[first], list[second]) > 0)
			{
				var Temp = list[first];
				list[first] = list[second];
				list[second] = Temp;
			}
		}
	}
}
