using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Numerics;
#if !NETSTANDARD
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace System.Buffers
{
	/// <summary>
	/// Provides a block equality comparer for <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>, <see cref="Span{T}"/>, <see cref="ReadOnlySpan{T}"/>, and <see cref="Array"/>
	/// </summary>
	public abstract class BlockEqualityComparer
	{
		/// <summary>
		/// Gets an equality comparer 
		/// </summary>
		/// <param name="comparer"></param>
		/// <returns></returns>
		public static BlockEqualityComparer<T> From<T>(IEqualityComparer<T> comparer) => new WrappedEqualityComparer<T>(comparer ?? throw new ArgumentNullException(nameof(comparer)));

		/// <summary>
		/// Gets a block comparer using string comparison rules
		/// </summary>
		/// <param name="comparisonType">The string comparison to perform</param>
		/// <returns>The requested comparer</returns>
		public static BlockEqualityComparer<char> ForChar(StringComparison comparisonType = StringComparison.Ordinal)
		{
			return comparisonType switch
			{
#if NETSTANDARD
				StringComparison.Ordinal => BlockEqualityComparer<char>.Default,
#else
				StringComparison.Ordinal => StringOrdinalComparer.Ordinal,
#endif
				StringComparison.OrdinalIgnoreCase => StringEqualityComparer.OrdinalIgnoreCase,
				StringComparison.CurrentCulture => StringEqualityComparer.CurrentCulture,
				StringComparison.CurrentCultureIgnoreCase => StringEqualityComparer.CurrentCultureIgnoreCase,
				StringComparison.InvariantCulture => StringEqualityComparer.InvariantCulture,
				StringComparison.InvariantCultureIgnoreCase => StringEqualityComparer.InvariantCultureIgnoreCase,
				_ => throw new ArgumentOutOfRangeException(nameof(comparisonType)),
			};
		}

		//****************************************

		private protected sealed class ByteEqualityComparer : BlockEqualityComparer<byte>
		{ //****************************************
			private const int AdlerModulus = 65521;
			private const int AdlerMax = 5552;

#if !NETSTANDARD
			private const int BlockSize = 16;

			private const byte S23O1 = (((2) << 6) | ((3) << 4) | ((0) << 2) | ((1)));
			private const byte S1O32 = (((1) << 6) | ((0) << 4) | ((3) << 2) | ((2)));

			private static readonly Vector128<short> SseOnes;
			private static readonly Vector128<sbyte> SseTap;
#endif
			//****************************************

			static ByteEqualityComparer()
			{
#if !NETSTANDARD
				if (Ssse3.IsSupported)
				{
					SseTap = Vector128.Create(16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
					SseOnes = Vector128.Create((short)1);
				}
#endif
			}

			public override bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => left.SequenceEqual(right);

			public override int GetHashCode(ReadOnlySpan<byte> value)
			{
				// Adler-32 Checksum
				var A = 1u;
				var B = 0u;

#if !NETSTANDARD
				// Based on: https://github.com/SnowflakePowered/vcdiff/blob/b649ea08a6109b5fdbac4fca9565c98993c87e8c/src/VCDiff/Shared/Adler32.cs
				if (Ssse3.IsSupported)
				{
					var Vectors = MemoryMarshal.Cast<byte, Vector128<byte>>(value);

					while (!Vectors.IsEmpty)
					{
						var Count = Math.Min(Vectors.Length, AdlerMax / BlockSize);

						// B is the running total of every A value generated after each byte: For Each Value: (B = B + (A = A + Value)
						// We know the starting A will be added Count times, thus we can multiply it ahead of time by the number of operations.
						var PreviousSum = Vector128.Create(0, 0, 0, A * (uint)Count);
						var Sum = Vector128.Create(0u, 0, 0, 0);
						var SumSum = Vector128.Create(0, 0, 0, B);

						for (var Index = 0; Index < Count; Index++)
						{
							// Each block needs to include the sum from the previous block
							PreviousSum = Sse2.Add(PreviousSum, Sum);
							// Total up the bytes into two 16-bit numbers, and add them to the sum
							Sum = Sse2.Add(Sum, Sse2.SumAbsoluteDifferences(Vectors[Index], Vector128<byte>.Zero).AsUInt32());
							// The tap multiplies each byte by the number of times it would be added to B, then we sum them all together and add them to the secondary sum
							SumSum = Sse2.Add(SumSum, Sse2.MultiplyAddAdjacent(Ssse3.MultiplyAddAdjacent(Vectors[Index], SseTap), SseOnes).AsUInt32());
						}

						// PreviousSum needs to be multiplied by sixteen, since each operation processes that many bytes.
						// Add to the SumSum, since the loop doesn't total things fully
						SumSum = Sse2.Add(SumSum, Sse2.ShiftLeftLogical(PreviousSum, 4));

						// With 4 UInt32 values, calculate A+C, B+D, C+A, D+B
						Sum = Sse2.Add(Sum, Sse2.Shuffle(Sum, S23O1));
						// This then produces (A+C)+(B+D), (B+D)+(A+C), (C+A)+(D+B), (D+B)+(C+A)
						Sum = Sse2.Add(Sum, Sse2.Shuffle(Sum, S1O32));
						// Each UInt32 now has the same value. Extract and combine it with our A
						A = (A + Sse2.ConvertToUInt32(Sum)) % AdlerModulus;

						// Do the same with SumSum
						SumSum = Sse2.Add(SumSum, Sse2.Shuffle(SumSum, S23O1));
						SumSum = Sse2.Add(SumSum, Sse2.Shuffle(SumSum, S1O32));
						B = Sse2.ConvertToUInt32(SumSum) % AdlerModulus;

						// Once a section is processed, trim it off
						Vectors = Vectors.Slice(Count);
						value = value.Slice(Count * BlockSize);
					}
				}
				else
#endif
				{
					while (value.Length > AdlerMax)
					{
						for (var Index = 0; Index < AdlerMax; Index++)
						{
							A += value[Index];
							B += A;
						}

						A %= AdlerModulus;
						B %= AdlerModulus;

						value = value.Slice(AdlerMax);
					}
				}

				if (!value.IsEmpty)
				{
					for (var Index = 0; Index < value.Length; Index++)
					{
						A += value[Index];
						B += A;
					}

					A %= AdlerModulus;
					B %= AdlerModulus;
				}

				return (int)((B << 16) | A);
			}
		}

		private protected sealed class GenericEqualityComparer<T> : BlockEqualityComparer<T> where T : IEquatable<T>
		{
			public override bool Equals(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => left.SequenceEqual(right);

			public override int GetHashCode(ReadOnlySpan<T> value)
			{
				var HashCode = new HashCode();

				for (var Index = 0; Index < value.Length; Index++)
					HashCode.Add(value[Index]);

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
					HashCode.Add(value[Index]);

				return HashCode.ToHashCode();
			}
		}

#if !NETSTANDARD
		private protected sealed class StringOrdinalComparer : BlockEqualityComparer<char>
		{ //****************************************
			internal static readonly StringOrdinalComparer Ordinal = new();
			//****************************************

			private StringOrdinalComparer()
			{
			}

			//****************************************

			public override bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right) => left.SequenceEqual(right);

			public override int GetHashCode(ReadOnlySpan<char> value) => string.GetHashCode(value);
		}
#endif

		private protected sealed class StringEqualityComparer : BlockEqualityComparer<char>
		{ //****************************************
			internal static readonly StringEqualityComparer OrdinalIgnoreCase = new(StringComparison.OrdinalIgnoreCase);
			internal static readonly StringEqualityComparer CurrentCulture = new(StringComparison.CurrentCulture);
			internal static readonly StringEqualityComparer CurrentCultureIgnoreCase = new(StringComparison.CurrentCultureIgnoreCase);
			internal static readonly StringEqualityComparer InvariantCulture = new(StringComparison.InvariantCulture);
			internal static readonly StringEqualityComparer InvariantCultureIgnoreCase = new(StringComparison.InvariantCultureIgnoreCase);
			//****************************************
			private readonly StringComparison _Comparison;
			//****************************************

			private StringEqualityComparer(StringComparison comparison) => _Comparison = comparison;

			//****************************************

			public override bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right) => left.Equals(right, _Comparison);

			public override int GetHashCode(ReadOnlySpan<char> value) => value.GetHashCode(_Comparison);
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
				var HashCode = new HashCode();

				for (var Index = 0; Index < value.Length; Index++)
					HashCode.Add(value[Index], _Comparer);

				return HashCode.ToHashCode();
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
		public bool Equals(T[]? left, T[]? right) => Equals((ReadOnlySpan<T>)left.AsSpan(), right.AsSpan());

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
				return (BlockEqualityComparer<T>)Activator.CreateInstance(typeof(GenericEqualityComparer<>).MakeGenericType(MyType))!;

			// Nope, just use the default object comparer then
			return new ObjectEqualityComparer<T>();
		}
	}
}
