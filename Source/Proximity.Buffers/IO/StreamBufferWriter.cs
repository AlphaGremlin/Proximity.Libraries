using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	/// <summary>
	/// Provides an <see cref="IBufferWriter{Byte}"/> that writes to a <see cref="Stream"/>
	/// </summary>
	public sealed class StreamBufferWriter : IBufferWriter<byte>, IBufferWriterAsync<byte>, IDisposable
	{ //****************************************
		private byte[]? _Buffer;
		//****************************************

		/// <summary>
		/// Creates a new Stream Buffer Writer
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to write to</param>
		public StreamBufferWriter(Stream stream)
		{
			Stream = stream;
		}

		//****************************************

		/// <summary>
		/// Writes <paramref name="count"/> bytes in the buffer returned from <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/> to the underlying Stream
		/// </summary>
		/// <param name="count">The number of data items written</param>
		public void Advance(int count)
		{
			if (count > 0)
#if NETSTANDARD2_0
				Stream.Write(_Buffer, 0, count);
#else
				Stream.Write(_Buffer.AsSpan(0, count));
#endif
		}

		/// <summary>
		/// Asynchronously writes <paramref name="count"/> bytes in the buffer returned from <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/> to the underlying Stream
		/// </summary>
		/// <param name="count">The number of data items written</param>
		/// <param name="cancellationToken">A cancellation token to abort waiting</param>
		/// <returns>A task that completes when the writer is ready for more data</returns>
		public ValueTask AdvanceAsync(int count, CancellationToken cancellationToken = default)
		{
			if (count > 0)
#if NETSTANDARD2_0
				return new ValueTask(Stream.WriteAsync(_Buffer, 0, count, cancellationToken));
#else
				return Stream.WriteAsync(_Buffer.AsMemory(0, count), cancellationToken);
#endif

			return default;
		}

		/// <summary>
		/// Cleans up the writer, returning any rented resources
		/// </summary>
		public void Dispose()
		{
			if (_Buffer != null)
			{
				ArrayPool<byte>.Shared.Return(_Buffer);

				_Buffer = null;
			}
		}

		/// <summary>
		/// Retrieves a buffer to write to that is at least the requested size
		/// </summary>
		/// <param name="sizeHint">The minimum length of the returned buffer. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		public Memory<byte> GetMemory(int sizeHint = 0) => GetSegment(sizeHint);

		/// <summary>
		/// Retrieves a buffer to write to that is at least the requested size
		/// </summary>
		/// <param name="sizeHint">The minimum length of the returned buffer. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		public Span<byte> GetSpan(int sizeHint = 0) => GetSegment(sizeHint).Span;

		//****************************************

		private Memory<byte> GetSegment(int sizeHint)
		{
			if (_Buffer == null)
			{
				_Buffer = ArrayPool<byte>.Shared.Rent(sizeHint);
			}
			else if (_Buffer.Length < sizeHint)
			{
				ArrayPool<byte>.Shared.Resize(ref _Buffer, sizeHint);
			}

			return _Buffer;
		}

		//****************************************

		/// <summary>
		/// Gets the underlying <see cref="Stream"/> being written to
		/// </summary>
		public Stream Stream { get; }
	}
}
