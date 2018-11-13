﻿using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a Dictionary
	/// </summary>
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
	{	//****************************************
		private readonly IDictionary<TKey, TValue> _Dictionary;
		private readonly ReadOnlyDictionaryCollection<TKey> _Keys;
		private readonly ReadOnlyDictionaryCollection<TValue> _Values;
		//****************************************
		
		/// <summary>
		/// Creates a new, empty read-only dictionary
		/// </summary>
		public ReadOnlyDictionary()
		{
			_Dictionary = new Dictionary<TKey, TValue>();
			_Keys = new ReadOnlyDictionaryKeyCollection(_Dictionary);
			_Values = new ReadOnlyDictionaryValueCollection(_Dictionary);
		}
		
		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
		{
			_Dictionary = dictionary;
			_Keys = new ReadOnlyDictionaryKeyCollection(_Dictionary);
			_Values = new ReadOnlyDictionaryValueCollection(_Dictionary);
		}

		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TValue> item) => _Dictionary.Contains(item);

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if there is an element with this key, otherwise false</returns>
		public bool ContainsKey(TKey key) => _Dictionary.ContainsKey(key);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => _Dictionary.CopyTo(array, arrayIndex);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _Dictionary.GetEnumerator();

		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key, out TValue value) => _Dictionary.TryGetValue(key, out value);

		//****************************************

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException("Dictionary is read-only");

		void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException("Dictionary is read-only");

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

		bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException("Dictionary is read-only");

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Dictionary).GetEnumerator();

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => _Dictionary.Count;

		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public IReadOnlyCollection<TKey> Keys => _Keys; // Have to wrap as it doesn't implement IReadOnlyCollection

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public IReadOnlyCollection<TValue> Values => _Values; // Have to wrap as it doesn't implement IReadOnlyCollection

		/// <summary>
		/// Gets the value corresponding to the provided key
		/// </summary>
		public TValue this[TKey key] => _Dictionary[key];

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

		TValue IDictionary<TKey, TValue>.this[TKey key]
		{
			get => _Dictionary[key];
			set => throw new NotSupportedException("Dictionary is read-only");
		}

		ICollection<TKey> IDictionary<TKey, TValue>.Keys => _Keys;

		ICollection<TValue> IDictionary<TKey, TValue>.Values => _Values;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _Keys;

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _Values;

		//****************************************

		private abstract class ReadOnlyDictionaryCollection<T> : IReadOnlyCollection<T>, ICollection<T>
		{
			public ReadOnlyDictionaryCollection(IDictionary<TKey, TValue> dictionary) => Dictionary = dictionary;

			//****************************************

			void ICollection<T>.Add(T item) => throw new NotSupportedException("Collection is read-only");

			void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only");

			public abstract bool Contains(T item);

			public abstract void CopyTo(T[] array, int arrayIndex);

			bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only");

			public abstract IEnumerator<T> GetEnumerator();

			//****************************************

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			//****************************************

			public int Count => Dictionary.Count;

			public bool IsReadOnly => true;

			protected IDictionary<TKey, TValue> Dictionary { get; }
		}

		private sealed class ReadOnlyDictionaryKeyCollection : ReadOnlyDictionaryCollection<TKey>
		{
			public ReadOnlyDictionaryKeyCollection(IDictionary<TKey, TValue> dictionary) : base(dictionary)
			{
			}

			//****************************************

			public override bool Contains(TKey item) => Dictionary.ContainsKey(item);

			public override void CopyTo(TKey[] array, int arrayIndex) => Dictionary.Keys.CopyTo(array, arrayIndex);

			public override IEnumerator<TKey> GetEnumerator() => Dictionary.Keys.GetEnumerator();
		}

		private sealed class ReadOnlyDictionaryValueCollection : ReadOnlyDictionaryCollection<TValue>
		{
			public ReadOnlyDictionaryValueCollection(IDictionary<TKey, TValue> dictionary) : base(dictionary)
			{
			}

			//****************************************

			public override bool Contains(TValue item) => Dictionary.Values.Contains(item);

			public override void CopyTo(TValue[] array, int arrayIndex) => Dictionary.Values.CopyTo(array, arrayIndex);

			public override IEnumerator<TValue> GetEnumerator() => Dictionary.Values.GetEnumerator();
		}
	}
}
