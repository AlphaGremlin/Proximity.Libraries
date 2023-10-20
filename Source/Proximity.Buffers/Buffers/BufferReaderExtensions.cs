using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Buffers
{
	/// <summary>
	/// Provides some extensions for <see cref="IBufferReader{T}"/> and <see cref="IBufferReaderAsync{T}"/>
	/// </summary>
	public static class BufferReaderExtensions
	{
		/// <summary>
		/// Reads a <see cref="IBufferReader{T}"/> to completion, writing to a <see cref="IBufferWriter{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="reader">The reader to read from</param>
		/// <param name="writer">The writer to write to</param>
		public static void CopyTo<T>(this IBufferReader<T> reader, IBufferWriter<T> writer)
		{
			while (reader.CanRead)
			{
				var InBuffer = reader.GetSpan(0);

				if (InBuffer.IsEmpty)
					break;

				var OutBuffer = writer.GetSpan(InBuffer.Length);

				InBuffer.CopyTo(OutBuffer);

				writer.Advance(InBuffer.Length);
				reader.Advance(InBuffer.Length);
			}
		}

		/// <summary>
		/// Reads a <see cref="IBufferReader{T}"/> to completion, writing to a <see cref="IBufferWriterAsync{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="reader">The reader to read from</param>
		/// <param name="writer">The writer to write to</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A task representing the progress of the write operation</returns>
		public static async ValueTask CopyToAsync<T>(this IBufferReader<T> reader, IBufferWriterAsync<T> writer, CancellationToken token = default)
		{
			while (reader.CanRead)
			{
				var InBuffer = reader.GetMemory(0);

				if (InBuffer.IsEmpty)
					break;

				InBuffer.Span.CopyTo(writer.GetSpan(InBuffer.Length));

				await writer.AdvanceAsync(InBuffer.Length, token);
				reader.Advance(InBuffer.Length);
			}
		}

		/// <summary>
		/// Reads a <see cref="IBufferReaderAsync{T}"/> to completion, writing to a <see cref="IBufferWriter{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="reader">The reader to read from</param>
		/// <param name="writer">The writer to write to</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A task representing the progress of the write operation</returns>
		public static async ValueTask CopyToAsync<T>(this IBufferReaderAsync<T> reader, IBufferWriter<T> writer, CancellationToken token = default)
		{
			while (reader.CanRead)
			{
				var InBuffer = await reader.GetMemoryAsync(0, token);

				if (InBuffer.IsEmpty)
					break;

				InBuffer.Span.CopyTo(writer.GetSpan(InBuffer.Length));

				writer.Advance(InBuffer.Length);
				reader.Advance(InBuffer.Length);
			}
		}

		/// <summary>
		/// Reads a <see cref="IBufferReaderAsync{T}"/> to completion, writing to a <see cref="IBufferWriterAsync{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="reader">The reader to read from</param>
		/// <param name="writer">The writer to write to</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A task representing the progress of the write operation</returns>
		public static async ValueTask CopyToAsync<T>(this IBufferReaderAsync<T> reader, IBufferWriterAsync<T> writer, CancellationToken token = default)
		{
			while (reader.CanRead)
			{
				var InBuffer = await reader.GetMemoryAsync(0, token);

				if (InBuffer.IsEmpty)
					break;

				InBuffer.Span.CopyTo(writer.GetSpan(InBuffer.Length));

				await writer.AdvanceAsync(InBuffer.Length, token);
				reader.Advance(InBuffer.Length);
			}
		}

		/// <summary>
		/// Reads a <see cref="IBufferReader{T}"/> to completion, producing an array
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="reader">The reader to read from</param>
		public static T[] ReadAll<T>(this IBufferReader<T> reader)
		{
			using var Writer = new BufferWriter<T>();

			reader.CopyTo(Writer);

			return Writer.ToArray();
		}

		/// <summary>
		/// Reads a <see cref="IBufferReaderAsync{T}"/> to completion, producing an array
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="reader">The reader to read from</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A task representing the progress of the read, returning the resulting array</returns>
		public static async ValueTask<T[]> ReadAllAsync<T>(this IBufferReaderAsync<T> reader, CancellationToken token = default)
		{
			using var Writer = new BufferWriter<T>();

			await reader.CopyToAsync(Writer, token);

			return Writer.ToArray();
		}
	}
}
