using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Buffers
{
	/// <summary>
	/// Provides a buffer reader reading from a <see cref="ReadOnlySequence{T}"/> or compatible
	/// </summary>
	/// <typeparam name="T">The type of sequence element</typeparam>
	/// <remarks>When requesting 0-size blocks, we guarantee to not allocate any temporary buffers, and thus does not require <see cref="Dispose"/> to be called.</remarks>
	public sealed class BufferReader<T> : IBufferReader<T>, IBufferReaderAsync<T>, IDisposable
	{ //****************************************
		private ReadOnlySequence<T> _Buffer, _OriginalBuffer;
		private T[]? _OutputBuffer;
		private int _OutputUsed;
		//****************************************

		/// <summary>
		/// Creates a new Buffer Reader
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public BufferReader(T[] buffer) : this(new ReadOnlySequence<T>(buffer))
		{
		}

		/// <summary>
		/// Creates a new Buffer Reader
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public BufferReader(ReadOnlyMemory<T> buffer) : this(new ReadOnlySequence<T>(buffer))
		{
		}

		/// <summary>
		/// Creates a new Buffer Reader
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public BufferReader(ReadOnlySequence<T> buffer)
		{
			_Buffer = _OriginalBuffer = buffer;
		}

		//****************************************

		/// <summary>
		/// Reuses the reader with a new buffer
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public void Reset(ReadOnlySequence<T> buffer)
		{
			_Buffer = _OriginalBuffer = buffer;
			Position = 0;
			_OutputUsed = 0;
		}

		/// <summary>
		/// Restarts the reader from the start of the buffer
		/// </summary>
		public void Restart()
		{
			_Buffer = _OriginalBuffer;
			Position = 0;
			_OutputUsed = 0;
		}

		/// <summary>
		/// Restarts the reader from the given offset in the buffer
		/// </summary>
		/// <param name="position">The position into the buffer to start from</param>
		public void Restart(long position)
		{
			_Buffer = _OriginalBuffer.Slice(position);
			Position = position;
			_OutputUsed = 0;
		}

		/// <summary>
		/// Cleans up any buffers rented by the Reader
		/// </summary>
		public void Dispose()
		{
			var Buffer = Interlocked.Exchange(ref _OutputBuffer, null);

			if (Buffer != null)
				ArrayPool<T>.Shared.Return(Buffer);

			_Buffer = _OriginalBuffer = ReadOnlySequence<T>.Empty;
			_OutputUsed = 0;
			Position = 0;
		}

		/// <summary>
		/// Advances the reader forward
		/// </summary>
		/// <param name="count">The number of elements to advance over</param>
		public void Advance(int count)
		{
			if (count < 0 || count > _Buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (count == 0)
				return;

			// No going backwards
			_Buffer = _Buffer.Slice(count);

			// We allow Advance to move further than what's actually been retrieved.
			// This allows readers to skip bytes if necessary
			_OutputUsed = Math.Max(0, _OutputUsed - count);
			Position += count;
		}

		/// <summary>
		/// Retrieves a buffer representing the next unread block of data
		/// </summary>
		/// <param name="minSize">The minimum desired block size</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the underlying source has ended.</remarks>
		public ReadOnlyMemory<T> GetMemory(int minSize)
		{
			if (minSize > _Buffer.Length)
				minSize = (int)_Buffer.Length;

			// A 0-size block will always match this, so we never allocate
			if (_Buffer.First.Length >= minSize)
				return _Buffer.First;

			if (_OutputBuffer == null)
			{
				_OutputBuffer = ArrayPool<T>.Shared.Rent(minSize);
			}
			else if (_OutputBuffer.Length < minSize)
			{
				ArrayPool<T>.Shared.Resize(ref _OutputBuffer, minSize, _OutputUsed > 0);
			}

			_Buffer.Slice(0, minSize).CopyTo(_OutputBuffer.AsSpan(_OutputUsed));

			_OutputUsed += minSize;

			return _OutputBuffer;
		}

		/// <summary>
		/// Retrieves a buffer representing the next unread block of data
		/// </summary>
		/// <param name="minSize">The minimum desired block size</param>
		/// <param name="cancellationToken">A cancellation token to abort the operation</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the underlying source has ended.</remarks>
		public ValueTask<ReadOnlyMemory<T>> GetMemoryAsync(int minSize, CancellationToken cancellationToken = default) => new(GetMemory(minSize));

		/// <summary>
		/// Retrieves a buffer representing the next unread block of data
		/// </summary>
		/// <param name="minSize">The minimum desired block size</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the underlying source has ended.</remarks>
		public ReadOnlySpan<T> GetSpan(int minSize) => GetMemory(minSize).Span;

		//****************************************

		/// <summary>
		/// Gets the position the next call to <see cref="O:GetMemory"/> or <see cref="GetSpan"/> will read from
		/// </summary>
		public long Position { get; private set; }
	}
}
