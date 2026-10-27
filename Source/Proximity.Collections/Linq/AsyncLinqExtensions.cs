using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq;

/// <summary>
/// Provides useful Linq-style methods for Async operations
/// </summary>
public static class AsyncLinqExtensions
{
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

#if !NET10_0_OR_GREATER
	/// <summary>
	/// Converts an IAsyncEnumerable to an array
	/// </summary>
	/// <typeparam name="T">The type of element</typeparam>
	/// <param name="source">The source Async Enumerable to convert</param>
	/// <param name="token">A cancellation token to abort the operation</param>
	/// <returns>A ValueTask returning the complete enumerable</returns>
	public static async ValueTask<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source, CancellationToken token = default)
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
	public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
		=> new AsyncEnumerableWrapper<T>(source);

	//****************************************

	private sealed class AsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
	{ //****************************************
		private readonly IEnumerable<T> _Source;
		//****************************************

		public AsyncEnumerableWrapper(IEnumerable<T> source) => _Source = source;

		//****************************************

		IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
			=> new AsyncEnumeratorWrapper<T>(_Source.GetEnumerator());
	}

	private class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
	{ //****************************************
		private readonly IEnumerator<T> _Source;
		//****************************************

		public AsyncEnumeratorWrapper(IEnumerator<T> source) => _Source = source;

		//****************************************

		ValueTask IAsyncDisposable.DisposeAsync()
		{
			_Source.Dispose();

			return default;
		}

		ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => new(_Source.MoveNext());

		//****************************************

		T IAsyncEnumerator<T>.Current => _Source.Current;
	}
#endif
}
