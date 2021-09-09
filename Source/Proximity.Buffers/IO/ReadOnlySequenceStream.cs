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
	/// Provides a read-only stream over a <see cref="ReadOnlySequence{Byte}"/>
	/// </summary>
	public sealed class ReadOnlySequenceStream : Stream
	{ //****************************************
		private readonly ReadOnlySequence<byte> _Sequence;

		private long _Position;
		private SequencePosition _NextPosition;
		private ReadOnlyMemory<byte> _CurrentBlock;
		private int _CurrentOffset;
		//****************************************

		/// <summary>
		/// Creates a new ReadOnlySequenceStream
		/// </summary>
		/// <param name="buffer">The ReadOnlyMemory buffer to read from</param>
		public ReadOnlySequenceStream(ReadOnlyMemory<byte> buffer) : this(new ReadOnlySequence<byte>(buffer))
		{
		}

		/// <summary>
		/// Creates a new ReadOnlySequenceStream
		/// </summary>
		/// <param name="sequence">The Sequence to read from</param>
		public ReadOnlySequenceStream(ReadOnlySequence<byte> sequence)
		{
			_Sequence = sequence;

			_Position = 0;
			_NextPosition = _Sequence.Start;
			_Sequence.TryGet(ref _NextPosition, out _CurrentBlock, true);
			_CurrentOffset = 0;
		}

		//****************************************

		/// <inheritdoc />
		public 
#if NETSTANDARD2_0
			new
#else
			override
#endif
			void CopyTo(Stream destination, int bufferSize)
		{
			using var Buffer = AutoArrayPool<byte>.Shared.Rent(bufferSize);

			for (; ; )
			{
				var ReadBytes = Read(Buffer);

				if (ReadBytes == 0)
					break;

#if NETSTANDARD2_0
				destination.Write(Buffer, 0, ReadBytes);
#else
				destination.Write(Buffer.AsSpan(0, ReadBytes));
#endif
			}
		}

		/// <inheritdoc />
		public
#if NETSTANDARD2_0
			new
#else
			override
#endif
			async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			using var Buffer = AutoArrayPool<byte>.Shared.Rent(bufferSize);

			for (; ; )
			{
				var ReadBytes = Read(Buffer);

				if (ReadBytes == 0)
					break;

#if NETSTANDARD2_0
				await destination.WriteAsync(Buffer, 0, ReadBytes);
#else
				await destination.WriteAsync(Buffer.AsMemory(0, ReadBytes), cancellationToken);
#endif
			}
		}

		/// <inheritdoc />
		public override void Flush()
		{
		}

		/// <inheritdoc />
		public
#if NETSTANDARD2_0
			new
#else
			override
#endif
			Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override int ReadByte()
		{
			if (_CurrentBlock.Length == _CurrentOffset)
				return -1;

			var Result = _CurrentBlock.Span[_CurrentOffset++];
			_Position++;

			if (_CurrentBlock.Length == _CurrentOffset && _Sequence.TryGet(ref _NextPosition, out _CurrentBlock, true))
				_CurrentOffset = 0;

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
			if (_CurrentBlock.Length == _CurrentOffset)
				return 0; // No more data to read

			// Create a span with the space we're populating
			var Remainder = _Sequence.Length - _Position;

			if (buffer.Length > Remainder)
				buffer = buffer.Slice(0, (int)Remainder);

			var BytesRead = buffer.Length;

			for (; ; )
			{
				// Determine how much to read from the current block
				var Available = _CurrentBlock.Span.Slice(_CurrentOffset, Math.Min(_CurrentBlock.Length - _CurrentOffset, buffer.Length));

				// Copy what we can
				Available.CopyTo(buffer);
				// Now move the write location up
				buffer = buffer.Slice(Available.Length);

				// Is there still more data to write?
				if (buffer.Length == 0)
				{
					_CurrentOffset += Available.Length;
					_Position += BytesRead;
					return BytesRead;
				}

				// Still more data, advance to the next block
				if (!_Sequence.TryGet(ref _NextPosition, out _CurrentBlock, true))
					throw new InvalidDataException("Out of sequence");

				_CurrentOffset = 0;
			}
		}

		/// <inheritdoc />
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(Read(buffer.AsSpan(offset, count)));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => new(Read(buffer.Span));

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin)
		{
			return origin switch
			{
				SeekOrigin.Begin => Position = offset,
				SeekOrigin.Current => Position += offset,
				SeekOrigin.End => Position = _Sequence.Length + offset,
				_ => throw new ArgumentOutOfRangeException(nameof(origin)),
			};
		}

		/// <inheritdoc />
		public override void SetLength(long value) => throw new NotSupportedException();

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

		/// <inheritdoc />
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

		/// <inheritdoc />
		public override void WriteByte(byte value) => throw new NotSupportedException();

		//****************************************

		/// <inheritdoc />
		public override bool CanRead => true;

		/// <inheritdoc />
		public override bool CanSeek => true;

		/// <inheritdoc />
		public override bool CanTimeout => false;

		/// <inheritdoc />
		public override bool CanWrite => false;

		/// <inheritdoc />
		public override long Length => _Sequence.Length;

		/// <inheritdoc />
		public override long Position
		{
			get => _Position;
			set
			{
				if (value < 0 || value > _Sequence.Length)
					throw new ArgumentOutOfRangeException(nameof(value));

				_Position = value;
				_NextPosition = _Sequence.GetPosition(_Position);
				_Sequence.TryGet(ref _NextPosition, out _CurrentBlock, true);
				_CurrentOffset = 0;
			}
		}
	}
}
