using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Buffers.Buffers
{
	public abstract class MemoryEqualityComparer<T> : IEqualityComparer<Memory<T>>, IEqualityComparer<ReadOnlyMemory<T>> where T : IEquatable<T>
	{ //****************************************
		private const int AdlerModulus = 65521;
		//****************************************

		public bool Equals(Memory<T> left, Memory<T> right) => left.Span.SequenceEqual(right.Span);

		public bool Equals(ReadOnlyMemory<T> left, ReadOnlyMemory<T> right) => left.Span.SequenceEqual(right.Span);

		public int GetHashCode(Memory<T> value) => GetHashCode((ReadOnlyMemory<T>)value);

		public int GetHashCode(ReadOnlyMemory<T> value)
		{
			var Content = value.Span;

			// Adler-32 Checksum
			var A = 1;
			var B = 0;

			for (var Index = 0; Index < Content.Length; Index++)
			{
				A = (A + Content[Index].GetHashCode()) % AdlerModulus;
				B = (B + A) % AdlerModulus;
			}

			return (B << 16) | A;
		}

		//****************************************

		public static MemoryEqualityComparer<ReadOnlyMemory<T>> Default { get; } = new MemoryEqualityComparer<T>();

		//****************************************
	}
}
