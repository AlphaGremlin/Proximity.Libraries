using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace System.Linq
{
	/// <summary>
	/// Extension methods for sorted list searching
	/// </summary>
	public static class SortingExtensions
	{
		// Partial sort only works on things we can call sort on (otherwise we'd need to write a full QuickSort algorithm)

		// Based on https://blogs.msdn.microsoft.com/devdev/2006/01/18/efficient-selection-and-partial-sorting-based-on-quicksort/
		// and http://www.cs.yale.edu/homes/aspnes/pinewiki/QuickSelect.html

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		public static void PartialSort<T>(this List<T> list, int startIndex, int sortLength)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Count, Comparer<T>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<T>(this List<T> list, int startIndex, int sortLength, IComparer<T> comparer)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Count, comparer);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		public static void PartialSort<T>(this List<T> list, int startIndex, int sortLength, int offset, int count)
		{
			list.PartialSort(startIndex, sortLength, offset, count, Comparer<T>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<T>(this List<T> list, int startIndex, int sortLength, int offset, int count, IComparer<T> comparer)
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
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		public static void PartialSort<T>(this T[] list, int startIndex, int sortLength)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Length, Comparer<T>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<T>(this T[] list, int startIndex, int sortLength, IComparer<T> comparer)
		{
			list.PartialSort(startIndex, sortLength, 0, list.Length, comparer);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		public static void PartialSort<T>(this T[] list, int startIndex, int sortLength, int offset, int count)
		{
			list.PartialSort(startIndex, sortLength, offset, count, Comparer<T>.Default);
		}

		/// <summary>
		/// Performs a partial sort of a list
		/// </summary>
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="startIndex">The start index we wish to sort from</param>
		/// <param name="sortLength">The number of items to sort</param>
		/// <param name="offset">The start index of the list being partially sorted</param>
		/// <param name="count">The number of items in the list being partially sorted</param>
		/// <param name="comparer">A comparer to compare items</param>
		public static void PartialSort<T>(this T[] list, int startIndex, int sortLength, int offset, int count, IComparer<T> comparer)
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
		/// <typeparam name="T">The type of list element</typeparam>
		/// <param name="list">The target list</param>
		/// <param name="index">The index we want to guarantee is in the correct position</param>
		/// <param name="offset">The offset into the list we wish to partition from</param>
		/// <param name="count">The number of items from offset to partition</param>
		/// <param name="comparer">A comparer to compare items</param>
		/// <remarks>When complete, the Nth item in the list will be in its proper sort position. All items before the Nth item will be smaller, and all items after will be greater</remarks>
		public static void Partition<T>(this IList<T> list, int index, int offset, int count, IComparer<T> comparer)
		{	//****************************************
			int Min = offset, Max = offset + count - 1;
			//****************************************
			
			while (Min < Max)
			{
				// Select the middle item as our pivot
				var PivotIndex = Min + ((Max - Min) >> 1);

				// Sort our first and last items, plus the pivot
				list.SwapIfGreater(Min, PivotIndex, comparer);
				list.SwapIfGreater(Min, Max, comparer);
				list.SwapIfGreater(PivotIndex, Max, comparer);

				T Pivot = list[PivotIndex];

				int Left = Min, Right = Max;

				// Sort everything so all items less than the pivot are to its left, and all items greater are on its right
				while (Left < Right)
				{
					// Move the left boundary up as long as we're less than the pivot
					while (comparer.Compare(list[Left], Pivot) < 0)
						Left++;

					// Move the right boundary down as long as we're greater than the pivot
					while (comparer.Compare(Pivot, list[Right]) < 0)
						Right--;

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

		private static void SwapIfGreater<T>(this IList<T> list, int first, int second, IComparer<T> comparer)
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
