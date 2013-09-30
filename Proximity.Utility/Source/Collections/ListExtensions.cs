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
				MiddleIndex = (StartIndex + EndIndex) >> 1;
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
			
			// Start location is greater than our target, move one down
			StartIndex = StartIndex + 1;
			
			// Ensure we're still within range
			if (StartIndex <= list.Count - 1)
				return StartIndex;
			
			return -1;
		}
	}
}
