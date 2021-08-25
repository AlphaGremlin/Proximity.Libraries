using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// A wrapper for an Array Pool that provides automated disposal
	/// </summary>
	public static class AutoArrayPool
	{
		/// <summary>
		/// Creates an <see cref="AutoArrayPool{T}"/> over the given <see cref="ArrayPool{T}"/> implementation
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="pool">The underlying <see cref="ArrayPool{T}"/> to rent from</param>
		/// <returns>A wrapper for the Array Pool that provides automated disposal</returns>
		public static AutoArrayPool<T> Over<T>(ArrayPool<T> pool) => new (pool);
	}

	/// <summary>
	/// A wrapper for an Array Pool that provides automated disposal
	/// </summary>
	/// <typeparam name="T">The type of array element</typeparam>
	public readonly struct AutoArrayPool<T>
	{
		/// <summary>
		/// Creates an <see cref="AutoArrayPool{T}"/> over the given <see cref="ArrayPool{T}"/> implementation
		/// </summary>
		/// <param name="pool">The underlying <see cref="ArrayPool{T}"/> to rent from</param>
		public AutoArrayPool(ArrayPool<T> pool) => Pool = pool;

		//****************************************

		/// <summary>
		/// Rents an array from the Array Pool
		/// </summary>
		/// <param name="minimumLength">The minimum length of the returned array</param>
		/// <returns>An <see cref="AutoArray{T}"/> providing automated disposal of the returned array</returns>
		public AutoArray<T> Rent(int minimumLength) => new (Pool.Rent(minimumLength), Pool);

		/// <summary>
		/// Rents an array from the Array Pool, clearing it upon return
		/// </summary>
		/// <param name="minimumLength">The minimum length of the returned array</param>
		/// <returns>An <see cref="AutoArray{T}"/> providing automated disposal of the returned array</returns>
		public AutoArray<T> RentWithClear(int minimumLength) => new (Pool.Rent(minimumLength), Pool, true);

		//****************************************

		/// <summary>
		/// Gets the underlying <see cref="ArrayPool{T}"/> to rent from
		/// </summary>
		public ArrayPool<T> Pool { get; }

		//****************************************

		/// <summary>
		/// Gets an <see cref="AutoArrayPool{T}"/> over <see cref="ArrayPool{T}.Shared"/>
		/// </summary>
		public static AutoArrayPool<T> Shared => new (ArrayPool<T>.Shared);

		/// <summary>
		/// Converts an <see cref="AutoArrayPool{T}"/> to the underlying <see cref="ArrayPool{T}"/>
		/// </summary>
		/// <param name="autoArrayPool">The <see cref="AutoArrayPool{T}"/> to convert</param>
		public static implicit operator ArrayPool<T>(AutoArrayPool<T> autoArrayPool) => autoArrayPool.Pool;
	}
}
