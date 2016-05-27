﻿/****************************************\
 WeakDictionary.cs
 Created: 2013-08-20
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a dictionary that holds only weak references to its values
	/// </summary>
	/// <remarks>This class does not implement IDictionary or ICollection, as many of the methods have no meaning until you have strong references to the contents</remarks>
	public class WeakDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable where TValue : class
	{	//****************************************
		private readonly Dictionary<TKey, GCReference> _Dictionary;
		private readonly GCHandleType _HandleType;
		private readonly IEqualityComparer<TKey> _Comparer;
		//****************************************

		/// <summary>
		/// Creates a new WeakDictionary
		/// </summary>
		public WeakDictionary() : this(EqualityComparer<TKey>.Default, GCHandleType.Weak)
		{
		}
		
		/// <summary>
		/// Creates a new WeakDictionary with the given equality comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public WeakDictionary(IEqualityComparer<TKey> comparer) : this(comparer, GCHandleType.Weak)
		{
		}
		
		/// <summary>
		/// Creates a new WeakDictionary with the given equality comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		/// <param name="handleType">The type of GCHandle to use</param>
		public WeakDictionary(IEqualityComparer<TKey> comparer, GCHandleType handleType)
		{
			_Dictionary = new Dictionary<TKey, GCReference>(comparer);
			_Comparer = comparer;
			_HandleType = handleType;
		}

		/// <summary>
		/// Creates a new WeakDictionary of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		public WeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, EqualityComparer<TKey>.Default, GCHandleType.Weak)
		{
		}
		
		/// <summary>
		/// Creates a new WeakDictionary of references to the contents of the collection with the given equality comparer
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public WeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : this(collection, comparer, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new WeakDictionary of references to the contents of the collection with the given equality comparer
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		/// <param name="handleType">The type of GCHandle to use</param>
		public WeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer, GCHandleType handleType)
		{
			_HandleType = handleType;
			_Dictionary = collection.ToDictionary((value) => value.Key, (value) => new GCReference(value.Value, _HandleType), comparer);
			_Comparer = comparer;
		}

		//****************************************

		/// <summary>
		/// Adds a new pair to the dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value that will be weakly referenced</param>
		/// <exception cref="ArgumentNullException">Value was null</exception>
		/// <exception cref="ArgumentException">Key already exists</exception>
		public void Add(TKey key, TValue value)
		{	//****************************************
			GCReference MyHandle;
			//****************************************
			
			if (value == null)
				throw new ArgumentNullException("Cannot add null to a Weak Dictionary");
			
			// Is this key already in the dictionary?
			if (_Dictionary.TryGetValue(key, out MyHandle))
			{
				// Yes, is the object available?
				if (MyHandle.IsAlive)
					throw new ArgumentException("Key already exists in the Weak Dictionary");
				
				// No, free the handle and replace it
				MyHandle.Dispose();
				
				_Dictionary[key] = new GCReference(value, _HandleType);
			}
			else
			{
				// Not in the dictionary, add it
				_Dictionary.Add(key, new GCReference(value, _HandleType));
			}
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			foreach (var MyHandle in _Dictionary.Values)
				MyHandle.Dispose();

			_Dictionary.Clear();
		}

		/// <summary>
		/// Compacts the dictionary
		/// </summary>
		/// <returns>A list of keys where the values have expired</returns>
		public IEnumerable<TKey> Compact()
		{	//****************************************
			List<TKey> ExpiredKeys = new List<TKey>();
			//****************************************

			// Locate all the items in the dictionary that are still valid
			foreach (var Pair in _Dictionary)
			{
				if (Pair.Value.IsAlive)
					continue;

				// Add this key to the list of expired keys
				Pair.Value.Dispose();

				ExpiredKeys.Add(Pair.Key);
			}

			// If we found any expired keys, remove them
			foreach (var MyKey in ExpiredKeys)
				_Dictionary.Remove(MyKey);

			return ExpiredKeys;
		}

		/// <summary>
		/// Checks whether a key has an active value in the dictionary
		/// </summary>
		/// <param name="key">The key to check for</param>
		/// <returns>True if the key exists and the value is still valid, otherwise False</returns>
		/// <remarks>Note that the value may be garbage collected after or even during this call</remarks>
		public bool ContainsKey(TKey key)
		{	//****************************************
			GCReference MyHandle;
			//****************************************
			
			// Does the item exist in the dictionary?
			if (_Dictionary.TryGetValue(key, out MyHandle))
				// Yes, is the reference valid?
				return MyHandle.Target != null;

			return false;
		}

		/// <summary>
		/// Disposes of the Weak Dictionary, cleaning up any weak references
		/// </summary>
		public void Dispose()
		{
			foreach (var MyValue in _Dictionary.Values)
				MyValue.Dispose();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the live values in the dictionary
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public Enumerator GetEnumerator()
		{
			return new Enumerator(_Dictionary);
		}

		/// <summary>
		/// Removes a pair from the dictionary
		/// </summary>
		/// <param name="key">The key associated with the value to remove</param>
		/// <returns>True if the item was found and removed, otherwise False</returns>
		public bool Remove(TKey key)
		{	//****************************************
			GCReference MyHandle;
			bool HasValue;
			//****************************************
			
			if (!_Dictionary.TryGetValue(key, out MyHandle))
				return false;

			HasValue = MyHandle.IsAlive;

			_Dictionary.Remove(key); // Should always succeed

			MyHandle.Dispose();

			return HasValue; // Only return true if the reference was still valid
		}

		/// <summary>
		/// Removes the specified key/value pair
		/// </summary>
		/// <param name="key">The key to remove</param>
		/// <param name="value">The value to remove</param>
		/// <returns>True if the key was found with the expected value, otherwise false</returns>
		public bool Remove(TKey key, TValue value)
		{	//****************************************
			GCReference MyHandle;
			//****************************************

			if (value == null)
				throw new ArgumentNullException("Cannot have a null in a Weak Dictionary");

			// Is this key in the dictionary?
			if (!_Dictionary.TryGetValue(key, out MyHandle))
				return false;

			// Is the referenced value as expected?
			if ((TValue)MyHandle.Target != value)
				return false;
			
			_Dictionary.Remove(key); // Should always succeed

			MyHandle.Dispose();

			return true; // Only return true if the reference was still valid
		}
		
		/// <summary>
		/// Retrieves the value associated with the specified key
		/// </summary>
		/// <param name="key">The key to retrieve the value for</param>
		/// <param name="value">Receives the value associated with the key, or null if the key does not exist or the value is no longer available</param>
		/// <returns>True if the key was found and the value was available, otherwise False</returns>
		/// <remarks>Does not remove the key if the value is no longer available</remarks>
		public bool TryGetValue(TKey key, out TValue value)
		{	//****************************************
			GCReference MyHandle;
			//****************************************
			
			// Does the item exist in the dictionary?
			if (!_Dictionary.TryGetValue(key, out MyHandle))
			{
				value = null;
				
				return false;
			}
			
			// Yes, is the reference valid?
			value = (TValue)MyHandle.Target;
			
			return value != null;
		}

		/// <summary>
		/// Removes the value associated with the specified key
		/// </summary>
		/// <param name="key">The key to remove</param>
		/// <param name="value">The value to remove, if still referenced. Null if the key was not found or was found but the reference expired</param>
		/// <returns>True if the key was found and still referenced, otherwise false</returns>
		public bool TryRemove(TKey key, out TValue value)
		{	//****************************************
			GCReference MyHandle;
			//****************************************

			if (!_Dictionary.TryGetValue(key, out MyHandle))
			{
				value = null;

				return false;
			}

			value = (TValue)MyHandle.Target;

			_Dictionary.Remove(key); // Should always succeed

			MyHandle.Dispose();

			return value != null; // Only return true if the reference was still valid
		}

		/// <summary>
		/// Constructs a list of strong references to the values in the dictionary
		/// </summary>
		/// <returns>A list containing all the live values in this dictionary</returns>
		/// <remarks>Changes made to the returned list will not be reflected in the Weak Dictionary</remarks>
		public IList<TValue> ToValueList()
		{	//****************************************
			var MyList = new List<TValue>(_Dictionary.Count);
			//****************************************

			foreach (var MyPair in this)
				MyList.Add(MyPair.Value);

			return MyList;
		}

		/// <summary>
		/// Constructs a list of strong references to the current key/value pairs in the dictionary
		/// </summary>
		/// <returns>A list containing all the live keys/value pairs in this dictionary</returns>
		/// <remarks>Changes made to the returned list will not be reflected in the Weak Dictionary</remarks>
		public IList<KeyValuePair<TKey, TValue>> ToList()
		{	//****************************************
			var MyList = new List<KeyValuePair<TKey, TValue>>(_Dictionary.Count);
			//****************************************

			MyList.AddRange(this);

			return MyList;
		}

		/// <summary>
		/// Constructs a strong dictionary from the live values in the weak dictionary
		/// </summary>
		/// <returns>A dictionary containing all the live values in this dictionary</returns>
		/// <remarks>Changes made to the returned dictionary will not be reflected in the Weak Dictionary</remarks>
		public IDictionary<TKey, TValue> ToDictionary()
		{	//****************************************
			var MyDictionary = new Dictionary<TKey, TValue>(_Dictionary.Count, _Comparer);
			//****************************************

			foreach (var MyPair in this)
				MyDictionary.Add(MyPair.Key, MyPair.Value);

			return MyDictionary;
		}
		
		//****************************************
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_Dictionary);
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return new Enumerator(_Dictionary);
		}

		//****************************************
		
		/// <summary>
		/// Gets/Sets the value associated with a key
		/// </summary>
		/// <param name="key">The key of the value to get or set</param>
		/// <exception cref="KeyNotFoundException">Thrown if the key is not found, or the value is no longer available</exception>
		/// <remarks>Returns null if the value has expired</remarks>
		public TValue this[TKey key]
		{
			get
			{	//****************************************
				GCReference MyHandle;
				//****************************************
				
				// Does the item exist in the dictionary?
				if (_Dictionary.TryGetValue(key, out MyHandle))
				{
					// Yes, return the reference whether valid or not
					return (TValue)MyHandle.Target;
				}
				
				throw new KeyNotFoundException();
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("Cannot add null to a Weak Dictionary");
				
				// If the key already exists, we need to free the weak reference
				if (_Dictionary.ContainsKey(key))
					_Dictionary[key].Dispose();

				_Dictionary[key] = new GCReference(value, _HandleType);
			}
		}

		/// <summary>
		/// Gets the equality comparer being used for the Key
		/// </summary>
		public IEqualityComparer<TKey> Comparer
		{
			get { return _Dictionary.Comparer; }
		}

		//****************************************

		/// <summary>
		/// Enumerates the dictionary while avoiding memory allocations
		/// </summary>
		public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
		{	//****************************************
			private readonly IEnumerator<KeyValuePair<TKey, GCReference>> _Enumerator;
			private KeyValuePair<TKey, TValue> _Current;
			//****************************************

			internal Enumerator(Dictionary<TKey, GCReference> dictionary)
			{
				_Enumerator = dictionary.GetEnumerator();
				_Current = default(KeyValuePair<TKey, TValue>);
			}

			//****************************************

			/// <summary>
			/// Disposes of the enumerator
			/// </summary>
			[SecuritySafeCritical]
			public void Dispose()
			{
			}

			/// <summary>
			/// Tries to move to the next item
			/// </summary>
			/// <returns>True if there's another item to enumerate, otherwise False</returns>
			[SecuritySafeCritical]
			public bool MoveNext()
			{
				for (; ; )
				{
					if (!_Enumerator.MoveNext())
					{
						_Current = default(KeyValuePair<TKey, TValue>);

						return false;
					}

					var MyCurrent = _Enumerator.Current;
					TValue MyValue;

					try
					{
						MyValue = (TValue)MyCurrent.Value.Target;
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed, try again
						continue;
					}

					if (MyValue == null)
						continue;

					_Current = new KeyValuePair<TKey, TValue>(MyCurrent.Key, MyValue);

					return true;
				}
			}

			[SecuritySafeCritical]
			void IEnumerator.Reset()
			{
				_Enumerator.Reset();
				_Current = default(KeyValuePair<TKey, TValue>);
			}

			//****************************************

			/// <summary>
			/// Gets the current item being enumerated
			/// </summary>
			public KeyValuePair<TKey, TValue> Current
			{
				[SecuritySafeCritical]
				get { return _Current; }
			}

			object IEnumerator.Current
			{
				[SecuritySafeCritical]
				get { return _Current; }
			}
		}
	}
}
