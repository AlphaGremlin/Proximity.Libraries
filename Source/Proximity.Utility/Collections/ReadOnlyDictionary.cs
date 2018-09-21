/****************************************\
 ReadOnlyDictionary.cs
 Created: 2011-04-03
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a Dictionary
	/// </summary>
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
#if !NET40
		, IReadOnlyDictionary<TKey, TValue>
#endif
	{	//****************************************
		private readonly IDictionary<TKey, TValue> _Dictionary;
		private readonly ReadOnlyCollection<TKey> _Keys;
		private readonly ReadOnlyCollection<TValue> _Values;
		//****************************************
		
		/// <summary>
		/// Creates a new, empty read-only dictionary
		/// </summary>
		public ReadOnlyDictionary()
		{
			_Dictionary = new Dictionary<TKey, TValue>();
			_Keys = new ReadOnlyCollection<TKey>(_Dictionary.Keys);
			_Values = new ReadOnlyCollection<TValue>(_Dictionary.Values);
		}
		
		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
		{
			_Dictionary = dictionary;
			_Keys = new ReadOnlyCollection<TKey>(_Dictionary.Keys);
			_Values = new ReadOnlyCollection<TValue>(_Dictionary.Values);
		}
		
		//****************************************
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return _Dictionary.Contains(item);
		}
		
		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if there is an element with this key, otherwise false</returns>
		public bool ContainsKey(TKey key)
		{
			return _Dictionary.ContainsKey(key);
		}
		
		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			_Dictionary.CopyTo(array, arrayIndex);
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _Dictionary.GetEnumerator();
		}
		
		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			return _Dictionary.TryGetValue(key, out value);
		}
		
		//****************************************
		
		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void ICollection<KeyValuePair<TKey, TValue>>.Clear()
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool IDictionary<TKey, TValue>.Remove(TKey key)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((System.Collections.IEnumerable)_Dictionary).GetEnumerator();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Dictionary.Count; }
		}
		
#if NET40
		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public ICollection<TKey> Keys
		{
			get { return _Keys; } // Have to wrap as it doesn't implement IReadOnlyCollection
		}

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public ICollection<TValue> Values
		{
			get { return _Values; } // Have to wrap as it doesn't implement IReadOnlyCollection
		}
#else
		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public IReadOnlyCollection<TKey> Keys
		{
			get { return _Keys; } // Have to wrap as it doesn't implement IReadOnlyCollection
		}
		
		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public IReadOnlyCollection<TValue> Values
		{
			get { return _Values; } // Have to wrap as it doesn't implement IReadOnlyCollection
		}
#endif
		/// <summary>
		/// Gets the value corresponding to the provided key
		/// </summary>
		public TValue this[TKey key]
		{
			get { return _Dictionary[key]; }
		}
		
		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
		{
			get { return true; }
		}
		
		TValue IDictionary<TKey, TValue>.this[TKey key]
		{
			get { return _Dictionary[key]; }
			set { throw new NotSupportedException("Dictionary is read-only"); }
		}
		
		ICollection<TKey> IDictionary<TKey, TValue>.Keys
		{
			get { return _Dictionary.Keys; } // Already Read-Only
		}
		
		ICollection<TValue> IDictionary<TKey, TValue>.Values
		{
			get { return _Dictionary.Values; } // Already Read-Only
		}

#if !NET40
		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
		{
			get { return _Dictionary.Keys; }
		}
		
		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
		{
			get { return _Dictionary.Values; }
		}
#endif
	}
}
