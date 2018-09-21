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
	public abstract class ReadOnlyDictionaryConverter<TKey, TSource, TTarget> : IDictionary<TKey, TTarget>, ICollection<TTarget>
#if !NET40
		, IReadOnlyCollection<TTarget>, IReadOnlyDictionary<TKey, TTarget>
#endif
	{	//****************************************
#if NET40
		private readonly IDictionary<TKey, TSource> _Dictionary;
#else
		private readonly IReadOnlyDictionary<TKey, TSource> _Dictionary;
		private readonly IReadOnlyCollection<TKey> _KeysReadOnly;
		private readonly ICollection<TKey> _Keys;
#endif
		//****************************************
		
		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionaryConverter(IDictionary<TKey, TSource> dictionary)
		{
#if NET40
			_Dictionary = dictionary;
#else
			_Dictionary = dictionary as IReadOnlyDictionary<TKey, TSource>;

			if (_Dictionary == null)
			{
				// If the dictionary doesn't implement IReadOnlyDictionary, wrap it so it does
				var NewDictionary = new ReadOnlyDictionary<TKey, TSource>(dictionary);

				_Dictionary = NewDictionary;
				_Keys = ((IDictionary<TKey, TSource>)NewDictionary).Keys;
				_KeysReadOnly = NewDictionary.Keys;
			}
			else
			{
				// Check the dictionary keys implement IReadOnlyCollection
				// We want to ensure all the Keys properties return the same reference
				var KeysReadOnly = dictionary.Keys as IReadOnlyCollection<TKey>;
				
				if (KeysReadOnly == null)
				{
					// Wrap it so it does
					var NewKeys = new ReadOnlyCollection<TKey>(dictionary.Keys);

					_Keys = NewKeys;
					_KeysReadOnly = NewKeys;
				}
				else
				{
					_Keys = dictionary.Keys;
					_KeysReadOnly = KeysReadOnly;
				}
			}
#endif
		}

#if !NET40
		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionaryConverter(IReadOnlyDictionary<TKey, TSource> dictionary)
		{
			_Dictionary = dictionary;

			var KeysCollection = dictionary.Keys as ICollection<TKey>;
			var KeysReadOnly = dictionary.Keys as IReadOnlyCollection<TKey>;

			// We want to ensure all the Keys properties return the same values
			if (KeysCollection != null)
			{
				if (KeysReadOnly != null)
				{
					// The source Keys implement both interfaces we need
					_KeysReadOnly = KeysReadOnly;
					_Keys = KeysCollection;
				}
				else
				{
					// The source Keys implement ICollection but not IReadOnlyCollection
					var NewKeyCollection = new ReadOnlyCollection<TKey>(KeysCollection);

					_KeysReadOnly = NewKeyCollection;
					_Keys = NewKeyCollection;
				}
			}
			else
			{
				// The source Keys doesn't implement ICollection
				var NewKeyCollection = new KeyCollection(this);

				_KeysReadOnly = NewKeyCollection;
				_Keys = NewKeyCollection;
			}
		}
#endif
		
		//****************************************
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TTarget> item)
		{
			var SourcePair = new KeyValuePair<TKey, TSource>(item.Key, ConvertFrom(item.Value));

#if NET40
			return _Dictionary.Contains(SourcePair);
#else
			// If our source implements ICollection, use it and convert back
			if (_Dictionary is ICollection<KeyValuePair<TKey, TSource>>)
				return ((ICollection<KeyValuePair<TKey, TSource>>)_Dictionary).Contains(SourcePair);

			// No ICollection, so convert back and search for it the hard way
			return System.Linq.Enumerable.Contains(_Dictionary, SourcePair);
#endif
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
		public void CopyTo(KeyValuePair<TKey, TTarget>[] array, int arrayIndex)
		{
			foreach(var MyPair in _Dictionary)
			{
				array[arrayIndex++] = new KeyValuePair<TKey, TTarget>(MyPair.Key, ConvertTo(MyPair.Value));
			}
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public ChildPairEnumerator GetEnumerator()
		{
			return new ChildPairEnumerator(this);
		}
		
		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key, out TTarget value)
		{
			TSource MyValue;
			
			if (_Dictionary.TryGetValue(key, out MyValue))
			{
				value = ConvertTo(MyValue);
				
				return true;
			}
			
			value = default(TTarget);
			
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
		protected virtual TSource ConvertFrom(TTarget value)
		{
			throw new NotSupportedException("Conversion is one-way");
		}
		
		//****************************************
		
		void ICollection<KeyValuePair<TKey, TTarget>>.Add(KeyValuePair<TKey, TTarget> item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void IDictionary<TKey, TTarget>.Add(TKey key, TTarget value)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void ICollection<KeyValuePair<TKey, TTarget>>.Clear()
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool ICollection<KeyValuePair<TKey, TTarget>>.Remove(KeyValuePair<TKey, TTarget> item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool IDictionary<TKey, TTarget>.Remove(TKey key)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ChildPairEnumerator(this);
		}

		IEnumerator<KeyValuePair<TKey, TTarget>> IEnumerable<KeyValuePair<TKey, TTarget>>.GetEnumerator()
		{
			return new ChildPairEnumerator(this);
		}
		
		void ICollection<TTarget>.Add(TTarget item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void ICollection<TTarget>.Clear()
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool ICollection<TTarget>.Contains(TTarget item)
		{
			var SourceValue = ConvertFrom(item);

#if NET40
			return _Dictionary.Values.Contains(SourceValue);
#else
			// If our source implements IDictionary, use it and convert back
			if (_Dictionary is IDictionary<TKey, TSource>)
				return ((IDictionary<TKey, TSource>)_Dictionary).Values.Contains(SourceValue);

			// No ICollection, so convert back and search for it the hard way
			return System.Linq.Enumerable.Contains(_Dictionary.Values, SourceValue);
#endif
		}
		
		void ICollection<TTarget>.CopyTo(TTarget[] array, int arrayIndex)
		{
			foreach(var MyValue in _Dictionary.Values)
			{
				array[arrayIndex++] = ConvertTo(MyValue);
			}
		}
		
		bool ICollection<TTarget>.Remove(TTarget item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		IEnumerator<TTarget> IEnumerable<TTarget>.GetEnumerator()
		{
			return new ChildEnumerator(this);
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
			get { return _Dictionary.Keys; }
		}

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public ICollection<TTarget> Values
		{
			get { return this; }
		}
#else
		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public IReadOnlyCollection<TKey> Keys
		{
			get { return _KeysReadOnly; }
		}
		
		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public IReadOnlyCollection<TTarget> Values
		{
			get { return this; }
		}
#endif

		/// <summary>
		/// Gets the value corresponding to the provided key
		/// </summary>
		public TTarget this[TKey key]
		{
			get
			{
				TSource MyValue;
				
				if (_Dictionary.TryGetValue(key, out MyValue))
					return ConvertTo(MyValue);
				
				return default(TTarget);
			}
		}
		
		/// <summary>
		/// Gets the parent dictionary
		/// </summary>
#if NET40
		public IDictionary<TKey, TSource> Parent
#else
		public IReadOnlyDictionary<TKey, TSource> Parent
#endif
		{
			get { return _Dictionary; }
		}
		
		//****************************************
		
		bool ICollection<KeyValuePair<TKey, TTarget>>.IsReadOnly
		{
			get { return true; }
		}
		
		bool ICollection<TTarget>.IsReadOnly
		{
			get { return true; }
		}
		
		TTarget IDictionary<TKey, TTarget>.this[TKey key]
		{
			get
			{
				TSource MyValue;
				
				if (_Dictionary.TryGetValue(key, out MyValue))
					return ConvertTo(MyValue);
				
				return default(TTarget);
			}
			set { throw new NotSupportedException("Dictionary is read-only"); }
		}
		
		ICollection<TKey> IDictionary<TKey, TTarget>.Keys
		{
#if NET40
			get { return _Dictionary.Keys; } // Already Read-Only
#else
			get { return _Keys; } // Already Read-Only
#endif
		}
		
		ICollection<TTarget> IDictionary<TKey, TTarget>.Values
		{
			get { return this; }
		}
		
#if !NET40
		IEnumerable<TKey> IReadOnlyDictionary<TKey, TTarget>.Keys
		{
			get { return _Dictionary.Keys; }
		}
		
		IEnumerable<TTarget> IReadOnlyDictionary<TKey, TTarget>.Values
		{
			get { return this; }
		}
#endif

		//****************************************
		
		private class ChildEnumerator : IEnumerator<TTarget>
		{	//****************************************
			private readonly ReadOnlyDictionaryConverter<TKey, TSource, TTarget> _Parent;
			private readonly IEnumerator<KeyValuePair<TKey, TSource>> _Enumerator;
			
			private TTarget _Current;
			//****************************************
			
			public ChildEnumerator(ReadOnlyDictionaryConverter<TKey, TSource, TTarget> parent)
			{
				_Parent = parent;
				_Enumerator = _Parent._Dictionary.GetEnumerator();
			}
			
			//****************************************
			
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				_Current = default(TTarget);
			}
			
			public bool MoveNext()
			{
				var Result = _Enumerator.MoveNext();
				
				if (Result)
					_Current = _Parent.ConvertTo(_Enumerator.Current.Value);
				else
					_Current = default(TTarget);
				
				return Result;
			}
			
			public void Reset()
			{
				_Enumerator.Reset();
				
				_Current = default(TTarget);
			}
			
			//****************************************
			
			public TTarget Current
			{
				get { return _Current; }
			}
			
			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}
		
		/// <summary>
		/// Enumerates the collection while avoiding memory allocations
		/// </summary>
		public struct ChildPairEnumerator : IEnumerator<KeyValuePair<TKey, TTarget>>
		{	//****************************************
			private readonly ReadOnlyDictionaryConverter<TKey, TSource, TTarget> _Parent;
			private readonly IEnumerator<KeyValuePair<TKey, TSource>> _Enumerator;
			
			private KeyValuePair<TKey, TTarget> _Current;
			//****************************************
			
			internal ChildPairEnumerator(ReadOnlyDictionaryConverter<TKey, TSource, TTarget> parent)
			{
				_Parent = parent;
				_Enumerator = _Parent._Dictionary.GetEnumerator();
				_Current = default(KeyValuePair<TKey, TTarget>);
			}
			
			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				_Current = new KeyValuePair<TKey, TTarget>();
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
					_Current = new KeyValuePair<TKey, TTarget>();
				
				return Result;
			}
			
			void IEnumerator.Reset()
			{
				_Enumerator.Reset();
				
				_Current = new KeyValuePair<TKey, TTarget>();
			}
			
			//****************************************
			
			private void SetCurrent(KeyValuePair<TKey, TSource> current)
			{
				_Current = new KeyValuePair<TKey, TTarget>(current.Key, _Parent.ConvertTo(current.Value));
			}
			
			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public KeyValuePair<TKey, TTarget> Current
			{
				get { return _Current; }
			}
			
			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}

		private class KeyCollection : ICollection<TKey>
#if !NET40
			, IReadOnlyCollection<TKey>
#endif
		{	//****************************************
			private readonly ReadOnlyDictionaryConverter<TKey, TSource, TTarget> _Source;
			//****************************************

			public KeyCollection(ReadOnlyDictionaryConverter<TKey, TSource, TTarget> source)
			{
				_Source = source;
			}

			//****************************************

			public void CopyTo(TKey[] array, int arrayIndex)
			{
				foreach (var MyKey in _Source._Dictionary.Keys)
				{
					array[arrayIndex++] = MyKey;
				}
			}

			//****************************************

			void ICollection<TKey>.Add(TKey item)
			{
				throw new NotSupportedException("Dictionary is read-only");
			}

			void ICollection<TKey>.Clear()
			{
				throw new NotSupportedException("Dictionary is read-only");
			}

			bool ICollection<TKey>.Contains(TKey item)
			{
				return _Source._Dictionary.ContainsKey(item);
			}

			bool ICollection<TKey>.Remove(TKey item)
			{
				throw new NotSupportedException("Dictionary is read-only");
			}

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
			{
				return _Source._Dictionary.Keys.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return _Source._Dictionary.Keys.GetEnumerator();
			}

			//****************************************

			public int Count
			{
				get { return _Source._Dictionary.Count; }
			}

			bool ICollection<TKey>.IsReadOnly
			{
				get { return true; }
			}
		}
	}
}
