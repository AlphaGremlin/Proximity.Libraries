using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proximity.Utility.IO
{
	/// <summary>
	/// Implements a TextReader that works directly from a char array, rather than requiring a conversion to string for StringReader
	/// </summary>
	public sealed class CharTextReader : TextReader
	{ //****************************************
		private readonly char[] _Buffer;
		private readonly int _StartIndex;
		private int _BufferIndex, _BufferLength;
		//****************************************

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		public CharTextReader(ArraySegment<char> array) : this(array.Array, array.Offset, array.Count)
		{

		}

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="array">The array to read from</param>
		public CharTextReader(char[] array) : this(array, 0, array.Length)
		{
		}

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="array">The char array to read from</param>
		/// <param name="offset">The offset into the char array to start from</param>
		/// <param name="length">The maximum chars to read from the array</param>
		public CharTextReader(char[] array, int offset, int length)
		{
			_Buffer = array;
			_BufferIndex = _StartIndex = offset;
			_BufferLength = length;
		}

		//****************************************

		/// <inheritdoc />
		public override int Peek()
		{
			if (_BufferLength == 0)
				return -1;

			return _Buffer[_BufferIndex];
		}

		/// <inheritdoc />
		public override int Read()
		{
			if (_BufferLength == 0)
				return -1;

			var Result = _Buffer[_BufferIndex];

			_BufferIndex++;
			_BufferLength--;

			return Result;
		}

		/// <inheritdoc />
		public override int Read(char[] buffer, int index, int count)
		{ //****************************************
			int WriteLength = Math.Min(_BufferLength, count);
			//****************************************

			Array.Copy(_Buffer, _BufferIndex, buffer, index, WriteLength);

			_BufferIndex += WriteLength;
			_BufferLength -= WriteLength;

			return WriteLength;
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
			// Read more data if available
			if (_BufferLength == 0)
				return string.Empty; // Nothing else to read

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

			// None? Return the remaining data
			if (CharIndex == EndIndex)
				return new string(_Buffer, _BufferIndex, _BufferLength);

			var CharsBefore = CharIndex - _BufferIndex;

			// Found a CR or LF. Append what we've read so far, minus the line character
			var Result = new string(_Buffer, _BufferIndex, CharsBefore);

			// If it's a carriage-return, check if the next character exists (could be a terminating line)
			if (_Buffer[CharIndex] == '\r' && _BufferLength == CharsBefore + 1)
			{
				_BufferIndex += _BufferLength;
				_BufferLength = 0; // No more data to read

				return Result;
			}
				
			// Character is available, push up by one
			CharIndex++;

			_BufferIndex += CharsBefore;
			_BufferLength -= CharsBefore;

			// Buffer is refilled. If the next char is a line-feed, skip it
			if (_Buffer[CharIndex] == '\n')
			{
				_BufferIndex++;
				_BufferLength--;
			}

			return Result;
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
			var Result = new string(_Buffer, _BufferIndex, _BufferLength);

			_BufferIndex += _BufferLength;
			_BufferLength = 0; // No more data to read

			return Result;
		}

#if !NET40
		/// <inheritdoc />
		public override Task<string> ReadToEndAsync()
		{
			return Task.FromResult(ReadToEnd());
		}
#endif

		/// <summary>
		/// Resets the reader to the start
		/// </summary>
		public void Reset()
		{
			_BufferLength += _BufferIndex - _StartIndex;
			_BufferIndex = _StartIndex;
		}

		//****************************************

		/// <summary>
		/// Gets the position we're reading from in the source Array
		/// </summary>
		public int Position
		{
			get { return _BufferIndex - _StartIndex; }
		}

		/// <summary>
		/// Gets the number of remaining chars to read from the source Array
		/// </summary>
		public int CharsLeft
		{
			get { return _BufferLength; }
		}
	}
}
