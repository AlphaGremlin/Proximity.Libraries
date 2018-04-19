using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proximity.Utility.Threading;

namespace Proximity.Utility.IO
{
	/// <summary>
	/// Implements a TextWriter that writes directly to a char array, rather than requiring conversions
	/// </summary>
	public sealed class CharTextWriter : TextWriter
	{ //****************************************
		private const int MinBufferSize = 128;
		//****************************************
		private char[] _CharBuffer;
		private int _CharLength; // The bytes that have been written to the buffer so far
		//****************************************

		/// <summary>
		/// Creates a new Char Text Writer
		/// </summary>
		public CharTextWriter() : this(MinBufferSize)
		{
		}

		/// <summary>
		/// Creates a new Char Text Writer
		/// </summary>
		/// <param name="capacity">The initial capacity for the Char Text Writer before resizing</param>
		public CharTextWriter(int capacity)
		{
			if (capacity < MinBufferSize)
				capacity = MinBufferSize;

			_CharBuffer = new char[capacity];
		}

		//****************************************

		/// <inheritdoc />
		public override void Flush()
		{
		}

#if !NET40
		/// <inheritdoc />
		public override Task FlushAsync()
		{
			return VoidStruct.EmptyTask;
		}
#endif

		/// <summary>
		/// Gets the underlying array for this TextWriter
		/// </summary>
		/// <returns>The underlying array</returns>
		/// <remarks>This buffer may contain bytes that are unused. Use the <see cref="Length"/> property to tell the true length of data in the buffer, or utilise <see cref="GetBufferSegment"/></remarks>
		public char[] GetBuffer()
		{
			return _CharBuffer;
		}

		/// <summary>
		/// Gets the underlying array for this TextWriter in the form of an Array Segment
		/// </summary>
		/// <returns>An array segment describing the underlying buffer for this TextWriter</returns>
		public ArraySegment<char> GetBufferSegment()
		{
			return new ArraySegment<char>(_CharBuffer, 0, _CharLength);
		}

		/// <summary>
		/// Retrieves the bytes written so far to the Binary Text Writer
		/// </summary>
		/// <returns>A new byte array containing the bytes written</returns>
		public char[] ToArray()
		{
			if (_CharLength == _CharBuffer.Length)
				return _CharBuffer;

			var NewArray = new char[_CharLength];

			Array.Copy(_CharBuffer, 0, NewArray, 0, _CharLength);

			return NewArray;
		}

		/// <inheritdoc />
		public override void Write(char value)
		{
			if (_CharLength == _CharBuffer.Length)
				EnsureCapacity(_CharLength + 1);

			_CharBuffer[_CharLength] = value;
			_CharLength++;
		}

		/// <inheritdoc />
		public override void Write(char[] buffer)
		{
			Write(buffer, 0, buffer.Length);
		}

		/// <inheritdoc />
		public override void Write(char[] buffer, int index, int count)
		{
			EnsureCapacity(_CharLength + count);

			Array.Copy(buffer, index, _CharBuffer, _CharLength, count);

			_CharLength += count;
		}

		/// <inheritdoc />
		public override void Write(string value)
		{
			EnsureCapacity(_CharLength + value.Length);

			value.CopyTo(0, _CharBuffer, _CharLength, value.Length);

			_CharLength += value.Length;
		}

#if !NET40
		/// <inheritdoc />
		public override Task WriteAsync(char value)
		{
			Write(value);

			return VoidStruct.EmptyTask;
		}

		/// <inheritdoc />
		public override Task WriteAsync(char[] buffer, int index, int count)
		{
			Write(buffer, index, count);

			return VoidStruct.EmptyTask;
		}

		/// <inheritdoc />
		public override Task WriteAsync(string value)
		{
			Write(value);

			return VoidStruct.EmptyTask;
		}
#endif

		/// <inheritdoc />
		public override void WriteLine(string value)
		{
			Write(value);
			WriteLine(base.CoreNewLine);
		}

#if !NET40
		/// <inheritdoc />
		public override Task WriteLineAsync()
		{
			Write(CoreNewLine);

			return VoidStruct.EmptyTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(char value)
		{
			Write(value);
			Write(CoreNewLine);

			return VoidStruct.EmptyTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(char[] buffer, int index, int count)
		{
			Write(buffer, index, count);
			Write(CoreNewLine);

			return VoidStruct.EmptyTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(string value)
		{
			Write(value);
			Write(CoreNewLine);

			return VoidStruct.EmptyTask;
		}
#endif

		//****************************************

		private void EnsureCapacity(int capacity)
		{
			int num = (_CharBuffer.Length == 0 ? MinBufferSize : _CharBuffer.Length * 2);

			if (num > 0x7FEFFFFF)
				num = 0x7FEFFFFF;

			if (num < capacity)
				num = capacity;

			Capacity = num;
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the number of chars that the Char Text Writer can currently receive before resizing
		/// </summary>
		public int Capacity
		{
			get { return _CharBuffer.Length; }
			set
			{
				if (value == _CharBuffer.Length)
					return;

				if (value < _CharLength)
					throw new ArgumentException("value");

				if (value < MinBufferSize)
					value = MinBufferSize;

				var NewBuffer = new char[value];

				if (_CharLength > 0)
					Array.Copy(_CharBuffer, 0, NewBuffer, 0, _CharLength);

				_CharBuffer = NewBuffer;
			}
		}

		/// <summary>
		/// Gets the number of bcharsytes currently written
		/// </summary>
		public int Length
		{
			get { return _CharLength; }
		}

		/// <summary>
		/// Gets the encoding used by this Char Text Writer
		/// </summary>
		/// <remarks>Null, since there is no encoding peformed</remarks>
		public override Encoding Encoding => null;
	}
}
