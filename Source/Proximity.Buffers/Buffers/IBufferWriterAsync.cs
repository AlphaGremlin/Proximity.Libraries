using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Buffers
{
	/// <summary>
	/// Provides asynchronous incremental push writing
	/// </summary>
	/// <typeparam name="T">The type of element to write</typeparam>
	public interface IBufferWriterAsync<T>
	{
		/// <summary>
		/// Notifies the writer that <paramref name="count"/> items were written to the buffer from <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/>
		/// </summary>
		/// <param name="count">The number of data items written</param>
		/// <param name="cancellationToken">A cancellation token to abort waiting</param>
		/// <returns>A task that completes when the writer is ready for more data</returns>
		ValueTask AdvanceAsync(int count, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves a buffer to write to that is at least the requested size
		/// </summary>
		/// <param name="sizeHint">The minimum length of the returned buffer. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		Memory<T> GetMemory(int sizeHint = 0);

		/// <summary>
		/// Retrieves a buffer representing the next unread block of data
		/// </summary>
		/// <param name="sizeHint">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		Span<T> GetSpan(int sizeHint = 0);
	}
}
