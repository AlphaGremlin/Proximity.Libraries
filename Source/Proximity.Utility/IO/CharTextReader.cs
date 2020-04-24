using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proximity.Utility.IO
{
	/// <summary>
	/// Implements a TextReader that works directly from a char array, rather than requiring a conversion to string for StringReader
	/// </summary>
	public sealed class CharTextReader : TextReader
	{ //****************************************
		private readonly ReadOnlySequence<char> _Initial;

		private ReadOnlySequence<char> _Remainder;
		//****************************************

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		public CharTextReader(ArraySegment<char> array) : this(new ReadOnlySequence<char>(array))
		{
		}

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="array">The array to read from</param>
		public CharTextReader(char[] array) : this(new ReadOnlySequence<char>(array))
		{
		}

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="array">The array to read from</param>
		public CharTextReader(ReadOnlyMemory<char> array) : this(new ReadOnlySequence<char>(array))
		{
		}

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="array">The char array to read from</param>
		/// <param name="offset">The offset into the char array to start from</param>
		/// <param name="length">The maximum chars to read from the array</param>
		public CharTextReader(char[] array, int offset, int length) : this(new ReadOnlySequence<char>(array, offset, length))
		{
		}

		/// <summary>
		/// Creates a new Char Text Reader
		/// </summary>
		/// <param name="sequence">The array segment to read from</param>
		public CharTextReader(ReadOnlySequence<char> sequence)
		{
			_Remainder = _Initial = sequence;
		}

		//****************************************

		/// <inheritdoc />
		public override int Peek()
		{
			if (_Remainder.IsEmpty)
				return -1;

			return _Remainder.First.Span[0];
		}

		/// <inheritdoc />
		public override int Read()
		{
			if (_Remainder.IsEmpty)
				return -1;

			var Result = _Remainder.First.Span[0];

			_Remainder = _Remainder.Slice(1);

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
		{ //****************************************
			var WriteLength = (int)Math.Min(_Remainder.Length, buffer.Length);
			//****************************************

			_Remainder.Slice(0, WriteLength).CopyTo(buffer.Slice(0, WriteLength));

			_Remainder = _Remainder.Slice(WriteLength);

			return WriteLength;
		}

		/// <inheritdoc />
		public override Task<int> ReadAsync(char[] buffer, int index, int count) => Task.FromResult(Read(buffer.AsSpan(index, count)));

#if !NETSTANDARD2_0
		/// <inheritdoc />
		public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken token = default) => new ValueTask<int>(Read(buffer.Span));
#endif

		/// <inheritdoc />
		public override int ReadBlock(char[] buffer, int index, int count) => Read(buffer, index, count);

		/// <inheritdoc />
		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) => Task.FromResult(Read(buffer.AsSpan(index, count)));

#if !NETSTANDARD2_0
		/// <inheritdoc />
		public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken token = default) => new ValueTask<int>(ReadBlock(buffer.Span));
#endif

		/// <inheritdoc />
		public override string ReadLine()
		{
			// Read more data if available
			if (_Remainder.IsEmpty)
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

		/// <inheritdoc />
		public override Task<string> ReadLineAsync() => Task.FromResult(ReadLine());

		/// <inheritdoc />
		public override string ReadToEnd()
		{
			_Remainder.ToString();
			var Result = new string(_Buffer, _BufferIndex, _BufferLength);

			_BufferIndex += _BufferLength;
			_BufferLength = 0; // No more data to read

			return Result;
		}

		/// <inheritdoc />
		public override Task<string> ReadToEndAsync() => Task.FromResult(ReadToEnd());

		/// <summary>
		/// Resets the reader to the start
		/// </summary>
		public void Reset() => _Remainder = _Initial;

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
