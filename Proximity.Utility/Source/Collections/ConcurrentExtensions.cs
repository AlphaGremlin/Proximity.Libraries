/****************************************\
 ConcurrentExtensions.cs
 Created: 2013-11-07
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Extension methods for concurrent collections
	/// </summary>
	public static class ConcurrentExtensions
	{
		/// <summary>
		/// Adds a key/value pair to the dictionary if it does not already exist.
		/// </summary>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item in the dictionary</param>
		/// <param name="newValue">The new value to assign, if the key is not set</param>
		/// <param name="wasAdded">Set to True if a new value was added, False if the value was retrieved</param>
		/// <returns>The value associated with key.</returns>
		/// <remarks>Implements a loop around TryGetValue and TryAdd</remarks>
		public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, TValue newValue, out bool wasAdded)
		{	//****************************************
			TValue OutValue;
			//****************************************
			
			while (!target.TryGetValue(key, out OutValue))
			{
				if (target.TryAdd(key, newValue))
				{
					wasAdded = true;
					
					return newValue;
				}
			}
			
			wasAdded = false;
			
			return OutValue;
		}
		
		/// <summary>
		/// Adds a key/value pair to the dictionary if it does not already exist.
		/// </summary>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item in the dictionary</param>
		/// <param name="callback">A callback that generates the new value</param>
		/// <param name="wasAdded">Set to True if a new value was added, False if the value was retrieved</param>
		/// <returns>The value associated with key.</returns>
		/// <remarks>Implements a loop around TryGetValue and TryAdd</remarks>
		public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TValue> callback, out bool wasAdded)
		{	//****************************************
			TValue OutValue;
			//****************************************
			
			while (!target.TryGetValue(key, out OutValue))
			{
				var NewValue = callback(key);
				
				if (target.TryAdd(key, NewValue))
				{
					wasAdded = true;
					
					return NewValue;
				}
			}
			
			wasAdded = false;
			
			return OutValue;
		}
		
		/// <summary>
		/// Updates an item if it exists in the dictionary
		/// </summary>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item to the update</param>
		/// <param name="update">A callback that performs the update</param>
		/// <returns>True if the item was updated, False if it does not exist</returns>
		/// <remarks>Implements a loop around TryGetValue and TryUpdate</remarks>
		public static bool UpdateIfExists<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TValue, TValue> update)
		{	//****************************************
			TValue OldValue;
			//****************************************
			
			while (target.TryGetValue(key, out OldValue))
			{
				var NewValue = update(key, OldValue);
				
				if (target.TryUpdate(key, NewValue, OldValue))
					return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Removes a value from the dictionary if the key and value both match
		/// </summary>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item to remove</param>
		/// <param name="expectedValue">The expected item in the dictionary</param>
		/// <returns>True if the item was removed, False if key was not found, or the value was not as expected</returns>
		/// <remarks>Wraps calling IDictionary.Remove(KeyValuePair)</remarks>
		public static bool TryRemovePair<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, TValue expectedValue)
		{
			return ((IDictionary<TKey, TValue>)target).Remove(new KeyValuePair<TKey, TValue>(key, expectedValue));
		}
	}
}
