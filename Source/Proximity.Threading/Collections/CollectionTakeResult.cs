using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Describes the result of a TakeFromAny operation
	/// </summary>
	public readonly struct CollectionTakeResult<T>
	{
		internal CollectionTakeResult(AsyncCollection<T> source, T item)
		{
			Source = source;
			Item = item;
		}

		//****************************************

			/// <summary>
			/// Deconstructs the results
			/// </summary>
			/// <param name="source">The source of the result, if any</param>
			/// <param name="item">The item that was retrieved, if any</param>
		public void Deconstruct(out AsyncCollection<T>? source,
#if !NETSTANDARD2_0
			[MaybeNull]
#endif
			out T item
			)
		{
			source = Source;
			item = Item;
		}

		//****************************************

		/// <summary>
		/// Gets whether this result has an item
		/// </summary>
		public bool HasItem => Source != null;

		/// <summary>
		/// Gets the collection the item was retrieved from
		/// </summary>
		/// <remarks>Null if no item was retrieved</remarks>
		public AsyncCollection<T>? Source { get; }

		/// <summary>
		/// Gets the item in this result
		/// </summary>
#if !NETSTANDARD2_0
		[MaybeNull]
#endif
		public T Item { get; }
	}
}
