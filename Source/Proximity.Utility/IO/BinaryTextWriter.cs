/****************************************\
 BinaryTextReader.cs
 Created: 2016-02-26
\****************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.IO
{
	/// <summary>
	/// Implements a TextWriter that writes directly to a byte array, rather than requiring a MemoryStream wrapper
	/// </summary>
	public sealed class BinaryTextWriter : TextWriter
	{	//****************************************
		private const int MinBufferSize = 128;
		//****************************************
		private readonly Encoding _Encoding;
		private readonly Encoder _Encoder;

		private byte[] _ByteBuffer;
		private char[] _CharBuffer;
		private int _ByteLength, _CharLength; // The bytes that have been written to each buffer so far
		//****************************************

		/// <summary>
		/// Creates a new Binary Text Writer
		/// </summary>
		/// <param name="encoding">The encoding to use for writing to the byte array</param>
		public BinaryTextWriter(Encoding encoding) : this(encoding, MinBufferSize, null)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Writer
		/// </summary>
		/// <param name="encoding">The encoding to use for writing to the byte array</param>
		/// <param name="capacity">The buffer size for the text writer</param>
		public BinaryTextWriter(Encoding encoding, int capacity) : this(encoding, capacity, null)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Writer
		/// </summary>
		/// <param name="encoding">The encoding to use for writing to the byte array</param>
		/// <param name="capacity">The buffer size for the text writer</param>
		/// <param name="formatProvider">A format provider for formatting objects to write</param>
		public BinaryTextWriter(Encoding encoding, int capacity, IFormatProvider formatProvider) : base(formatProvider)
		{
			_Encoding = encoding;
			_Encoder = _Encoding.GetEncoder();

			if (capacity < MinBufferSize)
				capacity = MinBufferSize;

			_CharBuffer = new char[capacity];
			_ByteBuffer = new byte[_Encoding.GetMaxByteCount(capacity)];
		}

		//****************************************

		/// <inheritdoc />
		public override void Flush()
		{
			Flush(true);
		}

#if !NET40
		/// <inheritdoc />
		public override Task FlushAsync()
		{
			Flush(true);

			return VoidStruct.EmptyTask;
		}
#endif

		/// <summary>
		/// Retrieves the bytes written so far to the Binary Text Writer
		/// </summary>
		/// <returns></returns>
		public byte[] ToArray()
		{
			Flush(true);

			if (_ByteLength == _ByteBuffer.Length)
				return _ByteBuffer;

			var NewArray = new byte[_ByteLength];

			Array.Copy(_ByteBuffer, 0, NewArray, 0, _ByteLength);

			return NewArray;
		}

		/// <inheritdoc />
		public override void Write(char value)
		{
			if (_CharLength == _CharBuffer.Length)
				Flush(false);

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
		{	//****************************************
			int UsedChars = 0;
			//****************************************

			while (count > 0)
			{
				if (_CharLength == _CharBuffer.Length)
					Flush(false);

				UsedChars = Math.Min(_CharBuffer.Length - _CharLength, count);

				Array.Copy(buffer, index, _CharBuffer, _CharLength, UsedChars);

				_CharLength += UsedChars;
				index += UsedChars;
				count -= UsedChars;
			}
		}

		/// <inheritdoc />
		public override void Write(string value)
		{	//****************************************
			int UsedChars = 0, Index = 0, Count = value.Length;
			//****************************************

			while (Count > 0)
			{
				if (_CharLength == _CharBuffer.Length)
					Flush(false);

				UsedChars = Math.Min(_CharBuffer.Length - _CharLength, Count);

				value.CopyTo(Index, _CharBuffer, _CharLength, UsedChars);

				_CharLength += UsedChars;
				Index += UsedChars;
				Count -= UsedChars;
			}
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
			int num = (_ByteBuffer.Length == 0 ? MinBufferSize : _ByteBuffer.Length * 2);

			if (num > 0x7FEFFFFF)
				num = 0x7FEFFFFF;

			if (num < capacity)
				num = capacity;

			Capacity = capacity;
		}

		private void Flush(bool flushEncoder)
		{
			if (_CharLength == 0 && !flushEncoder)
				return;

			// Ensure we have enough space in the byte buffer to write to
			var RequiredCapacity = _Encoding.GetMaxByteCount(_CharLength) + _ByteLength;

			if (RequiredCapacity > _ByteBuffer.Length)
				EnsureCapacity(RequiredCapacity);

			// Convert the chars written to the char buffer into bytes
			var OutBytes = _Encoder.GetBytes(_CharBuffer, 0, _CharLength, _ByteBuffer, _ByteLength, flushEncoder);

			_CharLength = 0;

			_ByteLength += OutBytes;
		}

		//****************************************

		/// <inheritdoc />
		public override Encoding Encoding
		{
			get { return _Encoding; }
		}

		/// <summary>
		/// Gets/Sets the number of bytes that the Binary Text Writer can currently receive before resizing
		/// </summary>
		public int Capacity
		{
			get { return _ByteBuffer.Length; }
			set
			{

				if (value == _ByteBuffer.Length)
					return;

				if (value < _ByteLength)
					throw new ArgumentException("value");

				if (value < MinBufferSize)
					value = MinBufferSize;

				var NewBuffer = new byte[value];

				if (_ByteLength > 0)
					Array.Copy(_ByteBuffer, 0, NewBuffer, 0, _ByteLength);

				_ByteBuffer = NewBuffer;
			}
		}

		/// <summary>
		/// Gets the number of bytes currently written
		/// </summary>
		public int Length
		{
			get { return _ByteLength; }
		}
	}
}
