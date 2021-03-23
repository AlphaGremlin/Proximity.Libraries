using System;
using System.Collections.Generic;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace System.IO
{
	/// <summary>
	/// Provides a Stream that reads from an <see cref="IBufferReader{Byte}"/>
	/// </summary>
	public sealed class BufferReaderStream : Stream
	{ //****************************************
		private const int MinimumReadSize = 16;
		//****************************************
		private int? _BlockSize;
		private ReadOnlyMemory<byte> _Buffer;
		private int _BufferRead;

		private long _Position;
		//****************************************

		/// <summary>
		/// Creates a Stream that reads directly from the underlying Buffer Reader
		/// </summary>
		/// <param name="reader">The Buffer Reader to write to</param>
		public BufferReaderStream(IBufferReader<byte> reader)
		{
			Reader = reader ?? throw new ArgumentNullException(nameof(reader));
			_BlockSize = null;
		}

		/// <summary>
		/// Creates a Stream that reads directly from the underlying Buffer Reader
		/// </summary>
		/// <param name="reader">The Buffer Reader to write to</param>
		/// <param name="blockSize">The block size to use when reading</param>
		public BufferReaderStream(IBufferReader<byte> reader, int? blockSize)
		{
			Reader = reader ?? throw new ArgumentNullException(nameof(reader));
			_BlockSize = blockSize;
		}

		//****************************************

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				if (_BufferRead != 0)
				{
					Reader.Advance(_BufferRead);

					_BufferRead = 0;
				}
			}
		}

		/// <inheritdoc />
		public override void Flush()
		{
		}

		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override int ReadByte()
		{
			if (_Buffer.IsEmpty)
			{
				// If a block size is set, read that instead of trying to get the minimum size
				var ReadLength = _BlockSize != null ? _BlockSize.GetValueOrDefault() : MinimumReadSize;

				_Buffer = Reader.GetMemory(ReadLength);

				if (_Buffer.IsEmpty)
					return -1;
			}

			// Read a single byte from the buffer
			var Result = _Buffer.Span[0];

			_Buffer = _Buffer.Slice(1);
			_BufferRead++;

			// If we exhausted the buffer, advance the number of bytes we read in total
			if (_Buffer.IsEmpty)
			{
				Reader.Advance(_BufferRead);

				_BufferRead = 0;
			}

			return Result;
		}

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			int Read(Span<byte> buffer)
		{
			if (buffer.IsEmpty)
				return 0;

			int BytesRead;

			if (_Buffer.IsEmpty)
			{
				// No outstanding data, so the reader is in the Get phase
				BytesRead = 0;
			}
			else
			{
				// We have outstanding data, so the reader is in the Advance phase.
				// Copy from the outstanding buffer.
				BytesRead = ReadFromBuffer(buffer);

				// If we exhausted the outstanding data, the reader will be in the Get phase. Otherwise, we'll still be in Advance
				buffer = buffer.Slice(BytesRead);
			}

			// Did we fill the output buffer? If it's full, the reader may still be in Advance phase, but that's okay since we won't call GetMemory
			while (!buffer.IsEmpty)
			{
				// The buffer isn't full, which means we exhausted the outstanding data (or there was none).
				// The reader is in the Get phase.

				// If a block size is set, read that instead of trying to get enough to fill the whole buffer
				var ReadLength = _BlockSize != null ? _BlockSize.GetValueOrDefault() : buffer.Length;

				_Buffer = Reader.GetMemory(ReadLength);

				// Have we exhausted the reader?
				if (_Buffer.IsEmpty)
					return BytesRead;

				// Copy from the buffer
				ReadLength = ReadFromBuffer(buffer);

				// If we exhausted the outstanding data, the reader will be in the Get phase. Otherwise, we'll still be in Advance
				buffer = buffer.Slice(ReadLength);
				BytesRead += ReadLength;
			}

			return BytesRead;
		}

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		/// <inheritdoc />
		public override void SetLength(long value) => throw new NotSupportedException();

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		//****************************************

		private int ReadFromBuffer(Span<byte> target)
		{
			var BufferLength = Math.Min(_Buffer.Length, target.Length);

			_Buffer.Slice(0, BufferLength).Span.CopyTo(target);
			_Buffer = _Buffer.Slice(BufferLength);
			_BufferRead += BufferLength;

			// If we exhausted the buffer, advance the number of bytes we read in total
			if (_Buffer.IsEmpty)
			{
				Reader.Advance(_BufferRead);

				_BufferRead = 0;
			}

			return BufferLength;
		}

		//****************************************

		/// <summary>
		/// Gets the Buffer Reader we're reading from
		/// </summary>
		public IBufferReader<byte> Reader { get; }

		/// <inheritdoc />
		public override bool CanRead => true;

		/// <inheritdoc />
		public override bool CanSeek => false;

		/// <inheritdoc />
		public override bool CanTimeout => false;

		/// <inheritdoc />
		public override bool CanWrite => false;

		/// <inheritdoc />
		public override long Length => throw new NotSupportedException();

		/// <inheritdoc />
		public override long Position
		{
			get => _Position;
			set => throw new NotSupportedException();
		}
	}
}
