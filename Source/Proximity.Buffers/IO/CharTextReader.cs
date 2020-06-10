using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	/// <summary>
	/// Implements a TextReader that works directly from a character sequence
	/// </summary>
	public sealed class CharTextReader : TextReader
	{ //****************************************
		private readonly ReadOnlySequence<char> _Initial;

		private ReadOnlySequence<char> _Remainder;
		private SequencePosition _Position;
		//****************************************

		/// <summary>
		/// Creates a new character sequence Text Reader
		/// </summary>
		/// <param name="array">The array segment to read from</param>
		public CharTextReader(ArraySegment<char> array) : this(new ReadOnlySequence<char>(array))
		{
		}

		/// <summary>
		/// Creates a new character sequence Text Reader
		/// </summary>
		/// <param name="array">The array to read from</param>
		public CharTextReader(char[] array) : this(new ReadOnlySequence<char>(array))
		{
		}

		/// <summary>
		/// Creates a new character sequence Text Reader
		/// </summary>
		/// <param name="array">The array to read from</param>
		public CharTextReader(ReadOnlyMemory<char> array) : this(new ReadOnlySequence<char>(array))
		{
		}

		/// <summary>
		/// Creates a new character sequence Text Reader
		/// </summary>
		/// <param name="array">The char array to read from</param>
		/// <param name="offset">The offset into the char array to start from</param>
		/// <param name="length">The maximum chars to read from the array</param>
		public CharTextReader(char[] array, int offset, int length) : this(new ReadOnlySequence<char>(array, offset, length))
		{
		}

		/// <summary>
		/// Creates a new character sequence Text Reader
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

			CompleteRead(1);

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

			CompleteRead(WriteLength);

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

			var Buffer = _Remainder;

			// Find the next carriage-return or line-feed in the current buffer
			var CharIndex = Buffer.IndexOfAny("\r\n".AsSpan());

			string Result;

			// None? Return the remaining data
			if (CharIndex == -1)
			{
				Result = _Remainder.ToString();

				CompleteRead((int)_Remainder.Length);
			}
			else
			{
				// Found a CR or LF. Append what we've read so far, minus the line character
				Result = _Remainder.Slice(0, CharIndex).ToString();

				// Check if the next character exists (could be a terminating line)
				if (_Remainder.Length != CharIndex + 1)
				{
					if (_Remainder.Get(CharIndex) == '\r')
					{
						// If it's a carriage-return, look for a line-feed (Windows)
						if (_Remainder.Get(CharIndex + 1) == '\n')
							CharIndex++; // Skip it also
					}
					else
					{
						// If it's a line-feed, look for a carriage-return (Mac)
						if (_Remainder.Get(CharIndex + 1) == '\r')
							CharIndex++; // Skip it also
					}
				}

				CompleteRead((int)CharIndex + 1);
			}

			return Result;
		}

		/// <inheritdoc />
		public override Task<string?> ReadLineAsync() => Task.FromResult<string?>(ReadLine());

		/// <inheritdoc />
		public override string ReadToEnd()
		{
			var Result = _Remainder.ToString();

			CompleteRead((int)_Remainder.Length);

			return Result;
		}

		/// <inheritdoc />
		public override Task<string> ReadToEndAsync() => Task.FromResult(ReadToEnd());

		/// <summary>
		/// Resets the reader to the start
		/// </summary>
		public void Reset() => _Remainder = _Initial;

		//****************************************

		private void CompleteRead(int charsRead)
		{
			_Position = _Remainder.GetPosition(charsRead);

			_Remainder = _Remainder.Slice(_Position);
		}

		//****************************************

		/// <summary>
		/// Gets the position we're reading from in the source Sequence
		/// </summary>
		public long Position => _Initial.Slice(0, _Position).Length;

		/// <summary>
		/// Gets the sequence position we're reading from in the source Sequence
		/// </summary>
		public SequencePosition SequencePosition => _Position;

		/// <summary>
		/// Gets the number of remaining chars to read from the source Array
		/// </summary>
		public long CharsLeft => _Remainder.Length;
	}
}
