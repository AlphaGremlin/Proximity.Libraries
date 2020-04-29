using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
//****************************************

namespace System.Collections.ReadOnly
{
	/// <summary>
	/// Represents a read-only wrapper around a Dictionary that converts the values
	/// </summary>
	public class ReadOnlyDictionaryConverter<TKey, TInput, TOutput> : IDictionary<TKey, TOutput>, ICollection<TOutput>, IReadOnlyCollection<TOutput>, IReadOnlyDictionary<TKey, TOutput>
	{ //****************************************
		private readonly ICollection<TKey> _Keys;

		private readonly Func<TInput, TOutput> _Conversion;
		private readonly Func<TOutput, TInput>? _ReverseConversion;
		//****************************************

		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		/// <param name="conversion">A function that converts the input to the output</param>
		/// <param name="reverseConversion">An optional function to convert the output to the input, if possible</param>
		public ReadOnlyDictionaryConverter(IDictionary<TKey, TInput> dictionary, Func<TInput, TOutput> conversion, Func<TOutput, TInput>? reverseConversion = null)
		{
			if (dictionary is IReadOnlyDictionary<TKey, TInput> Parent)
			{
				this.Parent = Parent;

				// Check the dictionary keys implement IReadOnlyCollection
				// We want to ensure all the Keys properties return the same reference
				if (dictionary.Keys is IReadOnlyCollection<TKey> KeysReadOnly)
				{
					_Keys = dictionary.Keys;
					Keys = KeysReadOnly;
				}
				else
				{
					// Wrap it so it does
					var NewKeys = new ReadOnlyCollection<TKey>(dictionary.Keys);

					_Keys = NewKeys;
					Keys = NewKeys;
				}
			}
			else
			{
				// If the dictionary doesn't implement IReadOnlyDictionary, wrap it so it does
				var NewDictionary = new ReadOnlyDictionary<TKey, TInput>(dictionary);

				this.Parent = NewDictionary;

				_Keys = ((IDictionary<TKey, TInput>)NewDictionary).Keys;
				Keys = NewDictionary.Keys;
			}

			_Conversion = conversion;
			_ReverseConversion = reverseConversion;
		}

		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		/// <param name="conversion">A function that converts the input to the output</param>
		/// <param name="reverseConversion">An optional function to convert the output to the input, if possible</param>
		public ReadOnlyDictionaryConverter(IReadOnlyDictionary<TKey, TInput> dictionary, Func<TInput, TOutput> conversion, Func<TOutput, TInput>? reverseConversion = null)
		{
			Parent = dictionary;

			// We want to ensure all the Keys properties return the same values
			if (dictionary.Keys is ICollection<TKey> KeysCollection)
			{
				if (dictionary.Keys is IReadOnlyCollection<TKey> KeysReadOnly)
				{
					// The source Keys implement both interfaces we need
					Keys = KeysReadOnly;
					_Keys = KeysCollection;
				}
				else
				{
					// The source Keys implement ICollection but not IReadOnlyCollection
					var NewKeyCollection = new ReadOnlyCollection<TKey>(KeysCollection);

					Keys = NewKeyCollection;
					_Keys = NewKeyCollection;
				}
			}
			else
			{
				// The source Keys doesn't implement ICollection
				var NewKeyCollection = new KeyCollection(this);

				Keys = NewKeyCollection;
				_Keys = NewKeyCollection;
			}

			_Conversion = conversion;
			_ReverseConversion = reverseConversion;
		}

		//****************************************

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TOutput> item) => TryGetValue(item.Key, out var Value) && Equals(item.Value, Value);

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>True if there is an element with this key, otherwise false</returns>
		public bool ContainsKey(TKey key) => Parent.ContainsKey(key);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(KeyValuePair<TKey, TOutput>[] array, int arrayIndex)
		{
			foreach(var MyPair in Parent)
			{
				array[arrayIndex++] = new KeyValuePair<TKey, TOutput>(MyPair.Key, _Conversion(MyPair.Value));
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public ChildPairEnumerator GetEnumerator() => new ChildPairEnumerator(this);

		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TOutput value)
		{
			if (Parent.TryGetValue(key, out var MyValue))
			{
				value = _Conversion(MyValue);
				
				return true;
			}
			
			value = default!;
			
			return false;
		}
		
		//****************************************

		void ICollection<KeyValuePair<TKey, TOutput>>.Add(KeyValuePair<TKey, TOutput> item) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary<TKey, TOutput>.Add(TKey key, TOutput value) => throw new NotSupportedException("Dictionary is read-only");

		void ICollection<KeyValuePair<TKey, TOutput>>.Clear() => throw new NotSupportedException("Dictionary is read-only");

		bool ICollection<KeyValuePair<TKey, TOutput>>.Remove(KeyValuePair<TKey, TOutput> item) => throw new NotSupportedException("Dictionary is read-only");

		bool IDictionary<TKey, TOutput>.Remove(TKey key) => throw new NotSupportedException("Dictionary is read-only");

		IEnumerator IEnumerable.GetEnumerator() => new ChildPairEnumerator(this);

		IEnumerator<KeyValuePair<TKey, TOutput>> IEnumerable<KeyValuePair<TKey, TOutput>>.GetEnumerator() => new ChildPairEnumerator(this);

		void ICollection<TOutput>.Add(TOutput item) => throw new NotSupportedException("Dictionary is read-only");

		void ICollection<TOutput>.Clear() => throw new NotSupportedException("Dictionary is read-only");

		bool ICollection<TOutput>.Contains(TOutput item)
		{
			// If there's no reversal operation, do it the slow way
			if (_ReverseConversion == null)
				return Parent.Values.Select(_Conversion).Contains(item);

			var SourceValue = _ReverseConversion(item);

			// If our source implements IDictionary, use it and convert back
			if (Parent is IDictionary<TKey, TInput> Dictionary)
				return Dictionary.Values.Contains(SourceValue);

			// No ICollection, so convert back and search for it the hard way
			return System.Linq.Enumerable.Contains(Parent.Values, SourceValue);
		}
		
		void ICollection<TOutput>.CopyTo(TOutput[] array, int arrayIndex)
		{
			foreach(var MyValue in Parent.Values)
			{
				array[arrayIndex++] = _Conversion(MyValue);
			}
		}

		bool ICollection<TOutput>.Remove(TOutput item) => throw new NotSupportedException("Dictionary is read-only");

		IEnumerator<TOutput> IEnumerable<TOutput>.GetEnumerator() => new ChildEnumerator(this);

		//****************************************

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => Parent.Count;

		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public IReadOnlyCollection<TKey> Keys { get; }

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public IReadOnlyCollection<TOutput> Values => this;

		/// <summary>
		/// Gets the value corresponding to the provided key
		/// </summary>
		public TOutput this[TKey key] => _Conversion(Parent[key]);

		/// <summary>
		/// Gets the parent dictionary
		/// </summary>
		public IReadOnlyDictionary<TKey, TInput> Parent { get; }

		//****************************************

		bool ICollection<KeyValuePair<TKey, TOutput>>.IsReadOnly => true;

		bool ICollection<TOutput>.IsReadOnly => true;

		TOutput IDictionary<TKey, TOutput>.this[TKey key]
		{
			get => _Conversion(Parent[key]);
			set => throw new NotSupportedException("Dictionary is read-only");
		}

		ICollection<TKey> IDictionary<TKey, TOutput>.Keys => _Keys;

		ICollection<TOutput> IDictionary<TKey, TOutput>.Values => this;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TOutput>.Keys => Parent.Keys;

		IEnumerable<TOutput> IReadOnlyDictionary<TKey, TOutput>.Values => this;

		//****************************************

		private class ChildEnumerator : IEnumerator<TOutput>
		{ //****************************************
			private readonly Func<TInput, TOutput> _Conversion;
			private readonly IEnumerator<KeyValuePair<TKey, TInput>> _Enumerator;
			//****************************************

			public ChildEnumerator(ReadOnlyDictionaryConverter<TKey, TInput, TOutput> parent)
			{
				_Conversion= parent._Conversion;
				_Enumerator = parent.Parent.GetEnumerator();
				Current = default!;
			}
			
			//****************************************
			
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				Current = default!;
			}
			
			public bool MoveNext()
			{
				var Result = _Enumerator.MoveNext();
				
				if (Result)
					Current = _Conversion(_Enumerator.Current.Value);
				else
					Current = default!;
				
				return Result;
			}
			
			public void Reset()
			{
				_Enumerator.Reset();
				
				Current = default!;
			}

			//****************************************

			public TOutput Current { get; private set; }

			object? IEnumerator.Current => Current;
		}
		
		/// <summary>
		/// Enumerates the collection while avoiding memory allocations
		/// </summary>
		public struct ChildPairEnumerator : IEnumerator<KeyValuePair<TKey, TOutput>>
		{ //****************************************
			private readonly Func<TInput, TOutput> _Conversion;
			private readonly IEnumerator<KeyValuePair<TKey, TInput>> _Enumerator;
			//****************************************

			internal ChildPairEnumerator(ReadOnlyDictionaryConverter<TKey, TInput, TOutput> parent)
			{
				_Conversion = parent._Conversion;
				_Enumerator = parent.Parent.GetEnumerator();
				Current = default;
			}
			
			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				Current = new KeyValuePair<TKey, TOutput>();
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			public bool MoveNext()
			{
				var Result = _Enumerator.MoveNext();
				
				if (Result)
					SetCurrent(_Enumerator.Current);
				else
					Current = new KeyValuePair<TKey, TOutput>();
				
				return Result;
			}
			
			void IEnumerator.Reset()
			{
				_Enumerator.Reset();
				
				Current = new KeyValuePair<TKey, TOutput>();
			}

			//****************************************

			private void SetCurrent(KeyValuePair<TKey, TInput> current) => Current = new KeyValuePair<TKey, TOutput>(current.Key, _Conversion(current.Value));

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public KeyValuePair<TKey, TOutput> Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		private class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
		{	//****************************************
			private readonly ReadOnlyDictionaryConverter<TKey, TInput, TOutput> _Source;
			//****************************************

			public KeyCollection(ReadOnlyDictionaryConverter<TKey, TInput, TOutput> source) => _Source = source;

			//****************************************

			public void CopyTo(TKey[] array, int arrayIndex)
			{
				foreach (var MyKey in _Source.Parent.Keys)
				{
					array[arrayIndex++] = MyKey;
				}
			}

			//****************************************

			void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException("Dictionary is read-only");

			void ICollection<TKey>.Clear() => throw new NotSupportedException("Dictionary is read-only");

			bool ICollection<TKey>.Contains(TKey item) => _Source.Parent.ContainsKey(item);

			bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException("Dictionary is read-only");

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => _Source.Parent.Keys.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _Source.Parent.Keys.GetEnumerator();

			//****************************************

			public int Count => _Source.Parent.Count;

			bool ICollection<TKey>.IsReadOnly => true;
		}
	}
}
