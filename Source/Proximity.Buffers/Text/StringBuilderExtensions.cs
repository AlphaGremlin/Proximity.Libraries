using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.Text
{
	/// <summary>
	/// Provides useful extensions for <see cref="StringBuilder"/>
	/// </summary>
	public static class StringBuilderExtensions
	{
#if NETSTANDARD2_0
		/// <summary>
		/// Appends a character span to a StringBuilder
		/// </summary>
		/// <param name="builder">The StringBuilder to write to</param>
		/// <param name="content">The char sequence to append</param>
		/// <returns>The target StringBuilder</returns>
		public static StringBuilder Append(this StringBuilder builder, ReadOnlySpan<char> content)
		{
			if (content.IsEmpty)
				return builder;

			var OutBuffer = ArrayPool<char>.Shared.Rent(1024 * 2);

			try
			{
				while (!content.IsEmpty)
				{
					var MaxCopy = Math.Min(content.Length, OutBuffer.Length);

					content.Slice(0, MaxCopy).CopyTo(OutBuffer);

					builder.Append(OutBuffer, 0, MaxCopy);

					content = content.Slice(MaxCopy);
				}
			}
			finally
			{
				ArrayPool<char>.Shared.Return(OutBuffer);
			}

			return builder;
		}
#endif

		/// <summary>
		/// Appends a char sequence to a StringBuilder
		/// </summary>
		/// <param name="builder">The StringBuilder to write to</param>
		/// <param name="sequence">The char sequence to encode</param>
		/// <returns>The target StringBuilder</returns>
		public static StringBuilder Append(this StringBuilder builder, ReadOnlySequence<char> sequence)
		{
			foreach (var MySegment in sequence)
				builder.Append(MySegment.Span);

			return builder;
		}

		/// <summary>
		/// Decodes a byte sequence and appends it to a <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="builder">The StringBuilder to write to</param>
		/// <param name="sequence">The byte sequence to encode</param>
		/// <param name="encoding">The encoding to use</param>
		/// <returns>The decoded contents of the sequence in a <see cref="StringBuilder"/></returns>
		public static StringBuilder Append(this StringBuilder builder, ReadOnlySequence<byte> sequence, Encoding encoding)
		{
			var RemainingBytes = sequence.Length;
			char[]? OutBuffer = null;

			try
			{
				OutBuffer = ArrayPool<char>.Shared.Rent(1024 * 2);

				var Decoder = encoding.GetDecoder();

				foreach (var MySegment in sequence)
				{
					var InBuffer = MySegment.Span;
					bool IsCompleted;

					do
					{
						// Decode the bytes into our char array
						Decoder.Convert(
							InBuffer,
							OutBuffer,
							RemainingBytes == InBuffer.Length,
							out var BytesRead, out var WrittenChars, out IsCompleted
							);

						builder.Append(OutBuffer, 0, WrittenChars);

						RemainingBytes -= BytesRead;

						InBuffer = InBuffer.Slice(BytesRead);

						// Loop while there are more bytes unread, or there are no bytes left but there's still data to flush
					}
					while (!InBuffer.IsEmpty || (RemainingBytes == 0 && !IsCompleted));
				}
			}
			finally
			{
				if (OutBuffer != null)
					ArrayPool<char>.Shared.Return(OutBuffer);
			}

			return builder;
		}
	}
}
