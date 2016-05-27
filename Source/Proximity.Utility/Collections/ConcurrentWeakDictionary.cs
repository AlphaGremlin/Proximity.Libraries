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
using System.Security;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents a concurrent dictionary that holds only weak references to its values
	/// </summary>
	/// <remarks>This class does not implement IDictionary or ICollection, as many of the methods have no meaning until you have strong references to the contents</remarks>
	public class ConcurrentWeakDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TValue : class
	{	//****************************************
		private readonly ConcurrentDictionary<TKey, GCReference> _Dictionary;
		private readonly IEqualityComparer<TKey> _Comparer;
		private readonly GCHandleType _HandleType;
		//****************************************

		/// <summary>
		/// Creates a new Concurrent Weak Dictionary
		/// </summary>
		public ConcurrentWeakDictionary() : this(EqualityComparer<TKey>.Default, GCHandleType.Weak)
		{
		}
		
		/// <summary>
		/// Creates a new Concurrent Weak Dictionary with the given equality comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public ConcurrentWeakDictionary(IEqualityComparer<TKey> comparer) : this(comparer, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new Concurrent Weak Dictionary with the given equality comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		/// <param name="handleType">The type of GCHandle to use</param>
		public ConcurrentWeakDictionary(IEqualityComparer<TKey> comparer, GCHandleType handleType)
		{
			_Dictionary = new ConcurrentDictionary<TKey, GCReference>(comparer);
			_Comparer = comparer;
			_HandleType = handleType;
		}

		/// <summary>
		/// Creates a new Concurrent Weak Dictionary of references to the contents of the collection
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		public ConcurrentWeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, EqualityComparer<TKey>.Default, GCHandleType.Weak)
		{
		}
		
		/// <summary>
		/// Creates a new Concurrent Weak Dictionary of references to the contents of the collection with the given equality comparer
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		public ConcurrentWeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : this(collection, comparer, GCHandleType.Weak)
		{
		}

		/// <summary>
		/// Creates a new Concurrent Weak Dictionary of references to the contents of the collection with the given equality comparer
		/// </summary>
		/// <param name="collection">The collection holding the key/value pairs to add</param>
		/// <param name="comparer">The equality comparer to use when comparing keys</param>
		/// <param name="handleType">The type of GCHandle to use</param>
		public ConcurrentWeakDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer, GCHandleType handleType)
		{
			_Dictionary = new ConcurrentDictionary<TKey, GCReference>(collection.Select((value) => new KeyValuePair<TKey, GCReference>(value.Key, new GCReference(value.Value, handleType))), comparer);
			_Comparer = comparer;
			_HandleType = handleType;
		}
		
		//****************************************

		/// <summary>
		/// Adds or replaces a key/value pair
		/// </summary>
		/// <param name="key">The key to add or replace</param>
		/// <param name="value">The value to associate with the key</param>
		public void AddOrReplace(TKey key, TValue value)
		{	//****************************************
			GCReference MyHandle, NewHandle;
			//****************************************
		
			if (value == null)
				throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
		
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (_Dictionary.TryGetValue(key, out MyHandle))
				{
					try
					{
						var OldValue = (TValue)MyHandle.Target;

						// Yes. If the reference is the same, no need to change anything
						if (object.ReferenceEquals(OldValue, value))
							return;
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed, try again
						continue;
					}

					// Reference has changed, create a new GCReference
					NewHandle = new GCReference(value, _HandleType);
					
					// Try and update the dictionary with the replacement value
					if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
					{
						// Success, now we can safely expire the old handle
						MyHandle.Dispose();
						
						return;
					}
					
					// Key was updated elsewhere, ditch the updated value and try again
					NewHandle.Dispose();
					
					continue;
				}
				
				// Create a GC Handle to reference the object
				NewHandle = new GCReference(value, _HandleType);
				
				// Try and add it to the dictionary
				if (_Dictionary.TryAdd(key, NewHandle))
					return; // Success, return the result
				
				// Key was added concurrently, free the handle we no longer need
				NewHandle.Dispose();
				
				// Loop back and try again
			}
		}

		/// <summary>
		/// Adds or updates a key/value pair
		/// </summary>
		/// <param name="key">The key to add or replace</param>
		/// <param name="valueCallback">A callback providing the new value</param>
		/// <param name="updateCallback">A callback updating an existing value</param>
		/// <returns>The added or updated value</returns>
		public TValue AddOrUpdate(TKey key, Func<TKey, TValue> valueCallback, Func<TKey, TValue, TValue> updateCallback)
		{	//****************************************
			GCReference MyHandle, NewHandle;
			
			TValue OldValue, NewValue;
			//****************************************
			
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (_Dictionary.TryGetValue(key, out MyHandle))
				{
					try
					{
						OldValue = (TValue)MyHandle.Target;
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed, try again
						continue;
					}
					
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

					// Reference has changed, create a new GCReference
					NewHandle = new GCReference(NewValue, _HandleType);
					
					// Try and update the dictionary with the replacement value
					if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
					{
						// Success, now we can safely expire the old handle
						MyHandle.Dispose();
						
						return NewValue;
					}
					
					// Key was updated elsewhere, ditch the updated value and try again
					NewHandle.Dispose();
					
					continue;
				}
				
				// Key not found, so let's try and add it
				NewValue = valueCallback(key);
				
				if (NewValue == null)
					throw new InvalidOperationException("Cannot add null to a Weak Dictionary");
			
				// Create a GC Handle to reference the object
				NewHandle = new GCReference(NewValue, _HandleType);
				
				// Try and add it to the dictionary
				if (_Dictionary.TryAdd(key, NewHandle))
					return NewValue; // Success, return the result
				
				// Key was added concurrently, free the handle we no longer need
				NewHandle.Dispose();
				
				// Loop back and try again
			}
		}

		/// <summary>
		/// Adds or updates a key/value pair
		/// </summary>
		/// <param name="key">The key to add or replace</param>
		/// <param name="value">The value to associate if the key doesn't exist</param>
		/// <param name="updateCallback">A callback updating an existing value</param>
		/// <returns>The added or updated value</returns>
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
			GCReference MyHandle;
			
			foreach (var MyPair in _Dictionary)
			{
				if (_Dictionary.TryRemove(MyPair.Key, out MyHandle))
					MyHandle.Dispose();
			}
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
				return MyHandle.IsAlive;

			return false;
		}

		/// <summary>
		/// Disposes of the Concurrent Weak Dictionary, cleaning up any weak references
		/// </summary>
		public void Dispose()
		{
			// Free our GC Handles so we don't create a memory leak
			foreach (var MyValue in _Dictionary.Values)
				MyValue.Dispose();
		}

		/// <summary>
		/// Adds or retrieves a value based on the key
		/// </summary>
		/// <param name="key">The key to add or retrieve</param>
		/// <param name="valueCallback">A callback providing the new value if the key doesn't exist</param>
		/// <returns>The new or existing value</returns>
		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueCallback)
		{	//****************************************
			GCReference MyHandle, NewHandle;
			TValue NewValue;
			//****************************************
			
			for (; ;)
			{
				// Is this key already in the dictionary?
				if (_Dictionary.TryGetValue(key, out MyHandle))
				{
					try
					{
						var MyValue = (TValue)MyHandle.Target;

						// Yes, does the target still exist?
						if (MyValue != null)
							return MyValue; // Yes, return it
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed, try again
						continue;
					}
					
					// Target reference has vanished, replace it with the new value
					NewValue = valueCallback(key);
					
					if (NewValue == null)
						throw new InvalidOperationException("Cannot add null to a Weak Dictionary");

					NewHandle = new GCReference(NewValue, _HandleType);
					
					// Try and update the dictionary with the replacement value
					if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
					{
						// Success, now we can safely expire the old handle
						MyHandle.Dispose();
						
						return NewValue;
					}
					
					// Key was updated elsewhere, ditch the updated value and try again
					NewHandle.Dispose();
					
					continue;
				}
				
				// Key not found, so let's try and add it
				NewValue = valueCallback(key);
				
				if (NewValue == null)
					throw new InvalidOperationException("Cannot add null to a Weak Dictionary");

				// Create a GC Handle to reference the object
				NewHandle = new GCReference(NewValue, _HandleType);
				
				// Try and add it to the dictionary
				if (_Dictionary.TryAdd(key, NewHandle))
					return NewValue; // Success, return the result
				
				// Key was added concurrently, free the handle we no longer need
				NewHandle.Dispose();
				
				// Loop back and try again
			}
		}

		/// <summary>
		/// Adds or retrieves a value based on the key
		/// </summary>
		/// <param name="key">The key to add or retrieve</param>
		/// <param name="value">A value to associate if the key doesn't exist</param>
		/// <returns>The new or existing value</returns>
		public TValue GetOrAdd(TKey key, TValue value)
		{
			return GetOrAdd(key, (innerKey) => value);
		}

		/// <summary>
		/// Removes the value associated with the specified key
		/// </summary>
		/// <param name="key">The key to remove</param>
		/// <returns>True if the key was found and still referenced, otherwise false</returns>
		public bool Remove(TKey key)
		{	//****************************************
			GCReference MyHandle;
			//****************************************

			if (!_Dictionary.TryRemove(key, out MyHandle))
				return false;

			var IsValid = MyHandle.IsAlive;

			MyHandle.Dispose();

			return IsValid;
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
			TValue MyValue;
			//****************************************
			
			if (value == null)
				throw new ArgumentNullException("Cannot have a null in a Weak Dictionary");
			
			// Is this key in the dictionary?
			if (!_Dictionary.TryGetValue(key, out MyHandle))
				return false;

			try
			{
				MyValue = (TValue)MyHandle.Target;

				// Is the referenced value as expected?
				if (MyValue != value)
					return false;
			}
			catch (InvalidOperationException)
			{
				// The GCHandle was disposed, so someone concurrently removed it
				return false;
			}
			
			// Yes, try and remove this key/GCReference pair
			if (!((IDictionary<TKey, GCReference>)_Dictionary).Remove(new KeyValuePair<TKey, GCReference>(key, MyHandle)))
				return false;

			// Success, free the handle
			MyHandle.Dispose();

			return true;
		}

		/// <summary>
		/// Removes all items from the concurrent weak dictionary
		/// </summary>
		/// <returns>An array of all the key/value pairs removed</returns>
		public KeyValuePair<TKey, TValue>[] RemoveAll()
		{	//****************************************
			var MyValues = new List<KeyValuePair<TKey, TValue>>(_Dictionary.Count);
			GCReference MyHandle;
			TValue MyValue;
			//****************************************

			while (_Dictionary.Count > 0)
			{
				foreach (var MyKey in _Dictionary.Keys)
				{
					if (!_Dictionary.TryRemove(MyKey, out MyHandle))
						continue;

					MyValue = (TValue)MyHandle.Target;

					if (MyValue != null)
						MyValues.Add(new KeyValuePair<TKey, TValue>(MyKey, MyValue));

					MyHandle.Dispose();
				}
			}

			return MyValues.ToArray();
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
			GCReference MyHandle, NewHandle;
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
					NewHandle = new GCReference(value, _HandleType);
					
					// Try and add it to the dictionary
					if (_Dictionary.TryAdd(key, NewHandle))
						return true; // Success, return the result
					
					// Key was added concurrently, free the handle we no longer need
					NewHandle.Dispose();
					
					return false;
				}
				
				// Key found, is the weak reference still valid?
				if (MyHandle.IsAlive)
					return false; // Yes, can't add
				
				// Target reference has vanished, we can replace it with the new value
				NewHandle = new GCReference(value, _HandleType);
				
				// Try and update the dictionary with the replacement value
				if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
				{
					// Success, now we can safely expire the old handle
					MyHandle.Dispose();
					
					return true;
				}
				
				// Key was updated elsewhere. Could have been removed or simply updated with a valid value elsewhere
				// Ditch the updated value and try again
				NewHandle.Dispose();
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
			GCReference MyHandle;
			//****************************************
			
			// Does the item exist in the dictionary?
			if (!_Dictionary.TryGetValue(key, out MyHandle))
			{
				value = null;
				
				return false;
			}

			try
			{
				// Yes, is the reference valid?
				value = (TValue)MyHandle.Target;

				return value != null;
			}
			catch (InvalidOperationException)
			{
				// We can get Disposed of between TryGetValue and get_Target
				value = null;

				return false;
			}
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
			
			if (_Dictionary.TryRemove(key, out MyHandle))
			{
				value = (TValue)MyHandle.Target;
				
				MyHandle.Dispose();
				
				return value != null;
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
			GCReference MyHandle, NewHandle;
			
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

				try
				{
					OldValue = (TValue)MyHandle.Target;
				}
				catch (InvalidOperationException)
				{
					// The GCHandle was disposed, try again
					continue;
				}
				
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
				
				// Reference has changed, create a new GCReference
				NewHandle = new GCReference(NewValue, _HandleType);
				
				// Try and update the dictionary with the replacement value
				if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
				{
					// Success, now we can safely expire the old handle
					MyHandle.Dispose();
					
					newValue = NewValue;
					
					return true;
				}
			
				// Key was updated elsewhere, ditch the updated value and try again
				NewHandle.Dispose();
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
			GCReference MyHandle, NewHandle;
			
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

				try
				{
					OldValue = (TValue)MyHandle.Target;
				}
				catch (InvalidOperationException)
				{
					// The GCHandle was disposed, try again
					continue;
				}
				
				// Yes, is it what we expected?
				if (!object.ReferenceEquals(oldValue, OldValue))
					return false; // No, update fails
				
				// Yes. If the old and new references are the same, no need to change anything
				// Check now, rather than earlier, so we can return true if it was 'updated' correctly
				if (object.ReferenceEquals(OldValue, newValue))
					return true;

				// Reference has changed, create a new GCReference
				NewHandle = new GCReference(newValue, _HandleType);
				
				// Try and update the dictionary with the replacement value
				if (_Dictionary.TryUpdate(key, NewHandle, MyHandle))
				{
					// Success, now we can safely expire the old handle
					MyHandle.Dispose();
					
					return true;
				}
			
				// Key was updated elsewhere, ditch the updated value and try again
				NewHandle.Dispose();
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
				if (Pair.Value.IsAlive)
					continue;
				
				// Try and remove this exact pair
				if (!((IDictionary<TKey, GCReference>)_Dictionary).Remove(Pair))
					continue;
				
				// Free the GCReference
				Pair.Value.Dispose();
				
				// Add this key to the list of expired keys
				ExpiredKeys.Add(Pair.Key);
			}
			
			return ExpiredKeys;
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

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return new Enumerator(_Dictionary);
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
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
					try
					{
						// Yes, return the reference whether valid or not
						return (TValue)MyHandle.Target;
					}
					catch (InvalidOperationException)
					{
						// The GCHandle was disposed
					}
				}
				
				throw new KeyNotFoundException();
			}
			set { AddOrReplace(key, value); }
		}

		/// <summary>
		/// Gets the equality comparer being used for the Key
		/// </summary>
		public IEqualityComparer<TKey> Comparer
		{
			get { return _Comparer; }
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

			internal Enumerator(ConcurrentDictionary<TKey, GCReference> dictionary)
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
