using System;
using System.Buffers;
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
		/// Converts a character span to a string
		/// </summary>
		/// <param name="span">The span to convert</param>
		/// <param name="offset">The offset to start at</param>
		/// <returns>A new string with the contents of the span</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string AsString(this Span<char> span, int offset) => span.Slice(offset).AsString();

		/// <summary>
		/// Converts a character span to a string
		/// </summary>
		/// <param name="span">The span to convert</param>
		/// <param name="offset">The offset to start at</param>
		/// <returns>A new string with the contents of the span</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string AsString(this ReadOnlySpan<char> span, int offset) => span.Slice(offset).AsString();

		/// <summary>
		/// Converts a character span to a string
		/// </summary>
		/// <param name="span">The span to convert</param>
		/// <param name="offset">The offset to start at</param>
		/// <param name="count">The number of character to convert</param>
		/// <returns>A new string with the contents of the span</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string AsString(this Span<char> span, int offset, int count) => span.Slice(offset, count).AsString();

		/// <summary>
		/// Converts a character span to a string
		/// </summary>
		/// <param name="span">The span to convert</param>
		/// <param name="offset">The offset to start at</param>
		/// <param name="count">The number of character to convert</param>
		/// <returns>A new string with the contents of the span</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string AsString(this ReadOnlySpan<char> span, int offset, int count) => span.Slice(offset, count).AsString();

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

			using var OutBuffer = AutoArrayPool<char>.Shared.Rent(Length);

			var OutChars = encoding.GetChars(source.Span, OutBuffer);

			return new ReadOnlySpan<char>(OutBuffer, 0, OutChars).IndexOf(value, comparisonType);
		}

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

		/// <summary>
		/// Splits a sequence based on a separator without allocations
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to split</param>
		/// <param name="separator">The separator to split on</param>
		/// <param name="omitEmpty">Whether to omit empty items</param>
		/// <returns>An enumerable that returns multiple sequences based on the split</returns>
		public static SplitSingleEnumerator<T> Split<T>(this ReadOnlySpan<T> sequence, T separator, bool omitEmpty = false) where T : IEquatable<T> => new(sequence, separator, omitEmpty);

		/// <summary>
		/// Splits a sequence based on a separator without allocations
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to split</param>
		/// <param name="separator">The separator to split on</param>
		/// <param name="omitEmpty">Whether to omit empty items</param>
		/// <returns>An enumerable that returns multiple sequences based on the split</returns>
		public static SplitEnumerator<T> Split<T>(this ReadOnlySpan<T> sequence, ReadOnlySpan<T> separator, bool omitEmpty = false) where T : IEquatable<T> => new(sequence, separator, omitEmpty);

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
			// What's the maximum number of bytes that can make up a character string of this length?
			var MaxLength = encoding.GetMaxByteCount(value.Length);

			// We need to decode enough to guarantee we will make the input string, no matter how the comparison works
			if (source.Length > MaxLength)
				source = source.Slice(0, MaxLength);

			using var OutBuffer = AutoArrayPool<char>.Shared.Rent(source.Length);

			// Decode only as much as we need to verify the source starts with our value (or doesn't)
			var OutChars = encoding.GetChars(source.Span, OutBuffer);

			return new ReadOnlySpan<char>(OutBuffer, 0, OutChars).StartsWith(value, comparisonType);
		}

		/// <summary>
		/// Converts a char sequence into a StringBuilder
		/// </summary>
		/// <param name="source">The char sequence to encode</param>
		/// <returns>The converted contents of the sequence</returns>
		public static StringBuilder ToStringBuilder(this ReadOnlySpan<char> source) => new StringBuilder(source.Length).Append(source);

		/// <summary>
		/// Converts a char sequence into a StringBuilder
		/// </summary>
		/// <param name="source">The char sequence to encode</param>
		/// <returns>The converted contents of the sequence</returns>
		public static StringBuilder ToStringBuilder(this Span<char> source) => new StringBuilder(source.Length).Append(source);

		//****************************************

		/// <summary>
		/// Splits a sequence based on a separator
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		public ref struct SplitSingleEnumerator<T> where T : IEquatable<T>
		{ //****************************************
			private SplitEnumerator<T> _Enumerator;

			private T _Separator;
			//****************************************

			internal SplitSingleEnumerator(ReadOnlySpan<T> sequence, T separator, bool omitEmpty)
			{
				_Separator = separator;

				// We can create a Span from an internal field safely here, since this is a Ref struct
				_Enumerator = new SplitEnumerator<T>(sequence, default, omitEmpty);
			}

			//****************************************

			/// <inheritdoc />
			public SplitSingleEnumerator<T> GetEnumerator()
			{
#if NETSTANDARD2_0
				unsafe
				{
					// This should theoretically be safe, since we're a ref struct and can only exist on the stack (and thus won't be relocated by the GC), so the intermediary pointer is okay
					_Enumerator = new SplitEnumerator<T>(_Enumerator.Sequence, new Span<T>(System.Runtime.CompilerServices.Unsafe.AsPointer(ref _Separator), 1), _Enumerator.OmitEmpty);
				}
#else
				// Safe, since we're a ref struct, so creating a span over our own separator is okay
				_Enumerator = new SplitEnumerator<T>(_Enumerator.Sequence, MemoryMarshal.CreateSpan(ref _Separator, 1), _Enumerator.OmitEmpty);
#endif

				return this;
			}

			/// <inheritdoc />
			public bool MoveNext() => _Enumerator.MoveNext();

			/// <inheritdoc />
			public void Reset() => _Enumerator.Reset();

			/// <inheritdoc />
			public void Dispose() => _Enumerator.Dispose();

			//****************************************

			/// <inheritdoc />
			public ReadOnlySpan<T> Current => _Enumerator.Current;
		}

		/// <summary>
		/// Splits a sequence based on a separator
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		public ref struct SplitEnumerator<T> where T : IEquatable<T>
		{ //****************************************
			private readonly ReadOnlySpan<T> _Sequence;
			private readonly ReadOnlySpan<T> _Separator;
			private readonly bool _OmitEmpty;

			private ReadOnlySpan<T> _Position;
			//****************************************

			internal SplitEnumerator(ReadOnlySpan<T> sequence, ReadOnlySpan<T> separator, bool omitEmpty)
			{
				_Sequence = sequence;
				_Separator = separator;
				_OmitEmpty = omitEmpty;

				_Position = _Sequence;
				Current = default;
			}

			//****************************************

			/// <inheritdoc />
			public SplitEnumerator<T> GetEnumerator() => this;

			/// <inheritdoc />
			public bool MoveNext()
			{
				int NextSplit;

				for (; ; )
				{
					// Are there any more splits to give?
					if (_Position.Length == 0)
					{
						Current = default;

						return false;
					}

					// Can we find another split?
					NextSplit = _Position.IndexOf(_Separator);

					if (!_OmitEmpty)
						break;

					if (NextSplit != 0)
						break;

					// Skip empty items
					_Position = _Position.Slice(_Separator.Length);
				}

				// If there's no more separators, end the split at the end of the sequence
				if (NextSplit == -1)
					NextSplit = _Position.Length;

				Current = _Position.Slice(0, NextSplit);

				// Trim off this segment
				_Position = _Position.Slice(Math.Min(NextSplit + _Separator.Length, _Position.Length));

				return true;
			}

			/// <inheritdoc />
			public void Reset() => _Position = _Sequence;

			/// <inheritdoc />
			public void Dispose()
			{
			}

			//****************************************

			/// <inheritdoc />
			public ReadOnlySpan<T> Current { get; private set; }

			internal ReadOnlySpan<T> Sequence => _Sequence;

			internal bool OmitEmpty => _OmitEmpty;
		}

	}
}
