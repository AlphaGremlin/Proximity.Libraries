/****************************************\
 BinaryTextReader.cs
 Created: 2016-02-26
\****************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.IO
{
	/// <summary>
	/// Implements a TextReader that works directly from a byte array, rather than requiring a MemoryStream wrapper
	/// </summary>
	public sealed class BinaryTextReader : TextReader
	{	//****************************************
		private const int MinBufferSize = 128;
		//****************************************
		private readonly byte[] _Array;
		private int _Index, _Length;

		private readonly Encoding _Encoding;
		private readonly Decoder _Decoder;
		private readonly char[] _Buffer;
		private int _BufferIndex, _BufferLength;

		//****************************************

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding) : this(array.Array, array.Offset, array.Count, encoding, 0)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding, int bufferSize) : this(array.Array, array.Offset, array.Count, encoding, bufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(byte[] array, Encoding encoding) : this(array, 0, array.Length, encoding, 0)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(byte[] array, Encoding encoding, int bufferSize) : this(array, 0, array.Length, encoding, bufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding) : this(array, offset, length, encoding, 0)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding, int bufferSize) : base()
		{
			if (bufferSize < MinBufferSize)
				bufferSize = MinBufferSize;

			_Array = array;
			_Index = offset;
			_Length = length;
			_Encoding = encoding;
			_Decoder = encoding.GetDecoder();

			_Buffer = new char[encoding.GetMaxCharCount(bufferSize)];
		}

		//****************************************

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
		public override int Read(char[] buffer, int index, int count)
		{	//****************************************
			int CharsWritten = 0;
			int WriteLength = 0, TempLength = 0;
			//****************************************

			// Empty the buffer if there's anything in it
			if (_BufferLength > 0)
			{
				WriteLength = Math.Min(_BufferLength, count);

				Array.Copy(_Buffer, _BufferIndex, buffer, index, WriteLength);

				count -= WriteLength;
				index += WriteLength;

				CharsWritten += WriteLength;

				_BufferIndex += WriteLength;
				_BufferLength -= WriteLength;
			}

			if (count == 0)
				return CharsWritten;

			// Keep trying to read until we fill the buffer, or the source array runs out
			for (; ; )
			{
				var ReadLength = Math.Min(_Length, _Buffer.Length);

				// No more to read?
				if (ReadLength == 0)
					return CharsWritten;

				// Decode the next block of chars
				TempLength = _Decoder.GetChars(_Array, _Index, ReadLength, _Buffer, 0);

				// Write what we can to the output buffer
				WriteLength = Math.Min(TempLength, count);

				Array.Copy(_Buffer, 0, buffer, index, WriteLength);

				// Update the output counters
				index += WriteLength;
				count -= WriteLength;

				CharsWritten += WriteLength;

				// Update the source counters
				_Index += ReadLength;
				_Length -= ReadLength;

				// If we've run out of output space, update the buffer counters and return
				if (count == 0)
				{
					_BufferIndex = WriteLength;
					_BufferLength = TempLength - WriteLength;

					return CharsWritten;
				}
			}
		}

#if !NET40
		/// <inheritdoc />
		public override Task<int> ReadAsync(char[] buffer, int index, int count)
		{
			return Task.FromResult(Read(buffer, index, count));
		}
#endif

		/// <inheritdoc />
		public override int ReadBlock(char[] buffer, int index, int count)
		{
			return Read(buffer, index, count);
		}

#if !NET40
		/// <inheritdoc />
		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
		{
			return Task.FromResult(Read(buffer, index, count));
		}
#endif

		/// <inheritdoc />
		public override string ReadLine()
		{
			var MyBuilder = new StringBuilder();

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
					MyBuilder.Append(_Buffer, _BufferIndex, _BufferLength);

					_BufferLength = 0;

					continue;
				}

				var CharsBefore = CharIndex - _BufferIndex;

				// Found a CR or LF. Append what we've read so far, minus the line character
				MyBuilder.Append(_Buffer, _BufferIndex, CharsBefore);

				// If it's a carriage-return, check if the next character is available (might be a CRLF pair, so we need to skip it)
				if (_Buffer[CharIndex] == '\r' && _BufferLength == CharsBefore + 1)
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

				_BufferIndex += CharsBefore;
				_BufferLength -= CharsBefore;

				// Buffer is refilled. If the next char is a line-feed, skip it
				if (_Buffer[CharIndex] == '\n')
				{
					_BufferIndex++;
					_BufferLength--;
				}

				break; // We found a new-line, complete
			}

			return MyBuilder.ToString();
		}

#if !NET40
		/// <inheritdoc />
		public override Task<string> ReadLineAsync()
		{
			return Task.FromResult(ReadLine());
		}
#endif

		/// <inheritdoc />
		public override string ReadToEnd()
		{
			var MyBuilder = new StringBuilder(_Encoding.GetMaxCharCount(_Length));

			if (_BufferLength != 0)
			{
				MyBuilder.Append(_Buffer, _BufferIndex, _BufferLength);
			}

			while (FillBuffer())
			{
				MyBuilder.Append(_Buffer, _BufferIndex, _BufferLength);
			}

			return MyBuilder.ToString();
		}

#if !NET40
		/// <inheritdoc />
		public override Task<string> ReadToEndAsync()
		{
			return Task.FromResult(ReadToEnd());
		}
#endif

		//****************************************

		private bool FillBuffer()
		{	//****************************************
			var ReadLength = Math.Min(_Length, _Buffer.Length);
			//****************************************

			if (ReadLength == 0)
				return false;

			_BufferIndex = 0;
			_BufferLength = _Decoder.GetChars(_Array, _Index, ReadLength, _Buffer, 0);

			_Index += ReadLength;
			_Length -= ReadLength;

			return true;
		}
	}
}
