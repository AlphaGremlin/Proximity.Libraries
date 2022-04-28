using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace System.IO
{
	/// <summary>
	/// Implements a TextReader that works directly from a byte sequence, rather than requiring a MemoryStream wrapper
	/// </summary>
	public sealed class BinaryTextReader : TextReader, IBufferReader<char>
	{ //****************************************
		private const int MinBufferSize = 128;
		private const int DefaultBufferSize = 1024;
		//****************************************
		private readonly IBufferReader<byte> _Reader;
		private long _Position;

		private Decoder _Decoder;
		private char[] _Buffer;
		private int _BufferIndex, _BufferLength;
		private readonly int _BufferSize;

		private bool _DetectEncoding, _CheckPreamble;
		private byte[] _Preamble;
		//****************************************

		/// <summary>
		/// Creates a new Binary Text Reader with auto-detected encoding
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		public BinaryTextReader(ArraySegment<byte> array) : this(new ReadOnlySequence<byte>(array))
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding) : this(new ReadOnlySequence<byte>(array), encoding)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding, bool detectEncoding) : this(new ReadOnlySequence<byte>(array), encoding, detectEncoding)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding, bool detectEncoding, int bufferSize) : this(new ReadOnlySequence<byte>(array), encoding, detectEncoding, bufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader with auto-detected encoding
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		public BinaryTextReader(byte[] array) : this(new ReadOnlySequence<byte>(array))
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(byte[] array, Encoding encoding) : this(new ReadOnlySequence<byte>(array), encoding)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		public BinaryTextReader(byte[] array, Encoding encoding, bool detectEncoding) : this(new ReadOnlySequence<byte>(array), encoding, detectEncoding)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(byte[] array, Encoding encoding, bool detectEncoding, int bufferSize) : this(new ReadOnlySequence<byte>(array), encoding, detectEncoding, bufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader with auto-detected encoding
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		public BinaryTextReader(byte[] array, int offset, int length) : this(new ReadOnlySequence<byte>(array, offset, length))
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding) : this(new ReadOnlySequence<byte>(array, offset, length), encoding)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding, bool detectEncoding) : this(new ReadOnlySequence<byte>(array, offset, length), encoding, detectEncoding)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding, bool detectEncoding, int bufferSize) : this(new ReadOnlySequence<byte>(array, offset, length), encoding, detectEncoding, bufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader with auto-detected encoding
		/// </summary>
		/// <param name="sequence">The byte sequence to read from</param>
		public BinaryTextReader(ReadOnlySequence<byte> sequence) : this(sequence, Encoding.UTF8, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="sequence">The byte sequence to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(ReadOnlySequence<byte> sequence, Encoding encoding) : this(sequence, encoding, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="sequence">The byte sequence to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		public BinaryTextReader(ReadOnlySequence<byte> sequence, Encoding encoding, bool detectEncoding) : this(sequence, encoding, detectEncoding, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="sequence">The byte sequence to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(ReadOnlySequence<byte> sequence, Encoding encoding, bool detectEncoding, int bufferSize) : this(new BufferReader<byte>(sequence), encoding, detectEncoding, bufferSize)
		{
			// The BufferReader will never be disposed of, but as long as we use the 0 read, it will never rent any buffers and thus should be safe
		}

		/// <summary>
		/// Creates a new Binary Text Reader with auto-detected encoding
		/// </summary>
		/// <param name="reader">The buffer reader to read from</param>
		public BinaryTextReader(IBufferReader<byte> reader) : this(reader, Encoding.UTF8, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="reader">The buffer reader to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(IBufferReader<byte> reader, Encoding encoding) : this(reader, encoding, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="reader">The buffer reader to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		public BinaryTextReader(IBufferReader<byte> reader, Encoding encoding, bool detectEncoding) : this(reader, encoding, detectEncoding, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="reader">The buffer reader to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(IBufferReader<byte> reader, Encoding encoding, bool detectEncoding, int bufferSize) : base()
		{
			if (bufferSize < MinBufferSize)
				bufferSize = MinBufferSize;

			_Reader = reader;

			Encoding = encoding;
			_Decoder = encoding.GetDecoder();

			_DetectEncoding = detectEncoding;
			_Preamble = encoding.GetPreamble();
			_CheckPreamble = (_Preamble.Length > 0);

			_BufferSize = bufferSize;
			_Buffer = new char[encoding.GetMaxCharCount(bufferSize)];
		}

		//****************************************

		/// <summary>
		/// Advances the reader forward
		/// </summary>
		/// <param name="count">The number of bytes to advance over</param>
		public void Advance(int count)
		{
			if (count < 0 || count > _BufferLength)
				throw new ArgumentOutOfRangeException(nameof(count));

			_BufferIndex += count;
			_BufferLength -= count;
		}

		/// <summary>
		/// Reads data into the buffer from the underlying Reader.
		/// </summary>
		/// <param name="minSize">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer.</returns>
		/// <remarks>Can be less than requested if the reader has ended. When this occurs, subsequent calls should return an empty buffer.</remarks>
		public ReadOnlyMemory<char> GetMemory(int minSize)
		{
			// If we have enough data already to satisfy this request, just return it
			if (_BufferLength >= minSize)
				return _Buffer.AsMemory(_BufferIndex, _BufferLength);

			if (_DetectEncoding)
				DetectEncoding();

			if (_Buffer.Length < minSize)
			{
				// Buffer isn't large enough
				var NewBuffer = new char[minSize];

				if (_BufferLength > 0)
				{
					// Copy any remaining data over
					Array.Copy(_Buffer, _BufferIndex, NewBuffer, 0, _BufferLength);

					_BufferIndex = 0;
					_BufferLength = 0;
				}

				_Buffer = NewBuffer;
			}
			
			// Buffer is now large enough, but there's not enough data inside it
			if (_BufferIndex > 0)
			{
				// Shuffle the remaining down to the start
				if (_BufferLength > 0)
					Array.Copy(_Buffer, _BufferIndex, _Buffer, 0, _BufferLength);

				_BufferIndex = 0;
			}

			// Fill the remaining buffer
			FillWithoutReset();

			return _Buffer.AsMemory(_BufferIndex, _BufferLength);
		}

		/// <summary>
		/// Reads data into the buffer from the underlying Reader.
		/// </summary>
		/// <param name="minSize">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the reader has ended. When this occurs, subsequent calls should return an empty buffer.</remarks>
		public ReadOnlySpan<char> GetSpan(int minSize) => GetMemory(minSize).Span;

		/// <inheritdoc />
		public override int Peek()
		{
			if (_BufferLength == 0 && !FillBuffer())
				return -1;

			return _Buffer[_BufferIndex];
		}

		/// <inheritdoc />
		public override int Read()
		{
			if (_BufferLength == 0 && !FillBuffer())
				return -1;

			var Result = _Buffer[_BufferIndex];

			_BufferIndex++;
			_BufferLength--;

			return Result;
		}

		/// <inheritdoc />
		public override int Read(char[] buffer, int index, int count) => Read(buffer.AsSpan(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			int Read(Span<char> buffer)
		{	//****************************************
			var CharsWritten = 0;
			//****************************************

			if (buffer.IsEmpty)
				return 0;

			// Empty the buffer if there's anything in it
			if (_BufferLength > 0)
			{
				var WriteLength = Math.Min(_BufferLength, buffer.Length);

				_Buffer.AsSpan(_BufferIndex, WriteLength).CopyTo(buffer);

				buffer = buffer.Slice(WriteLength);

				CharsWritten += WriteLength;

				_BufferIndex += WriteLength;
				_BufferLength -= WriteLength;

				if (buffer.IsEmpty)
					return CharsWritten;
			}

			if (_CheckPreamble)
				CheckPreamble();

			if (_DetectEncoding)
				DetectEncoding();

			ReadOnlySpan<byte> InBuffer;
			var OutBuffer = _Buffer.AsSpan(0, _Buffer.Length);
			bool IsCompleted;

			// Keep trying to read until we fill the buffer, or the source reader runs out
			do
			{
				InBuffer = _Reader.GetSpan(0);

				// Decode the bytes into our char buffer
				_Decoder.Convert(
					InBuffer,
					OutBuffer,
					InBuffer.IsEmpty,
					out var BytesRead, out var WrittenChars, out IsCompleted
					);

				var ReadLength = Math.Min(WrittenChars, buffer.Length);

				OutBuffer.Slice(0, ReadLength).CopyTo(buffer);

				buffer = buffer.Slice(ReadLength);

				CharsWritten += ReadLength;
				_Position += BytesRead;

				_Reader.Advance(BytesRead);

				if (buffer.IsEmpty)
				{
					// Buffer is filled. Save any data left over for the next read operation
					_BufferIndex = ReadLength;
					_BufferLength = WrittenChars - ReadLength;

					return CharsWritten;
				}

				// Loop while there are more bytes unread, or there are no bytes left but there's still data to flush
			}
			while (!InBuffer.IsEmpty || !IsCompleted);

			_BufferLength = 0;

			return CharsWritten;
		}

		/// <inheritdoc />
		public override Task<int> ReadAsync(char[] buffer, int index, int count) => Task.FromResult(Read(buffer, index, count));

#if !NETSTANDARD2_0
		/// <inheritdoc />
		public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken token = default) => new(Read(buffer.Span));
#endif

		/// <inheritdoc />
		public override int ReadBlock(char[] buffer, int index, int count) => Read(buffer, index, count);

#if !NETSTANDARD2_0
		/// <inheritdoc />
		public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken token = default) => new(Read(buffer.Span));
#endif

		/// <inheritdoc />
		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) => Task.FromResult(Read(buffer, index, count));

		/// <inheritdoc />
		public override string ReadLine()
		{
			var Builder = new StringBuilder();

			for (; ; )
			{
				// Read more data if available
				if (_BufferLength == 0 && !FillBuffer())
					break; // No data left, return what we've read so far

				var CharIndex = _BufferIndex;
				var EndIndex = _BufferIndex + _BufferLength;

				// Find the next carriage-return or line-feed in the current buffer
				while (CharIndex < EndIndex)
				{
					var MyChar = _Buffer[CharIndex];

					if (MyChar == '\r' || MyChar == '\n')
						break;

					CharIndex++;
				}

				// None? Clear the buffer and keep looking
				if (CharIndex == EndIndex)
				{
					Builder.Append(_Buffer, _BufferIndex, _BufferLength);

					_BufferLength = 0;

					continue;
				}

				var CharsBefore = CharIndex - _BufferIndex;

				// Found a CR or LF. Append what we've read so far, minus the line character
				Builder.Append(_Buffer, _BufferIndex, CharsBefore);

				// If it's a carriage-return, check if the next character is available (might be a CRLF pair, so we need to skip it)
				if (_Buffer[CharIndex] == '\r')
				{
					if (_BufferLength == CharsBefore + 1)
					{
						// Read more data if available
						if (!FillBuffer())
							break; // No data left, return what we've read so far

						CharIndex = 0;
						CharsBefore = 0;
					}
					else
					{
						// Character is available, push up by one
						CharIndex++;
					}

					// Buffer is refilled. If the next char is a line-feed, skip it
					if (_Buffer[CharIndex] == '\n')
						CharsBefore++;
				}

				_BufferIndex += CharsBefore + 1;
				_BufferLength -= CharsBefore + 1;

				break; // We found a new-line, complete
			}

			return Builder.ToString();
		}

		/// <summary>
		/// Attempts to read a line of characters into a buffer
		/// </summary>
		/// <param name="target">The target buffer</param>
		/// <param name="charsWritten">The number of characters written</param>
		/// <returns>True if the end of the line was reached, False if another buffer is required to continue reading into.</returns>
		/// <remarks>Every call will read as many characters from the current line as possible. A result of False means the next call will continue returning more of the same line.</remarks>
		public bool TryReadLine(Span<char> target, out int charsWritten)
		{
			charsWritten = 0;

			for (; ; )
			{
				// Is there enough space for more data?
				if (target.IsEmpty)
					return false;

				// Read more data if available
				if (_BufferLength == 0 && !FillBuffer())
					return true; // No data left, return what we've read so far

				var CharIndex = _BufferIndex;
				var ReadLength = Math.Min(target.Length, _BufferLength);
				var EndIndex = _BufferIndex + ReadLength;

				// Find the next carriage-return or line-feed in the current buffer
				while (CharIndex < EndIndex)
				{
					var MyChar = _Buffer[CharIndex];

					if (MyChar == '\r' || MyChar == '\n')
						break;

					CharIndex++;
				}

				// None? Clear the buffer and keep looking
				if (CharIndex == EndIndex)
				{
					_Buffer.AsSpan(_BufferIndex, ReadLength).CopyTo(target);

					_BufferIndex += ReadLength;
					_BufferLength -= ReadLength;
					target = target.Slice(ReadLength);
					charsWritten += ReadLength;

					continue;
				}

				var CharsBefore = CharIndex - _BufferIndex;

				// Found a CR or LF. Append what we've read so far, minus the line character
				var WriteLength = Math.Min(target.Length, CharsBefore);

				_Buffer.AsSpan(_BufferIndex, WriteLength).CopyTo(target);

				charsWritten += WriteLength;

				// If we weren't able to fit the remainder, abort early
				if (CharsBefore > WriteLength)
				{
					_BufferIndex += WriteLength;
					_BufferLength -= WriteLength;

					return false; // Still more characters to read, so we'll hit the CR or LF again on the next attempt
				}

				// If it's a carriage-return, check if the next character is available (might be a CRLF pair, so we need to skip it)
				if (_Buffer[CharIndex] == '\r')
				{
					if (_BufferLength == CharsBefore + 1)
					{
						// Read more data if available
						if (!FillBuffer())
						{
							_BufferIndex += CharsBefore + 1;
							_BufferLength -= CharsBefore + 1;

							return true; // No data left, return what we've read so far
						}

						CharIndex = 0;
					}
					else
					{
						// Character is available, push up by one
						CharIndex++;
					}

					// Buffer is refilled. If the next char is a line-feed, skip it
					if (_Buffer[CharIndex] == '\n')
						CharsBefore++;
				}

				_BufferIndex += CharsBefore + 1;
				_BufferLength -= CharsBefore + 1;

				return true; // We found a new-line, complete
			}
		}

		/// <inheritdoc />
		public override Task<string?> ReadLineAsync() => Task.FromResult<string?>(ReadLine());

		/// <inheritdoc />
		public override string ReadToEnd()
		{
			var Builder = new StringBuilder();

			if (_BufferLength != 0)
			{
				Builder.Append(_Buffer, _BufferIndex, _BufferLength);
			}

			while (FillBuffer())
			{
				Builder.Append(_Buffer, _BufferIndex, _BufferLength);
			}

			return Builder.ToString();
		}

		/// <inheritdoc />
		public override Task<string> ReadToEndAsync() => Task.FromResult(ReadToEnd());

		/// <summary>
		/// Resets the reader to the start, if supported
		/// </summary>
		public void Reset()
		{
			if (_Reader is not BufferReader<byte> BufferReader)
				throw new NotSupportedException();

			BufferReader.Restart();

			_BufferIndex = 0;
			_BufferLength = 0;
			_Decoder = Encoding.GetDecoder();
			_CheckPreamble = true;
		}

		/// <summary>
		/// Skips the remaining line of characters
		/// </summary>
		public void SkipLine()
		{
			for (; ; )
			{
				// Read more data if available
				if (_BufferLength == 0 && !FillBuffer())
					return; // No data left

				var CharIndex = _BufferIndex;
				var EndIndex = _BufferIndex + _BufferLength;

				// Find the next carriage-return or line-feed in the current buffer
				while (CharIndex < EndIndex)
				{
					var MyChar = _Buffer[CharIndex];

					if (MyChar == '\r' || MyChar == '\n')
						break;

					CharIndex++;
				}

				// None? Clear the buffer and keep looking
				if (CharIndex == EndIndex)
				{
					_BufferLength = 0;

					continue;
				}

				// Found a CR or LF. Skip what we've read so far, minus the line character
				var CharsBefore = CharIndex - _BufferIndex;

				// If it's a carriage-return, check if the next character is available (might be a CRLF pair, so we need to skip it)
				if (_Buffer[CharIndex] == '\r')
				{
					if (_BufferLength == CharsBefore + 1)
					{
						// Read more data if available
						if (!FillBuffer())
							return; // No data left, return what we've read so far

						CharIndex = 0;
						CharsBefore = 0;
					}
					else
					{
						CharIndex++;
					}

					// Buffer is refilled. If the next char is a line-feed, skip it
					if (_Buffer[CharIndex] == '\n')
						CharsBefore++;
				}

				_BufferIndex += CharsBefore + 1;
				_BufferLength -= CharsBefore + 1;

				return; // We found a new-line, complete
			}
		}

		//****************************************

		private bool FillBuffer()
		{
			if (_CheckPreamble)
				CheckPreamble();

			if (_DetectEncoding)
				DetectEncoding();

			//****************************************

			// Reset the buffer and refill
			_BufferIndex = 0;
			_BufferLength = 0;

			return FillWithoutReset();
		}

		private bool FillWithoutReset()
		{
			ReadOnlySpan<byte> InBuffer;
			var OutBuffer = _Buffer.AsSpan(_BufferIndex + _BufferLength);
			bool IsCompleted;

			// Keep trying to read until we fill the buffer, or the source sequence runs out
			do
			{
				InBuffer = _Reader.GetSpan(0);

				// Decode the bytes into our char buffer
				_Decoder.Convert(
					InBuffer,
					OutBuffer,
					InBuffer.IsEmpty,
					out var BytesRead, out var WrittenChars, out IsCompleted
					);

				_BufferLength += WrittenChars;
				_Position += BytesRead;

				OutBuffer = OutBuffer.Slice(WrittenChars);

				_Reader.Advance(BytesRead);

				if (OutBuffer.IsEmpty)
					return true;// Buffer is filled, complete

				// Loop while there are more bytes unread, or there are no bytes left but there's still data to flush
			}
			while (!InBuffer.IsEmpty || !IsCompleted);

			return _BufferLength != 0;
		}

		private void CheckPreamble()
		{
			var InBuffer = _Reader.GetSpan(_Preamble.Length);

			// Do we have enough bytes for a Preamble?
			if (InBuffer.Length < _Preamble.Length)
			{
				// No, so assume there isn't one
				_CheckPreamble = false;

				return;
			}

			// Match the bytes in our input against the Preamble
			if (InBuffer.StartsWith(_Preamble))
			{
				// Success. Skip over the Preamble
				_Position += _Preamble.Length;
				_Reader.Advance(_Preamble.Length);
			}
			else
			{
				// No preamble, finish the read without making progress
				_Reader.Advance(0);
			}

			_CheckPreamble = false;
		}

		private void DetectEncoding()
		{
			var InBuffer = _Reader.GetSpan(4);

			_DetectEncoding = false;

			// Is there enough space for a byte-order mark?
			if (InBuffer.Length < 2)
			{
				_Reader.Advance(0);

				return;
			}

			//****************************************

			var TotalLength = Math.Min(InBuffer.Length, 4);
			var FirstByte = InBuffer[0];
			var SecondByte = InBuffer[1];
			var ThirdByte = TotalLength >= 3 ? InBuffer[2] : (byte)0;
			var FourthByte = TotalLength >= 4 ? InBuffer[3] : (byte)0;

			var PreambleLength = 0;

			//****************************************

			// Detect big-endian Unicode
			if (FirstByte == 0xFE && SecondByte == 0xFF)
			{
				Encoding = new UnicodeEncoding(true, true);

				PreambleLength  = 2;
			}
			// Detect little-endian UTF32 and Unicode
			else if (FirstByte == 0xFF && SecondByte == 0xFE)
			{
				if (TotalLength < 4 || ThirdByte != 0 || FourthByte != 0)
				{
					Encoding = new UnicodeEncoding(false, true);

					PreambleLength = 2;
				}
				else
				{
					Encoding = new UTF32Encoding(false, true);

					PreambleLength = 4;
				}
			}
			// Detect plain UTF8
			else if (TotalLength >= 3 && FirstByte == 0xEF && SecondByte == 0xBB && ThirdByte == 0xBF)
			{
				Encoding = Encoding.UTF8;

				PreambleLength = 3;
			}
			// Detect big-endian UTF32
			else if (TotalLength >= 4 && FirstByte == 0 && SecondByte == 0 && ThirdByte == 0xFE && FourthByte == 0xFF)
			{
				Encoding = new UTF32Encoding(true, true);

				PreambleLength = 4;
			}

			//****************************************

			// Skip over the Preamble, if any
			_Position += PreambleLength;
			_Reader.Advance(PreambleLength);

			if (PreambleLength != 0)
			{
				// Prepare the appropriate encoding if we have a preamble
				_Decoder = Encoding.GetDecoder();
				_Preamble = Encoding.GetPreamble();
				_Buffer = new char[Encoding.GetMaxCharCount(_BufferSize)];
			}
		}

		//****************************************

		/// <summary>
		/// Gets the position we're reading from in the source Sequence
		/// </summary>
		/// <remarks>Due to buffering, this may not reflect the position of the most recent character</remarks>
		public long Position => _Position;

		/// <summary>
		/// Gets whether the end of the stream has been reached
		/// </summary>
		public bool EndOfReader => _BufferLength == 0 && !FillBuffer();

		/// <summary>
		/// Gets the Encoding being used to decode the source Sequence
		/// </summary>
		public Encoding Encoding { get; private set; }
	}
}
