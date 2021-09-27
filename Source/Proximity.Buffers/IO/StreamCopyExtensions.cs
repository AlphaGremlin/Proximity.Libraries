using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	/// <summary>
	/// Provides CopyTo extensions that write to a Buffer Writer
	/// </summary>
	public static class StreamCopyExtensions
	{
		/// <summary>
		/// Synchronously reads the bytes from the current stream and writes them to an <see cref="IBufferReader{Byte}"/>
		/// </summary>
		/// <param name="source">The current stream to read from</param>
		/// <param name="target">The buffer writer to write to</param>
		public static void CopyTo(this Stream source, IBufferWriter<byte> target) => source.CopyTo(target, 81920); // Same buffer size as Stream.CopyTo

		/// <summary>
		/// Synchronously reads the bytes from the current stream and writes them to an <see cref="IBufferReader{Byte}"/>
		/// </summary>
		/// <param name="source">The current stream to read from</param>
		/// <param name="target">The buffer writer to write to</param>
		/// <param name="blockSize">The size of the blocks to read and write</param>
		public static void CopyTo(this Stream source, IBufferWriter<byte> target, int blockSize)
		{
			if (blockSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(blockSize));

			int ReadSize;

#if NETSTANDARD2_0
			byte[]? InBuffer = null;

			try
			{
				do
				{
					var Buffer = target.GetMemory(blockSize);

					if (MemoryMarshal.TryGetArray<byte>(Buffer, out var Segment))
					{
						// Have the stream read directly into the destination buffer
						ReadSize = source.Read(Segment.Array, Segment.Offset, Segment.Count);
					}
					else
					{
						// Not a buffer we can pass to Stream.Read, so we have to copy
						if (InBuffer == null)
							InBuffer = ArrayPool<byte>.Shared.Rent(blockSize);

						var Length = Math.Min(Buffer.Length, InBuffer.Length);

						ReadSize = source.Read(InBuffer, 0, Length);

						InBuffer.AsSpan(0, ReadSize).CopyTo(Buffer.Span);
					}

					target.Advance(ReadSize);
				}
				while (ReadSize > 0);
			}
			finally
			{
				if (InBuffer != null)
					ArrayPool<byte>.Shared.Return(InBuffer);
			}
#else
			do
			{
				var Buffer = target.GetSpan(blockSize);

				ReadSize = source.Read(Buffer);

				target.Advance(ReadSize);
			}
			while (ReadSize > 0);
#endif
		}

		/// <summary>
		/// Asynchronously reads the bytes from the current stream and writes them to an <see cref="IBufferReader{Byte}"/>
		/// </summary>
		/// <param name="source">The current stream to read from</param>
		/// <param name="target">The buffer writer to write to</param>
		/// <returns>A Task that completes when the stream has been read to completion</returns>
		public static ValueTask CopyToAsync(this Stream source, IBufferWriter<byte> target) => source.CopyToAsync(target, 81920, CancellationToken.None); // Same buffer size as Stream.CopyTo

		/// <summary>
		/// Asynchronously reads the bytes from the current stream and writes them to an <see cref="IBufferReader{Byte}"/>
		/// </summary>
		/// <param name="source">The current stream to read from</param>
		/// <param name="target">The buffer writer to write to</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A Task that completes when the stream has been read to completion</returns>
		public static ValueTask CopyToAsync(this Stream source, IBufferWriter<byte> target, CancellationToken token) => source.CopyToAsync(target, 81920, token); // Same buffer size as Stream.CopyTo

		/// <summary>
		/// Asynchronously reads the bytes from the current stream and writes them to an <see cref="IBufferReader{Byte}"/>
		/// </summary>
		/// <param name="source">The current stream to read from</param>
		/// <param name="target">The buffer writer to write to</param>
		/// <param name="blockSize">The size of the blocks to read and write</param>
		/// <returns>A Task that completes when the stream has been read to completion</returns>
		public static ValueTask CopyToAsync(this Stream source, IBufferWriter<byte> target, int blockSize) => source.CopyToAsync(target, blockSize, CancellationToken.None);

		/// <summary>
		/// Asynchronously reads the bytes from the current stream and writes them to an <see cref="IBufferReader{Byte}"/>
		/// </summary>
		/// <param name="source">The current stream to read from</param>
		/// <param name="target">The buffer writer to write to</param>
		/// <param name="blockSize">The size of the blocks to read and write</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A Task that completes when the stream has been read to completion</returns>
		public static async ValueTask CopyToAsync(this Stream source, IBufferWriter<byte> target, int blockSize, CancellationToken token)
		{
			if (blockSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(blockSize));

			int ReadSize;

#if NETSTANDARD2_0
			byte[]? InBuffer = null;

			try
			{
				do
				{
					var Buffer = target.GetMemory(blockSize);

					if (MemoryMarshal.TryGetArray<byte>(Buffer, out var Segment))
					{
						// Have the stream read directly into the destination buffer
						ReadSize = await source.ReadAsync(Segment.Array, Segment.Offset, Segment.Count, token);
					}
					else
					{
						// Not a buffer we can pass to Stream.Read, so we have to copy
						if (InBuffer == null)
							InBuffer = ArrayPool<byte>.Shared.Rent(blockSize);

						var Length = Math.Min(Buffer.Length, InBuffer.Length);

						ReadSize = await source.ReadAsync(InBuffer, 0, Length, token);

						InBuffer.AsSpan(0, ReadSize).CopyTo(Buffer.Span);
					}

					target.Advance(ReadSize);
				}
				while (ReadSize > 0);
			}
			finally
			{
				if (InBuffer != null)
					ArrayPool<byte>.Shared.Return(InBuffer);
			}
#else
			do
			{
				var Buffer = target.GetMemory(blockSize);

				ReadSize = await source.ReadAsync(Buffer, token);

				target.Advance(ReadSize);
			}
			while (ReadSize > 0);
#endif
		}
	}
}
