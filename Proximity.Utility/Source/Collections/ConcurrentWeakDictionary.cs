/****************************************\
 WeakDictionary.cs
 Created: 2013-08-20
\****************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
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
	public class ConcurrentWeakDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TValue : class
	{	//****************************************
		private readonly ConcurrentDictionary<TKey, GCHandle> _Dictionary;
		//****************************************
		
		/// <summary>
		/// Creates a new WeakDictionary
		/// </summary>
		public ConcurrentWeakDictionary()
		{
			_Dictionary = new ConcurrentDictionary<TKey, GCHandle>();
		}
		
		/// <summary>
		/// Creates a new WeakDictionary of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		public ConcurrentWeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null)
		{
		}
		
		/// <summary>
		/// Creates a new WeakDictionary with the given equality comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public ConcurrentWeakDictionary(IEqualityComparer<TKey> comparer)
		{
			_Dictionary = new ConcurrentDictionary<TKey, GCHandle>(comparer);
		}
		
		/// <summary>
		/// Creates a new WeakDictionary of references to the contents of the collection with the given equality comparer
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public ConcurrentWeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
		{
			_Dictionary = new ConcurrentDictionary<TKey, GCHandle>(collection.Select((value) => new KeyValuePair<TKey, GCHandle>(value.Key, CreateFrom(value.Value))), comparer);
		}
		
		//****************************************
		
		public void AddOrReplace(TKey key, TValue value)
		{	//****************************************
			GCHandle MyHandle, NewHandle;
			//****************************************
		
			if (value == null)
				throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
		
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (_Dictionary.TryGetValue(key, out MyHandle))
				{
					var OldValue = (TValue)MyHandle.Target;
					
					// Yes. If the reference is the same, no need to change anything
					if (object.ReferenceEquals(OldValue, value))
						return;
					
					// Reference has changed, create a new GCHandle
					NewHandle = CreateFrom(value);
					
					// Try and update the dictionary with the replacement value
					if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
					{
						// Success, now we can safely expire the old handle
						MyHandle.Free();
						
						return;
					}
					
					// Key was updated elsewhere, ditch the updated value and try again
					NewHandle.Free();
					
					continue;
				}
				
				// Create a GC Handle to reference the object
				NewHandle = CreateFrom(value);
				
				// Try and add it to the dictionary
				if (_Dictionary.TryAdd(key, NewHandle))
					return; // Success, return the result
				
				// Key was added concurrently, free the handle we no longer need
				NewHandle.Free();
				
				// Loop back and try again
			}
		}
		
		public TValue AddOrUpdate(TKey key, Func<TKey, TValue> valueCallback, Func<TKey, TValue, TValue> updateCallback)
		{	//****************************************
			GCHandle MyHandle, NewHandle;
			
			TValue OldValue, NewValue;
			//****************************************
			
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (_Dictionary.TryGetValue(key, out MyHandle))
				{
					OldValue = (TValue)MyHandle.Target;
					
					// Yes, does the target still exist?
					if (OldValue != null)
					{
						// Yes, let's try and update it
						NewValue = updateCallback(key, OldValue);
					
						// If the reference is the same, no need to change anything
						if (object.ReferenceEquals(OldValue, NewValue))
							return NewValue;
					}
					else
					{
						// Target reference has vanished, replace it with the new value
						NewValue = valueCallback(key);
						
						if (NewValue == null)
							throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
					}
						
					// Reference has changed, create a new GCHandle
					NewHandle = CreateFrom(NewValue);
					
					// Try and update the dictionary with the replacement value
					if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
					{
						// Success, now we can safely expire the old handle
						MyHandle.Free();
						
						return NewValue;
					}
					
					// Key was updated elsewhere, ditch the updated value and try again
					NewHandle.Free();
					
					continue;
				}
				
				// Key not found, so let's try and add it
				NewValue = valueCallback(key);
				
				if (NewValue == null)
					throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
			
				// Create a GC Handle to reference the object
				NewHandle = CreateFrom(NewValue);
				
				// Try and add it to the dictionary
				if (_Dictionary.TryAdd(key, NewHandle))
					return NewValue; // Success, return the result
				
				// Key was added concurrently, free the handle we no longer need
				NewHandle.Free();
				
				// Loop back and try again
			}
		}
		
		public TValue AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateCallback)
		{
			if (value == null)
				throw new ArgumentNullException("Cannot add null to a Weak Dictionary");
			
			return AddOrUpdate(key, (innerKey) => value, updateCallback);
		}
		
		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			GCHandle MyHandle;
			
			foreach (var MyPair in _Dictionary)
			{
				if (_Dictionary.TryRemove(MyPair.Key, out MyHandle))
					MyHandle.Free();
			}
		}
		
		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueCallback)
		{	//****************************************
			GCHandle MyHandle, NewHandle;
			TValue NewValue;
			//****************************************
			
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (_Dictionary.TryGetValue(key, out MyHandle))
				{
					var MyValue = (TValue)MyHandle.Target;
					
					// Yes, does the target still exist?
					if (MyValue != null)
						return MyValue; // Yes, return it
					
					// Target reference has vanished, replace it with the new value
					NewValue = valueCallback(key);
					
					if (NewValue == null)
						throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
					
					NewHandle = CreateFrom(NewValue);
					
					// Try and update the dictionary with the replacement value
					if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
					{
						// Success, now we can safely expire the old handle
						MyHandle.Free();
						
						return NewValue;
					}
					
					// Key was updated elsewhere, ditch the updated value and try again
					NewHandle.Free();
					
					continue;
				}
				
				// Key not found, so let's try and add it
				NewValue = valueCallback(key);
				
				if (NewValue == null)
					throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
			
				// Create a GC Handle to reference the object
				NewHandle = CreateFrom(NewValue);
				
				// Try and add it to the dictionary
				if (_Dictionary.TryAdd(key, NewHandle))
					return NewValue; // Success, return the result
				
				// Key was added concurrently, free the handle we no longer need
				NewHandle.Free();
				
				// Loop back and try again
			}
		}
		
		public TValue GetOrAdd(TKey key, TValue value)
		{
			return GetOrAdd(key, (innerKey) => value);
		}
		
		/// <summary>
		/// Removes the specified key/value pair
		/// </summary>
		/// <param name="key">The key to remove</param>
		/// <param name="value">The value to remove</param>
		/// <returns>True if the key was found with the expected value, otherwise false</returns>
		public bool Remove(TKey key, TValue value)
		{	//****************************************
			GCHandle MyHandle;
			TValue MyValue;
			//****************************************
			
			if (value == null)
				throw new ArgumentNullException("Cannot have a null in a Weak Dictionary");
			
			// Is this key in the dictionary?
			if (!_Dictionary.TryGetValue(key, out MyHandle))
				return false;
			
			MyValue = (TValue)MyHandle.Target;
			
			// Is the referenced value as expected?
			if (MyValue != value)
				return false;
			
			// Yes, try and remove this key/gchandle pair
			return ((IDictionary<TKey, GCHandle>)_Dictionary).Remove(new KeyValuePair<TKey, GCHandle>(key, MyHandle));
		}
		
		/// <summary>
		/// Adds an item to the dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		/// <returns>True if the item was added, otherwise false</returns>
		/// <remarks>Will add if the key is not set, and replace if the value is no longer available</remarks>
		public bool TryAdd(TKey key, TValue value)
		{	//****************************************
			GCHandle MyHandle, NewHandle;
			TValue NewValue;
			//****************************************
		
			if (value == null)
				throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
			
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (!_Dictionary.TryGetValue(key, out MyHandle))
				{
					// Key not found, so let's try and add it
					// Create a GC Handle to reference the object
					NewHandle = CreateFrom(value);
					
					// Try and add it to the dictionary
					if (_Dictionary.TryAdd(key, NewHandle))
						return true; // Success, return the result
					
					// Key was added concurrently, free the handle we no longer need
					NewHandle.Free();
					
					return false;
				}
				
				// Key found, get the target reference
				var MyValue = (TValue)MyHandle.Target;
				
				// Does it still exist?
				if (MyValue != null)
					return false; // Yes, can't add
				
				// Target reference has vanished, we can replace it with the new value
				NewHandle = CreateFrom(value);
				
				// Try and update the dictionary with the replacement value
				if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
				{
					// Success, now we can safely expire the old handle
					MyHandle.Free();
					
					return true;
				}
				
				// Key was updated elsewhere. Could have been removed or simply updated with a valid value elsewhere
				// Ditch the updated value and try again
				NewHandle.Free();
			}
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
			
			return value != null;
		}
		
		/// <summary>
		/// Removes the value associated with the specified key
		/// </summary>
		/// <param name="key">The key to remove</param>
		/// <param name="value">The value to remove, if still referenced. Null if the key was not found or was found but the reference expired</param>
		/// <returns>True if the key was found, otherwise false</returns>
		/// <remarks>Will return true with null as value if the reference expired</remarks>
		public bool TryRemove(TKey key, out TValue value)
		{	//****************************************
			GCHandle MyHandle;
			//****************************************
			
			if (_Dictionary.TryRemove(key, out MyHandle))
			{
				value = (TValue)MyHandle.Target;
				
				MyHandle.Free();
				
				return true;
			}
			
			value = null;
			
			return false;
		}
		
		/// <summary>
		/// Updates an item if it exists in the dictionary
		/// </summary>
		/// <param name="key">The key of the item to update</param>
		/// <param name="updateCallback">A callback that performs the update</param>
		/// <param name="newValue">Receives the new value if the dictionary was updated</param>
		/// <returns>True if the item was updated, False if it does not exist or the reference expired</returns>
		public bool TryUpdate(TKey key, Func<TKey, TValue, TValue> updateCallback, out TValue newValue)
		{	//****************************************
			GCHandle MyHandle, NewHandle;
			
			TValue OldValue, NewValue;
			//****************************************
			
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (!_Dictionary.TryGetValue(key, out MyHandle))
				{
					newValue = null;
					
					return false; // No, update fails
				}
				
				OldValue = (TValue)MyHandle.Target;
				
				// Yes, is the reference still valid?
				if (OldValue == null)
				{
					newValue = null;
					
					return false; // No, update fails
				}
				
				// Yes, update it
				NewValue = updateCallback(key, OldValue);
				
				if (NewValue == null)
					throw new ArgumentNullException("Cannot add null to a Weak Dictionary");
				
				// Yes. If the old and new references are the same, no need to change anything
				// Check now, rather than earlier, so we can return true if it was 'updated' correctly
				if (object.ReferenceEquals(OldValue, NewValue))
				{
					newValue = NewValue;
					
					return true;
				}
				
				// Reference has changed, create a new GCHandle
				NewHandle = CreateFrom(NewValue);
				
				// Try and update the dictionary with the replacement value
				if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
				{
					// Success, now we can safely expire the old handle
					MyHandle.Free();
					
					newValue = NewValue;
					
					return true;
				}
			
				// Key was updated elsewhere, ditch the updated value and try again
				NewHandle.Free();
			}
		}
		
		/// <summary>
		/// Updates an item in the dictionary
		/// </summary>
		/// <param name="key">The key of the item to update</param>
		/// <param name="newValue">The new value of the item</param>
		/// <param name="oldValue">The old value we expect to replace</param>
		/// <returns>True if the key exists and old value was replaced with new value, otherwise false</returns>
		/// <remarks>Will return false if the reference has expired</remarks>
		public bool TryUpdate(TKey key, TValue newValue, TValue oldValue)
		{	//****************************************
			GCHandle MyHandle, NewHandle;
			
			TValue OldValue;
			//****************************************
			
			if (newValue == null)
				throw new ArgumentNullException("Cannot add null to a Weak Dictionary");
			
			if (oldValue == null)
				throw new ArgumentNullException("Cannot have a null in a Weak Dictionary");
			
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (!_Dictionary.TryGetValue(key, out MyHandle))
					return false; // No, update fails
				
				OldValue = (TValue)MyHandle.Target;
				
				// Yes, is it what we expected?
				if (!object.ReferenceEquals(oldValue, OldValue))
					return false; // No, update fails
				
				// Yes. If the old and new references are the same, no need to change anything
				// Check now, rather than earlier, so we can return true if it was 'updated' correctly
				if (object.ReferenceEquals(OldValue, newValue))
					return true;
				
				// Reference has changed, create a new GCHandle
				NewHandle = CreateFrom(newValue);
				
				// Try and update the dictionary with the replacement value
				if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
				{
					// Success, now we can safely expire the old handle
					MyHandle.Free();
					
					return true;
				}
			
				// Key was updated elsewhere, ditch the updated value and try again
				NewHandle.Free();
			}
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
				
				// Try and remove this exact pair
				if (!((IDictionary<TKey, GCHandle>)_Dictionary).Remove(Pair))
					continue;
				
				// Free the GCHandle
				Pair.Value.Free();
				
				// Add this key to the list of expired keys
				ExpiredKeys.Add(Pair.Key);
			}
			
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
			foreach(var MyResult in _Dictionary) // Get the base dictionary enumerator, as on ConcurrentDictionary getting Values will make a copy
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
			set { AddOrReplace(key, value); }
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
