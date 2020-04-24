using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
//****************************************

namespace System.IO
{
	/// <summary>
	/// Implements a TextReader that works directly from a byte array, rather than requiring a MemoryStream wrapper
	/// </summary>
	public sealed class BinaryTextReader : TextReader
	{	//****************************************
		private const int MinBufferSize = 128;
		private const int DefaultBufferSize = 1024;
		//****************************************
		private readonly byte[] _Array;
		private readonly int _StartIndex;
		private int _Index, _Length;
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
		public BinaryTextReader(ArraySegment<byte> array) : this(array.Array, array.Offset, array.Count, Encoding.UTF8, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding) : this(array.Array, array.Offset, array.Count, encoding, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding, bool detectEncoding) : this(array.Array, array.Offset, array.Count, encoding, detectEncoding, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(ArraySegment<byte> array, Encoding encoding, bool detectEncoding, int bufferSize) : this(array.Array, array.Offset, array.Count, encoding, detectEncoding, bufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader with auto-detected encoding
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		public BinaryTextReader(byte[] array) : this(array, 0, array.Length, Encoding.UTF8, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(byte[] array, Encoding encoding) : this(array, 0, array.Length, encoding, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		public BinaryTextReader(byte[] array, Encoding encoding, bool detectEncoding) : this(array, 0, array.Length, encoding, detectEncoding, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		/// <param name="detectEncoding">Whether to detect the encoding using the preamble</param>
		/// <param name="bufferSize">The buffer size to use when reading the text</param>
		public BinaryTextReader(byte[] array, Encoding encoding, bool detectEncoding, int bufferSize) : this(array, 0, array.Length, encoding, detectEncoding, bufferSize)
		{
		}
		
		/// <summary>
		/// Creates a new Binary Text Reader with auto-detected encoding
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		public BinaryTextReader(byte[] array, int offset, int length) : this(array, offset, length, Encoding.UTF8, true, DefaultBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Reader
		/// </summary>
		/// <param name="array">The byte array to read from</param>
		/// <param name="offset">The offset into the byte array to start from</param>
		/// <param name="length">The maximum bytes to read from the array</param>
		/// <param name="encoding">The encoding of the text in the byte array</param>
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding) : this(array, offset, length, encoding, true, DefaultBufferSize)
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
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding, bool detectEncoding) : this(array, offset, length, encoding, detectEncoding, DefaultBufferSize)
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
		public BinaryTextReader(byte[] array, int offset, int length, Encoding encoding, bool detectEncoding, int bufferSize) : base()
		{
			if (bufferSize < MinBufferSize)
				bufferSize = MinBufferSize;

			_Array = array;
			_StartIndex = _Index = offset;
			_Length = length;
			Encoding = encoding;
			_Decoder = encoding.GetDecoder();

			_DetectEncoding = detectEncoding;
			_Preamble = encoding.GetPreamble();
			_CheckPreamble = (_Preamble.Length > 0);

			_BufferSize = bufferSize;
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
			var CharsWritten = 0;
			int WriteLength, TempLength;
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

			if (_CheckPreamble)
				CheckPreamble();

			if (_DetectEncoding && _Length >= 2)
				DetectEncoding();

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

		/// <inheritdoc />
		public override Task<int> ReadAsync(char[] buffer, int index, int count) => Task.FromResult(Read(buffer, index, count));

		/// <inheritdoc />
		public override int ReadBlock(char[] buffer, int index, int count) => Read(buffer, index, count);

		/// <inheritdoc />
		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) => Task.FromResult(Read(buffer, index, count));

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

		/// <inheritdoc />
		public override Task<string> ReadLineAsync() => Task.FromResult(ReadLine());

		/// <inheritdoc />
		public override string ReadToEnd()
		{
			var MyBuilder = new StringBuilder(Encoding.GetMaxCharCount(_Length));

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

		/// <inheritdoc />
		public override Task<string> ReadToEndAsync() => Task.FromResult(ReadToEnd());

		/// <summary>
		/// Resets the reader to the start
		/// </summary>
		public void Reset()
		{
			_Length += _Index - _StartIndex;
			_Index = _StartIndex;
			_BufferIndex = 0;
			_BufferLength = 0;
			_Decoder = Encoding.GetDecoder();
			_CheckPreamble = true;
		}

		//****************************************

		private bool FillBuffer()
		{	//****************************************
			int ReadLength;
			//****************************************

			if (_CheckPreamble)
				CheckPreamble();

			if (_DetectEncoding && _Length >= 2)
				DetectEncoding();

			//****************************************

			ReadLength = Math.Min(_Length, _Buffer.Length);

			if (ReadLength == 0)
				return false;

			_BufferIndex = 0;
			_BufferLength = _Decoder.GetChars(_Array, _Index, ReadLength, _Buffer, 0);

			_Index += ReadLength;
			_Length -= ReadLength;

			return true;
		}

		private void CheckPreamble()
		{
			// Do we have enough bytes for a Preamble?
			if (_Length < _Preamble.Length)
			{
				// No, so assume there isn't one
				_CheckPreamble = false;

				return;
			}

			// Match the bytes in our input against the Preamble
			for (var Index = 0; Index < _Preamble.Length; Index++)
			{
				if (_Preamble[Index] != _Array[_Index + Index])
				{
					// Preamble doesn't match
					_CheckPreamble = false;
				}
			}

			if (_CheckPreamble)
			{
				// Success. Skip over the Preamble
				_Index += _Preamble.Length;
				_Length -= _Preamble.Length;

				_CheckPreamble = false;
			}
		}

		private void DetectEncoding()
		{	//****************************************
			var PreambleLength = 0;
			var FirstByte = _Array[_Index];
			var SecondByte = _Array[_Index + 1];
			var ThirdByte = _Length >= 3 ? _Array[_Index + 2] : (byte)0;
			var FourthByte = _Length >= 4 ? _Array[_Index + 3] : (byte)0;
			//****************************************

			_DetectEncoding = false;

			// Detect big-endian Unicode
			if (FirstByte == 0xFE && SecondByte == 0xFF)
			{
				Encoding = new UnicodeEncoding(true, true);

				PreambleLength  = 2;
			}
			// Detect little-endian UTF32 and Unicode
			else if (FirstByte == 0xFF && SecondByte == 0xFE)
			{
				if (_Length < 4 || ThirdByte != 0 || FourthByte != 0)
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
			else if (_Length >= 3 && FirstByte == 0xEF && SecondByte == 0xBB && ThirdByte == 0xBF)
			{
				Encoding = Encoding.UTF8;

				PreambleLength = 3;
			}
			// Detect big-endian UTF32
			else if (_Length >= 4 && FirstByte == 0 && SecondByte == 0 && ThirdByte == 0xFE && FourthByte == 0xFF)
			{
				Encoding = new UTF32Encoding(true, true);

				PreambleLength = 4;
			}

			//****************************************

			if (PreambleLength != 0)
			{
				_Decoder = Encoding.GetDecoder();
				_Preamble = Encoding.GetPreamble();
				_Buffer = new char[Encoding.GetMaxCharCount(_BufferSize)];

				// Skip over the Preamble
				_Index += PreambleLength;
				_Length -= PreambleLength;
			}
		}

		//****************************************

		/// <summary>
		/// Gets the position we're reading from in the source Array
		/// </summary>
		public int Position => _Index - _StartIndex;

		/// <summary>
		/// Gets the number of remaining bytes to read from the source Array
		/// </summary>
		public int BytesLeft => _Length;

		/// <summary>
		/// Gets the Encoding being used to decode the source Array
		/// </summary>
		public Encoding Encoding { get; private set; }
	}
}
