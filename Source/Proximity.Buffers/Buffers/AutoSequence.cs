using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides disposal over a <see cref="ReadOnlySequence{T}"/> with buffers rented from an <see cref="ArrayPool{T}"/>
	/// </summary>
	/// <typeparam name="T">The type of array element</typeparam>
	public readonly struct AutoSequence<T> : IDisposable
	{ //****************************************
		private readonly ReadOnlySequenceSegment<T>? _Head;
		//****************************************

		internal AutoSequence(ReadOnlySequence<T> sequence, ReadOnlySequenceSegment<T>? head, ArrayPool<T> pool, bool willClear = false)
		{
			Sequence = sequence;
			_Head = head;
			Pool = pool;
			WillClear = willClear;
		}

		//****************************************

		/// <summary>
		/// Returns the rented buffers to the source pool
		/// </summary>
		public void Dispose()
		{
			var Segment = _Head;

			while (Segment != null)
			{
				if (MemoryMarshal.TryGetArray(Segment.Memory, out var Buffer))
					Pool.Return(Buffer.Array!, WillClear);

				Segment = Segment.Next;
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
