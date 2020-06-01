using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides some useful extensions related to ReadOnlySequence
	/// </summary>
	public static class ReadOnlySequenceExtensions
	{
		/// <summary>
		/// Concatenates a char sequence as a string
		/// </summary>
		/// <param name="sequence">The char sequence to concatenate</param>
		/// <returns>The string equivalent of the sequence</returns>
		public static string AsString(this ReadOnlySequence<char> sequence)
		{
			if (sequence.IsSingleSegment)
				return sequence.First.Span.AsString();

			var StringBuilder = new StringBuilder((int)sequence.Length);

			foreach (var MySegment in sequence)
				StringBuilder.Append(MySegment.Span);

			return StringBuilder.ToString();
		}

		/// <summary>
		/// Combines one or more ArraySegment blocks into a single ReadOnlySequence
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="source">An enumeration of ArraySegment blocks</param>
		/// <returns>A ReadOnlySequence from the given blocks</returns>
		public static ReadOnlySequence<T> Combine<T>(this IEnumerable<ArraySegment<T>> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			ArraySegment<T>? FirstSegment = null;
			CombinedSegment<T>? StartSegment = null, CurrentSegment = null;

			foreach (var Segment in source)
			{
				if (Segment.Count == 0)
					continue;

				if (CurrentSegment == null)
				{
					// Cache the first segment. If there are no others, we can just wrap it as-is
					if (FirstSegment == null)
					{
						FirstSegment = Segment;
						continue;
					}

					// Found a second segment
					// Create the first segment of the linked list
					StartSegment = CurrentSegment = new CombinedSegment<T>(FirstSegment.Value);
				}

				CurrentSegment = new CombinedSegment<T>(CurrentSegment, Segment);
			}

			// Only found one non-empty segment
			if (FirstSegment != null)
				return new ReadOnlySequence<T>(FirstSegment.Value);

			// Found no non-empty sequences
			if (CurrentSegment == null)
				return ReadOnlySequence<T>.Empty;

			return new ReadOnlySequence<T>(StartSegment!, 0, CurrentSegment, CurrentSegment.Memory.Length);
		}

		/// <summary>
		/// Combines one or more ReadOnlyMemory blocks into a single ReadOnlySequence
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="source">An enumeration of ReadOnlyMemory blocks</param>
		/// <returns>A ReadOnlySequence from the given blocks</returns>
		public static ReadOnlySequence<T> Combine<T>(this IEnumerable<ReadOnlyMemory<T>> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			ReadOnlyMemory<T>? FirstSegment = null;
			CombinedSegment<T>? StartSegment = null, CurrentSegment = null;

			foreach (var Segment in source)
			{
				if (Segment.IsEmpty)
					continue;

				if (CurrentSegment == null)
				{
					// Cache the first segment. If there are no others, we can just wrap it as-is
					if (FirstSegment == null)
					{
						FirstSegment = Segment;
						continue;
					}

					// Found a second segment
					// Create the first segment of the linked list
					StartSegment = CurrentSegment = new CombinedSegment<T>(FirstSegment.Value);
				}

				CurrentSegment = new CombinedSegment<T>(CurrentSegment, Segment);
			}

			// Only found one non-empty segment
			if (FirstSegment != null)
				return new ReadOnlySequence<T>(FirstSegment.Value);

			// Found no non-empty sequences
			if (CurrentSegment == null)
				return ReadOnlySequence<T>.Empty;

			return new ReadOnlySequence<T>(StartSegment!, 0, CurrentSegment, CurrentSegment.Memory.Length);
		}

		/// <summary>
		/// Combines one or more ReadOnlySequences into a single ReadOnlySequence
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="source">An enumeration of ReadOnlySequences</param>
		/// <returns>The concatenation of the given ReadOnlySequences</returns>
		public static ReadOnlySequence<T> Combine<T>(this IEnumerable<ReadOnlySequence<T>> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			ReadOnlySequence<T>? FirstSequence = null;
			CombinedSegment<T>? StartSegment = null, CurrentSegment = null;

			// Merge all the sequences into one
			foreach (var Sequence in source)
			{
				if (Sequence.IsEmpty)
					continue;

				if (CurrentSegment == null)
				{
					// Cache the first sequence. If there are no others, we can just return it as-is
					if (FirstSequence == null)
					{
						FirstSequence = Sequence;
						continue;
					}

					// Found a second sequence
					// Create the first segment of the linked list
					StartSegment = CurrentSegment = new CombinedSegment<T>(FirstSequence.Value.First);

					// Append any subsequent segments
					foreach (var ChildSegment in FirstSequence.Value.Slice(StartSegment.Memory.Length))
						CurrentSegment = new CombinedSegment<T>(CurrentSegment, ChildSegment);
				}

				// Append the segments of this sequence to the linked list as well
				foreach (var ChildSegment in Sequence)
					CurrentSegment = new CombinedSegment<T>(CurrentSegment, ChildSegment);
			}

			// Only found one non-empty sequence
			if (FirstSequence != null)
				return FirstSequence.Value;

			// Found no non-empty sequences
			if (CurrentSegment == null)
				return ReadOnlySequence<T>.Empty;

			return new ReadOnlySequence<T>(StartSegment!, 0, CurrentSegment, CurrentSegment.Memory.Length);
		}

		/// <summary>
		/// Compares the sequence to a span using string comparison rules
		/// </summary>
		/// <param name="left">The left char sequence</param>
		/// <param name="right">The right char sequence</param>
		/// <param name="comparisonType">The string comparison to perform</param>
		/// <returns>Positive if left is greater than right, negative if left is less than right, zero if left is equal to right</returns>
		public static int CompareTo(this ReadOnlySequence<char> left, ReadOnlySpan<char> right, StringComparison comparisonType)
		{
			if (left.IsSingleSegment)
				return left.First.Span.CompareTo(right, comparisonType);

			var HasPartial = false;
			Span<char> PartLeft = stackalloc char[2];
			int Result;

			foreach (var Segment in left)
			{
				if (Segment.IsEmpty)
					continue;

				var Left = Segment.Span;

				if (HasPartial)
				{
					// We're comparing a surrogate pair split across segments. If the pair is invalid, ignore it
					if (char.IsLowSurrogate(Left[0]))
					{
						PartLeft[1] = Left[0];

						Result = ((ReadOnlySpan<char>)PartLeft).CompareTo(right.Slice(0, 2), comparisonType);

						if (Result != 0)
							return Result;

						Left = Left.Slice(1);
						right = right.Slice(2);
					}

					HasPartial = false;
				}

				var CompareLength = Math.Min(Segment.Length, right.Length);

				// If the source ends on a high surrogate pair, save it for the next segment
				if (char.IsHighSurrogate(Left[CompareLength - 1]))
				{
					PartLeft[0] = Left[CompareLength - 1];

					HasPartial = true;

					if (--CompareLength == 0)
						continue;
				}

				Result = Left.CompareTo(right.Slice(0, CompareLength), comparisonType);
				
				if (Result != 0)
					return Result;

				right = right.Slice(CompareLength);
			}

			// If the left has a high surrogate left over, treat it as higher even though it's an invalid string
			if (HasPartial || left.Length > right.Length)
				return 1;

			if (left.Length < right.Length)
				return -1;

			return 0;
		}

		/// <summary>
		/// Decodes a byte sequence into a char sequence
		/// </summary>
		/// <param name="sequence">The byte sequence to encode</param>
		/// <param name="encoding">The encoding to use</param>
		/// <returns>The decoded contents of the sequence</returns>
		public static ReadOnlySequence<char> Decode(this ReadOnlySequence<byte> sequence, Encoding encoding)
		{
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			if (sequence.IsSingleSegment)
				return new ReadOnlySequence<char>(encoding.GetChars(sequence.First.Span));

			var Decoder = encoding.GetDecoder();

			CombinedSegment<char>? StartSegment = null, CurrentSegment = null;

			foreach (var MySegment in sequence)
			{
				var InBuffer = MySegment.Span;

				var OutBuffer = new char[Decoder.GetCharCount(InBuffer, false)];
				var WrittenChars = Decoder.GetChars(InBuffer, OutBuffer, false);
				var OutSegment = new ReadOnlyMemory<char>(OutBuffer, 0, WrittenChars);

				if (StartSegment == null)
					StartSegment = CurrentSegment = new CombinedSegment<char>(OutSegment);
				else
					CurrentSegment = new CombinedSegment<char>(CurrentSegment!, OutSegment);
			}

			// Flush the decoder
			var RemainingChars = Decoder.GetCharCount(Array.Empty<byte>(), true);

			if (RemainingChars > 0)
			{
				var OutBuffer = new char[RemainingChars];
				var WrittenChars = Decoder.GetChars(Array.Empty<byte>(), OutBuffer, true);
				var OutSegment = new ReadOnlyMemory<char>(OutBuffer, 0, WrittenChars);

				if (StartSegment == null)
					StartSegment = CurrentSegment = new CombinedSegment<char>(OutSegment);
				else
					CurrentSegment = new CombinedSegment<char>(CurrentSegment!, OutSegment);
			}

			if (CurrentSegment == null)
				return ReadOnlySequence<char>.Empty;

			return new ReadOnlySequence<char>(StartSegment!, 0, CurrentSegment, CurrentSegment.Memory.Length);
		}

		/// <summary>
		/// Encodes a char sequence as a byte sequence
		/// </summary>
		/// <param name="sequence">The char sequence to decode</param>
		/// <param name="encoding">The encoding to use</param>
		/// <returns>The encoded contents of the sequence</returns>
		public static ReadOnlySequence<byte> Encode(this ReadOnlySequence<char> sequence, Encoding encoding)
		{
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			if (sequence.IsSingleSegment)
				return new ReadOnlySequence<byte>(encoding.GetBytes(sequence.First.Span));

			var Encoder = encoding.GetEncoder();

			CombinedSegment<byte>? StartSegment = null, CurrentSegment = null;

			foreach (var MySegment in sequence)
			{
				var InBuffer = MySegment.Span;

				var OutBuffer = new byte[Encoder.GetByteCount(InBuffer, false)];
				var WrittenChars = Encoder.GetBytes(InBuffer, OutBuffer, false);
				var OutSegment = new ReadOnlyMemory<byte>(OutBuffer, 0, WrittenChars);

				if (StartSegment == null)
					StartSegment = CurrentSegment = new CombinedSegment<byte>(OutSegment);
				else
					CurrentSegment = new CombinedSegment<byte>(CurrentSegment!, OutSegment);
			}

			// Flush the encoder
			var RemainingBytes = Encoder.GetByteCount(Array.Empty<char>(), true);

			if (RemainingBytes > 0)
			{
				var OutBuffer = new byte[RemainingBytes];
				var WrittenChars = Encoder.GetBytes(Array.Empty<char>(), OutBuffer, true);
				var OutSegment = new ReadOnlyMemory<byte>(OutBuffer, 0, WrittenChars);

				if (StartSegment == null)
					StartSegment = CurrentSegment = new CombinedSegment<byte>(OutSegment);
				else
					CurrentSegment = new CombinedSegment<byte>(CurrentSegment!, OutSegment);
			}

			if (CurrentSegment == null)
				return ReadOnlySequence<byte>.Empty;

			return new ReadOnlySequence<byte>(StartSegment!, 0, CurrentSegment, CurrentSegment.Memory.Length);
		}

		/// <summary>
		/// Compares the start of a character sequence against a character span
		/// </summary>
		/// <param name="sequence">The sequence to check</param>
		/// <param name="value">The value to check against</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>True if the sequence starts with the span, otherwise False</returns>
		public static bool EndsWith(this ReadOnlySequence<char> sequence, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (sequence.Length < value.Length)
				return false;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.EndsWith(value, comparisonType);

			return sequence.Slice(sequence.Length - value.Length).SequenceEqual(value, comparisonType);
		}

		/// <summary>
		/// Gets the value from a ReadOnlySequence at a particular index
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="source">The source sequence</param>
		/// <param name="index">The index to seeek</param>
		/// <returns>The value at the given index</returns>
		public static T Get<T>(in this ReadOnlySequence<T> source, long index) => source.Get(source.GetPosition(index));

		/// <summary>
		/// Gets the value from a ReadOnlySequence at a particular position
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="source">The source sequence</param>
		/// <param name="position">The position to seeek</param>
		/// <returns>The value at the given index</returns>
		public static T Get<T>(in this ReadOnlySequence<T> source, SequencePosition position)
		{
			if (!source.TryGet(ref position, out var MyMemory, false))
				throw new ArgumentOutOfRangeException(nameof(position));

			return MyMemory.Span[0];
		}

		/// <summary>
		/// Searches for the occurrence of a character within a character sequence
		/// </summary>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The index of the first occurrence of the character, or -1 if it was not found</returns>
		public static long IndexOf(this ReadOnlySequence<char> sequence, char value, StringComparison comparisonType)
		{
			if (sequence.Length == 0)
				return -1;

			Span<char> MyChar = stackalloc char[1] { value };

			return sequence.IndexOf(MyChar, comparisonType);
		}

		/// <summary>
		/// Searches for the occurrence of a value within a sequence
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <returns>The index of the first occurrence of the value, or -1 if it was not found</returns>
		public static long IndexOf<T>(this ReadOnlySequence<T> sequence, T value) where T : IEquatable<T>
		{
			if (sequence.Length == 0)
				return -1;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.IndexOf(value);

			var Offset = 0L;

			foreach (var Segment in sequence)
			{
				var Index = Segment.Span.IndexOf(value);

				if (Index != -1)
					return Offset + Index;

				Offset += Segment.Length;
			}

			return -1;
		}

		/// <summary>
		/// Searches for the occurrence of a character span within a character sequence
		/// </summary>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The index of the first occurrence of the span, or -1 if it was not found</returns>
		public static long IndexOf(this ReadOnlySequence<char> sequence, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (sequence.Length < value.Length)
				return -1;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.IndexOf(value, comparisonType);

			var Head = value[0];
			var Tail = value.Slice(1);
			var Offset = 0L;

			var Enumerator = sequence.GetEnumerator();

			while (Enumerator.MoveNext())
			{
				// Can we find the head?
				for (int Index = Enumerator.Current.Span.IndexOf(Head), LastIndex = 0; Index > LastIndex; Index += Enumerator.Current.Span.Slice(Index + 1).IndexOf(Head) + 1)
				{
					LastIndex = Index;
					var SegmentSpace = Enumerator.Current.Span.Slice(Index + 1);

					// Head found, does the tail follow?
					var SearchSpace = SegmentSpace;
					var ValueSpace = Tail;

					// Copy the enumerator
					for (var InnerEnumerator = Enumerator; ;)
					{
						var Length = Math.Min(SearchSpace.Length, ValueSpace.Length);

						if (!SearchSpace.Slice(0, Length).SequenceEqual(ValueSpace.Slice(0, Length), comparisonType))
							break; // Segment does not match

						if (Length == ValueSpace.Length)
							return Offset + Index; // We matched the entire value

						// We've matched part of the value in this segment, check the next one
						if (!InnerEnumerator.MoveNext())
							return -1; // There is no next segment, so we find nothing

						SearchSpace = InnerEnumerator.Current.Span;
						ValueSpace = ValueSpace.Slice(Length);
					}

					// Tail does not follow, search the remaining space
				}

				Offset += Enumerator.Current.Length;
			}

			return -1;
		}

		/// <summary>
		/// Searches for the occurrence of a span within a sequence
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <returns>The index of the first occurrence of the span, or -1 if it was not found</returns>
		public static long IndexOf<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (sequence.Length < value.Length)
				return -1;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.IndexOf(value);

			var Head = value[0];
			var Tail = value.Slice(1);
			var Offset = 0L;

			var Enumerator = sequence.GetEnumerator();

			while (Enumerator.MoveNext())
			{
				// Can we find the head?
				for (int Index = Enumerator.Current.Span.IndexOf(Head), LastIndex = 0; Index > LastIndex; Index += Enumerator.Current.Span.Slice(Index + 1).IndexOf(Head) + 1)
				{
					LastIndex = Index;
					var SegmentSpace = Enumerator.Current.Span.Slice(Index + 1);

					// Head found, does the tail follow?
					var SearchSpace = SegmentSpace;
					var ValueSpace = Tail;

					// Copy the enumerator
					for (var InnerEnumerator = Enumerator; ;)
					{
						var Length = Math.Min(SearchSpace.Length, ValueSpace.Length);

						if (!SearchSpace.Slice(0, Length).SequenceEqual(ValueSpace.Slice(0, Length)))
							break; // Segment does not match

						if (Length == ValueSpace.Length)
							return Offset + Index; // We matched the entire value

						// We've matched part of the value in this segment, check the next one
						if (!InnerEnumerator.MoveNext())
							return -1; // There is no next segment, so we find nothing

						SearchSpace = InnerEnumerator.Current.Span;
						ValueSpace = ValueSpace.Slice(Length);
					}

					// Tail does not follow, search the remaining space
				}

				Offset += Enumerator.Current.Length;
			}

			return -1;
		}

		/// <summary>
		/// Searches for the occurrence of a value within a sequence
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="values">The values to search for</param>
		/// <returns>The index of the first occurrence of the value, or -1 if it was not found</returns>
		public static long IndexOfAny<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> values) where T : IEquatable<T>
		{
			if (sequence.Length == 0)
				return -1;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.IndexOfAny(values);

			var Offset = 0L;

			foreach (var Segment in sequence)
			{
				var Index = Segment.Span.IndexOfAny(values);

				if (Index != -1)
					return Offset + Index;

				Offset += Segment.Length;
			}

			return -1;
		}

		/// <summary>
		/// Searches for the first occurrence of a span within a sequence
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <returns>The position of the first occurrence of the span, or null if it was not found</returns>
		public static SequencePosition? PositionOf<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (value.Length == 0)
				throw new ArgumentException(nameof(value));

			if (sequence.Length < value.Length)
				return null;

			var Head = value[0];

			for (var Position = sequence.PositionOf(Head); Position.HasValue; Position = sequence.Slice(1).PositionOf(Head))
			{
				// Move the sequence up to our match
				sequence = sequence.Slice(Position.Value);

				if (sequence.StartsWith(value))
					return Position;

				// No, so we advance by one and look for another match
			}

			return null;
		}

		/// <summary>
		/// Searches for the first occurrence of a value within a sequence
		/// </summary>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The position of the first occurrence of the value, or null if it was not found</returns>
		public static SequencePosition? PositionOf(this ReadOnlySequence<char> sequence, char value, StringComparison comparisonType)
		{
			if (sequence.IsSingleSegment)
			{
				var Index = sequence.First.Span.IndexOf(value, comparisonType);

				if (Index == -1)
					return null;

				return sequence.GetPosition(Index);
			}

			var Position = sequence.Start;
			var LastPosition = Position;

			while (sequence.TryGet(ref Position, out var Memory))
			{
				var Index = Memory.Span.IndexOf(value, comparisonType);

				if (Index != -1)
					return sequence.GetPosition(Index, LastPosition);

				if (Position.GetObject() == null)
					break;

				LastPosition = Position;
			}

			return null;
		}

		/// <summary>
		/// Searches for the first occurrence of a span within a sequence
		/// </summary>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The position of the first occurrence of the span, or null if it was not found</returns>
		public static SequencePosition? PositionOf(this ReadOnlySequence<char> sequence, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (value.Length == 0)
				throw new ArgumentException(nameof(value));

			if (sequence.Length < value.Length)
				return null;

			var Head = value[0];

			for (var Position = sequence.PositionOf(Head, comparisonType); Position.HasValue; Position = sequence.Slice(1).PositionOf(Head, comparisonType))
			{
				// Move the sequence up to our match
				sequence = sequence.Slice(Position.Value);

				if (sequence.StartsWith(value, comparisonType))
					return Position;

				// No, so we advance by one and look for another match
			}

			return null;
		}

		/// <summary>
		/// Compares the sequence to a span for sorting
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to compare</param>
		/// <param name="value">The span to compare to</param>
		/// <returns>The result of the comparison</returns>
		public static int SequenceCompareTo<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value) where T : IComparable<T>
		{
			if (sequence.IsSingleSegment)
				return sequence.First.Span.SequenceCompareTo(value);

			foreach (var Segment in sequence)
			{
				var Comparison = Segment.Span.SequenceCompareTo(value.Slice(0, Segment.Length));

				if (Comparison != 0)
					return Comparison;

				value = value.Slice(Segment.Length);
			}

			return 0;
		}

		/// <summary>
		/// Compares the sequence to a span for sorting
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to compare</param>
		/// <param name="value">The span to compare to</param>
		/// <param name="comparer">A comparer to use</param>
		/// <returns>The result of the comparison</returns>
		public static int SequenceCompareTo<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value, IComparer<T> comparer)
		{
			if (sequence.IsSingleSegment)
				return sequence.First.Span.SequenceCompareTo(value, comparer);

			foreach (var Segment in sequence)
			{
				var Comparison = Segment.Span.SequenceCompareTo(value.Slice(0, Segment.Length), comparer);

				if (Comparison != 0)
					return Comparison;

				value = value.Slice(Segment.Length);
			}

			return 0;
		}

		/// <summary>
		/// Compares the sequence to a span for equality
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to compare</param>
		/// <param name="value">The span to compare to</param>
		/// <returns>True if the two sequences match, otherwise false</returns>
		public static bool SequenceEqual<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (sequence.Length != value.Length)
				return false;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.SequenceEqual(value);

			foreach (var MySegment in sequence)
			{
				if (!MySegment.Span.SequenceEqual(value.Slice(0, MySegment.Length)))
					return false;

				value = value.Slice(MySegment.Length);
			}

			return true;
		}

		/// <summary>
		/// Compares the sequence to a span for equality
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to compare</param>
		/// <param name="value">The span to compare to</param>
		/// <param name="comparer">The comparer to use</param>
		/// <returns>True if the two sequences match, otherwise false</returns>
		public static bool SequenceEqual<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value, IEqualityComparer<T> comparer)
		{
			if (sequence.Length != value.Length)
				return false;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.SequenceEqual(value, comparer);

			foreach (var Segment in sequence)
			{
				if (!Segment.Span.SequenceEqual(value.Slice(0, Segment.Length), comparer))
					return false;

				value = value.Slice(Segment.Length);
			}

			return true;
		}

		/// <summary>
		/// Compares the character sequence to a span
		/// </summary>
		/// <param name="sequence">The sequence to compare</param>
		/// <param name="value">The span to compare to</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>True if the two sequences match, otherwise false</returns>
		public static bool SequenceEqual(this ReadOnlySequence<char> sequence, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (sequence.IsSingleSegment)
				return sequence.First.Span.SequenceEqual(value, comparisonType);

			if (sequence.Length != value.Length)
				return false;

			foreach (var MySegment in sequence)
			{
				var CompareLength = Math.Min(MySegment.Length, value.Length);

				if (!MySegment.Span.SequenceEqual(value.Slice(0, CompareLength), comparisonType))
					return false;

				value = value.Slice(CompareLength);
			}

			return true;
		}

		/// <summary>
		/// Searches for the occurrence of a span within a sequence, and returns everything before
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <returns>The sequence up to the first occurrence of the span, or the entire sequence if it was not found</returns>
		public static ReadOnlySequence<T> SliceTo<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			var Offset = sequence.PositionOf(value);

			return Offset == null ? sequence : sequence.Slice(sequence.Start, Offset.Value);
		}

		/// <summary>
		/// Searches for the occurrence of a span within a sequence, and returns everything before
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <returns>The sequence up to the first occurrence of the span, or the entire sequence if it was not found</returns>
		public static ReadOnlySequence<T> SliceTo<T>(this ReadOnlySequence<T> sequence, T value) where T : IEquatable<T>
		{
			var Offset = sequence.PositionOf(value);

			return Offset == null ? sequence : sequence.Slice(sequence.Start, Offset.Value);
		}

		/// <summary>
		/// Searches for the occurrence of a span within a character sequence, and returns everything before
		/// </summary>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The sequence up to the first occurrence of the span, or the entire sequence if it was not found</returns>
		public static ReadOnlySequence<char> SliceTo(this ReadOnlySequence<char> sequence, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			var Offset = sequence.PositionOf(value, comparisonType);

			return Offset == null ? sequence : sequence.Slice(sequence.Start, Offset.Value);
		}

		/// <summary>
		/// Searches for the occurrence of a character within a character sequence, and returns everything before
		/// </summary>
		/// <param name="sequence">The sequence to search</param>
		/// <param name="value">The value to search for</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>The sequence up to the first occurrence of the span, or the entire sequence if it was not found</returns>
		public static ReadOnlySequence<char> SliceTo(this ReadOnlySequence<char> sequence, char value, StringComparison comparisonType)
		{
			var Offset = sequence.PositionOf(value, comparisonType);

			return Offset == null ? sequence : sequence.Slice(sequence.Start, Offset.Value);
		}

		/// <summary>
		/// Compares the start of a byte sequence against a character span
		/// </summary>
		/// <param name="sequence">The sequence to check</param>
		/// <param name="value">The value to check against</param>
		/// <param name="encoding">The character encoding to use</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>True if the sequence starts with the span, otherwise False</returns>
		public static bool StartsWith(this ReadOnlySequence<byte> sequence, ReadOnlySpan<char> value, Encoding encoding, StringComparison comparisonType)
		{
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			if (sequence.Length < value.Length)
				return false;

			if (sequence.IsSingleSegment)
				return sequence.First.StartsWith(value, encoding, comparisonType);

			// What's the maximum number of bytes that can make up a character string of this length?
			var MaxLength = encoding.GetMaxByteCount(value.Length);

			// We need to decode enough to guarantee we will make the input string, no matter how the comparison works
			if (sequence.Length > MaxLength)
				sequence = sequence.Slice(0, MaxLength);

			// If we only care about enough bytes to consume a single segment, we can just use the span comparison
			if (sequence.IsSingleSegment)
				return sequence.First.StartsWith(value, encoding, comparisonType);

			var OutBuffer = ArrayPool<char>.Shared.Rent((int)sequence.Length);

			try
			{
				// Decode only as much as we need to verify the value
				var OutChars = encoding.GetChars(sequence, OutBuffer);

				return new ReadOnlySpan<char>(OutBuffer, 0, OutChars).StartsWith(value, comparisonType);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(OutBuffer);
			}
		}

		/// <summary>
		/// Compares the start of a character sequence against a character span
		/// </summary>
		/// <param name="sequence">The sequence to check</param>
		/// <param name="value">The value to check against</param>
		/// <param name="comparisonType">The string comparison to use</param>
		/// <returns>True if the sequence starts with the span, otherwise False</returns>
		public static bool StartsWith(this ReadOnlySequence<char> sequence, ReadOnlySpan<char> value, StringComparison comparisonType)
		{
			if (sequence.IsSingleSegment)
				return sequence.First.Span.StartsWith(value, comparisonType);

			if (sequence.Length < value.Length)
				return false;

			foreach (var MySegment in sequence)
			{
				var CompareLength = Math.Min(MySegment.Length, value.Length);

				if (!MySegment.Span.StartsWith(value.Slice(0, CompareLength), comparisonType))
					return false;

				value = value.Slice(CompareLength);

				if (value.IsEmpty)
					break;
			}

			return true;
		}

		/// <summary>
		/// Compares the start of a sequence against a span
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to check</param>
		/// <param name="value">The value to check against</param>
		/// <returns>True if the sequence starts with the span, otherwise False</returns>
		public static bool StartsWith<T>(this ReadOnlySequence<T> sequence, ReadOnlySpan<T> value) where T : IEquatable<T>
		{
			if (sequence.Length < value.Length)
				return false;

			if (sequence.IsSingleSegment)
				return sequence.First.Span.StartsWith(value);

			foreach (var MySegment in sequence)
			{
				var CompareLength = Math.Min(MySegment.Length, value.Length);

				if (!MySegment.Span.StartsWith(value.Slice(0, CompareLength)))
					return false;

				value = value.Slice(CompareLength);

				if (value.IsEmpty)
					break;
			}

			return true;
		}

		/// <summary>
		/// Decodes a byte sequence as a string
		/// </summary>
		/// <param name="sequence">The byte sequence to encode</param>
		/// <param name="encoding">The encoding to use</param>
		/// <returns>The decoded contents of the sequence</returns>
		public static string ToString(this ReadOnlySequence<byte> sequence, Encoding encoding)
		{
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			if (sequence.IsSingleSegment)
				return encoding.GetString(sequence.First.Span);

			return ToStringBuilder(sequence, encoding).ToString();
		}

		/// <summary>
		/// Converts a char sequence into a StringBuilder
		/// </summary>
		/// <param name="sequence">The char sequence to encode</param>
		/// <returns>The converted contents of the sequence</returns>
		public static StringBuilder ToStringBuilder(this ReadOnlySequence<char> sequence) => new StringBuilder((int)sequence.Length).Append(sequence);

		/// <summary>
		/// Decodes a byte sequence into a <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="sequence">The byte sequence to encode</param>
		/// <param name="encoding">The encoding to use</param>
		/// <returns>The decoded contents of the sequence in a <see cref="StringBuilder"/></returns>
		public static StringBuilder ToStringBuilder(this ReadOnlySequence<byte> sequence, Encoding encoding)
		{
			if (encoding == null)
				throw new ArgumentNullException(nameof(encoding));

			return new StringBuilder(encoding.GetMaxCharCount((int)sequence.Length)).Append(sequence, encoding);
		}

		//****************************************

		private sealed class CombinedSegment<T> : ReadOnlySequenceSegment<T>
		{
			internal CombinedSegment(ReadOnlyMemory<T> memory)
			{
				Memory = memory;
			}

			internal CombinedSegment(CombinedSegment<T> previous, ReadOnlyMemory<T> memory)
			{
				Memory = memory;
				RunningIndex = previous.RunningIndex + previous.Memory.Length;
				previous.Next = this;
			}
		}
	}
}
