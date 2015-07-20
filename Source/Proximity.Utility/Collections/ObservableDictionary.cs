/****************************************\
 ObservableDictionary.cs
 Created: 2014-03-21
\****************************************/
#if !PORTABLE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Implements an Observable Dictionary for WPF binding
	/// </summary>
	/// <remarks>Based on http://blogs.microsoft.co.il/shimmy/2010/12/26/observabledictionarylttkey-tvaluegt-c/, modified to use SortedList and provide observable Keys and Values lists</remarks>
	public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
	{	//****************************************
		private const string CountString = "Count";
		private const string IndexerName = "Item[]";
		private const string KeysName = "Keys";
		private const string ValuesName = "Values";
		//****************************************
		private readonly SortedList<TKey, TValue> _Dictionary;
		private readonly ObservableDictionaryCollection<TKey> _Keys;
		private readonly ObservableDictionaryCollection<TValue> _Values;
		
		private int _UpdateCount = 0;
		//****************************************

		/// <summary>
		/// Creates a new, empty observable dictionary
		/// </summary>
		public ObservableDictionary()
		{
			_Dictionary = new SortedList<TKey, TValue>();
			_Keys = new ObservableDictionaryCollection<TKey>(_Dictionary.Keys);
			_Values = new ObservableDictionaryCollection<TValue>(_Dictionary.Values);
		}

		/// <summary>
		/// Creates a new pre-filled observable dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
		{
			_Dictionary = new SortedList<TKey, TValue>(dictionary);
			_Keys = new ObservableDictionaryCollection<TKey>(_Dictionary.Keys);
			_Values = new ObservableDictionaryCollection<TValue>(_Dictionary.Values);
		}

		/// <summary>
		/// Creates a new empty observable dictionary with the specified default capacity
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		public ObservableDictionary(int capacity)
		{
			_Dictionary = new SortedList<TKey, TValue>(capacity);
			_Keys = new ObservableDictionaryCollection<TKey>(_Dictionary.Keys);
			_Values = new ObservableDictionaryCollection<TValue>(_Dictionary.Values);
		}
		
		/// <summary>
		/// Creates a new empty observable dictionary with the specified comparer
		/// </summary>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableDictionary(IComparer<TKey> comparer)
		{
			_Dictionary = new SortedList<TKey, TValue>(comparer);
			_Keys = new ObservableDictionaryCollection<TKey>(_Dictionary.Keys);
			_Values = new ObservableDictionaryCollection<TValue>(_Dictionary.Values);
		}

		/// <summary>
		/// Creates a new pre-filled observable dictionary with the specified comparer
		/// </summary>
		/// <param name="dictionary">The dictionary to retrieve the contents from</param>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
		{
			_Dictionary = new SortedList<TKey, TValue>(dictionary, comparer);
			_Keys = new ObservableDictionaryCollection<TKey>(_Dictionary.Keys);
			_Values = new ObservableDictionaryCollection<TValue>(_Dictionary.Values);
		}

		/// <summary>
		/// Creates a new empty observable dictionary with the specified comparer
		/// </summary>
		/// <param name="capacity">The default capacity of the dictionary</param>
		/// <param name="comparer">The equality comparer to use</param>
		public ObservableDictionary(int capacity, IComparer<TKey> comparer)
		{
			_Dictionary = new SortedList<TKey, TValue>(capacity, comparer);
			_Keys = new ObservableDictionaryCollection<TKey>(_Dictionary.Keys);
			_Values = new ObservableDictionaryCollection<TValue>(_Dictionary.Values);
		}
		
		//****************************************

		/// <summary>
		/// Adds a new element the Dictionary
		/// </summary>
		/// <param name="key">The key of the item to add</param>
		/// <param name="value">The value of the item to add</param>
		public void Add(TKey key, TValue value)
		{
			Insert(key, value, true);
		}

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		/// <exception cref="ArgumentNullException">Item was null</exception>
		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Insert(item.Key, item.Value, true);
		}

		/// <summary>
		/// Adds a range of elements to the collection
		/// </summary>
		/// <param name="items">The elements to add</param>
		public void AddRange(IDictionary<TKey, TValue> items)
		{
			if (items == null) throw new ArgumentNullException("items");

			if (items.Count > 0)
			{
				if (items.Keys.Any((k) => _Dictionary.ContainsKey(k)))
					throw new ArgumentException("An item with the same key has already been added.");
				else
					foreach (var item in items) _Dictionary.Add(item.Key, item.Value);

				OnCollectionChanged(NotifyCollectionChangedAction.Add, items);
			}
		}

		/// <summary>
		/// Begins a major update operation, suspending change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void BeginUpdate()
		{
			_UpdateCount++;
		}

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			if (_Dictionary.Count > 0)
			{
				_Dictionary.Clear();
				OnCollectionChanged();
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return _Dictionary.Contains(item);
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
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			for (int Index = 0; Index < _Dictionary.Count; Index++)
			{
				array[arrayIndex++] = new KeyValuePair<TKey, TValue>(_Dictionary.Keys[Index], _Dictionary.Values[Index]);
			}

			//_Dictionary.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Ends a major update operation, resuming change notifications
		/// </summary>
		/// <remarks>Maintains a reference count. Each call to <see cref="BeginUpdate"/> must be matched with a call to <see cref="EndUpdate"/></remarks>
		public void EndUpdate()
		{
			if (_UpdateCount == 0)
				return;

			_UpdateCount--;

			// Raise changes (only if we're zero)
			OnCollectionChanged();
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="key">The key of the element to remove</param>
		public bool Remove(TKey key)
		{
			if (key == null) throw new ArgumentNullException("key");

			var Index = _Dictionary.IndexOfKey(key);

			if (Index == -1)
				return false;

			var Value = _Dictionary.Values[Index];

			_Dictionary.RemoveAt(Index);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, Value), Index);

			return true;
		}

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}

		/// <summary>
		/// Gets the value associated with the specified key
		/// </summary>
		/// <param name="key">The key whose value to get</param>
		/// <param name="value">When complete, contains the value associed with the given key, otherwise the default value for the type</param>
		/// <returns>True if the key was found, otherwise false</returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			return _Dictionary.TryGetValue(key, out value);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _Dictionary.GetEnumerator();
		}

		//****************************************

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_Dictionary).GetEnumerator();
		}

		/// <summary>
		/// Raises the PropertyChanged event
		/// </summary>
		/// <param name="propertyName">The name of the property that has changed</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (_UpdateCount == 0 && PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		//****************************************

		private void Insert(TKey key, TValue value, bool isAdd)
		{
			if (key == null) throw new ArgumentNullException("key");

			TValue item;
			if (_Dictionary.TryGetValue(key, out item))
			{
				if (isAdd) throw new ArgumentException("An item with the same key has already been added.");
				if (Equals(item, value)) return;
				_Dictionary[key] = value;

				OnCollectionChanged(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, item), _Dictionary.IndexOfKey(key));
			}
			else
			{
				_Dictionary[key] = value;

				OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value), _Dictionary.IndexOfKey(key));
			}
		}

		private void OnPropertyChanged()
		{
			OnPropertyChanged(CountString);
			OnPropertyChanged(IndexerName);
			OnPropertyChanged(KeysName);
			OnPropertyChanged(ValuesName);
		}

		private void OnCollectionChanged()
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			_Keys.OnCollectionChanged();
			_Values.OnCollectionChanged();
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem));

			_Keys.OnCollectionChanged(action, changedItem.Key);
			_Values.OnCollectionChanged(action, changedItem.Value);
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem, int index)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, changedItem, index));

			_Keys.OnCollectionChanged(action, changedItem.Key, index);
			_Values.OnCollectionChanged(action, changedItem.Value, index);
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem, int index)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));

			_Keys.OnCollectionChanged(action, newItem.Key, oldItem.Key, index);
			_Values.OnCollectionChanged(action, newItem.Value, oldItem.Value, index);
		}

		private void OnCollectionChanged(NotifyCollectionChangedAction action, IDictionary<TKey, TValue> newItems)
		{
			if (_UpdateCount != 0)
				return;

			OnPropertyChanged();

			if (CollectionChanged != null)
				CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItems.ToArray()));

			_Keys.OnCollectionChanged(action, newItems.Keys);
			_Values.OnCollectionChanged(action, newItems.Values);
		}

		//****************************************

		/// <summary>
		/// Raised when the collection changes
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Raised when a property of the dictionary changes
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count
		{
			get { return _Dictionary.Count; }
		}

		/// <summary>
		/// Gets whether this collection is read-only
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Gets a read-only collection of the dictionary keys
		/// </summary>
		public ObservableDictionaryCollection<TKey> Keys
		{
			get { return _Keys; }
		}

		/// <summary>
		/// Gets a read-only collection of the dictionary values
		/// </summary>
		public ObservableDictionaryCollection<TValue> Values
		{
			get { return _Values; }
		}

		/// <summary>
		/// Gets/Sets the value corresponding to the provided key
		/// </summary>
		[System.Runtime.CompilerServices.IndexerName("Item")]
		public TValue this[TKey key]
		{
			get { return _Dictionary[key]; }
			set { Insert(key, value, false); }
		}

		/// <summary>
		/// Gets the underlying dictionary object
		/// </summary>
		protected IDictionary<TKey, TValue> Dictionary
		{
			get { return _Dictionary; }
		}

		ICollection<TKey> IDictionary<TKey, TValue>.Keys
		{
			get { return _Keys; }
		}

		ICollection<TValue> IDictionary<TKey, TValue>.Values
		{
			get { return _Values; }
		}
	}
}
#endif