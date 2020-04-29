using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Buffers.Buffers
{
	public sealed class MemoryComparer<T> : IComparer<ReadOnlyMemory<T>>, IComparer<Memory<T>> where T : IComparable<T>
	{
		public int Compare(ReadOnlyMemory<T> left, ReadOnlyMemory<T> right) => left.Span.SequenceCompareTo(right.Span);

		public int Compare(Memory<T> left, Memory<T> right) => left.Span.SequenceCompareTo(right.Span);

		//****************************************

		public static MemoryComparer<T> Default { get; } = new MemoryComparer<T>();
	}
}
