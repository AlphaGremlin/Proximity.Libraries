using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ReadOnly;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
	/// <summary>
	/// A delegate describing a SelectWhere implementation
	/// </summary>
	/// <typeparam name="TInput">The type of input to the select</typeparam>
	/// <typeparam name="TOutput">The type of output from the select</typeparam>
	/// <param name="input">The input value</param>
	/// <param name="output">The output value</param>
	/// <returns>True to select this value, otherwise False</returns>
	public delegate bool SelectWherePredicate<in TInput, TOutput>(TInput input,
#if !NETSTANDARD2_0 && !NET40
		[MaybeNullWhen(false)]
#endif
		out TOutput output);

	/// <summary>
	/// Provides useful Linq-style methods
	/// </summary>
	public static class LinqExtensions
	{
		/// <summary>
		/// Splits an enumeration into chunks of equal or lesser size
		/// </summary>
		/// <typeparam name="T">The enumeration element type</typeparam>
		/// <param name="source">The source enumeration</param>
		/// <param name="size">The size of each chunk</param>
		/// <returns>An enumeration of lists where each chunk is at most <paramref name="size"/> length</returns>
		/// <remarks>If <paramref name="source"/> yields no results, no chunks are returned</remarks>
		public static IEnumerable<IReadOnlyList<T>> Chunk<T>(this IEnumerable<T> source, int size)
		{
			List<T>? CurrentChunk = null;

			foreach (var Item in source)
			{
				if (CurrentChunk == null)
					// Creating a new List after each yield prevents unexpected behaviour when saving the results of Chunk() (such as calling .ToArray())
					CurrentChunk = new List<T>(size);

				CurrentChunk.Add(Item);

				if (CurrentChunk.Count == size)
				{
#if NET40
					yield return new ReadOnlyList<T>(CurrentChunk);
#else

					yield return CurrentChunk;
#endif

					CurrentChunk = null;
				}
			}

			// Return any partial chunk
			if (CurrentChunk != null)
			{
#if NET40
				yield return new ReadOnlyList<T>(CurrentChunk);
#else

				yield return CurrentChunk;
#endif
			}
		}

		/// <summary>
		/// Splits an enumeration into chunks of equal or lesser size
		/// </summary>
		/// <typeparam name="T">The enumeration element type</typeparam>
		/// <param name="source">The source enumeration</param>
		/// <param name="size">The size of each chunk</param>
		/// <returns>An enumeration of lists where each chunk is at most <paramref name="size"/> length</returns>
		/// <remarks>If <paramref name="source"/> yields no results, a single empty chunk is returned</remarks>
		public static IEnumerable<IReadOnlyList<T>> ChunkOrDefault<T>(this IEnumerable<T> source, int size)
		{
			var HasYielded = false;
			List<T>? CurrentChunk = null;

			foreach (var Item in source)
			{
				if (CurrentChunk == null)
					// Creating a new List after each yield prevents unexpected behaviour when saving the results of Chunk() (such as calling .ToArray())
					CurrentChunk = new List<T>(size);

				CurrentChunk.Add(Item);

				if (CurrentChunk.Count == size)
				{
					HasYielded = true;

#if NET40
					yield return new ReadOnlyList<T>(CurrentChunk);
#else

					yield return CurrentChunk;
#endif

					CurrentChunk = null;
				}
			}

			// If we haven't returned a single chunk, return an empty one
			if (!HasYielded)
				yield return Empty.ReadOnlyList<T>();
		}

		/// <summary>
		/// Performs a Select-Where operation
		/// </summary>
		/// <typeparam name="TInput">The type of input to the select</typeparam>
		/// <typeparam name="TOutput">The type of output from the select</typeparam>
		/// <param name="input">The input enumeration</param>
		/// <param name="predicate">A predicate that performs the select-where</param>
		/// <returns>An enumeration of outputs where the SelectWhere predicate returned True</returns>
		public static IEnumerable<TOutput> SelectWhere<TInput, TOutput>(this IEnumerable<TInput> input, SelectWherePredicate<TInput, TOutput> predicate)
		{
			foreach (var InRecord in input)
			{
				if (predicate(InRecord, out var OutRecord))
					yield return OutRecord;
			}
		}

		/// <summary>
		/// Performs a Select-Where operation
		/// </summary>
		/// <typeparam name="TInput">The type of input to the select</typeparam>
		/// <typeparam name="TOutput">The type of output from the select</typeparam>
		/// <typeparam name="TResult">The type of output from the result selector</typeparam>
		/// <param name="input">The input enumeration</param>
		/// <param name="predicate">A predicate that performs the select-where</param>
		/// <param name="resultSelector">A selector that generates the result of the operation</param>
		/// <returns>An enumeration of outputs where the SelectWhere predicate returned True</returns>
		public static IEnumerable<TResult> SelectWhere<TInput, TOutput, TResult>(this IEnumerable<TInput> input, SelectWherePredicate<TInput, TOutput> predicate, Func<TInput, TOutput, TResult> resultSelector)
		{
			foreach (var InRecord in input)
			{
				if (predicate(InRecord, out var OutRecord))
					yield return resultSelector(InRecord, OutRecord);
			}
		}

		/// <summary>
		/// Compares two sequences and ensures they have the same values, including duplicates, regardless of order
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The first sequence to compare</param>
		/// <param name="other">The second sequence to compare</param>
		/// <returns>True if the sequences have the same values, regardless of order</returns>
		public static bool SequenceEquivalent<T>(this IEnumerable<T> source, IEnumerable<T> other) => SequenceEquivalent(source, other, null);

		/// <summary>
		/// Compares two sequences and ensures they have the same values, including duplicates, regardless of order
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The first sequence to compare</param>
		/// <param name="other">The second sequence to compare</param>
		/// <param name="comparer">A comparer to use for the values. If omitted, uses the default Equality Comparer</param>
		/// <returns>True if the sequences have the same values, regardless of order</returns>
		public static bool SequenceEquivalent<T>(this IEnumerable<T> source, IEnumerable<T> other, IEqualityComparer<T>? comparer)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (other is null)
				throw new ArgumentNullException(nameof(other));

			if (comparer is null)
				comparer = EqualityComparer<T>.Default;

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
			// We explicitly ensure that if T is a null, we count it separately
			var Table = new Dictionary<T, int>(comparer);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
			var NullCount = 0;

			foreach (var Item in source)
			{
				if (Item is null)
					NullCount++;
				else if (Table.TryGetValue(Item, out var Count))
					Table[Item] = Count + 1;
				else
					Table[Item] = 1;
			}

			foreach (var Item in other)
			{
				if (Item is null)
					NullCount--;
				else if (Table.TryGetValue(Item, out var Count))
				{
					if (Count == 1)
						Table.Remove(Item);
					else
						Table[Item] = Count - 1;
				}
				else
					Table[Item] = -1;
			}

			return Table.Count == 0 && NullCount == 0;
		}

		/// <summary>
		/// Generates a copy of an enumeration as a dictionary
		/// </summary>
		/// <typeparam name="TKey">The type of key element</typeparam>
		/// <typeparam name="TValue">The type of value element</typeparam>
		/// <param name="source">The source enumeration</param>
		/// <returns>A copy of the enumeration as a dictionary</returns>
		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull => ToDictionary(source, null);

		/// <summary>
		/// Generates a copy of an enumeration as a dictionary
		/// </summary>
		/// <typeparam name="TKey">The type of key element</typeparam>
		/// <typeparam name="TValue">The type of value element</typeparam>
		/// <param name="source">The source enumeration</param>
		/// <param name="comparer">A comparer to use for the keys. If omitted, uses the default Equality Comparer</param>
		/// <returns>A copy of the enumeration as a dictionary</returns>
		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer) where TKey : notnull
		{
#if NETSTANDARD2_0
			var Result = new Dictionary<TKey, TValue>(comparer);

			foreach (var Item in source)
				Result.Add(Item.Key, Item.Value);

			return Result;
#else
			return new(source, comparer);
#endif
		}

		/// <summary>
		/// Converts an enumerable to a read-only collection
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The enumeration to convert</param>
		/// <returns>A read-only collection</returns>
		public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (source is IReadOnlyCollection<T> ReadOnlyCollection)
				return ReadOnlyCollection;

#if NET40
			return new ReadOnlyCollection<T>(source.ToArray());
#else
			return source.ToArray();
#endif
		}

		/// <summary>
		/// Converts an enumerable of key-value pairs to a read-only dictionary
		/// </summary>
		/// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
		/// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
		/// <param name="source">The enumeration to convert</param>
		/// <returns>A read-only dictionary</returns>
		public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (source is IReadOnlyDictionary<TKey, TValue> ReadOnlyDict)
				return ReadOnlyDict;

			if (source is IDictionary<TKey, TValue> Dict)
				return new ReadOnlyDictionary<TKey, TValue>(Dict);

			return new ReadOnlyDictionary<TKey, TValue>((IDictionary<TKey, TValue>)source.ToDictionary());
		}

		/// <summary>
		/// Converts an enumerable to a read-only list
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The enumeration to convert</param>
		/// <returns>A read-only list</returns>
		public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (source is IReadOnlyList<T> ReadOnlyList)
				return ReadOnlyList;

#if NET40
			return new ReadOnlyList<T>(source.ToArray());
#else
			return source.ToArray();
#endif
		}

#if !NETSTANDARD && !NET40
		/// <summary>
		/// Converts an enumerable to a read-only set
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The enumeration to convert</param>
		/// <returns>A read-only list</returns>
		public static IReadOnlySet<T> ToReadOnlySet<T>(this IEnumerable<T> source)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (source is IReadOnlySet<T> ReadOnlySet)
				return ReadOnlySet;

			return source.ToHashSet();
		}
#endif

		/// <summary>
		/// Converts an enumerable to a queue
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The enumeration to convert</param>
		/// <returns>A queue with the contents of the enumerable</returns>
		public static Queue<T> ToQueue<T>(this IEnumerable<T> source) => new(source);
	}
}
