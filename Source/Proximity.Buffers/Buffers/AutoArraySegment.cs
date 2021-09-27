using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides disposal over an <see cref="System.ArraySegment{T}"/> rented from an <see cref="ArrayPool{T}"/>
	/// </summary>
	public static class AutoArraySegment
	{
		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArraySegment{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="segment">The rented array segment</param>
		/// <returns>An <see cref="AutoArraySegment{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArraySegment<T> Over<T>(ArraySegment<T> segment) => new(segment, ArrayPool<T>.Shared);

		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArraySegment{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="segment">The rented array segment</param>
		/// <param name="from">The <see cref="ArrayPool{T}"/> the array was originally rented from</param>
		/// <returns>An <see cref="AutoArraySegment{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArraySegment<T> Over<T>(ArraySegment<T> segment, ArrayPool<T> from) => new(segment, from);

		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArraySegment{T}"/>, clearing it upon return
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="segment">The rented array segment</param>
		/// <returns>An <see cref="AutoArraySegment{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArraySegment<T> OverWithClear<T>(ArraySegment<T> segment) => new(segment, ArrayPool<T>.Shared, true);

		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArraySegment{T}"/>, clearing it upon return
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="segment">The rented array segment</param>
		/// <param name="from">The <see cref="ArrayPool{T}"/> the array was originally rented from</param>
		/// <returns>An <see cref="AutoArraySegment{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArraySegment<T> OverWithClear<T>(ArraySegment<T> segment, ArrayPool<T> from) => new(segment, from, true);
	}

	/// <summary>
	/// Provides disposal over an <see cref="System.ArraySegment{T}"/> rented from an <see cref="ArrayPool{T}"/>
	/// </summary>
	/// <typeparam name="T">The type of array element</typeparam>
	public readonly struct AutoArraySegment<T> : IDisposable
	{
		internal AutoArraySegment(ArraySegment<T> segment,  ArrayPool<T> pool, bool willClear = false)
		{
			Segment = segment;
			Pool = pool;
			WillClear = willClear;
		}

		//****************************************

		/// <summary>
		/// Retrieves the underlying array
		/// </summary>
		/// <returns>The underlying array as a <see cref="Memory{T}"/></returns>
		public Memory<T> AsMemory() => Segment.AsMemory();

		/// <summary>
		/// Retrieves the underlying array
		/// </summary>
		/// <returns>The underlying array as a <see cref="Span{T}"/></returns>
		public Span<T> AsSpan() => Segment.AsSpan();

		/// <summary>
		/// Returns the rented buffer to the source pool
		/// </summary>
		public void Dispose()
		{
			if (Segment.Array != null)
				Pool.Return(Segment.Array, WillClear);
		}

		//****************************************

		/// <summary>
		/// Gets the rented <see cref="System.ArraySegment{T}"/>
		/// </summary>
		public ArraySegment<T> Segment { get; }

		/// <summary>
		/// Gets the <see cref="ArrayPool{T}"/> the array was rented from
		/// </summary>
		public ArrayPool<T> Pool { get; }

		/// <summary>
		/// Gets whether the rented array will be cleared when returned
		/// </summary>
		public bool WillClear { get; }

		//****************************************

		/// <summary>
		/// Converts an Auto Array Segment to the underlying <see cref="System.ArraySegment{T}"/>
		/// </summary>
		/// <param name="autoArrayPool">The <see cref="AutoArraySegment{T}"/> to convert</param>
		public static implicit operator ArraySegment<T>(AutoArraySegment<T> autoArrayPool) => autoArrayPool.Segment;

		/// <summary>
		/// Converts an Auto Array Segment to a <see cref="Span{T}"/> over the underlying array
		/// </summary>
		/// <param name="autoArrayPool">The <see cref="AutoArraySegment{T}"/> to convert</param>
		public static implicit operator Span<T>(AutoArraySegment<T> autoArrayPool) => autoArrayPool.Segment;
	}
}
