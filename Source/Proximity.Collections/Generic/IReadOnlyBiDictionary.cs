using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic
{
	/// <summary>
	/// Represents a generic read-only bi-directional dictionary
	/// </summary>
	/// <typeparam name="TKey">The type of key</typeparam>
	/// <typeparam name="TValue">The type of value</typeparam>
	public interface IReadOnlyBiDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
		/// <summary>
		/// Gets the inverse of this dictionary
		/// </summary>
		IReadOnlyBiDictionary<TValue, TKey> Inverse { get; }
	}
}
