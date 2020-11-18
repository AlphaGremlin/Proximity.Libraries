using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Collections.Concurrent
{
	public sealed partial class AsyncCollection<T>
	{
		/// <summary>
		/// Attempts to add an item to any of the given collections
		/// </summary>
		/// <param name="collections">An enumeration of the collections to add to</param>
		/// <param name="item">The item to try and add</param>
		/// <param name="collection">Receives the collection the item was added to</param>
		/// <returns>True if the item was added, False if none of the collections had capacity</returns>
		public bool TryAddToAny(IEnumerable<AsyncCollection<T>> collections, T item,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out AsyncCollection<T> collection
			)
		{
			foreach (var SourceCollection in collections)
			{
				if (SourceCollection.TryAdd(item))
				{
					collection = SourceCollection;

					return true;
				}
			}

			collection = null!;

			return false;
		}
	}
}
