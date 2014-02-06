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
	public abstract class ReadOnlyDictionaryChild<TKey, TValue, TChild> : IDictionary<TKey, TChild>, IReadOnlyCollection<TChild>, ICollection<TChild>, IReadOnlyDictionary<TKey, TChild>
	{	//****************************************
		private readonly IDictionary<TKey, TValue> _Dictionary;
		private readonly ReadOnlyCollection<TKey> _Keys;
		//****************************************
		
		/// <summary>
		/// Creastes a new, empty read-only dictionary
		/// </summary>
		public ReadOnlyDictionaryChild()
		{
			_Dictionary = new Dictionary<TKey, TValue>();
			_Keys = new ReadOnlyCollection<TKey>(_Dictionary.Keys);
		}
		
		/// <summary>
		/// Creates a new read-only wrapper around a dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to wrap as read-only</param>
		public ReadOnlyDictionaryChild(IDictionary<TKey, TValue> dictionary)
		{
			_Dictionary = dictionary;
			_Keys = new ReadOnlyCollection<TKey>(_Dictionary.Keys);
		}
		
		//****************************************
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TChild> item)
		{
			throw new NotSupportedException("Cannot perform a Contains with a Key-Value Pair on a child dictionary");
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
		public void CopyTo(KeyValuePair<TKey, TChild>[] array, int arrayIndex)
		{
			foreach(var MyPair in _Dictionary)
			{
				array[arrayIndex++] = new KeyValuePair<TKey, TChild>(MyPair.Key, SelectChild(MyPair.Value));
			}
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<KeyValuePair<TKey, TChild>> GetEnumerator()
		{
			return new ChildPairEnumerator(this);
		}
		
		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key, out TChild value)
		{
			TValue MyValue;
			
			if (_Dictionary.TryGetValue(key, out MyValue))
			{
				value = SelectChild(MyValue);
				
				return true;
			}
			
			value = default(TChild);
			
			return false;
		}
		
		//****************************************
		
		/// <summary>
		/// Converts the parent value into the desired child value
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <returns>The child value, or default(TChild) if the parent value does not provide it</returns>
		protected abstract TChild SelectChild(TValue value);
		
		//****************************************
		
		void ICollection<KeyValuePair<TKey, TChild>>.Add(KeyValuePair<TKey, TChild> item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void IDictionary<TKey, TChild>.Add(TKey key, TChild value)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void ICollection<KeyValuePair<TKey, TChild>>.Clear()
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool ICollection<KeyValuePair<TKey, TChild>>.Remove(KeyValuePair<TKey, TChild> item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool IDictionary<TKey, TChild>.Remove(TKey key)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new ChildPairEnumerator(this);
		}
		
		void ICollection<TChild>.Add(TChild item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		void ICollection<TChild>.Clear()
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		bool ICollection<TChild>.Contains(TChild item)
		{
			throw new NotSupportedException("Cannot perform a Contains with a child dictionary");
		}
		
		void ICollection<TChild>.CopyTo(TChild[] array, int arrayIndex)
		{
			foreach(var MyValue in _Dictionary.Values)
			{
				array[arrayIndex++] = SelectChild(MyValue);
			}
		}
		
		bool ICollection<TChild>.Remove(TChild item)
		{
			throw new NotSupportedException("Dictionary is read-only");
		}
		
		IEnumerator<TChild> IEnumerable<TChild>.GetEnumerator()
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
		public IReadOnlyCollection<TChild> Values
		{
			get { return this; }
		}
		
		/// <summary>
		/// Gets the value corresponding to the provided key
		/// </summary>
		public TChild this[TKey key]
		{
			get
			{
				TValue MyValue;
				
				if (_Dictionary.TryGetValue(key, out MyValue))
					return SelectChild(MyValue);
				
				return default(TChild);
			}
		}
		
		//****************************************
		
		bool ICollection<KeyValuePair<TKey, TChild>>.IsReadOnly
		{
			get { return true; }
		}
		
		bool ICollection<TChild>.IsReadOnly
		{
			get { return true; }
		}
		
		TChild IDictionary<TKey, TChild>.this[TKey key]
		{
			get
			{
				TValue MyValue;
				
				if (_Dictionary.TryGetValue(key, out MyValue))
					return SelectChild(MyValue);
				
				return default(TChild);
			}
			set { throw new NotSupportedException("Dictionary is read-only"); }
		}
		
		ICollection<TKey> IDictionary<TKey, TChild>.Keys
		{
			get { return _Dictionary.Keys; } // Already Read-Only
		}
		
		ICollection<TChild> IDictionary<TKey, TChild>.Values
		{
			get { return this; }
		}
		
		IEnumerable<TKey> IReadOnlyDictionary<TKey, TChild>.Keys
		{
			get { return _Dictionary.Keys; }
		}
		
		IEnumerable<TChild> IReadOnlyDictionary<TKey, TChild>.Values
		{
			get { return this; }
		}
		
		//****************************************
		
		private class ChildEnumerator : IEnumerator<TChild>
		{	//****************************************
			private readonly ReadOnlyDictionaryChild<TKey, TValue, TChild> _Parent;
			private readonly IEnumerator<KeyValuePair<TKey, TValue>> _Enumerator;
			
			private TChild _Current;
			//****************************************
			
			public ChildEnumerator(ReadOnlyDictionaryChild<TKey, TValue, TChild> parent)
			{
				_Parent = parent;
				_Enumerator = _Parent._Dictionary.GetEnumerator();
			}
			
			//****************************************
			
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				_Current = _Parent.SelectChild(_Enumerator.Current.Value);
			}
			
			public bool MoveNext()
			{
				var Result = _Enumerator.MoveNext();
				
				_Current = _Parent.SelectChild(_Enumerator.Current.Value);
				
				return Result;
			}
			
			public void Reset()
			{
				_Enumerator.Reset();
				
				_Current = _Parent.SelectChild(_Enumerator.Current.Value);
			}
			
			//****************************************
			
			public TChild Current
			{
				get { return _Current; }
			}
			
			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}
		
		private class ChildPairEnumerator : IEnumerator<KeyValuePair<TKey, TChild>>
		{	//****************************************
			private readonly ReadOnlyDictionaryChild<TKey, TValue, TChild> _Parent;
			private readonly IEnumerator<KeyValuePair<TKey, TValue>> _Enumerator;
			
			private KeyValuePair<TKey, TChild> _Current;
			//****************************************
			
			public ChildPairEnumerator(ReadOnlyDictionaryChild<TKey, TValue, TChild> parent)
			{
				_Parent = parent;
				_Enumerator = _Parent._Dictionary.GetEnumerator();
			}
			
			//****************************************
			
			public void Dispose()
			{
				_Enumerator.Dispose();
				
				SetCurrent(_Enumerator.Current);
			}
			
			public bool MoveNext()
			{
				var Result = _Enumerator.MoveNext();
				
				SetCurrent(_Enumerator.Current);
				
				return Result;
			}
			
			public void Reset()
			{
				_Enumerator.Reset();
				
				SetCurrent(_Enumerator.Current);
			}
			
			//****************************************
			
			private void SetCurrent(KeyValuePair<TKey, TValue> current)
			{
				_Current = new KeyValuePair<TKey, TChild>(current.Key, _Parent.SelectChild(current.Value));
			}
			
			//****************************************
			
			public KeyValuePair<TKey, TChild> Current
			{
				get { return _Current; }
			}
			
			object IEnumerator.Current
			{
				get { return _Current; }
			}
		}
	}
}
