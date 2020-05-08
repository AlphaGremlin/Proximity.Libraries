using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System
{
	/// <summary>
	/// Provides some useful extensions related to <see cref="ReadOnlySpan{T}"/>
	/// </summary>
	public static class MemoryExtensionsEx
	{
		/// <summary>
		/// Converts a character span to a string
		/// </summary>
		/// <param name="span">The span to convert</param>
		/// <returns>A new string with the contents of the span</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETSTANDARD2_0
		public static string AsString(this ReadOnlySpan<char> span) => span.IsEmpty ? string.Empty : span.ToString();
#else
		public static string AsString(this ReadOnlySpan<char> span) => span.IsEmpty ? string.Empty : new string(span);
#endif

		/// <summary>
		/// Converts a character span to a string
		/// </summary>
		/// <param name="span">The span to convert</param>
		/// <returns>A new string with the contents of the span</returns>
#if NETSTANDARD2_0
		public static string AsString(this Span<char> span) => span.IsEmpty ? string.Empty : span.ToString();
#else
		public static string AsString(this Span<char> span) => span.IsEmpty ? string.Empty : new string(span);
#endif

		/// <summary>
		/// Compares two spans for sorting
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="left">The span to compare</param>
		/// <param name="right">The span to compare to</param>
		/// <param name="comparer">A comparer to use</param>
		/// <returns>The result of the comparison</returns>
		public static int SequenceCompareTo<T>(this ReadOnlySpan<T> left, ReadOnlySpan<T> right, IComparer<T> comparer)
		{
			var CompareLength = Math.Min(left.Length, right.Length);

			for (var Index = 0; Index < CompareLength; Index++)
			{
				var Result = comparer.Compare(left[Index], right[Index]);

				if (Result != 0)
					return Result;
			}

			if (left.Length > right.Length)
				return 1;

			if (left.Length < right.Length)
				return -1;

			return 0;
		}

		/// <summary>
		/// Compares two spans for equality
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="left">The sequence to compare</param>
		/// <param name="right">The span to compare to</param>
		/// <param name="comparer">The comparer to use</param>
		/// <returns>True if the two sequences match, otherwise false</returns>
		public static bool SequenceEqual<T>(this ReadOnlySpan<T> left, ReadOnlySpan<T> right, IEqualityComparer<T> comparer)
		{
			if (left.Length != right.Length)
				return false;

			for (var Index = 0; Index < left.Length; Index++)
			{
				if (!comparer.Equals(left[Index], right[Index]))
					return false;
			}

			return true;
		}
	}
}
