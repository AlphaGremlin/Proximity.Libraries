using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Buffers
{
	/// <summary>
	/// Provides a block comparer for <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>, and <see cref="Array"/>
	/// </summary>
	public abstract class BlockComparer
	{
		/// <summary>
		/// Gets a block comparer wrapping an <see cref="IComparer{T}"/>
		/// </summary>
		/// <param name="comparer">The comparer to wrap</param>
		/// <typeparam name="T">The type of element in the block</typeparam>
		/// <returns>A block comparer wrapping the given comparer</returns>
		public static BlockComparer<T> From<T>(IComparer<T> comparer) => new WrappedComparer<T>(comparer ?? throw new ArgumentNullException(nameof(comparer)));

		/// <summary>
		/// Gets a block comparer using string comparison rules
		/// </summary>
		/// <param name="comparisonType">The string comparison to perform</param>
		/// <returns>The requested comparer</returns>
		public static BlockComparer<char> ForChar(StringComparison comparisonType = StringComparison.Ordinal)
		{
			return comparisonType switch
			{
				StringComparison.Ordinal => BlockComparer<char>.Default,
				StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
				StringComparison.CurrentCulture => StringComparer.CurrentCulture,
				StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
				StringComparison.InvariantCulture => StringComparer.InvariantCulture,
				StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
				_ => throw new ArgumentOutOfRangeException(nameof(comparisonType)),
			};
		}

		//****************************************

		private protected sealed class GenericComparer<T> : BlockComparer<T> where T : IComparable<T>
		{
			public override int Compare(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => left.SequenceCompareTo(right);
		}

		private protected sealed class ObjectComparer<T> : BlockComparer<T> where T : IComparable
		{
			public override int Compare(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
			{
				var CompareLength = Math.Min(left.Length, right.Length);

				for (var Index = 0; Index < CompareLength; Index++)
				{
					var Left = left[Index];

					if (Left == null)
					{
						if (right[Index] == null)
							continue;

						return -1;
					}

					var Right = right[Index];

					if (Right == null)
						return 1;

					var Result = Left.CompareTo(Right);

					if (Result != 0)
						return Result;
				}

				if (left.Length > right.Length)
					return 1;

				if (left.Length < right.Length)
					return -1;

				return 0;
			}
		}

		private protected sealed class StringComparer : BlockComparer<char>
		{ //****************************************
			internal static readonly StringComparer OrdinalIgnoreCase = new StringComparer(StringComparison.OrdinalIgnoreCase);
			internal static readonly StringComparer CurrentCulture = new StringComparer(StringComparison.CurrentCulture);
			internal static readonly StringComparer CurrentCultureIgnoreCase = new StringComparer(StringComparison.CurrentCultureIgnoreCase);
			internal static readonly StringComparer InvariantCulture = new StringComparer(StringComparison.InvariantCulture);
			internal static readonly StringComparer InvariantCultureIgnoreCase = new StringComparer(StringComparison.InvariantCultureIgnoreCase);
			//****************************************
			private readonly StringComparison _Comparison;
			//****************************************

			private StringComparer(StringComparison comparison) => _Comparison = comparison;

			//****************************************

			public override int Compare(ReadOnlySpan<char> left, ReadOnlySpan<char> right) => left.CompareTo(right, _Comparison);
		}

		/// <summary>
		/// Provides a reversed comparer wrapping an existing comparer
		/// </summary>
		private protected sealed class WrappedComparer<T> : BlockComparer<T>
		{ //****************************************
			private readonly IComparer<T> _Comparer;
			//****************************************

			internal WrappedComparer(IComparer<T> comparer) => _Comparer = comparer;

			//****************************************

			public override int Compare(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => left.SequenceCompareTo(right, _Comparer);
		}
	}

	/// <summary>
	/// Provides a block comparer for <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory{T}"/>, and <see cref="Array"/>
	/// </summary>
	/// <typeparam name="T">The type of element in the block</typeparam>
	public abstract class BlockComparer<T> : BlockComparer, IComparer<ReadOnlyMemory<T>>, IComparer<Memory<T>>, IComparer<T[]>
	{ //****************************************
		private static BlockComparer<T>? _DefaultComparer;
		//****************************************

		/// <summary>
		/// Compares two blocks
		/// </summary>
		/// <param name="left">The left block</param>
		/// <param name="right">The right block</param>
		/// <returns>Positive if left is greater than right, negative if left is less than right, zero if left is equal to right</returns>
		public int Compare(ReadOnlyMemory<T> left, ReadOnlyMemory<T> right) => Compare(left.Span, right.Span);

		/// <summary>
		/// Compares two blocks
		/// </summary>
		/// <param name="left">The left block</param>
		/// <param name="right">The right block</param>
		/// <returns>Positive if left is greater than right, negative if left is less than right, zero if left is equal to right</returns>
		public int Compare(Memory<T> left, Memory<T> right) => Compare((ReadOnlySpan<T>)left.Span, right.Span);

		/// <summary>
		/// Compares two blocks
		/// </summary>
		/// <param name="left">The left block</param>
		/// <param name="right">The right block</param>
		/// <returns>Positive if left is greater than right, negative if left is less than right, zero if left is equal to right</returns>
		public int Compare(T[]? left, T[]? right) => Compare((ReadOnlySpan<T>)left.AsSpan(), right.AsSpan());

		/// <summary>
		/// Compares two blocks
		/// </summary>
		/// <param name="left">The left block</param>
		/// <param name="right">The right block</param>
		/// <returns>Positive if left is greater than right, negative if left is less than right, zero if left is equal to right</returns>
		public int Compare(Span<T> left, Span<T> right) => Compare((ReadOnlySpan<T>)left, right);

		/// <summary>
		/// Compares two blocks
		/// </summary>
		/// <param name="left">The left block</param>
		/// <param name="right">The right block</param>
		/// <returns>Positive if left is greater than right, negative if left is less than right, zero if left is equal to right</returns>
		public abstract int Compare(ReadOnlySpan<T> left, ReadOnlySpan<T> right);

		//****************************************

		/// <summary>
		/// Gets a comparer for <see cref="Memory{T}"/> and <see cref="ReadOnlyMemory{T}"/>
		/// </summary>
		public static BlockComparer<T> Default
		{
			get
			{
				if (_DefaultComparer == null)
					Interlocked.CompareExchange(ref _DefaultComparer, CreateComparer(), null);

				return _DefaultComparer;
			}
		}

		//****************************************

		private static BlockComparer<T> CreateComparer()
		{ //****************************************
			var Type = typeof(T);
			//****************************************

			// Does TValue implement the generic IComparable?
			if (typeof(IComparable<T>).IsAssignableFrom(Type))
				return (BlockComparer<T>)Activator.CreateInstance(typeof(GenericComparer<>).MakeGenericType(Type))!;

			// The object IComparable?
			if (typeof(IComparable).IsAssignableFrom(Type))
				return (BlockComparer<T>)Activator.CreateInstance(typeof(ObjectComparer<>).MakeGenericType(Type))!;

			// Nope, so we can't compare them
			throw new InvalidOperationException("Cannot compare this type");
		}
	}
}

