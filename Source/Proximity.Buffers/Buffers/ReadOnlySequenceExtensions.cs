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

			return new ReadOnlySequence<T>(StartSegment, 0, CurrentSegment, CurrentSegment.Memory.Length);
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

			return new ReadOnlySequence<T>(StartSegment, 0, CurrentSegment, CurrentSegment.Memory.Length);
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

			return new ReadOnlySequence<T>(StartSegment, 0, CurrentSegment, CurrentSegment.Memory.Length);
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

			return new ReadOnlySequence<char>(StartSegment, 0, CurrentSegment, CurrentSegment!.Memory.Length);
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

			return new ReadOnlySequence<byte>(StartSegment, 0, CurrentSegment, CurrentSegment!.Memory.Length);
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
		/// Compares the sequence to a span
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

			foreach (var MySegment in sequence)
			{
				var CompareLength = Math.Min(MySegment.Length, value.Length);

				if (!MySegment.StartsWith(value.Slice(0, CompareLength), encoding, comparisonType))
					return false;

				value = value.Slice(CompareLength);

				if (value.IsEmpty)
					break;
			}

			return true;
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

		/// <summary>
		/// Splits a sequence based on a separator without allocations
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to split</param>
		/// <param name="separator">The separator to split on</param>
		/// <param name="omitEmpty">Whether to omit empty items</param>
		/// <returns>An enumerable that returns multiple sequences based on the split</returns>
		public static SplitSingleEnumerator<T> Split<T>(this ReadOnlySpan<T> sequence, T separator, bool omitEmpty = false) where T : IEquatable<T> => new SplitSingleEnumerator<T>(sequence, separator, omitEmpty);

		/// <summary>
		/// Splits a sequence based on a separator without allocations
		/// </summary>
		/// <typeparam name="T">The element type in the sequence</typeparam>
		/// <param name="sequence">The sequence to split</param>
		/// <param name="separator">The separator to split on</param>
		/// <param name="omitEmpty">Whether to omit empty items</param>
		/// <returns>An enumerable that returns multiple sequences based on the split</returns>
		public static SplitEnumerator<T> Split<T>(this ReadOnlySpan<T> sequence, ReadOnlySpan<T> separator, bool omitEmpty = false) where T : IEquatable<T> => new SplitEnumerator<T>(sequence, separator, omitEmpty);

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
				_Enumerator = new SplitEnumerator<T>(_Enumerator.Sequence, MemoryMarshal.CreateSpan(ref _Separator, 1), _Enumerator.OmitEmpty);

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
