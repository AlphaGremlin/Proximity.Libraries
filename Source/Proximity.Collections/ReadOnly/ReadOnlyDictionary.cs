using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only wrapper around a Dictionary
	/// </summary>
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
	{	//****************************************
		private readonly IDictionary<TKey, TValue> _Dictionary;
		private KeyCollection? _Keys;
		private ValueCollection? _Values;
		//****************************************
		
		/// <summary>
		/// Creates a new, empty read-only dictionary
		/// </summary>
		public ReadOnlyDictionary()
		{
			_Dictionary = new Dictionary<TKey, TValue>();
		}
		
		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
		{
			_Dictionary = dictionary;
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
		public bool TryGetValue(TKey key,
#if !NETSTANDARD
			[MaybeNullWhen(false)]
#endif
			out TValue value) => _Dictionary.TryGetValue(key, out value);

		//****************************************

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary.Add(object key, object value) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException("Dictionary is read-only");

		void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary.Clear() => throw new NotSupportedException("Dictionary is read-only");

		bool IDictionary.Contains(object key) => key is TKey Key && ContainsKey(Key);

		void ICollection.CopyTo(Array array, int index) => _Dictionary.CopyTo((KeyValuePair<TKey, TValue>[])array, index);

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Dictionary).GetEnumerator();

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary.Remove(object key) => throw new NotSupportedException("Dictionary is read-only");

		bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException("Dictionary is read-only");

		//****************************************

		private KeyCollection GetKeys() => _Keys ?? Interlocked.CompareExchange(ref _Keys, new KeyCollection(_Dictionary), null) ?? _Keys!;

		private ValueCollection GetValues() => _Values ?? Interlocked.CompareExchange(ref _Values, new ValueCollection(_Dictionary), null) ?? _Values!;

		IDictionaryEnumerator IDictionary.GetEnumerator() => (_Dictionary as IDictionary)?.GetEnumerator() ?? new DictionaryEnumerator(_Dictionary);

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => _Dictionary.Count;

		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public KeyCollection Keys => GetKeys(); // Have to wrap as IDictionary doesn't implement IReadOnlyCollection

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public ValueCollection Values => GetValues(); // Have to wrap as IDictionary doesn't implement IReadOnlyCollection

		/// <summary>
		/// Gets the value corresponding to the provided key
		/// </summary>
		public TValue this[TKey key] => _Dictionary[key];

		bool IDictionary.IsFixedSize => true;

		bool ICollection.IsSynchronized => false;

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

		bool IDictionary.IsReadOnly => true;

		object ICollection.SyncRoot => this;

		TValue IDictionary<TKey, TValue>.this[TKey key]
		{
			get => _Dictionary[key];
			set => throw new NotSupportedException("Dictionary is read-only");
		}
		
		object IDictionary.this[object key]
		{
			get => _Dictionary[(TKey)key]!;
			set => throw new NotSupportedException("Dictionary is read-only");
		}

		ICollection<TKey> IDictionary<TKey, TValue>.Keys => GetKeys(); // The base dictionary implements this, but we wrap so code can up-cast if it wants to

		ICollection<TValue> IDictionary<TKey, TValue>.Values => GetValues();

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => GetKeys();

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => GetValues();

		ICollection IDictionary.Keys => GetKeys();

		ICollection IDictionary.Values => GetValues();

		//****************************************

		/// <summary>
		/// Represents the common collection for the Keys or Values collection
		/// </summary>
		public abstract class Collection<T> : IReadOnlyCollection<T>, ICollection<T>, ICollection
		{
			internal Collection(IDictionary<TKey, TValue> dictionary) => Dictionary = dictionary;

			//****************************************

			void ICollection<T>.Add(T item) => throw new NotSupportedException("Collection is read-only");

			void ICollection<T>.Clear() => throw new NotSupportedException("Collection is read-only");

			/// <inheritdoc/>
			public abstract bool Contains(T item);

			/// <inheritdoc/>
			public abstract void CopyTo(T[] array, int arrayIndex);

			bool ICollection<T>.Remove(T item) => throw new NotSupportedException("Collection is read-only");

			/// <inheritdoc/>
			public abstract IEnumerator<T> GetEnumerator();

			//****************************************

			void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			//****************************************

			/// <inheritdoc/>
			public int Count => Dictionary.Count;

			bool ICollection<T>.IsReadOnly => true;

			object ICollection.SyncRoot => Dictionary;

			bool ICollection.IsSynchronized => false;

			internal IDictionary<TKey, TValue> Dictionary { get; }
		}

		/// <summary>
		/// Provides a read-only keys wrapper
		/// </summary>
		public sealed class KeyCollection : Collection<TKey>
		{
			internal KeyCollection(IDictionary<TKey, TValue> dictionary) : base(dictionary)
			{
			}

			//****************************************

			/// <inheritdoc/>
			public override bool Contains(TKey item) => Dictionary.ContainsKey(item);

			/// <inheritdoc/>
			public override void CopyTo(TKey[] array, int arrayIndex) => Dictionary.Keys.CopyTo(array, arrayIndex);

			/// <inheritdoc/>
			public override IEnumerator<TKey> GetEnumerator() => Dictionary.Keys.GetEnumerator();
		}

		/// <summary>
		/// Provides a read-only values wrapper
		/// </summary>
		public sealed class ValueCollection : Collection<TValue>
		{
			internal ValueCollection(IDictionary<TKey, TValue> dictionary) : base(dictionary)
			{
			}

			//****************************************

			/// <inheritdoc/>
			public override bool Contains(TValue item) => Dictionary.Values.Contains(item);

			/// <inheritdoc/>
			public override void CopyTo(TValue[] array, int arrayIndex) => Dictionary.Values.CopyTo(array, arrayIndex);

			/// <inheritdoc/>
			public override IEnumerator<TValue> GetEnumerator() => Dictionary.Values.GetEnumerator();
		}

		private sealed class DictionaryEnumerator : IDictionaryEnumerator
		{ //****************************************
			private readonly IEnumerator<KeyValuePair<TKey, TValue>> _Enumerator;
			//****************************************

			public DictionaryEnumerator(IDictionary<TKey, TValue> dictionary) => _Enumerator = dictionary.GetEnumerator();

			//****************************************

			public bool MoveNext() => _Enumerator.MoveNext();

			public void Reset() => _Enumerator.Reset();

			//****************************************

			public DictionaryEntry Entry => new DictionaryEntry(_Enumerator.Current.Key, _Enumerator.Current.Value);

			public object Key => Entry.Key;

			public object Value => Entry.Value;

			public object Current => _Enumerator.Current;
		}
	}
}
