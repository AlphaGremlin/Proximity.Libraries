using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Buffers
{
	/// <summary>
	/// Provides some extensions for <see cref="IBufferWriter{T}"/> and <see cref="IBufferWriterAsync{T}"/>
	/// </summary>
	public static class BufferWriterExtensions
	{
		/// <summary>
		/// Writes a <see cref="ReadOnlySequence{T}"/> to a <see cref="IBufferWriter{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="writer">The writer to write to</param>
		/// <param name="value">The <see cref="ReadOnlySequence{T}"/> to read from</param>
		public static void Write<T>(this IBufferWriter<T> writer, ReadOnlySequence<T> value)
		{
			foreach (var Segment in value)
				writer.Write(Segment.Span);
		}

		/// <summary>
		/// Writes a <see cref="ReadOnlySequence{T}"/> to a <see cref="IBufferWriterAsync{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="writer">The writer to write to</param>
		/// <param name="value">The <see cref="ReadOnlySequence{T}"/> to read from</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A task representing the progress fo the write operation</returns>
		public static async ValueTask WriteAsync<T>(this IBufferWriterAsync<T> writer, ReadOnlySequence<T> value, CancellationToken token = default)
		{
			foreach (var Segment in value)
				await writer.WriteAsync(Segment.Span);
		}

		/// <summary>
		/// Writes a <see cref="ReadOnlyMemory{T}"/> to a <see cref="IBufferWriterAsync{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="writer">The writer to write to</param>
		/// <param name="value">The <see cref="ReadOnlyMemory{T}"/> to read from</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A task representing the progress fo the write operation</returns>
		public static ValueTask WriteAsync<T>(this IBufferWriterAsync<T> writer, ReadOnlyMemory<T> value, CancellationToken token = default) => writer.WriteAsync(value.Span, token);

		/// <summary>
		/// Writes a <see cref="ReadOnlySpan{T}"/> to a <see cref="IBufferWriterAsync{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="writer">The writer to write to</param>
		/// <param name="value">The <see cref="ReadOnlySpan{T}"/> to read from</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A task representing the progress fo the write operation</returns>
		public static ValueTask WriteAsync<T>(this IBufferWriterAsync<T> writer, ReadOnlySpan<T> value, CancellationToken token = default)
		{
			var Buffer = writer.GetMemory(value.Length);

			value.CopyTo(Buffer.Span);

			return writer.AdvanceAsync(value.Length, token);
		}
	}
}
