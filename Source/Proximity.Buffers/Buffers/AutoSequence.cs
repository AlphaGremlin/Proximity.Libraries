using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides disposal over an <see cref="System.ArraySegment{T}"/> rented from an <see cref="ArrayPool{T}"/>
	/// </summary>
	public static class AutoSequence
	{
		/// <summary>
		/// Converts an <see cref="AutoArray{T}"/> into an AutoSequence
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="array">The auto-rented array</param>
		/// <returns>An <see cref="AutoSequence{T}"/> providing automated disposal of the rented array</returns>
		/// <remarks>The original array must not be disposed of</remarks>
		public static AutoSequence<T> From<T>(AutoArray<T> array) => From(array.AsMemory(), array.Pool, array.WillClear);

		/// <summary>
		/// Converts an <see cref="AutoArray{T}"/> into an AutoSequence
		/// </summary>
		/// <typeparam name="T">The type of array element</typeparam>
		/// <param name="array">The auto-rented array</param>
		/// <returns>An <see cref="AutoSequence{T}"/> providing automated disposal of the rented array</returns>
		/// <remarks>The original array must not be disposed of</remarks>
		public static AutoSequence<T> From<T>(AutoArraySegment<T> array) => From(array.AsMemory(), array.Pool, array.WillClear);

		//****************************************

		private static AutoSequence<T> From<T>(ReadOnlyMemory<T> segment, ArrayPool<T> pool, bool willClear) => new(new ReadOnlySequence<T>(segment), pool, willClear);

		//****************************************

		private sealed class AutoSegment<T> : ReadOnlySequenceSegment<T>
		{
			internal AutoSegment(ReadOnlyMemory<T> memory) => Memory = memory;
		}
	}

	/// <summary>
	/// Provides disposal over a <see cref="ReadOnlySequence{T}"/> with buffers rented from an <see cref="ArrayPool{T}"/>
	/// </summary>
	/// <typeparam name="T">The type of array element</typeparam>
	public readonly struct AutoSequence<T> : IDisposable
	{
		internal AutoSequence(ReadOnlySequence<T> sequence, ArrayPool<T> pool, bool willClear = false)
		{
			Sequence = sequence;
			Pool = pool;
			WillClear = willClear;
		}

		//****************************************

		/// <summary>
		/// Returns the rented buffers to the source pool
		/// </summary>
		public void Dispose()
		{
			foreach (var Memory in Sequence)
			{
				if (MemoryMarshal.TryGetArray(Memory, out var Buffer))
					Pool.Return(Buffer.Array!, WillClear);
			}
		}

		//****************************************

		/// <summary>
		/// Gets the rented <see cref="ReadOnlySequence{T}"/>
		/// </summary>
		public ReadOnlySequence<T> Sequence { get; }

		/// <summary>
		/// Gets the <see cref="ArrayPool{T}"/> the underlying arrays were rented from
		/// </summary>
		public ArrayPool<T> Pool { get; }

		/// <summary>
		/// Gets whether the rented arrays will be cleared when returned
		/// </summary>
		public bool WillClear { get; }

		//****************************************

		/// <summary>
		/// Converts an Auto Sequence to the underlying <see cref="ReadOnlySequence{T}"/>
		/// </summary>
		/// <param name="autoSequence">The <see cref="AutoSequence{T}"/> to convert</param>
		public static implicit operator ReadOnlySequence<T>(AutoSequence<T> autoSequence) => autoSequence.Sequence;
	}
}
