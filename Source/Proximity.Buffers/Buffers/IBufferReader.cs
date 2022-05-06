using System;
using System.Collections.Generic;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides incremental pull reading
	/// </summary>
	/// <typeparam name="T">The type of element to read</typeparam>
	public interface IBufferReader<T>
	{
		/// <summary>
		/// Retrieves a buffer representing the next unread block of data
		/// </summary>
		/// <param name="minSize">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the underlying source has ended. When this occurs, subsequent calls should return an empty buffer.</remarks>
		ReadOnlyMemory<T> GetMemory(int minSize);

		/// <summary>
		/// Retrieves a buffer representing the next unread block of data
		/// </summary>
		/// <param name="minSize">The minimum desired block size. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		/// <remarks>Can be less than requested if the underlying source has ended. When this occurs, subsequent calls should return an empty buffer.</remarks>
		ReadOnlySpan<T> GetSpan(int minSize);

		/// <summary>
		/// Advances the reader forward
		/// </summary>
		/// <param name="count">The number of elements to advance over</param>
		void Advance(int count);

		//****************************************

		/// <summary>
		/// Gets whether there is more data to read
		/// </summary>
		/// <remarks>Can change from False to True when the underlying source receives more data (a file being written to, a socket, etc)</remarks>
		bool CanRead { get; }
	}
}
