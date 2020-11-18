using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Collections.Concurrent
{
	public sealed partial class AsyncCollection<T>
	{
		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The timeout elapsed while we were waiting for an item</exception>
		public static ValueTask<CollectionTakeResult<T>> TakeFromAny(IEnumerable<AsyncCollection<T>> collections, CancellationToken token = default) => TakeFromAny(collections, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <param name="token">A cancellation token to abort the take operation</param>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The timeout elapsed while we were waiting for an item</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public static ValueTask<CollectionTakeResult<T>> TakeFromAny(IEnumerable<AsyncCollection<T>> collections, TimeSpan timeout, CancellationToken token = default)
		{
			var TakeResult = TryTakeFromAny(collections);

			if (TakeResult.HasItem)
				return new ValueTask<CollectionTakeResult<T>>(TakeResult);

			var Operation = AsyncCollectionTakeAny<T>.GetOrCreate(token, timeout);

			foreach (var Collection in collections)
				Operation.Attach(Collection);

			Operation.Activate();

			return new ValueTask<CollectionTakeResult<T>>(Operation, Operation.Version);
		}

		//public static Task<AsyncCollection<TItem>> PeekAtAny(IEnumerable<AsyncCollection<TItem>> collections, CancellationToken token)
		//{
		//	
		//}

		/// <summary>
		/// Peeks at a set of collections and finds the first with data available
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <param name="collection">Receives the collection that has an item available</param>
		/// <returns>True if an item is available, otherwise False</returns>
		public static bool TryPeekAtAny(IEnumerable<AsyncCollection<T>> collections,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out AsyncCollection<T> collection)
		{
			foreach (var SourceCollection in collections)
			{
				if (SourceCollection.TryPeek())
				{
					collection = SourceCollection;
					return true;
				}
			}

			collection = null!;

			return false;
		}

		/// <summary>
		/// Attempts to take an Item from a set of collections without waiting
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <param name="collection">Receives the collection we took the item from</param>
		/// <param name="item">The item that was taken</param>
		/// <returns>True if we took from a collection, otherwise False</returns>
		public static bool TryTakeFromAny(IEnumerable<AsyncCollection<T>> collections, out AsyncCollection<T> collection,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out T item)
		{
			foreach (var SourceCollection in collections)
			{
				if (SourceCollection.TryTake(out item!))
				{
					collection = SourceCollection;

					return true;
				}
			}

			collection = null!;
			item = default!;

			return false;
		}

		/// <summary>
		/// Attempts to take an Item from a set of collections without waiting
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>A Take Result describing the result of the operation</returns>
		public static CollectionTakeResult<T> TryTakeFromAny(params AsyncCollection<T>[] collections) => TryTakeFromAny((IEnumerable<AsyncCollection<T>>)collections);

		/// <summary>
		/// Attempts to take an Item from a set of collections without waiting
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>A Take Result describing the result of the operation</returns>
		public static CollectionTakeResult<T> TryTakeFromAny(IEnumerable<AsyncCollection<T>> collections)
		{
			foreach (var SourceCollection in collections)
			{
				if (SourceCollection.TryTake(out var Item))
					return new CollectionTakeResult<T>(SourceCollection, Item);
			}

			return default;
		}
	}
}
