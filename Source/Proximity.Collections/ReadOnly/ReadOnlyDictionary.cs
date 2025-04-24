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
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary where TKey : notnull
	{	//****************************************
		private readonly IDictionary<TKey, TValue> _Dictionary;
		private Collection<TKey>? _Keys;
		private Collection<TValue>? _Values;
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

		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionary(IReadOnlyDictionary<TKey, TValue> dictionary)
		{
			_Dictionary = new ReadOnlyWrapper(dictionary);
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
#if !NETSTANDARD && !NET40
			[MaybeNullWhen(false)]
#endif
			out TValue value) => _Dictionary.TryGetValue(key, out value);

		//****************************************

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary.Add(object? key, object? value) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException("Dictionary is read-only");

		void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary.Clear() => throw new NotSupportedException("Dictionary is read-only");

		bool IDictionary.Contains(object? key) => key is TKey Key && ContainsKey(Key);

		void ICollection.CopyTo(Array array, int index) => _Dictionary.CopyTo((KeyValuePair<TKey, TValue>[])array, index);

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_Dictionary).GetEnumerator();

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary.Remove(object? key) => throw new NotSupportedException("Dictionary is read-only");

		bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException("Dictionary is read-only");

		//****************************************

		private Collection<TKey> GetKeys() => _Keys ?? Interlocked.CompareExchange(ref _Keys, _Dictionary is ReadOnlyWrapper WrappedDictionary ? new ReadOnlyKeyCollection(WrappedDictionary.Dictionary) : new KeyCollection(_Dictionary), null) ?? _Keys!;

		private Collection<TValue> GetValues() => _Values ?? Interlocked.CompareExchange(ref _Values, new ValueCollection(_Dictionary), null) ?? _Values!;

		IDictionaryEnumerator IDictionary.GetEnumerator() => (_Dictionary as IDictionary)?.GetEnumerator() ?? new DictionaryEnumerator(_Dictionary);

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => _Dictionary.Count;

		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public Collection<TKey> Keys => GetKeys(); // Have to wrap as IDictionary doesn't implement IReadOnlyCollection

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public Collection<TValue> Values => GetValues(); // Have to wrap as IDictionary doesn't implement IReadOnlyCollection

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
		
		object? IDictionary.this[object key]
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
			public abstract int Count { get; }

			bool ICollection<T>.IsReadOnly => true;

			object ICollection.SyncRoot => SyncRoot;

			bool ICollection.IsSynchronized => false;

			internal abstract object SyncRoot { get; }
		}

		private sealed class KeyCollection : Collection<TKey>
		{ //****************************************
			private readonly IDictionary<TKey, TValue> _Dictionary;
			//****************************************

			internal KeyCollection(IDictionary<TKey, TValue> dictionary)
			{
				_Dictionary = dictionary;
			}

			//****************************************

			public override bool Contains(TKey item) => _Dictionary.ContainsKey(item);

			public override void CopyTo(TKey[] array, int arrayIndex) => _Dictionary.Keys.CopyTo(array, arrayIndex);

			public override IEnumerator<TKey> GetEnumerator() => _Dictionary.Keys.GetEnumerator();

			public override int Count => _Dictionary.Count;

			internal override object SyncRoot => _Dictionary;
		}

		private sealed class ValueCollection : Collection<TValue>
		{ //****************************************
			private readonly IDictionary<TKey, TValue> _Dictionary;
			//****************************************

			internal ValueCollection(IDictionary<TKey, TValue> dictionary)
			{
				_Dictionary = dictionary;
			}

			//****************************************

			public override bool Contains(TValue item) => _Dictionary.Values.Contains(item);

			public override void CopyTo(TValue[] array, int arrayIndex) => _Dictionary.Values.CopyTo(array, arrayIndex);

			public override IEnumerator<TValue> GetEnumerator() => _Dictionary.Values.GetEnumerator();

			public override int Count => _Dictionary.Count;

			internal override object SyncRoot => _Dictionary;
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

			public DictionaryEntry Entry => new(_Enumerator.Current.Key, _Enumerator.Current.Value);

			public object Key => Entry.Key;

			public object? Value => Entry.Value;

			public object Current => _Enumerator.Current;
		}

		private sealed class ReadOnlyWrapper : IDictionary<TKey, TValue>
		{
			public ReadOnlyWrapper(IReadOnlyDictionary<TKey, TValue> dictionary)
			{
				Dictionary = dictionary;
			}

			//****************************************

			public bool Contains(KeyValuePair<TKey, TValue> item)
			{
				var Comparer = EqualityComparer<TValue>.Default;

				return Dictionary.TryGetValue(item.Key, out var Value) && Comparer.Equals(item.Value, Value);
			}

			public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);

			public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
			{
				foreach (var Pair in Dictionary)
					array[arrayIndex++] = Pair;
			}

			public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dictionary.GetEnumerator();

			public bool TryGetValue(TKey key,
#if !NETSTANDARD && !NET40
				[MaybeNullWhen(false)]
#endif
				out TValue value) => Dictionary.TryGetValue(key, out value);

			IEnumerator IEnumerable.GetEnumerator() => Dictionary.GetEnumerator();

			//****************************************

			void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException("Dictionary is read-only");

			void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

			void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException("Dictionary is read-only");

			bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException("Dictionary is read-only");

			bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException("Dictionary is read-only");

			//****************************************

			public TValue this[TKey key]
			{
				get => Dictionary[key];
				set => throw new NotSupportedException("Dictionary is read-only");
			}

			public ICollection<TKey> Keys => throw new NotSupportedException();

			public ICollection<TValue> Values => throw new NotSupportedException();

			public int Count => Dictionary.Count;

			public bool IsReadOnly => true;

			internal IReadOnlyDictionary<TKey, TValue> Dictionary { get; }
		}

		private sealed class ReadOnlyKeyCollection : Collection<TKey>
		{ //****************************************
			private readonly IReadOnlyDictionary<TKey, TValue> _Dictionary;
			//****************************************

			internal ReadOnlyKeyCollection(IReadOnlyDictionary<TKey, TValue> dictionary)
			{
				_Dictionary = dictionary;
			}

			//****************************************

			public override bool Contains(TKey item) => _Dictionary.ContainsKey(item);

			public override void CopyTo(TKey[] array, int arrayIndex)
			{
				foreach (var Key in _Dictionary.Keys)
					array[arrayIndex++] = Key;
			}

			public override IEnumerator<TKey> GetEnumerator() => _Dictionary.Keys.GetEnumerator();

			public override int Count => _Dictionary.Count;

			internal override object SyncRoot => _Dictionary;
		}

	}
}
