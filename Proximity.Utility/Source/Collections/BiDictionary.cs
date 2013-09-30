/****************************************\
 BiDictionary.cs
 Created: 2011-09-05
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides a Bi-Directional Dictionary
	/// </summary>
	public class BiDictionary<TFirst, TSecond> : ICollection<KeyValuePair<TFirst, TSecond>>
	{	//****************************************
		private Dictionary<TFirst, TSecond> _First;
		private Dictionary<TSecond, TFirst> _Second;
		//****************************************
		
		/// <summary>
		/// Creates a new Bi-Directional Dictionary
		/// </summary>
		public BiDictionary()
		{
			_First = new Dictionary<TFirst, TSecond>();
			_Second = new Dictionary<TSecond, TFirst>();
		}
		
		/// <summary>
		/// Creates a new Bi-Directional Dictionary
		/// </summary>
		/// <param name="capacity">The initial capacity to reserve</param>
		public BiDictionary(int capacity)
		{
			_First = new Dictionary<TFirst, TSecond>(capacity);
			_Second = new Dictionary<TSecond, TFirst>(capacity);
		}
		
		/// <summary>
		/// Creates a new Bi-Directional Dictionary
		/// </summary>
		/// <param name="firstComparer">The Equality Comparer to use for the First values</param>
		/// <param name="secondComparer">The Equality Comparer to use for the Second values</param>
		public BiDictionary(IEqualityComparer<TFirst> firstComparer, IEqualityComparer<TSecond> secondComparer)
		{
			_First = new Dictionary<TFirst, TSecond>(firstComparer);
			_Second = new Dictionary<TSecond, TFirst>(secondComparer);
		}
		
		//****************************************
		
		/// <summary>
		/// Adds a new pair of items to the Dictionary
		/// </summary>
		/// <param name="first">The first item to add</param>
		/// <param name="second">The second item to add</param>
		public void Add(TFirst first, TSecond second)
		{
			if (_First.ContainsKey(first) || _Second.ContainsKey(second))
				throw new ArgumentException("Duplicate item");

			_First.Add(first, second);
			_Second.Add(second, first);
		}
		
		/// <summary>
		/// Adds a new pair of items to the Dictionary
		/// </summary>
		/// <param name="item">A KeyValue Pair representing the items to add</param>
		public void Add(KeyValuePair<TFirst, TSecond> item)
		{
			if (_First.ContainsKey(item.Key) || _Second.ContainsKey(item.Value))
				throw new ArgumentException("Duplicate item");

			((ICollection<KeyValuePair<TFirst, TSecond>>)_First).Add(item);
			_Second.Add(item.Value, item.Key);
		}
		
		/// <summary>
		/// Clears all items from the Dictionary
		/// </summary>
		public void Clear()
		{
			_First.Clear();
			_Second.Clear();
		}
		
		/// <summary>
		/// Determines whether the requested pair exists in the Dictionary
		/// </summary>
		/// <param name="item">The KeyValue Pair representing the items to check for</param>
		/// <returns>True if this pair exists, otherwise False</returns>
		public bool Contains(KeyValuePair<TFirst, TSecond> item)
		{
			return ((ICollection<KeyValuePair<TFirst, TSecond>>)_First).Contains(item);
		}
		
		/// <summary>
		/// Determines whether the First value exists in the Dictionary
		/// </summary>
		/// <param name="key">The First value to check for</param>
		/// <returns>True if the provided value is in the Firsts, otherwise False</returns>
		public bool ContainsFirst(TFirst key)
		{
			return _First.ContainsKey(key);
		}
		
		/// <summary>
		/// Determines whether the Second value exists in the Dictionary
		/// </summary>
		/// <param name="key">The Second value to check for</param>
		/// <returns>True if the provided value is in the Seconds, otherwise False</returns>
		public bool ContainsSecond(TSecond key)
		{
			return _Second.ContainsKey(key);
		}
		
		/// <summary>
		/// Copies the contents of the Dictionary to an Array
		/// </summary>
		/// <param name="array">The Array to copy to</param>
		/// <param name="arrayIndex">The destination index within the Array</param>
		public void CopyTo(KeyValuePair<TFirst, TSecond>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<TFirst, TSecond>>)_First).CopyTo(array, arrayIndex);
		}
		
		/// <summary>
		/// Retrieves the value associated with the First key
		/// </summary>
		/// <param name="key">The key associated with the desired value</param>
		/// <returns>The requested value</returns>
		/// <exception cref="KeyNotFoundException">Occurs when the desired Key does not exist</exception>
		public TSecond GetByFirst(TFirst key)
		{	//****************************************
			TSecond MyValue;
			//****************************************
			
			if (!_First.TryGetValue(key, out MyValue))
				throw new KeyNotFoundException();
			
			return MyValue;
		}
		
		/// <summary>
		/// Retrieves the value associated with the Second key
		/// </summary>
		/// <param name="key">The key associated with the desired value</param>
		/// <returns>The requested value</returns>
		/// <exception cref="KeyNotFoundException">Occurs when the desired Key does not exist</exception>
		public TFirst GetBySecond(TSecond key)
		{	//****************************************
			TFirst MyValue;
			//****************************************
			
			if (!_Second.TryGetValue(key, out MyValue))
				throw new KeyNotFoundException();
			
			return MyValue;
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the Dictionary
		/// </summary>
		/// <returns>A new Enumerator instance</returns>
		public IEnumerator<KeyValuePair<TFirst, TSecond>> GetEnumerator()
		{
			return ((ICollection<KeyValuePair<TFirst, TSecond>>)_First).GetEnumerator();
		}
		
		/// <summary>
		/// Removes a pair from the Dictionary
		/// </summary>
		/// <param name="item">The item to remove</param>
		/// <returns>True if the pair was removed, otherwise false</returns>
		public bool Remove(KeyValuePair<TFirst, TSecond> item)
		{	//****************************************
			TSecond MyValue;
			//****************************************
			
			if (!_First.TryGetValue(item.Key, out MyValue))
				return false;
			
			if (object.Equals(MyValue, item.Value))
				return false;
			
			_First.Remove(item.Key);
			
			return _Second.Remove(item.Value);
		}
		
		/// <summary>
		/// Removes a pair from the Dictionary
		/// </summary>
		/// <param name="key">The key of the First item to remove</param>
		/// <returns>True if the pair was removed, otherwise false</returns>
		public bool RemoveByFirst(TFirst key)
		{	//****************************************
			TSecond MyValue;
			//****************************************
			
			if (!_First.TryGetValue(key, out MyValue))
				return false;
			
			_First.Remove(key);
			_Second.Remove(MyValue);
			
			return true;
		}
		
		/// <summary>
		/// Removes a pair from the Dictionary
		/// </summary>
		/// <param name="key">The key of the Second item to remove</param>
		/// <returns>True if the pair was removed, otherwise false</returns>
		public bool RemoveBySecond(TSecond key)
		{	//****************************************
			TFirst MyValue;
			//****************************************
			
			if (!_Second.TryGetValue(key, out MyValue))
				return false;
				
			_First.Remove(MyValue);
			_Second.Remove(key);
			
			return true;
		}
		
		/// <summary>
		/// Retrieves the value associated with the First key
		/// </summary>
		/// <param name="first">The key associated with the desired value</param>
		/// <param name="second">Receives the value associated with the specified key, or if not found, the default value.</param>
		/// <returns>True if the pair was found, otherwise false</returns>
		public bool TryGetByFirst(TFirst first, out TSecond second)
		{
			return _First.TryGetValue(first, out second);
		}
		
		/// <summary>
		/// Retrieves the value associated with the Second key
		/// </summary>
		/// <param name="first">The key associated with the desired value</param>
		/// <param name="second">Receives the value associated with the specified key, or if not found, the default value.</param>
		/// <returns>True if the pair was found, otherwise false</returns>
		public bool TryGetBySecond(TSecond second, out TFirst first)
		{
			return _Second.TryGetValue(second, out first);
		}
		
		//****************************************
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _First.GetEnumerator();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of pairs in the Dictionary
		/// </summary>
		public int Count
		{
			get { return _First.Count; }
		}
		
		/// <summary>
		/// Gets a collection of all First values
		/// </summary>
		public ICollection<TFirst> Firsts
		{
			get { return _First.Keys; }
		}
		
		/// <summary>
		/// Gets a collection of all Second values
		/// </summary>
		public ICollection<TSecond> Seconds
		{
			get { return _Second.Keys; }
		}
		
		/// <summary>
		/// Gets whether this Dictionary is Read-Only
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}
	}
}
