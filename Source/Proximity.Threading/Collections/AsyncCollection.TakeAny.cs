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
	public sealed partial class AsyncCollection<TItem>
	{
		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <returns>A task returning the result of the take operation</returns>
		public static ValueTask<CollectionTakeResult<TItem>> TakeFromAny(params AsyncCollection<TItem>[] collections) => TakeFromAny(collections, CancellationToken.None);

		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="token">A cancellation token that can abort the take operation</param>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <returns>A task returning the result of the take operation</returns>
		public static ValueTask<CollectionTakeResult<TItem>> TakeFromAny(CancellationToken token, params AsyncCollection<TItem>[] collections) => TakeFromAny(collections, token);

		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <returns>A task returning the result of the take operation</returns>
		public static ValueTask<CollectionTakeResult<TItem>> TakeFromAny(TimeSpan timeout, params AsyncCollection<TItem>[] collections) => TakeFromAny(collections, timeout);

		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <returns>A task returning the result of the take operation</returns>
		public static ValueTask<CollectionTakeResult<TItem>> TakeFromAny(IEnumerable<AsyncCollection<TItem>> collections) => TakeFromAny(collections, CancellationToken.None);

		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <param name="timeout">The amount of time to wait for an item to arrive in the collection</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The timeout elapsed while we were waiting for an item</exception>
		public static ValueTask<CollectionTakeResult<TItem>> TakeFromAny(IEnumerable<AsyncCollection<TItem>> collections, TimeSpan timeout)
		{
			var TakeResult = TryTakeFromAny(collections);

			if (TakeResult.HasItem)
				return new ValueTask<CollectionTakeResult<TItem>>(TakeResult);

			var CancelSource = new CancellationTokenSource(timeout);
			var Operation = AsyncCollectionTakeAny<TItem>.GetOrCreate(CancelSource.Token);

			foreach (var Collection in collections)
				Operation.Attach(Collection);

			Operation.ApplyCancellation(CancelSource);

			return new ValueTask<CollectionTakeResult<TItem>>(Operation, Operation.Version);
		}

		/// <summary>
		/// Attempts to take an Item from a set of collections
		/// </summary>
		/// <param name="collections">An enumeration of collections to wait on</param>
		/// <param name="token">A cancellation token that can abort the take operation</param>
		/// <returns>A task returning the result of the take operation</returns>
		/// <exception cref="OperationCanceledException">The cancellation token was raised while we were waiting for an item</exception>
		public static ValueTask<CollectionTakeResult<TItem>> TakeFromAny(IEnumerable<AsyncCollection<TItem>> collections, CancellationToken token)
		{
			var TakeResult = TryTakeFromAny(collections);

			if (TakeResult.HasItem)
				return new ValueTask<CollectionTakeResult<TItem>>(TakeResult);

			var Operation = AsyncCollectionTakeAny<TItem>.GetOrCreate(token);

			foreach (var Collection in collections)
				Operation.Attach(Collection);

			Operation.ApplyCancellation(null);

			return new ValueTask<CollectionTakeResult<TItem>>(Operation, Operation.Version);
		}

		//public static Task<AsyncCollection<TItem>> PeekAtAny(IEnumerable<AsyncCollection<TItem>> collections, CancellationToken token)
		//{
		//	
		//}

		/// <summary>
		/// Peeks at a set of collections and finds the first with data available
		/// </summary>
		/// <param name="collection">Receives the collection that has an item available</param>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>The result of the operation</returns>
		public static bool TryPeekAtAny(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out AsyncCollection<TItem> collection, params AsyncCollection<TItem>[] collections) => TryPeekAtAny(collections, out collection!);

		/// <summary>
		/// Peeks at a set of collections and finds the first with data available
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <param name="collection">Receives the collection that has an item available</param>
		/// <returns>True if an item is available, otherwise False</returns>
		public static bool TryPeekAtAny(IEnumerable<AsyncCollection<TItem>> collections,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out AsyncCollection<TItem> collection)
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
		/// <param name="collection">Receives the collection we took the item from</param>
		/// <param name="item">The item that was taken</param>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>True if we took from a collection, otherwise False</returns>
		public static bool TryTakeFromAny(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
# endif
			out AsyncCollection<TItem> collection,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
# endif
			out TItem item, params AsyncCollection<TItem>[] collections) => TryTakeFromAny(collections, out collection, out item);

		/// <summary>
		/// Attempts to take an Item from a set of collections without waiting
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <param name="collection">Receives the collection we took the item from</param>
		/// <param name="item">The item that was taken</param>
		/// <returns>True if we took from a collection, otherwise False</returns>
		public static bool TryTakeFromAny(IEnumerable<AsyncCollection<TItem>> collections, out AsyncCollection<TItem> collection,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TItem item)
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
		public static CollectionTakeResult<TItem> TryTakeFromAny(params AsyncCollection<TItem>[] collections) => TryTakeFromAny((IEnumerable<AsyncCollection<TItem>>)collections);

		/// <summary>
		/// Attempts to take an Item from a set of collections without waiting
		/// </summary>
		/// <param name="collections">An enumeration of collections to pull from</param>
		/// <returns>A Take Result describing the result of the operation</returns>
		public static CollectionTakeResult<TItem> TryTakeFromAny(IEnumerable<AsyncCollection<TItem>> collections)
		{
			foreach (var SourceCollection in collections)
			{
				if (SourceCollection.TryTake(out var Item))
					return new CollectionTakeResult<TItem>(SourceCollection, Item);
			}

			return default;
		}
	}
}
