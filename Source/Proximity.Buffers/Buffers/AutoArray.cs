using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides disposal over an <see cref="System.Array"/> rented from an <see cref="ArrayPool{T}"/>
	/// </summary>
	public static class AutoArray
	{
		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArray{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="array">The rented array</param>
		/// <returns>An <see cref="AutoArray{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArray<T> Over<T>(T[] array) => new(array, ArrayPool<T>.Shared);

		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArray{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="array">The rented array</param>
		/// <param name="from">The <see cref="ArrayPool{T}"/> the array was originally rented from</param>
		/// <returns>An <see cref="AutoArray{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArray<T> Over<T>(T[] array, ArrayPool<T> from) => new(array, from);

		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArray{T}"/>, clearing it upon return
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="array">The rented array</param>
		/// <returns>An <see cref="AutoArray{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArray<T> OverWithClear<T>(T[] array) => new(array, ArrayPool<T>.Shared, true);

		/// <summary>
		/// Wraps a previously rented array with an <see cref="AutoArray{T}"/>, clearing it upon return
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="array">The rented array</param>
		/// <param name="from">The <see cref="ArrayPool{T}"/> the array was originally rented from</param>
		/// <returns>An <see cref="AutoArray{T}"/> providing automated disposal of the rented array</returns>
		public static AutoArray<T> OverWithClear<T>(T[] array, ArrayPool<T> from) => new(array, from, true);
	}

	/// <summary>
	/// Provides disposal over an <see cref="System.Array"/> rented from an <see cref="ArrayPool{T}"/>
	/// </summary>
	/// <typeparam name="T">The type of array element</typeparam>
	public readonly struct AutoArray<T> : IDisposable
	{
		internal AutoArray(T[] array, ArrayPool<T> pool, bool willClear = false)
		{
			Array = array;
			Pool = pool;
			WillClear = willClear;
		}

		//****************************************

		/// <summary>
		/// Retrieves the underlying array
		/// </summary>
		/// <returns>The underlying array as a <see cref="Memory{T}"/></returns>
		public Memory<T> AsMemory() => Array.AsMemory();

		/// <summary>
		/// Retrieves a segment of the underlying array
		/// </summary>
		/// <param name="start">The offset into the array to start</param>
		/// <returns>The underlying array as a <see cref="Memory{T}"/></returns>
		public Memory<T> AsMemory(int start) => Array.AsMemory(start);

		/// <summary>
		/// Retrieves a segment of the underlying array
		/// </summary>
		/// <param name="start">The offset into the array to start</param>
		/// <param name="length">The length of the array to retrieve</param>
		/// <returns>The underlying array as a <see cref="Memory{T}"/></returns>
		public Memory<T> AsMemory(int start, int length) => Array.AsMemory(start, length);

		/// <summary>
		/// Retrieves the underlying array
		/// </summary>
		/// <returns>The underlying array as a <see cref="Span{T}"/></returns>
		public Span<T> AsSpan() => Array.AsSpan();

		/// <summary>
		/// Retrieves a segment of the underlying array
		/// </summary>
		/// <param name="start">The offset into the array to start</param>
		/// <returns>The underlying array as a <see cref="Span{T}"/></returns>
		public Span<T> AsSpan(int start) => Array.AsSpan(start);

		/// <summary>
		/// Retrieves a segment of the underlying array
		/// </summary>
		/// <param name="start">The offset into the array to start</param>
		/// <param name="length">The length of the array to retrieve</param>
		/// <returns>The underlying array as a <see cref="Span{T}"/></returns>
		public Span<T> AsSpan(int start, int length) => Array.AsSpan(start, length);

		/// <summary>
		/// Returns the rented buffer to the source pool
		/// </summary>
		public void Dispose()
		{
			if (Array != null)
				Pool.Return(Array, WillClear);
		}

		//****************************************

		/// <summary>
		/// Gets the rented <see cref="System.Array"/>
		/// </summary>
		public T[] Array { get; }

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
		/// Converts an Auto Array to the underlying <see cref="System.Array"/>
		/// </summary>
		/// <param name="autoArrayPool">The <see cref="AutoArray{T}"/> to convert</param>
		public static implicit operator T[](AutoArray<T> autoArrayPool) => autoArrayPool.Array;

		/// <summary>
		/// Converts an Auto Array to a <see cref="Span{T}"/> over the underlying array
		/// </summary>
		/// <param name="autoArrayPool">The <see cref="AutoArray{T}"/> to convert</param>
		public static implicit operator Span<T>(AutoArray<T> autoArrayPool) => autoArrayPool.Array;
	}
}
