using System;
using System.Configuration;
using System.Collections.Generic;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Generic collection for configuration element collections
	/// </summary>
	public abstract class ConfigurationElementCollection<T> : ConfigurationElementCollection, ICollection<T> where T : ConfigurationElement, new()
	{	//****************************************
		private readonly ConfigurationElementCollectionType _CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap;
		//****************************************
		
		/// <summary>
		/// Creates a new configuration element collection
		/// </summary>
		protected ConfigurationElementCollection() : base()
		{
		}
		
		/// <summary>
		/// Creates a new configuration element collection
		/// </summary>
		protected ConfigurationElementCollection(ConfigurationElementCollectionType collectionType) : base()
		{
			_CollectionType = collectionType;
		}

		//****************************************

		/// <summary>
		/// Creates a new configuration element
		/// </summary>
		/// <returns>A new configuration element</returns>
		protected override ConfigurationElement CreateNewElement() => new T();

		/// <summary>
		/// Creates the key for an element
		/// </summary>
		/// <param name="element">The element to create a key for</param>
		/// <returns>The requested key</returns>
		protected override object GetElementKey(ConfigurationElement element) => (T)element;

		//****************************************

		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		public void Add(T item) => BaseAdd(item);

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear() => BaseClear();

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(T item) => BaseIndexOf(item) != -1;

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		public bool Remove(T item)
		{
			BaseRemove(item);
			
			return BaseIsRemoved(item);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public new ConfigurationEnumerator<T> GetEnumerator() => new ConfigurationEnumerator<T>(this);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(T[] array, int arrayIndex) => base.CopyTo(array, arrayIndex);

		//****************************************

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new ConfigurationEnumerator<T>(this);

		bool ICollection<T>.IsReadOnly => IsReadOnly();

		/// <inheritdoc />
		public override ConfigurationElementCollectionType CollectionType => _CollectionType;
	}
}
