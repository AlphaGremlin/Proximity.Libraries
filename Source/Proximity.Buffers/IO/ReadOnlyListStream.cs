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
	/// Provides a read-only stream over a <see cref="IReadOnlyList{Byte}"/>
	/// </summary>
	public sealed class ReadOnlyListStream : Stream
	{ //****************************************
		private readonly IReadOnlyList<byte> _List;

		private int _CurrentOffset;
		//****************************************

		/// <summary>
		/// Creates a new ListStream
		/// </summary>
		/// <param name="list">The IReadOnlyList to read from</param>
		public ReadOnlyListStream(IReadOnlyList<byte> list)
		{
			_List = list;
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
			if (_List.Count == _CurrentOffset)
				return -1;

			return _List[_CurrentOffset++];
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
			if (_List.Count == _CurrentOffset)
				return 0; // No more data to read

			var Length = Math.Min(buffer.Length, _List.Count - _CurrentOffset);

			for (var Index = 0; Index < Length; Index++)
				buffer[Index] = _List[_CurrentOffset + Index];

			_CurrentOffset += Length;

			return Length;
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
				SeekOrigin.End => Position = _List.Count + offset,
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
		public override long Length => _List.Count;

		/// <inheritdoc />
		public override long Position
		{
			get => _CurrentOffset;
			set
			{
				if (value < 0 || value > _List.Count)
					throw new ArgumentOutOfRangeException(nameof(value));

				_CurrentOffset = (int)value;
			}
		}
	}
}
