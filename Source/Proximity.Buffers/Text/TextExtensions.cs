using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.Text
{
	/// <summary>
	/// Provides some useful extensions related to Encoding and Decoding
	/// </summary>
	public static class TextExtensions
	{
#if NETSTANDARD2_0
		/// <summary>
		/// Encodes a block of input characters to bytes
		/// </summary>
		/// <param name="encoder">The encoder performing the encoding operation</param>
		/// <param name="input">The input characters</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the encoder, otherwise False</param>
		/// <param name="charsRead">Receives the number of characters read</param>
		/// <param name="bytesWritten">Receives the number of bytes written</param>
		/// <param name="isCompleted">Receives whether all output bytes have been written</param>
		public static unsafe void Convert(this Encoder encoder, ReadOnlySpan<char> input, Span<byte> output, bool flush, out int charsRead, out int bytesWritten, out bool isCompleted)
		{
			fixed (char* pInput = &MemoryMarshal.GetReference(input))
			fixed (byte* pOutput = &MemoryMarshal.GetReference(output))
				encoder.Convert(pInput, input.Length, pOutput, output.Length, flush, out charsRead, out bytesWritten, out isCompleted);
		}

		/// <summary>
		/// Decodes a block of input bytes into characters
		/// </summary>
		/// <param name="decoder">The decoder performing the decoding operation</param>
		/// <param name="input">The input bytes</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the decoder, otherwise False</param>
		/// <param name="bytesRead">Receives the number of bytes read</param>
		/// <param name="charsWritten">Receives the number of characters written</param>
		/// <param name="isCompleted">Receives whether all output characters have been written</param>
		public static unsafe void Convert(this Decoder decoder, ReadOnlySpan<byte> input, Span<char> output, bool flush, out int bytesRead, out int charsWritten, out bool isCompleted)
		{
			fixed (byte* pInput = &MemoryMarshal.GetReference(input))
			fixed (char* pOutput = &MemoryMarshal.GetReference(output))
				decoder.Convert(pInput, input.Length, pOutput, output.Length, flush, out bytesRead, out charsWritten, out isCompleted);
		}

		/// <summary>
		/// Determines the number of bytes that result from an encoding operation
		/// </summary>
		/// <param name="encoder">The encoder performing the encoding operation</param>
		/// <param name="span">The input characters</param>
		/// <param name="flush">True to flush the encoder, otherwise False</param>
		/// <returns>The number of bytes that would be output</returns>
		public static unsafe int GetByteCount(this Encoder encoder, ReadOnlySpan<char> span, bool flush)
		{
			fixed (char* pSpan = &MemoryMarshal.GetReference(span))
				return encoder.GetByteCount(pSpan, span.Length, flush);
		}

		/// <summary>
		/// Determines the number of bytes that result from an encoding operation
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="span">The input characters</param>
		/// <returns>The number of bytes that would be output</returns>
		public static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> span)
		{
			fixed (char* pSpan = &MemoryMarshal.GetReference(span))
				return encoding.GetByteCount(pSpan, span.Length);
		}

		/// <summary>
		/// Gets the encoded bytes as a Memory from a character Span
		/// </summary>
		/// <param name="encoder">The encoder performing the encoding operation</param>
		/// <param name="input">The input characters</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the encoder, otherwise False</param>
		/// <returns>The number of bytes written</returns>
		public static unsafe int GetBytes(this Encoder encoder, ReadOnlySpan<char> input, Span<byte> output, bool flush)
		{
			fixed (char* pInput = &MemoryMarshal.GetReference(input))
			fixed (byte* pOutput = &MemoryMarshal.GetReference(output))
				return encoder.GetBytes(pInput, input.Length, pOutput, output.Length, flush);
		}

		/// <summary>
		/// Gets the encoded bytes as a Memory from a character Span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input characters</param>
		/// <param name="output">The span that receives the output</param>
		/// <returns>The number of bytes written</returns>
		public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> input, Span<byte> output)
		{
			fixed (char* pInput = &MemoryMarshal.GetReference(input))
			fixed (byte* pOutput = &MemoryMarshal.GetReference(output))
				return encoding.GetBytes(pInput, input.Length, pOutput, output.Length);
		}
		
		/// <summary>
		/// Gets the encoded bytes as a Memory from a character Span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input characters</param>
		/// <returns>The bytes that make up the encoded value</returns>
		public static unsafe Memory<byte> GetBytes(this Encoding encoding, ReadOnlySpan<char> input)
		{
			var Output = new byte[encoding.GetByteCount(input)];

			fixed (char* pInput = &MemoryMarshal.GetReference(input))
			fixed (byte* pOutput = &MemoryMarshal.GetReference(Output.AsSpan()))
				return new Memory<byte>(Output, 0, encoding.GetBytes(pInput, input.Length, pOutput, Output.Length));
		}

		/// <summary>
		/// Determines the number of characters that result from a decoding operation
		/// </summary>
		/// <param name="decoder">The decoder performing the decoding operation</param>
		/// <param name="span">The input bytes</param>
		/// <param name="flush">True to flush the decoder, otherwise False</param>
		/// <returns>The number of characters that would be output</returns>
		public static unsafe int GetCharCount(this Decoder decoder, ReadOnlySpan<byte> span, bool flush)
		{
			fixed (byte* pSpan = &MemoryMarshal.GetReference(span))
				return decoder.GetCharCount(pSpan, span.Length, flush);
		}

		/// <summary>
		/// Determines the number of characters that result from a decoding operation
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="span">The input bytes</param>
		/// <returns>The number of characters that would be output</returns>
		public static unsafe int GetCharCount(this Encoding encoding, ReadOnlySpan<byte> span)
		{
			fixed (byte* pSpan = &MemoryMarshal.GetReference(span))
				return encoding.GetCharCount(pSpan, span.Length);
		}

		/// <summary>
		/// Decodes a byte span into a char span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input bytes</param>
		/// <param name="output">The span that receives the output</param>
		/// <returns>The number of chars written</returns>
		public static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> input, Span<char> output)
		{
			fixed (byte* pInput = &MemoryMarshal.GetReference(input))
			fixed (char* pOutput = &MemoryMarshal.GetReference(output))
				return encoding.GetChars(pInput, input.Length, pOutput, output.Length);
		}
		
		/// <summary>
		/// Gets the encoded characters as a Memory from a byte Span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input bytes</param>
		/// <returns>The characters that make up the text</returns>
		public static unsafe Memory<char> GetChars(this Encoding encoding, ReadOnlySpan<byte> input)
		{
			var Output = new char[encoding.GetCharCount(input)];

			fixed (byte* pInput = &MemoryMarshal.GetReference(input))
			fixed (char* pOutput = &MemoryMarshal.GetReference(Output.AsSpan()))
				return new Memory<char>(Output, 0, encoding.GetChars(pInput, input.Length, pOutput, Output.Length));
		}

		/// <summary>
		/// Decodes a byte span into a char span
		/// </summary>
		/// <param name="decoder">The decoder performing the decoding operation</param>
		/// <param name="input">The input bytes</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the decoder, otherwise False</param>
		/// <returns>The number of chars written</returns>
		public static unsafe int GetChars(this Decoder decoder, ReadOnlySpan<byte> input, Span<char> output, bool flush)
		{
			fixed (byte* pInput = &MemoryMarshal.GetReference(input))
			fixed (char* pOutput = &MemoryMarshal.GetReference(output))
				return decoder.GetChars(pInput, input.Length, pOutput, output.Length, flush);
		}

		/// <summary>
		/// Decodes a byte span into a string
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="span">The source byte span</param>
		/// <returns>The decoded string</returns>
		public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> span)
		{
			fixed (byte* pSpan = &MemoryMarshal.GetReference(span))
				return encoding.GetString(pSpan, span.Length);
		}
#else
		/// <summary>
		/// Encodes a block of input characters to bytes
		/// </summary>
		/// <param name="encoder">The encoder performing the encoding operation</param>
		/// <param name="input">The input characters</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the encoder, otherwise False</param>
		/// <param name="charsRead">Receives the number of characters read</param>
		/// <param name="bytesWritten">Receives the number of bytes written</param>
		/// <param name="isCompleted">Receives whether all output bytes have been written</param>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Convert(Encoder encoder, ReadOnlySpan<char> input, Span<byte> output, bool flush, out int charsRead, out int bytesWritten, out bool isCompleted) => encoder.Convert(input, output, flush, out charsRead, out bytesWritten, out isCompleted);

		/// <summary>
		/// Decodes a block of input bytes into characters
		/// </summary>
		/// <param name="decoder">The decoder performing the decoding operation</param>
		/// <param name="input">The input bytes</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the decoder, otherwise False</param>
		/// <param name="bytesRead">Receives the number of bytes read</param>
		/// <param name="charsWritten">Receives the number of characters written</param>
		/// <param name="isCompleted">Receives whether all output characters have been written</param>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Convert(Decoder decoder, ReadOnlySpan<byte> input, Span<char> output, bool flush, out int bytesRead, out int charsWritten, out bool isCompleted) => decoder.Convert(input, output, flush, out bytesRead, out charsWritten, out isCompleted);

		/// <summary>
		/// Determines the number of bytes that result from an encoding operation
		/// </summary>
		/// <param name="encoder">The encoder performing the encoding operation</param>
		/// <param name="span">The input characters</param>
		/// <param name="flush">True to flush the encoder, otherwise False</param>
		/// <returns>The number of bytes that would be output</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetByteCount(Encoder encoder, ReadOnlySpan<char> span, bool flush) => encoder.GetByteCount(span, flush);

		/// <summary>
		/// Determines the number of bytes that result from an encoding operation
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="span">The input characters</param>
		/// <returns>The number of bytes that would be output</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetByteCount(Encoding encoding, ReadOnlySpan<char> span) => encoding.GetByteCount(span);

		/// <summary>
		/// Gets the encoded bytes as a Memory from a character Span
		/// </summary>
		/// <param name="encoder">The encoder performing the encoding operation</param>
		/// <param name="input">The input characters</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the encoder, otherwise False</param>
		/// <returns>The number of bytes written</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetBytes(Encoder encoder, ReadOnlySpan<char> input, Span<byte> output, bool flush) => encoder.GetBytes(input, output, flush);

		/// <summary>
		/// Gets the encoded bytes as a Memory from a character Span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input characters</param>
		/// <param name="output">The span that receives the output</param>
		/// <returns>The number of bytes written</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetBytes(Encoding encoding, ReadOnlySpan<char> input, Span<byte> output) => encoding.GetBytes(input, output);

		/// <summary>
		/// Gets the encoded bytes as a Memory from a character Span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input characters</param>
		/// <returns>The bytes that make up the encoded value</returns>
		public static Memory<byte> GetBytes(this Encoding encoding, ReadOnlySpan<char> input)
		{
			var Output = new byte[encoding.GetByteCount(input)];

			return new Memory<byte>(Output, 0, encoding.GetBytes(input, Output));
		}

		/// <summary>
		/// Determines the number of characters that result from a decoding operation
		/// </summary>
		/// <param name="decoder">The decoder performing the decoding operation</param>
		/// <param name="span">The input bytes</param>
		/// <param name="flush">True to flush the decoder, otherwise False</param>
		/// <returns>The number of characters that would be output</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetCharCount(Decoder decoder, ReadOnlySpan<byte> span, bool flush) => decoder.GetCharCount(span, flush);

		/// <summary>
		/// Determines the number of characters that result from a decoding operation
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="span">The input bytes</param>
		/// <returns>The number of characters that would be output</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetCharCount(Encoding encoding, ReadOnlySpan<byte> span) => encoding.GetCharCount(span);

		/// <summary>
		/// Decodes a byte span into a char span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input bytes</param>
		/// <param name="output">The span that receives the output</param>
		/// <returns>The number of chars written</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetChars(Encoding encoding, ReadOnlySpan<byte> input, Span<char> output) => encoding.GetChars(input, output);

		/// <summary>
		/// Gets the encoded characters as a Memory from a byte Span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The input bytes</param>
		/// <returns>The characters that make up the text</returns>
		public static Memory<char> GetChars(this Encoding encoding, ReadOnlySpan<byte> input)
		{
			var Output = new char[encoding.GetCharCount(input)];

			return new Memory<char>(Output, 0, encoding.GetChars(input, Output));
		}

		/// <summary>
		/// Decodes a byte span into a char span
		/// </summary>
		/// <param name="decoder">The decoder performing the decoding operation</param>
		/// <param name="input">The input bytes</param>
		/// <param name="output">The span that receives the output</param>
		/// <param name="flush">True to flush the decoder, otherwise False</param>
		/// <returns>The number of chars written</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetChars(Decoder decoder, ReadOnlySpan<byte> input, Span<char> output, bool flush) => decoder.GetChars(input, output, flush);

		/// <summary>
		/// Decodes a byte span into a string
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="span">The source byte span</param>
		/// <returns>The decoded string</returns>
		[EditorBrowsable(EditorBrowsableState.Never), MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string GetString(Encoding encoding, ReadOnlySpan<byte> span) => encoding.GetString(span);
#endif

		/// <summary>
		/// Encodes a char sequence and writes it to a span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The ReadOnlySequence to read from</param>
		/// <param name="output">The span to write to</param>
		/// <returns>The number of bytes written to <paramref name="output"/></returns>
		public static int GetBytes(this Encoding encoding, ReadOnlySequence<char> input, Span<byte> output)
		{
			var RemainingChars = input.Length;
			var BytesUsed = 0;

			var Encoder = encoding.GetEncoder();

			foreach (var MySegment in input)
			{
				var InBuffer = MySegment.Span;
				bool IsCompleted;

				do
				{
					// Encode the chars into our byte array
					Encoder.Convert(
						InBuffer,
						output,
						RemainingChars == InBuffer.Length,
						out var CharsRead, out var WrittenBytes, out IsCompleted
						);

					BytesUsed += WrittenBytes;
					output = output.Slice(WrittenBytes);

					if (output.IsEmpty)
						return BytesUsed;

					RemainingChars -= CharsRead;

					InBuffer = InBuffer.Slice(CharsRead);

					// Loop while there are more chars unread, or there are no chars left but there's still data to flush
				}
				while (!InBuffer.IsEmpty || (RemainingChars == 0 && !IsCompleted));
			}

			return BytesUsed;
		}

		/// <summary>
		/// Decodes a byte sequence and writes it to a span
		/// </summary>
		/// <param name="encoding">The encoding to use</param>
		/// <param name="input">The ReadOnlySequence to read from</param>
		/// <param name="output">The span to write to</param>
		/// <returns>The number of characters written to <paramref name="output"/></returns>
		public static int GetChars(this Encoding encoding, ReadOnlySequence<byte> input, Span<char> output)
		{
			var RemainingBytes = input.Length;
			var CharsUsed = 0;

			var Decoder = encoding.GetDecoder();

			foreach (var MySegment in input)
			{
				var InBuffer = MySegment.Span;
				bool IsCompleted;

				do
				{
					// Decode the bytes into our char array
					Decoder.Convert(
						InBuffer,
						output,
						RemainingBytes == InBuffer.Length,
						out var BytesRead, out var WrittenChars, out IsCompleted
						);

					CharsUsed += WrittenChars;
					output = output.Slice(WrittenChars);

					if (output.IsEmpty)
						return CharsUsed;

					RemainingBytes -= BytesRead;

					InBuffer = InBuffer.Slice(BytesRead);

					// Loop while there are more bytes unread, or there are no bytes left but there's still data to flush
				}
				while (!InBuffer.IsEmpty || (RemainingBytes == 0 && !IsCompleted));
			}

			return CharsUsed;
		}
	}
}
