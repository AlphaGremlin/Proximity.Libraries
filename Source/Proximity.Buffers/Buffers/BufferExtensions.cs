using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides some useful extensions related to ReadOnlySequence
	/// </summary>
	public static class BufferExtensions
	{
		/// <summary>
		/// Searches for the occurrence of a character within a character span
		/// </summary>
		/// <param name="span">The span to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The index of the first occurrence of the character, or -1 if it was not found</returns>
		public static int IndexOf(this ReadOnlySpan<char> span, char value, StringComparison comparisonType)
		{
			if (span.Length == 0)
				return -1;

			Span<char> MyChar = stackalloc char[] { value };

			return span.IndexOf(MyChar, comparisonType);
		}

		/// <summary>
		/// Searches for the occurrence of a character within a byte block
		/// </summary>
		/// <param name="source">The span to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="encoding">The character encoding to use</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The index of the first occurrence of the character, or -1 if it was not found</returns>
		public static int IndexOf(this ReadOnlyMemory<byte> source, ReadOnlySpan<char> value, Encoding encoding, StringComparison comparisonType)
		{
			if (value.Length == 0)
				throw new ArgumentException(nameof(value));

			var Length = encoding.GetCharCount(source.Span);
			var OutBuffer = ArrayPool<char>.Shared.Rent(Length);

			try
			{
				var OutChars = encoding.GetChars(source.Span, OutBuffer);

				return new ReadOnlySpan<char>(OutBuffer, 0, OutChars).IndexOf(value, comparisonType);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(OutBuffer);
			}
		}

		/// <summary>
		/// Compares the character sequence to a span
		/// </summary>
		/// <param name="source">The sequence to compare</param>
		/// <param name="value">The span to compare to</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>True if the two sequences match, otherwise false</returns>
		public static bool SequenceEqual(this ReadOnlySpan<char> source, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (source.Length != value.Length)
				return false;

			return source.CompareTo(value, comparisonType) == 0;
		}

		/// <summary>
		/// Compares the start of a byte memory block against a character span
		/// </summary>
		/// <param name="source">The memory block to check</param>
		/// <param name="value">The value to check against</param>
		/// <param name="encoding">The character encoding to use</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>True if the sequence starts with the span, otherwise False</returns>
		public static bool StartsWith(this ReadOnlyMemory<byte> source, ReadOnlySpan<char> value, Encoding encoding, StringComparison comparisonType)
		{
			var Length = encoding.GetCharCount(source.Span);
			var OutBuffer = ArrayPool<char>.Shared.Rent(Length);

			try
			{
				var OutChars = encoding.GetChars(source.Span, OutBuffer);

				return new ReadOnlySpan<char>(OutBuffer, 0, OutChars).StartsWith(value, comparisonType);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(OutBuffer);
			}
		}
	}
}
