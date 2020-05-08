using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Buffers
{
	/// <summary>
	/// Provides a block equality comparer for <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>, and <see cref="Array"/>
	/// </summary>
	public abstract class BlockEqualityComparer
	{
		/// <summary>
		/// Gets an equality comparer 
		/// </summary>
		/// <param name="comparer"></param>
		/// <returns></returns>
		public static BlockEqualityComparer<T> From<T>(IEqualityComparer<T> comparer) => new WrappedEqualityComparer<T>(comparer ?? throw new ArgumentNullException(nameof(comparer)));

		//****************************************

		private protected sealed class ByteEqualityComparer : BlockEqualityComparer<byte>
		{ //****************************************
			private const int AdlerModulus = 65521;
			//****************************************

			public override bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => left.SequenceEqual(right);

			public override int GetHashCode(ReadOnlySpan<byte> value)
			{
				// Adler-32 Checksum
				var A = 1;
				var B = 0;

				for (var Index = 0; Index < value.Length; Index++)
				{
					A = (A + value[Index]) % AdlerModulus;
					B = (B + A) % AdlerModulus;
				}

				return (B << 16) | A;
			}
		}

		private protected sealed class GenericEqualityComparer<T> : BlockEqualityComparer<T> where T : IEquatable<T>
		{
			public override bool Equals(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => left.SequenceEqual(right);

			public override int GetHashCode(ReadOnlySpan<T> value)
			{
				var HashCode = new HashCode();

				for (var Index = 0; Index < value.Length; Index++)
					HashCode.Add(value[Index].GetHashCode());

				return HashCode.ToHashCode();
			}
		}

		private protected sealed class ObjectEqualityComparer<T> : BlockEqualityComparer<T>
		{
			public override bool Equals(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
			{
				if (left.Length != right.Length)
					return false;

				for (var Index = 0; Index < left.Length; Index++)
				{
					if (!Equals(left[Index], right[Index]))
						return false;
				}

				return true;
			}

			public override int GetHashCode(ReadOnlySpan<T> value)
			{
				var HashCode = new HashCode();

				for (var Index = 0; Index < value.Length; Index++)
					HashCode.Add(value[Index]?.GetHashCode());

				return HashCode.ToHashCode();
			}
		}

		private protected sealed class WrappedEqualityComparer<T> : BlockEqualityComparer<T>
		{ //****************************************
			private readonly IEqualityComparer<T> _Comparer;
			//****************************************

			public WrappedEqualityComparer(IEqualityComparer<T> comparer)
			{
				_Comparer = comparer;
			}

			//****************************************

			public override bool Equals(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
			{
				if (left.Length != right.Length)
					return false;

				for (var Index = 0; Index < left.Length; Index++)
				{
					if (!_Comparer.Equals(left[Index], right[Index]))
						return false;
				}

				return true;
			}

			public override int GetHashCode(ReadOnlySpan<T> value)
			{
				var HashCode = 0;

				for (var Index = 0; Index < value.Length; Index++)
					HashCode ^= _Comparer.GetHashCode(value[Index]);

				return HashCode;
			}
		}
	}

	/// <summary>
	/// Provides a block equality comparer for <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>, and <see cref="Array"/>
	/// </summary>
	/// <typeparam name="T">The type of element in the block</typeparam>
	public abstract class BlockEqualityComparer<T> : BlockEqualityComparer, IEqualityComparer<Memory<T>>, IEqualityComparer<ReadOnlyMemory<T>>, IEqualityComparer<T[]>
	{ //****************************************
		private static BlockEqualityComparer<T>? _DefaultComparer;
		//****************************************

		/// <summary>
		/// Determines whether the specified memory blocks are equal
		/// </summary>
		/// <param name="left">The left memory block</param>
		/// <param name="right">The right memory block</param>
		/// <returns>True if both blocks are equal, otherwise False</returns>
		public bool Equals(Memory<T> left, Memory<T> right) => Equals((ReadOnlySpan<T>)left.Span, right.Span);

		/// <summary>
		/// Determines whether the specified memory blocks are equal
		/// </summary>
		/// <param name="left">The left memory block</param>
		/// <param name="right">The right memory block</param>
		/// <returns>True if both blocks are equal, otherwise False</returns>
		public bool Equals(ReadOnlyMemory<T> left, ReadOnlyMemory<T> right) => Equals(left.Span, right.Span);

		/// <summary>
		/// Determines whether the specified spans are equal
		/// </summary>
		/// <param name="left">The left memory block</param>
		/// <param name="right">The right memory block</param>
		/// <returns>True if both spans are equal, otherwise False</returns>
		public bool Equals(Span<T> left, Span<T> right) => Equals((ReadOnlySpan<T>)left, right);

		/// <summary>
		/// Determines whether the specified arrays are equal
		/// </summary>
		/// <param name="left">The left array</param>
		/// <param name="right">The right array</param>
		/// <returns>True if both arrays are equal, otherwise False</returns>
		public bool Equals(T[] left, T[] right) => Equals((ReadOnlySpan<T>)left.AsSpan(), right.AsSpan());

		/// <summary>
		/// Determines whether the specified spans are equal
		/// </summary>
		/// <param name="left">The left memory block</param>
		/// <param name="right">The right memory block</param>
		/// <returns>True if both spans are equal, otherwise False</returns>
		public abstract bool Equals(ReadOnlySpan<T> left, ReadOnlySpan<T> right);

		/// <summary>
		/// Calculates a HashCode for a memory block
		/// </summary>
		/// <param name="value">The memory block to examine</param>
		/// <returns>A HashCode generated from the contents</returns>
		public int GetHashCode(Memory<T> value) => GetHashCode((ReadOnlySpan<T>)value.Span);

		/// <summary>
		/// Calculates a HashCode for a memory block
		/// </summary>
		/// <param name="value">The memory block to examine</param>
		/// <returns>A HashCode generated from the contents</returns>
		public int GetHashCode(ReadOnlyMemory<T> value) => GetHashCode(value.Span);

		/// <summary>
		/// Calculates a HashCode for a span
		/// </summary>
		/// <param name="value">The span to examine</param>
		/// <returns>A HashCode generated from the contents</returns>
		public int GetHashCode(Span<T> value) => GetHashCode((ReadOnlySpan<T>)value);

		/// <summary>
		/// Calculates a HashCode for an array
		/// </summary>
		/// <param name="value">The array to examine</param>
		/// <returns>A HashCode generated from the contents</returns>
		public int GetHashCode(T[] value) => GetHashCode((ReadOnlySpan<T>)value.AsSpan());

		/// <summary>
		/// Calculates a HashCode for a span
		/// </summary>
		/// <param name="value">The span to examine</param>
		/// <returns>A HashCode generated from the contents</returns>
		public abstract int GetHashCode(ReadOnlySpan<T> value);

		//****************************************

		/// <summary>
		/// Gets an equality comparer for <see cref="Memory{T}"/> and <see cref="ReadOnlyMemory{T}"/>
		/// </summary>
		public static BlockEqualityComparer<T> Default
		{
			get
			{
				if (_DefaultComparer == null)
					Interlocked.CompareExchange(ref _DefaultComparer, CreateComparer(), null);

				return _DefaultComparer;
			}
		}

		//****************************************

		private static BlockEqualityComparer<T> CreateComparer()
		{ //****************************************
			var MyType = typeof(T);
			//****************************************

			// Optimisation for Bytes
			if (typeof(T) == typeof(byte))
				return (BlockEqualityComparer<T>)(object)new ByteEqualityComparer();

			// Does TValue implement the generic IEquatable?
			if (typeof(IEquatable<T>).IsAssignableFrom(MyType))
				return (BlockEqualityComparer<T>)Activator.CreateInstance(typeof(GenericEqualityComparer<>).MakeGenericType(MyType));

			// Nope, just use the default object comparer then
			return new ObjectEqualityComparer<T>();
		}
	}
}
