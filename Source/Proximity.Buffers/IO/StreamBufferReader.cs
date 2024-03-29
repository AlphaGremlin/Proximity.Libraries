using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	/// <summary>
	/// Provides an <see cref="IBufferReader{Byte}"/> that reads from a <see cref="Stream"/>
	/// </summary>
	public sealed class StreamBufferReader : IBufferReader<byte>, IBufferReaderAsync<byte>, IDisposable
	{ //****************************************
		private const int DefaultBlockSize = 1024;
		//****************************************
		private readonly int _BlockSize;

		private byte[]? _Buffer;
		// The number of bytes that have been read from _Buffer
		private int _BufferIndex;
		// The number of bytes remaining in _Buffer
		private int _BufferCount;

		private bool _EndOfStream;
		//****************************************

		/// <summary>
		/// Creates a new Stream Buffer Reader
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read from</param>
		public StreamBufferReader(Stream stream) : this(stream, DefaultBlockSize)
		{
		}

		/// <summary>
		/// Creates a new Stream Buffer Reader
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read from</param>
		/// <param name="blockSize">The minimum block size to read.</param>
		/// <remarks>Buffers returned may be smaller than the block size, if the leftovers from a previous read are sufficient to cover the requested minimum.</remarks>
		public StreamBufferReader(Stream stream, int blockSize)
		{
			if (blockSize < 1)
				throw new ArgumentOutOfRangeException(nameof(blockSize));

			Stream = stream;
			_BlockSize = blockSize;
		}

		//****************************************

		/// <summary>
		/// Reads data into the buffer from the underlying Stream.
		/// </summary>
		/// <param name="minSize">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer.</returns>
		/// <remarks>Can be less than requested if the stream has ended. When this occurs, subsequent calls should return an empty buffer.</remarks>
		public ReadOnlyMemory<byte> GetMemory(int minSize)
		{
			if (PrepareForRead(minSize))
			{
#if NETSTANDARD2_0
				var BytesRead = Stream.Read(_Buffer, _BufferCount, _Buffer!.Length - _BufferCount);
#else
				var BytesRead = Stream.Read(_Buffer.AsSpan(_BufferCount));
#endif

				_BufferCount += BytesRead;
				_EndOfStream = BytesRead == 0;
			}

			return _Buffer.AsMemory(_BufferIndex, _BufferCount);
		}

		/// <summary>
		/// Asynchronously reads data into the buffer from the underlying Stream.
		/// </summary>
		/// <param name="minSize">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <param name="cancellationToken">A cancellation token to abort the operation.</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the stream has ended. When this occurs, subsequent calls should return an empty buffer.</remarks>
		public async ValueTask<ReadOnlyMemory<byte>> GetMemoryAsync(int minSize, CancellationToken cancellationToken = default)
		{
			if (PrepareForRead(minSize))
			{
#if NETSTANDARD2_0
				var BytesRead = await Stream.ReadAsync(_Buffer!, _BufferCount, _Buffer!.Length - _BufferCount, cancellationToken);
#else
				var BytesRead = await Stream.ReadAsync(_Buffer.AsMemory(_BufferCount), cancellationToken);
#endif

				_BufferCount += BytesRead;
				_EndOfStream = BytesRead == 0;
			}

			return _Buffer.AsMemory(_BufferIndex, _BufferCount);
		}

		/// <summary>
		/// Reads data into the buffer from the underlying Stream.
		/// </summary>
		/// <param name="minSize">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the stream has ended. When this occurs, subsequent calls should return an empty buffer.</remarks>
		public ReadOnlySpan<byte> GetSpan(int minSize) => GetMemory(minSize).Span;

		/// <summary>
		/// Advances the reader forward
		/// </summary>
		/// <param name="count">The number of bytes to advance over</param>
		public void Advance(int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (count == 0)
				return;

			if (count > _BufferCount)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_BufferCount == count)
			{
				ArrayPool<byte>.Shared.Return(_Buffer!);
				_Buffer = null;

				_BufferIndex = 0;
				_BufferCount = 0;
			}
			else
			{
				_BufferIndex += count;
				_BufferCount -= count;
			}
		}

		/// <summary>
		/// Cleans up the reader, returning any rented resources
		/// </summary>
		public void Dispose()
		{
			if (_Buffer != null)
			{
				ArrayPool<byte>.Shared.Return(_Buffer!);

				_Buffer = null;
				_BufferIndex = 0;
				_BufferCount = 0;
			}
		}

		//****************************************

		private bool PrepareForRead(int minSize)
		{
			if (_Buffer == null)
			{
				minSize = Math.Max(minSize, _BlockSize);

				// No buffer, refill the entire thing
				_Buffer = ArrayPool<byte>.Shared.Rent(minSize);

				return true;
			}

			if (_Buffer.Length < minSize)
			{
				var NewBuffer = ArrayPool<byte>.Shared.Rent(minSize);

				// If we have unread bytes, shift them to the start of the buffer
				if (_BufferCount > 0)
					Array.Copy(_Buffer, _BufferIndex, NewBuffer, 0, _BufferCount);

				ArrayPool<byte>.Shared.Return(_Buffer);

				_Buffer = NewBuffer;
				_BufferIndex = 0;

				return true;
			}

			// Check if we have enough data to satisfy the request
			if (minSize == 0)
			{
				// If the caller asked for zero, we only need 1 byte or more
				if (_BufferCount > 0)
					return false;
			}
			else
			{
				// If they asked for a specific amount of data, we need to ensure we can give at least that
				if (_BufferCount >= minSize)
					return false;
			}

			// We need more data, so we need to move whatever's leftover
			if (_BufferCount > 0)
				Array.Copy(_Buffer, _BufferIndex, _Buffer, 0, _BufferCount);

			_BufferIndex = 0;

			return true;
		}

		//****************************************

		/// <summary>
		/// Gets the underlying <see cref="Stream"/> being read from
		/// </summary>
		public Stream Stream { get; }

		/// <summary>
		/// Gets when the reader has reached the end of the stream
		/// </summary>
		public bool EndOfStream => _EndOfStream && _BufferCount == 0;

		/// <summary>
		/// Gets whether there is more data to read
		/// </summary>
		public bool CanRead => _BufferCount > 0 || !_EndOfStream;
	}
}
