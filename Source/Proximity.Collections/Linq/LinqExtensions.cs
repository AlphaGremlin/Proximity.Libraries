using System;
using System.Collections.Generic;
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
#if !NETSTANDARD2_0
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
					yield return CurrentChunk;

					CurrentChunk = null;
				}
			}

			// Return any partial chunk
			if (CurrentChunk != null)
				yield return CurrentChunk;
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

					yield return CurrentChunk;

					CurrentChunk = null;
				}
			}

			// If we haven't returned a single chunk, return an empty one
			if (!HasYielded)
				yield return Array.Empty<T>();
		}

		/// <summary>
		/// Splits an enumeration into chunks of equal or lesser size
		/// </summary>
		/// <typeparam name="T">The enumeration element type</typeparam>
		/// <param name="source">The source enumeration</param>
		/// <param name="size">The size of each chunk</param>
		/// <returns>An enumeration of lists where each chunk is at most <paramref name="size"/> length</returns>
		/// <remarks>If <paramref name="source"/> yields no results, no chunks are returned</remarks>
		public static async IAsyncEnumerable<IReadOnlyList<T>> Chunk<T>(this IAsyncEnumerable<T> source, int size)
		{
			List<T>? CurrentChunk = null;

			await foreach (var Item in source)
			{
				if (CurrentChunk == null)
					// Creating a new List after each yield prevents unexpected behaviour when saving the results of Chunk() (such as calling .ToArray())
					CurrentChunk = new List<T>(size);

				CurrentChunk.Add(Item);

				if (CurrentChunk.Count == size)
				{
					yield return CurrentChunk;

					CurrentChunk = null;
				}
			}

			// Return any partial chunk
			if (CurrentChunk != null)
				yield return CurrentChunk;
		}

		/// <summary>
		/// Splits an enumeration into chunks of equal or lesser size
		/// </summary>
		/// <typeparam name="T">The enumeration element type</typeparam>
		/// <param name="source">The source enumeration</param>
		/// <param name="size">The size of each chunk</param>
		/// <returns>An enumeration of lists where each chunk is at most <paramref name="size"/> length</returns>
		/// <remarks>If <paramref name="source"/> yields no results, a single empty chunk is returned</remarks>
		public static async IAsyncEnumerable<IReadOnlyList<T>> ChunkOrDefault<T>(this IAsyncEnumerable<T> source, int size)
		{
			var HasYielded = false;
			List<T>? CurrentChunk = null;

			await foreach (var Item in source)
			{
				if (CurrentChunk == null)
					// Creating a new List after each yield prevents unexpected behaviour when saving the results of Chunk() (such as calling .ToArray())
					CurrentChunk = new List<T>(size);

				CurrentChunk.Add(Item);

				if (CurrentChunk.Count == size)
				{
					HasYielded = true;

					yield return CurrentChunk;

					CurrentChunk = null;
				}
			}

			// If we haven't returned a single chunk, return an empty one
			if (!HasYielded)
				yield return Array.Empty<T>();
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
		/// Converts an IAsyncEnumerable to an array
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The source Async Enumerable to convert</param>
		/// <param name="token">A cancellation token to abort the operation</param>
		/// <returns>A ValueTask returning the complete enumerable</returns>
		public static async ValueTask<T[]> ToArray<T>(this IAsyncEnumerable<T> source, CancellationToken token = default)
		{
			var List = new List<T>();

			await foreach (var Item in source.WithCancellation(token))
			{
				List.Add(Item);
			}

			return List.ToArray();
		}

		/// <summary>
		/// Converts an IEnumerable to an IAsyncEnumerable
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="source">The source IEnumerable to convert</param>
		/// <returns>An IAsyncEnumerable that will complete synchronously with the contents of the enumerable</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			foreach (var Result in source)
				yield return Result;
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

			if (source is IReadOnlyCollection<T> ReadOnlyList)
				return ReadOnlyList;

			return source.ToArray();
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

			return source.ToArray();
		}

#if !NETSTANDARD
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
		public static Queue<T> ToQueue<T>(this IEnumerable<T> source) => new Queue<T>(source);
	}
}
