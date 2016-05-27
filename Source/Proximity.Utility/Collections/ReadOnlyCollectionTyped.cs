/****************************************\
 ReadOnlyCollectionTyped.cs
 Created: 2011-08-08
\****************************************/
#if !NET40
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a Collection that also converts to a base type
	/// </summary>
	public class ReadOnlyCollectionTyped<TSource, TTarget> : ReadOnlyCollectionConverter<TSource, TTarget> where TSource : class, TTarget where TTarget : class
	{
		/// <summary>
		/// Creates a new read-only wrapper around a collection that converts to a base type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollectionTyped(ICollection<TSource> collection) : base(collection)
		{
		}
		
		/// <summary>
		/// Creates a new read-only wrapper around a collection that converts to a base type
		/// </summary>
		/// <param name="collection">The collection to wrap as read-only</param>
		public ReadOnlyCollectionTyped(IReadOnlyCollection<TSource> collection) : base(collection)
		{
		}
		
		//****************************************

		/// <inheritdoc />
		protected override TSource ConvertFrom(TTarget value)
		{
			return (TSource)value;
		}

		/// <inheritdoc />
		protected override TTarget ConvertTo(TSource value)
		{
			return value;
		}
	}
}
#endif