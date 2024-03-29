using System;
using System.Collections.Generic;
using System.Text;

#if NET40
namespace System.Collections.Generic
{
	/// <summary>
	/// Represents a strongly-typed, read-only collection of elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	public interface IReadOnlyCollection<out T> : IEnumerable<T>, IEnumerable
	{
		/// <summary>
		/// Gets the number of elements in the collection.
		/// </summary>
		int Count { get; }
	}
}
#endif
