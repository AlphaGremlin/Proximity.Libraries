using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a read-only wrapper around a Dictionary
	/// </summary>
	public abstract class ReadOnlyDictionaryConverter<TKey, TSource, TTarget> : IDictionary<TKey, TTarget>, ICollection<TTarget>, IReadOnlyCollection<TTarget>, IReadOnlyDictionary<TKey, TTarget>
	{ //****************************************
		private readonly ICollection<TKey> _Keys;
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionaryConverter(IDictionary<TKey, TSource> dictionary)
		{
			Parent = dictionary as IReadOnlyDictionary<TKey, TSource>;

			if (Parent == null)
			{
				// If the dictionary doesn't implement IReadOnlyDictionary, wrap it so it does
				var NewDictionary = new ReadOnlyDictionary<TKey, TSource>(dictionary);

				Parent = NewDictionary;
				_Keys = ((IDictionary<TKey, TSource>)NewDictionary).Keys;
				Keys = NewDictionary.Keys;
			}
			else
			{
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
		}

		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionaryConverter(IReadOnlyDictionary<TKey, TSource> dictionary)
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
		}
		
		//****************************************
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TTarget> item)
		{
			var SourcePair = new KeyValuePair<TKey, TSource>(item.Key, ConvertFrom(item.Value));

			// If our source implements ICollection, use it and convert back
			if (Parent is ICollection<KeyValuePair<TKey, TSource>> Collection)
				return Collection.Contains(SourcePair);

			// No ICollection, so convert back and search for it the hard way
			return System.Linq.Enumerable.Contains(Parent, SourcePair);
		}

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
		public void CopyTo(KeyValuePair<TKey, TTarget>[] array, int arrayIndex)
		{
			foreach(var MyPair in Parent)
			{
				array[arrayIndex++] = new KeyValuePair<TKey, TTarget>(MyPair.Key, ConvertTo(MyPair.Value));
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
		public bool TryGetValue(TKey key, out TTarget value)
		{
			if (Parent.TryGetValue(key, out var MyValue))
			{
				value = ConvertTo(MyValue);
				
				return true;
			}
			
			value = default;
			
			return false;
		}
		
		//****************************************

		/// <summary>
		/// Converts the source into the desired target type
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The converted value</returns>
		protected abstract TTarget ConvertTo(TSource value);

		/// <summary>
		/// Converts the target type back to the source type
		/// </summary>
		/// <param name="value">The converted value</param>
		/// <returns>The original value</returns>
		protected virtual TSource ConvertFrom(TTarget value) => throw new NotSupportedException("Conversion is one-way");

		//****************************************

		void ICollection<KeyValuePair<TKey, TTarget>>.Add(KeyValuePair<TKey, TTarget> item) => throw new NotSupportedException("Dictionary is read-only");

		void IDictionary<TKey, TTarget>.Add(TKey key, TTarget value) => throw new NotSupportedException("Dictionary is read-only");

		void ICollection<KeyValuePair<TKey, TTarget>>.Clear() => throw new NotSupportedException("Dictionary is read-only");

		bool ICollection<KeyValuePair<TKey, TTarget>>.Remove(KeyValuePair<TKey, TTarget> item) => throw new NotSupportedException("Dictionary is read-only");

		bool IDictionary<TKey, TTarget>.Remove(TKey key) => throw new NotSupportedException("Dictionary is read-only");

		IEnumerator IEnumerable.GetEnumerator() => new ChildPairEnumerator(this);

		IEnumerator<KeyValuePair<TKey, TTarget>> IEnumerable<KeyValuePair<TKey, TTarget>>.GetEnumerator() => new ChildPairEnumerator(this);

		void ICollection<TTarget>.Add(TTarget item) => throw new NotSupportedException("Dictionary is read-only");

		void ICollection<TTarget>.Clear() => throw new NotSupportedException("Dictionary is read-only");

		bool ICollection<TTarget>.Contains(TTarget item)
		{
			var SourceValue = ConvertFrom(item);

			// If our source implements IDictionary, use it and convert back
			if (Parent is IDictionary<TKey, TSource> Dictionary)
				return Dictionary.Values.Contains(SourceValue);

			// No ICollection, so convert back and search for it the hard way
			return System.Linq.Enumerable.Contains(Parent.Values, SourceValue);
		}
		
		void ICollection<TTarget>.CopyTo(TTarget[] array, int arrayIndex)
		{
			foreach(var MyValue in Parent.Values)
			{
				array[arrayIndex++] = ConvertTo(MyValue);
			}
		}

		bool ICollection<TTarget>.Remove(TTarget item) => throw new NotSupportedException("Dictionary is read-only");

		IEnumerator<TTarget> IEnumerable<TTarget>.GetEnumerator() => new ChildEnumerator(this);

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
		public IReadOnlyCollection<TTarget> Values => this;

		/// <summary>
		/// Gets the value corresponding to the provided key
		/// </summary>
		public TTarget this[TKey key]
		{
			get
			{
				if (Parent.TryGetValue(key, out var MyValue))
					return ConvertTo(MyValue);
				
				return default;
			}
		}

		/// <summary>
		/// Gets the parent dictionary
		/// </summary>
		public IReadOnlyDictionary<TKey, TSource> Parent { get; }

		//****************************************

		bool ICollection<KeyValuePair<TKey, TTarget>>.IsReadOnly => true;

		bool ICollection<TTarget>.IsReadOnly => true;

		TTarget IDictionary<TKey, TTarget>.this[TKey key]
		{
			get
			{
				if (Parent.TryGetValue(key, out var MyValue))
					return ConvertTo(MyValue);
				
				return default;
			}
			set { throw new NotSupportedException("Dictionary is read-only"); }
		}

		ICollection<TKey> IDictionary<TKey, TTarget>.Keys => _Keys;

		ICollection<TTarget> IDictionary<TKey, TTarget>.Values => this;

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TTarget>.Keys => Parent.Keys;

		IEnumerable<TTarget> IReadOnlyDictionary<TKey, TTarget>.Values => this;

		//****************************************

		private class ChildEnumerator : IEnumerator<TTarget>
		{	//****************************************
			private readonly ReadOnlyDictionaryConverter<TKey, TSource, TTarget> _Parent;
			private readonly IEnumerator<KeyValuePair<TKey, TSource>> _Enumerator;

			//****************************************

			public ChildEnumerator(ReadOnlyDictionaryConverter<TKey, TSource, TTarget> parent)
			{
				_Parent = parent;
				_Enumerator = _Parent.Parent.GetEnumerator();
			}
			
			//****************************************
			
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				Current = default;
			}
			
			public bool MoveNext()
			{
				var Result = _Enumerator.MoveNext();
				
				if (Result)
					Current = _Parent.ConvertTo(_Enumerator.Current.Value);
				else
					Current = default;
				
				return Result;
			}
			
			public void Reset()
			{
				_Enumerator.Reset();
				
				Current = default;
			}

			//****************************************

			public TTarget Current { get; private set; }

			object IEnumerator.Current => Current;
		}
		
		/// <summary>
		/// Enumerates the collection while avoiding memory allocations
		/// </summary>
		public struct ChildPairEnumerator : IEnumerator<KeyValuePair<TKey, TTarget>>
		{	//****************************************
			private readonly ReadOnlyDictionaryConverter<TKey, TSource, TTarget> _Parent;
			private readonly IEnumerator<KeyValuePair<TKey, TSource>> _Enumerator;

			//****************************************

			internal ChildPairEnumerator(ReadOnlyDictionaryConverter<TKey, TSource, TTarget> parent)
			{
				_Parent = parent;
				_Enumerator = _Parent.Parent.GetEnumerator();
				Current = default;
			}
			
			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				Current = new KeyValuePair<TKey, TTarget>();
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
					Current = new KeyValuePair<TKey, TTarget>();
				
				return Result;
			}
			
			void IEnumerator.Reset()
			{
				_Enumerator.Reset();
				
				Current = new KeyValuePair<TKey, TTarget>();
			}
			
			//****************************************
			
			private void SetCurrent(KeyValuePair<TKey, TSource> current)
			{
				Current = new KeyValuePair<TKey, TTarget>(current.Key, _Parent.ConvertTo(current.Value));
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public KeyValuePair<TKey, TTarget> Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		private class KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
		{	//****************************************
			private readonly ReadOnlyDictionaryConverter<TKey, TSource, TTarget> _Source;
			//****************************************

			public KeyCollection(ReadOnlyDictionaryConverter<TKey, TSource, TTarget> source) => _Source = source;

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
