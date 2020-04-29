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
		/// Conditionally sizes up an array allocated from an array pool
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="pool">The array pool the array was allocated from</param>
		/// <param name="buffer">The array we may want to resize</param>
		/// <param name="size">The new minimum size of the array</param>
		/// <param name="alsoCopy">True to copy the contents of the previous buffer, False to leave it unallocated</param>
		public static void Resize<T>(this ArrayPool<T> pool,
#if !NETSTANDARD2_0
			[DisallowNull]
#endif
		ref T[] buffer, int size, bool alsoCopy = false)
		{
			// If our current buffer has enough space, do nothing
			if (buffer != null && buffer.Length >= size)
				return;

			T[]? OldBuffer = null;

			try
			{
				// Allocate a new buffer
				OldBuffer = Interlocked.Exchange(ref buffer!, pool.Rent(size));

				// Optionally copy the previous buffer contents
				if (alsoCopy)
					Array.Copy(OldBuffer, 0, buffer, 0, OldBuffer.Length);
			}
			finally
			{
				// If there was an old buffer, return it
				if (OldBuffer != null)
					pool.Return(OldBuffer);
			}
		}
	}
}
