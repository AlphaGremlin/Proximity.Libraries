using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Buffers
{
	/// <summary>
	/// Provides a buffer reader reading from a <see cref="ReadOnlySequence{T}"/> or compatible
	/// </summary>
	/// <typeparam name="T">The type of sequence element</typeparam>
	public sealed class BufferReader<T> : IBufferReader<T>, IDisposable
	{ //****************************************
		private ReadOnlySequence<T> _Buffer;
		private T[]? _OutputBuffer;
		private int _OutputUsed;
		//****************************************

		/// <summary>
		/// Creates a new Buffer Reader
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public BufferReader(T[] buffer)
		{
			_Buffer = new ReadOnlySequence<T>(buffer);
		}

		/// <summary>
		/// Creates a new Buffer Reader
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public BufferReader(ReadOnlyMemory<T> buffer)
		{
			_Buffer = new ReadOnlySequence<T>(buffer);
		}

		/// <summary>
		/// Creates a new Buffer Reader
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public BufferReader(ReadOnlySequence<T> buffer)
		{
			_Buffer = buffer;
		}

		//****************************************

		/// <summary>
		/// Reuses the reader with a new buffer
		/// </summary>
		/// <param name="buffer">The buffer to read from</param>
		public void Reset(ReadOnlySequence<T> buffer)
		{
			_Buffer = buffer;
		}

		/// <summary>
		/// Cleans up any buffers rented by the Reader
		/// </summary>
		public void Dispose()
		{
			var Buffer = Interlocked.Exchange(ref _OutputBuffer, null);

			if (Buffer != null)
				ArrayPool<T>.Shared.Return(Buffer);
		}

		/// <summary>
		/// Advances the reader forward
		/// </summary>
		/// <param name="count">The number of elements to advance over</param>
		public void Advance(int count)
		{
			// No going backwards
			_Buffer = _Buffer.Slice(count);

			// We allow Advance to move further than what's actually been retrieved.
			// This allows readers to skip bytes if necessary
			_OutputUsed = Math.Max(0, _OutputUsed - count);
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
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the underlying source has ended.</remarks>
		public ReadOnlySpan<T> GetSpan(int minSize) => GetMemory(minSize).Span;
	}
}
