using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace System.Buffers
{
	/// <summary>
	/// Provides extensions for <see cref="ArrayPool{T}"/>
	/// </summary>
	public static class ArrayPoolExtensions
	{
		/// <summary>
		/// Conditionally sizes up an array allocated from an <see cref="ArrayPool{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="pool">The <see cref="ArrayPool{T}"/> the array was allocated from</param>
		/// <param name="buffer">The array we may want to resize. Must be allocated from <paramref name="pool"/> or returned from <see cref="Array.Empty{T}" /></param>
		/// <param name="minimumLength">The new minimum length of the array</param>
		/// <param name="alsoCopy">True to copy the contents of the previous buffer, False to leave it unallocated</param>
		/// <param name="alsoClear">True to clear the buffer when returning it, otherwise False</param>
		/// <remarks>If <paramref name="buffer"/> is zero-length, we assume it's from <see cref="Array.Empty{T}"/> and do not return it to the <see cref="ArrayPool{T}"/></remarks>
		public static void Resize<T>(this ArrayPool<T> pool,
#if !NETSTANDARD2_0
			[DisallowNull]
#endif
		ref T[] buffer, int minimumLength, bool alsoCopy = false, bool alsoClear = false)
		{
			// If our current buffer has enough space, do nothing
			if (buffer != null && buffer.Length >= minimumLength)
				return;

			T[]? OldBuffer = null;

			try
			{
				// Allocate a new buffer
				OldBuffer = Interlocked.Exchange(ref buffer!, pool.Rent(minimumLength));

				// Optionally copy the previous buffer contents
				if (alsoCopy)
					Array.Copy(OldBuffer, 0, buffer, 0, OldBuffer.Length);
			}
			finally
			{
				// If there was an old buffer, return it
				if (OldBuffer != null && OldBuffer.Length > 0)
					pool.Return(OldBuffer, alsoClear);
			}
		}
	}
}
