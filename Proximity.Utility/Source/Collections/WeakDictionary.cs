/****************************************\
 WeakDictionary.cs
 Created: 2013-08-20
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a dictionary that holds only weak references to its values
	/// </summary>
	/// <remarks>This class does not implement IDictionary or ICollection, as many of the methods have no meaning until you have strong references to the contents</remarks>
	public class WeakDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TValue : class
	{	//****************************************
		private readonly Dictionary<TKey, GCHandle> _Dictionary;
		//****************************************
		
		/// <summary>
		/// Creates a new WeakDictionary
		/// </summary>
		public WeakDictionary()
		{
			_Dictionary = new Dictionary<TKey, GCHandle>();
		}
		
		/// <summary>
		/// Creates a new WeakDictionary of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		public WeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null)
		{
		}
		
		/// <summary>
		/// Creates a new WeakDictionary with the given equality comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public WeakDictionary(IEqualityComparer<TKey> comparer)
		{
			_Dictionary = new Dictionary<TKey, GCHandle>(comparer);
		}
		
		/// <summary>
		/// Creates a new WeakDictionary of references to the contents of the collection with the given equality comparer
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public WeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
		{
			_Dictionary = collection.ToDictionary((value) => value.Key, (value) => CreateFrom(value.Value), comparer);
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
			GCHandle MyHandle;
			//****************************************
			
			if (value == null)
				throw new ArgumentNullException("Cannot add null to a Weak Dictionary");
			
			// Is this key already in the dictionary?
			if (_Dictionary.TryGetValue(key, out MyHandle))
			{
				// Yes, is the object available?
				if (MyHandle.Target != null)
					throw new ArgumentException("Key already exists in the Weak Dictionary");
				
				// No, free the handle and replace it
				MyHandle.Free();
				
				_Dictionary[key] = CreateFrom(value);
			}
			else
			{
				// Not in the dictionary, add it
				_Dictionary.Add(key, CreateFrom(value));
			}
		}
		
		/// <summary>
		/// Removes a pair from the dictionary
		/// </summary>
		/// <param name="key">The key associated with the value to remove</param>
		/// <returns>True if the item was found and removed, otherwise False</returns>
		public bool Remove(TKey key)
		{	//****************************************
			GCHandle MyHandle;
			//****************************************
			
			if (!_Dictionary.TryGetValue(key, out MyHandle))
				return false;
			
			// Free the handle and remove the pair
			MyHandle.Free();
			
			return _Dictionary.Remove(key);
		}
		
		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			foreach(var MyHandle in _Dictionary.Values)
			{
				MyHandle.Free();
			}
			
			_Dictionary.Clear();
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
			GCHandle MyHandle;
			//****************************************
			
			// Does the item exist in the dictionary?
			if (!_Dictionary.TryGetValue(key, out MyHandle))
			{
				value = null;
				
				return false;
			}
			
			// Yes, is the reference valid?
			value = (TValue)MyHandle.Target;
			
			if (value != null)
				return true;
			
			return false;
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
			foreach(var Pair in _Dictionary)
			{
				var MyValue = (TValue)Pair.Value.Target;
				
				if (MyValue != null)
					continue;
				
				// Add this key to the list of expired keys
				Pair.Value.Free();
				
				ExpiredKeys.Add(Pair.Key);
			}
			
			// If we found any expired keys, remove them
			foreach(var MyKey in ExpiredKeys)
				_Dictionary.Remove(MyKey);
			
			return ExpiredKeys;
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the live values in the dictionary
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return GetContents().GetEnumerator();
		}
		
		/// <summary>
		/// Constructs a strong dictionary from the live values in the weak dictionary
		/// </summary>
		/// <returns>A dictionary containing all the live values in this dictionary</returns>
		/// <remarks>Changes made to the returned dictionary will not be reflected in the Weak Dictionary</remarks>
		public IDictionary<TKey, TValue> ToStrongDictionary()
		{
			return GetContents().ToDictionary(item => item.Key, item => item.Value);
		}
		
		//****************************************
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetContents().GetEnumerator();
		}
		
		private GCHandle CreateFrom(TValue item)
		{
			return GCHandle.Alloc(item, GCHandleType.Weak);
		}
		
		private IEnumerable<KeyValuePair<TKey, TValue>> GetContents()
		{
			foreach(var MyResult in _Dictionary)
			{
				var MyValue = (TValue)MyResult.Value.Target;
				
				if (MyValue == null)
					continue;
				
				yield return new KeyValuePair<TKey, TValue>(MyResult.Key, MyValue);
			}
		}
		
		private IEnumerable<TValue> GetValues()
		{
			foreach(var MyResult in _Dictionary)
			{
				var MyValue = (TValue)MyResult.Value.Target;
				
				if (MyValue == null)
					continue;
				
				yield return MyValue;
			}
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
				GCHandle MyHandle;
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
				{
					GCHandle MyHandle = _Dictionary[key];
					
					MyHandle.Free();
				}
				
				_Dictionary[key] = CreateFrom(value);
			}
		}
		
		/// <summary>
		/// Gets a list of strong references to the current key/value pairs in the dictionary
		/// </summary>
		public IList<KeyValuePair<TKey, TValue>> Contents
		{
			get { return GetContents().ToArray(); }
		}
		
		/// <summary>
		/// Gets a list of strong references to the values in the dictionary
		/// </summary>
		public IList<TValue> Values
		{
			get { return GetValues().ToList(); }
		}
	}
}
