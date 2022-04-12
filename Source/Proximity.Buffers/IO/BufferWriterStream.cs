using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0060 // Remove unused parameter

namespace System.IO
{
	/// <summary>
	/// Provides a <see cref="Stream"/> that writes to an <see cref="IBufferWriter{Byte}"/>
	/// </summary>
	public sealed class BufferWriterStream : Stream
	{ //****************************************
		private readonly int? _BlockSize;
		private readonly bool _FillBlock;

		private int _OutstandingBytes;
		private Memory<byte> _RemainingSegment;
		//****************************************

		/// <summary>
		/// Creates a Stream that writes directly to the underlying Buffer Writer
		/// </summary>
		/// <param name="writer">The Buffer Writer to write to</param>
		public BufferWriterStream(IBufferWriter<byte> writer)
		{
			Writer = writer ?? throw new ArgumentNullException(nameof(writer));
			_BlockSize = null;
		}

		/// <summary>
		/// Creates a Stream that writes to the underlying Buffer Writer by requesting a fixed block size
		/// </summary>
		/// <param name="writer">The Buffer Writer to write to</param>
		/// <param name="blockSize">The block size to request</param>
		/// <param name="fillBlock">True to completely fill the buffer before advancing, even if it's larger than the block size. False to only fill to the block size</param>
		public BufferWriterStream(IBufferWriter<byte> writer, int blockSize, bool fillBlock = false)
		{
			if (_BlockSize < 1)
				throw new ArgumentOutOfRangeException(nameof(blockSize));

			Writer = writer ?? throw new ArgumentNullException(nameof(writer));
			_BlockSize = blockSize;
			_FillBlock = fillBlock;
		}

		//****************************************

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				Flush();
		}

		/// <inheritdoc />
		public override void Close()
		{
			base.Close();

			Flush();
		}

		/// <inheritdoc />
		public override void Flush()
		{
			if (_BlockSize == null)
				return;

			if (_OutstandingBytes == 0)
				return;

			Writer.Advance(_OutstandingBytes);
			_OutstandingBytes = 0;
		}

		/// <inheritdoc />
		public
#if NETSTANDARD2_0
			new
#else
			override
#endif
			Task FlushAsync(CancellationToken cancellationToken)
		{
			Flush();

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		/// <inheritdoc />
		public override void SetLength(long value) => throw new NotSupportedException();

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void Write(ReadOnlySpan<byte> buffer)
		{
			if (buffer.IsEmpty)
				return;

			if (_BlockSize == null)
			{
				Writer.Write(buffer);

				BytesWritten += buffer.Length;

				return;
			}

			do
			{
				EnsureBlock();

				// Fill the block with as much as we can
				var WriteBytes = Math.Min(buffer.Length, _RemainingSegment.Length);

				buffer.Slice(0, WriteBytes).CopyTo(_RemainingSegment.Span);

				_RemainingSegment = _RemainingSegment.Slice(WriteBytes);
				_OutstandingBytes += WriteBytes;
				BytesWritten += WriteBytes;

				buffer = buffer.Slice(WriteBytes);
			}
			while (!buffer.IsEmpty);
		}

		/// <inheritdoc />
		public override void WriteByte(byte value)
		{
			if (_BlockSize == null)
			{
				var Buffer = Writer.GetSpan(1);

				Buffer[0] = value;

				Writer.Advance(1);

				BytesWritten++;

				return;
			}

			EnsureBlock();

			// Write the byte to the current block
			_RemainingSegment.Span[0] = value;

			_RemainingSegment = _RemainingSegment.Slice(1);
			_OutstandingBytes++;
			BytesWritten++;
		}

		/// <inheritdoc />
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			Write(buffer, offset, count);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			Write(buffer.Span);

			return default;
		}

		//****************************************

		private void EnsureBlock()
		{
			// Is there an active block with some free space?
			if (_RemainingSegment.IsEmpty)
			{
				// No. Are there outstanding bytes in the current block?
				if (_OutstandingBytes > 0)
				{
					// Yes, so advance the Buffer Writer
					Writer.Advance(_OutstandingBytes);
					_OutstandingBytes = 0;
				}

				// Retrieve a new block to write to
				_RemainingSegment = Writer.GetMemory(_BlockSize!.Value);

				// If we're not filling the entire supplied buffer, trim it to our block size
				if (!_FillBlock)
					_RemainingSegment = _RemainingSegment.Slice(0, _BlockSize.Value);
			}
		}

		//****************************************

		/// <summary>
		/// Gets the Buffer Writer we're writing to
		/// </summary>
		public IBufferWriter<byte> Writer { get; }

		/// <summary>
		/// Gets the number of bytes written to the Buffer Writer
		/// </summary>
		public long BytesWritten { get; private set; }

		/// <inheritdoc />
		public override bool CanRead => false;

		/// <inheritdoc />
		public override bool CanSeek => false;

		/// <inheritdoc />
		public override bool CanTimeout => false;

		/// <inheritdoc />
		public override bool CanWrite => true;

		/// <inheritdoc />
		public override long Length => BytesWritten;

		/// <inheritdoc />
		public override long Position
		{
			get => BytesWritten;
			set => throw new NotSupportedException();
		}
	}
}
