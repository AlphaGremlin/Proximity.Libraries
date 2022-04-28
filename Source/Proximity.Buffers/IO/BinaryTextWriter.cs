using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using System.Threading;

namespace System.IO
{
	/// <summary>
	/// Implements a TextWriter that writes directly to a <see cref="IBufferWriter{Byte}"/>
	/// </summary>
	public sealed class BinaryTextWriter : TextWriter, IBufferWriter<char>
	{	//****************************************
		private static readonly Encoding UTF8NoBOM = new UTF8Encoding(false, true);
		//****************************************
		private const int DefaultBufferSize = 1024;
		private const int PendingBufferSize = 128;
		private const int MinBufferSize = 16;
		//****************************************
		private readonly Encoding _Encoding;
		private readonly Encoder _Encoder;

		private readonly IBufferWriter<byte> _Writer;
		private readonly int _MaxBufferSize;

		private bool _HaveWrittenPreamble;

		private char[] _PendingBuffer;
		private int _PendingLength;
		private bool _Disposed = false;
		//****************************************

		/// <summary>
		/// Creates a new Buffer Writer
		/// </summary>
		/// <param name="writer">The Buffer Writer to write to</param>
		/// <param name="encoding">The encoding to use for writing to the byte array</param>
		/// <param name="maxBufferSize">The maximum buffer size to request from the Buffer Writer</param>
		/// <param name="formatProvider">A format provider for formatting objects to write</param>
		public BinaryTextWriter(IBufferWriter<byte> writer, Encoding encoding, int maxBufferSize, IFormatProvider? formatProvider) : base(formatProvider)
		{
			if (maxBufferSize < 0)
				throw new ArgumentOutOfRangeException("Buffer size must be a positive integer");

			_Writer = writer;
			_MaxBufferSize = maxBufferSize;

			_Encoding = encoding;
			_Encoder = _Encoding.GetEncoder();

			// We want a small buffer for storing pending characters
			_PendingBuffer = ArrayPool<char>.Shared.Rent(PendingBufferSize);
		}

		/// <summary>
		/// Creates a new Binary Text Writer
		/// </summary>
		/// <param name="writer">The Buffer Writer to write to</param>
		/// <param name="encoding">The encoding to use for writing to the byte array</param>
		/// <param name="maxBufferSize">The maximum buffer size to request from the Buffer Writer</param>
		public BinaryTextWriter(IBufferWriter<byte> writer, Encoding encoding, int maxBufferSize) : this(writer, encoding, maxBufferSize, null)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Writer
		/// </summary>
		/// <param name="writer">The Buffer Writer to write to</param>
		/// <param name="encoding">The encoding to use for writing to the byte array</param>
		public BinaryTextWriter(IBufferWriter<byte> writer, Encoding encoding) : this(writer, encoding, DefaultBufferSize, null)
		{
		}

		/// <summary>
		/// Creates a new Binary Text Writer using UTF8 with no BOM
		/// </summary>
		/// <param name="writer">The Buffer Writer to write to</param>
		public BinaryTextWriter(IBufferWriter<byte> writer) : this(writer, UTF8NoBOM, DefaultBufferSize, null)
		{
		}

		//****************************************

		/// <summary>
		/// Writes <paramref name="count"/> chars in the buffer returned from <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/> to the underlying writer
		/// </summary>
		/// <param name="count">The number of characters written</param>
		public void Advance(int count)
		{
			if (count < 0 || count + _PendingLength > _PendingBuffer.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			_PendingLength += count;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && !_Disposed)
			{
				Flush(true);

				_Disposed = true;

				ArrayPool<char>.Shared.Return(_PendingBuffer);
			}
		}

		/// <inheritdoc />
		public override void Flush() => Flush(true);

		/// <inheritdoc />
		public override Task FlushAsync()
		{
			Flush(true);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Retrieves a buffer to write to that is at least the requested size
		/// </summary>
		/// <param name="sizeHint">The minimum length of the returned buffer. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		public Memory<char> GetMemory(int sizeHint = 0)
		{
			// If we have enough space in the buffer to satisfy the request, do so immediately
			if (_PendingBuffer.Length - _PendingLength > sizeHint)
				return _PendingBuffer.AsMemory(_PendingLength);

			// The requested data won't fit, is there already data pending?
			if (_PendingLength > 0)
				Flush(false);

			// Is there now enough space?
			if (_PendingBuffer.Length <= sizeHint)
			{
				// No, so we need to resize our buffer
				ArrayPool<char>.Shared.Resize(ref _PendingBuffer, sizeHint);
			}

			// We now have enough space
			return _PendingBuffer.AsMemory();
		}

		/// <summary>
		/// Retrieves a buffer to write to that is at least the requested size
		/// </summary>
		/// <param name="sizeHint">The minimum length of the returned buffer. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		public Span<char> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

		/// <inheritdoc />
		public override void Write(char value)
		{
			if (_PendingBuffer.Length == _PendingLength)
				Flush(false);

			_PendingBuffer[_PendingLength++] = value;
		}

		/// <inheritdoc />
		public override void Write(char[]? buffer) => Write(buffer.AsSpan());

		/// <inheritdoc />
		public override void Write(char[] buffer, int index, int count) => Write(buffer.AsSpan(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void Write(ReadOnlySpan<char> buffer)
		{
			if (buffer.IsEmpty)
				return;

			var RemainingPending = _PendingBuffer.Length - _PendingLength;

			// If the incoming data will fit easily in the pending buffer, copy it there and exit
			if (RemainingPending >= buffer.Length)
			{
				buffer.CopyTo(_PendingBuffer.AsSpan(_PendingLength));

				_PendingLength += buffer.Length;

				return;
			}

			// The incoming data won't fit, is there already data pending?
			if (_PendingLength > 0)
				Flush(false);

			// Maybe it will fit now we've flushed the pending data
			if (_PendingBuffer.Length >= buffer.Length)
			{
				buffer.CopyTo(_PendingBuffer.AsSpan(_PendingLength));

				_PendingLength += buffer.Length;

				return;
			}

			// No, so let's stream it directly to the underlying writer. Make sure we've written any preamble first
			if (!_HaveWrittenPreamble)
				WritePreamble();

			bool IsCompleted;

			do
			{
				// Determine the size of the output buffer
				var EstimatedSize = _Encoding.GetMaxByteCount(buffer.Length);

				if (_MaxBufferSize > 0)
					EstimatedSize = Math.Min(_MaxBufferSize, EstimatedSize);

				var Output = _Writer.GetSpan(Math.Max(EstimatedSize, MinBufferSize));

				// Encode the characters into our output buffer
				_Encoder.Convert(
					buffer,
					Output,
					false,
					out var CharsRead, out var BytesWritten, out IsCompleted
					);

				_Writer.Advance(BytesWritten);

				buffer = buffer.Slice(CharsRead);

				// Loop while there are more characters unread
			}
			while (!IsCompleted);
		}

		/// <inheritdoc />
		public override void Write(string? value) => Write(value.AsSpan());

		/// <inheritdoc />
		public override Task WriteAsync(char value)
		{
			Write(value);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteAsync(char[] buffer, int index, int count) => WriteAsync(buffer.AsMemory(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
		{
			Write(buffer.Span);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteAsync(string? value) => WriteAsync(value.AsMemory());

		/// <inheritdoc />
		public override void WriteLine(char value)
		{
			Write(value);
			Write(CoreNewLine);
		}

		/// <inheritdoc />
		public override void WriteLine(char[]? buffer) => WriteLine(buffer.AsSpan());

		/// <inheritdoc />
		public override void WriteLine(char[] buffer, int index, int count) => WriteLine(buffer.AsSpan(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void WriteLine(ReadOnlySpan<char> buffer)
		{
			Write(buffer);
			Write(CoreNewLine);
		}

		/// <inheritdoc />
		public override void WriteLine(string? value) => WriteLine(value.AsSpan());

		/// <inheritdoc />
		public override Task WriteLineAsync()
		{
			Write(CoreNewLine);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(char value)
		{
			Write(value);
			Write(CoreNewLine);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(char[] buffer, int index, int count) => WriteLineAsync(buffer.AsMemory(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
		{
			Write(buffer);
			Write(CoreNewLine);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(string? value) => WriteLineAsync(value.AsMemory());

		//****************************************

		private void Flush(bool flushEncoder)
		{
			if (_PendingLength == 0 && !flushEncoder)
				return;

			if (!_HaveWrittenPreamble)
				WritePreamble();

			var Buffer = _PendingBuffer.AsSpan(0, _PendingLength);

			bool IsCompleted;

			do
			{
				// Determine the size of the output buffer
				var EstimatedSize = _Encoding.GetMaxByteCount(Buffer.Length);

				if (_MaxBufferSize > 0)
					EstimatedSize = Math.Min(_MaxBufferSize, EstimatedSize);

				var Output = _Writer.GetSpan(Math.Max(EstimatedSize, MinBufferSize));

				// Encode the characters into our output buffer
				_Encoder.Convert(
					Buffer,
					Output,
					flushEncoder,
					out var CharsRead, out var BytesWritten, out IsCompleted
					);

				_Writer.Advance(BytesWritten);

				Buffer = Buffer.Slice(CharsRead);

				// Loop while there are more characters unread
			}
			while (!IsCompleted);

			_PendingLength = 0;
		}

		private void WritePreamble()
		{
			_HaveWrittenPreamble = true;

			var MyPreamble = _Encoding.GetPreamble().AsSpan();

			if (MyPreamble.Length == 0)
				return;

			var Output = _Writer.GetSpan(MyPreamble.Length);

			MyPreamble.CopyTo(Output);

			_Writer.Advance(MyPreamble.Length);
		}

		//****************************************

		/// <inheritdoc />
		public override Encoding Encoding => _Encoding;
	}
}
