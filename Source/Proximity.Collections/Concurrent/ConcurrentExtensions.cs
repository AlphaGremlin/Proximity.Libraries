using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
//****************************************

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Extension methods for concurrent collections
	/// </summary>
	public static class ConcurrentExtensions
	{
		/// <summary>
		/// Adds a key/value pair to the dictionary if it does not already exist.
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item in the dictionary</param>
		/// <param name="newValue">The new value to assign, if the key is not set</param>
		/// <param name="wasAdded">Set to True if a new value was added, False if the value was retrieved</param>
		/// <returns>The value associated with key.</returns>
		/// <remarks>Implements a loop around TryGetValue and TryAdd</remarks>
		public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, TValue newValue, out bool wasAdded) where TKey : notnull
		{	//****************************************
			TValue? OutValue;
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
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item in the dictionary</param>
		/// <param name="valueFactory">A callback that generates the new value</param>
		/// <param name="wasAdded">Set to True if a new value was added, False if the value was retrieved</param>
		/// <returns>The value associated with key.</returns>
		/// <remarks>Implements a loop around TryGetValue and TryAdd</remarks>
		public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TValue> valueFactory, out bool wasAdded) where TKey : notnull
		{	//****************************************
			TValue? OutValue;
			//****************************************
			
			while (!target.TryGetValue(key, out OutValue))
			{
				var NewValue = valueFactory(key);
				
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
		/// Adds a key/value pair to the dictionary if it does not already exist.
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <typeparam name="TArg">The type of factory argument</typeparam>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item in the dictionary</param>
		/// <param name="valueFactory">A callback that generates the new value</param>
		/// <param name="factoryArgument">An argument to pass into <paramref name="valueFactory"/>.</param>
		/// <param name="wasAdded">Set to True if a new value was added, False if the value was retrieved</param>
		/// <returns>The value associated with key.</returns>
		/// <remarks>Implements a loop around TryGetValue and TryAdd</remarks>
		public static TValue GetOrAdd<TKey, TArg, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument, out bool wasAdded) where TKey : notnull
		{ //****************************************
			TValue? OutValue;
			//****************************************

			while (!target.TryGetValue(key, out OutValue))
			{
				var NewValue = valueFactory(key, factoryArgument);

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
		/// Adds or updates a key/value pair in the dictionary
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item to be added or updated</param>
		/// <param name="addValue">The value to add if the key is absent</param>
		/// <param name="updateValue">The function to generate a new value if the key exists</param>
		/// <param name="wasAdded">Receives whether the value was added or updated</param>
		/// <returns>The value stored in the dictionary</returns>
		/// <remarks>Implements a loop around TryGetValue, TryUpdate and TryAdd</remarks>
		public static TValue AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValue, out bool wasAdded) where TKey : notnull
		{	//****************************************
			TValue NewValue;
			//****************************************
			
			for (; ;)
			{
				if (target.TryGetValue(key, out var OutValue))
				{
					NewValue = updateValue(key, OutValue);
					
					if (target.TryUpdate(key, NewValue, OutValue))
					{
						wasAdded = false;
						
						return NewValue;
					}
				}
				else if (target.TryAdd(key, addValue))
				{
					wasAdded = true;
					
					return addValue;
				}
			}
		}

		/// <summary>
		/// Adds or updates a key/value pair in the dictionary
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item to be added or updated</param>
		/// <param name="addValue">The function to generate a new value if the key is absent</param>
		/// <param name="updateValue">The function to generate a new value if the key exists</param>
		/// <param name="wasAdded">Receives whether the value was added or updated</param>
		/// <returns>The value stored in the dictionary</returns>
		/// <remarks>Implements a loop around TryGetValue, TryUpdate and TryAdd</remarks>
		public static TValue AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TValue> addValue, Func<TKey, TValue, TValue> updateValue, out bool wasAdded) where TKey : notnull
		{	//****************************************
			TValue NewValue;
			//****************************************
			
			for (; ;)
			{
				if (target.TryGetValue(key, out var OutValue))
				{
					NewValue = updateValue(key, OutValue);
					
					if (target.TryUpdate(key, NewValue, OutValue))
					{
						wasAdded = false;
						
						return NewValue;
					}
				}
				else
				{
					NewValue = addValue(key);
					
					if (target.TryAdd(key, NewValue))
					{
						wasAdded = true;
						
						return NewValue;
					}
				}
			}
		}

		/// <summary>
		/// Adds or updates a key/value pair in the dictionary
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <typeparam name="TArg">The type of factory argument</typeparam>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item to be added or updated</param>
		/// <param name="addValue">The function to generate a new value if the key is absent</param>
		/// <param name="updateValue">The function to generate a new value if the key exists</param>
		/// <param name="factoryArgument">An argument to pass into <paramref name="addValue"/> and <paramref name="updateValue"/>.</param>
		/// <param name="wasAdded">Receives whether the value was added or updated</param>
		/// <returns>The value stored in the dictionary</returns>
		/// <remarks>Implements a loop around TryGetValue, TryUpdate and TryAdd</remarks>
		public static TValue AddOrUpdate<TKey, TValue, TArg>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TArg, TValue> addValue, Func<TKey, TValue, TArg, TValue> updateValue, TArg factoryArgument, out bool wasAdded) where TKey : notnull
		{ //****************************************
			TValue NewValue;
			//****************************************

			for (; ; )
			{
				if (target.TryGetValue(key, out var OutValue))
				{
					NewValue = updateValue(key, OutValue, factoryArgument);

					if (target.TryUpdate(key, NewValue, OutValue))
					{
						wasAdded = false;

						return NewValue;
					}
				}
				else
				{
					NewValue = addValue(key, factoryArgument);

					if (target.TryAdd(key, NewValue))
					{
						wasAdded = true;

						return NewValue;
					}
				}
			}
		}

		/// <summary>
		/// Updates an item if it exists in the dictionary
		/// </summary>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item to the update</param>
		/// <param name="update">A callback that performs the update</param>
		/// <param name="newValue">Receives the new value if the dictionary was updated</param>
		/// <returns>True if the item was updated, False if it does not exist</returns>
		/// <remarks>Implements a loop around TryGetValue and TryUpdate</remarks>
		public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TValue, TValue> update,
#if !NETSTANDARD2_0 && !NET40
			[MaybeNullWhen(false)]
#endif
			out TValue newValue) where TKey : notnull
		{
			while (target.TryGetValue(key, out var OldValue))
			{
				var NewValue = update(key, OldValue);
				
				if (target.TryUpdate(key, NewValue, OldValue))
				{
					newValue = NewValue;
					
					return true;
				}
			}
			
			newValue = default!;
			
			return false;
		}

		/// <summary>
		/// Updates an item if it exists in the dictionary
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <typeparam name="TArg">The type of factory argument</typeparam>
		/// <param name="target">The target concurrent dictionary</param>
		/// <param name="key">The key of the item to the update</param>
		/// <param name="updateValue">A callback that performs the update</param>
		/// <param name="factoryArgument">An argument to pass into <paramref name="updateValue"/>.</param>
		/// <param name="newValue">Receives the new value if the dictionary was updated</param>
		/// <returns>True if the item was updated, False if it does not exist</returns>
		/// <remarks>Implements a loop around TryGetValue and TryUpdate</remarks>
		public static bool TryUpdate<TKey, TValue, TArg>(this ConcurrentDictionary<TKey, TValue> target, TKey key, Func<TKey, TValue, TArg, TValue> updateValue, TArg factoryArgument,
#if !NETSTANDARD2_0 && !NET40
			[MaybeNullWhen(false)]
#endif
			out TValue newValue) where TKey : notnull
		{
			while (target.TryGetValue(key, out var OldValue))
			{
				var NewValue = updateValue(key, OldValue, factoryArgument);

				if (target.TryUpdate(key, NewValue, OldValue))
				{
					newValue = NewValue;

					return true;
				}
			}

			newValue = default!;

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
#if !NET40
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> target, TKey key, TValue expectedValue) where TKey : notnull
		{
			return ((IDictionary<TKey, TValue>)target).Remove(new KeyValuePair<TKey, TValue>(key, expectedValue));
		}

		/// <summary>
		/// Updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key exists, and optionally removes it
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <param name="dictionary">The <see cref="ConcurrentDictionary{TKey,TValue}"/> to update.</param>
		/// <param name="key">The key whose value should be updated or removed.</param>
		/// <param name="updateValueFactory">The function used to generate a new value for the existing key.</param>
		/// <param name="removeWhen">The function used to determine if the newly generated value meets the criteria for deletion</param>
		/// <param name="value">When this method returns, <paramref name="value"/> contains the updated value if updated, the value that matched <paramref name="removeWhen"/> if removed, or the default value of <typeparamref name="TValue"/> if the key was not found.</param>
		/// <returns>True if the key/value pair was updated or removed, False if it could not be found</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference (Nothing in Visual Basic).</exception>
		public static bool UpdateOrRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue, TValue> updateValueFactory, Func<TKey, TValue, bool> removeWhen,
#if !NETSTANDARD2_0 && !NET40
			[MaybeNullWhen(false)]
#endif
			out TValue value) where TKey : notnull
		{
			if (dictionary == null)
				throw new ArgumentNullException(nameof(dictionary));

			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (updateValueFactory == null)
				throw new ArgumentNullException(nameof(updateValueFactory));

			if (removeWhen == null)
				throw new ArgumentNullException(nameof(removeWhen));

			while (dictionary.TryGetValue(key, out var OldValue))
			{
				var NewValue = updateValueFactory(key, OldValue);

				if (removeWhen(key, NewValue))
				{
					if (((IDictionary<TKey, TValue>)dictionary).Remove(new KeyValuePair<TKey, TValue>(key, OldValue)))
					{
						value = NewValue;
						return true;
					}
				}
				else
				{
					if (dictionary.TryUpdate(key, NewValue, OldValue))
					{
						value = NewValue;
						return true;
					}
				}
			}

			value = default!;

			return false;
		}

		/// <summary>
		/// Updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key exists, and optionally removes it
		/// </summary>
		/// <typeparam name="TKey">The type of the dictionary key</typeparam>
		/// <typeparam name="TValue">The type of the dictionary value</typeparam>
		/// <typeparam name="TArg">The type of factory argument</typeparam>
		/// <param name="dictionary">The <see cref="ConcurrentDictionary{TKey,TValue}"/> to update.</param>
		/// <param name="key">The key whose value should be updated or removed.</param>
		/// <param name="updateValueFactory">The function used to generate a new value for the existing key.</param>
		/// <param name="removeWhen">The function used to determine if the newly generated value meets the criteria for deletion.</param>
		/// <param name="factoryArgument">An argument to pass into <paramref name="updateValueFactory"/>.</param>
		/// <param name="value">When this method returns, <paramref name="value"/> contains the updated value if updated, the value that matched <paramref name="removeWhen"/> if removed, or the default value of <typeparamref name="TValue"/> if the key was not found.</param>
		/// <returns>True if the key/value pair was updated or removed, False if it could not be found</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference (Nothing in Visual Basic).</exception>
		public static bool UpdateOrRemove<TKey, TValue, TArg>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue, TArg, TValue> updateValueFactory, Func<TKey, TValue, bool> removeWhen, TArg factoryArgument,
#if !NETSTANDARD2_0 && !NET40
			[MaybeNullWhen(false)]
#endif
			out TValue value) where TKey : notnull
		{
			if (dictionary == null)
				throw new ArgumentNullException(nameof(dictionary));

			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (updateValueFactory == null)
				throw new ArgumentNullException(nameof(updateValueFactory));

			if (removeWhen == null)
				throw new ArgumentNullException(nameof(removeWhen));

			while (dictionary.TryGetValue(key, out var OldValue))
			{
				var NewValue = updateValueFactory(key, OldValue, factoryArgument);

				if (removeWhen(key, NewValue))
				{
					if (((IDictionary<TKey, TValue>)dictionary).Remove(new KeyValuePair<TKey, TValue>(key, OldValue)))
					{
						value = NewValue;
						return true;
					}
				}
				else
				{
					if (dictionary.TryUpdate(key, NewValue, OldValue))
					{
						value = NewValue;
						return true;
					}
				}
			}

			value = default!;

			return false;
		}
	}
}
