/****************************************\
 SchedulesCollection.cs
 Created: 12-09-2009
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Configuration;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Generic collection for configuration element collections
	/// </summary>
	public abstract class ConfigCollection<TValue> : ConfigurationElementCollection, ICollection<TValue> where TValue : ConfigurationElement, new()
	{	//****************************************
		private readonly ConfigurationElementCollectionType _CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap;
		//****************************************
		
		/// <summary>
		/// Creates a new configuration element collection
		/// </summary>
		protected ConfigCollection() : base()
		{
		}
		
		/// <summary>
		/// Creates a new configuration element collection
		/// </summary>
		protected ConfigCollection(ConfigurationElementCollectionType collectionType) : base()
		{
			_CollectionType = collectionType;
		}
		
		//****************************************

		/// <summary>
		/// Creates a new configuration element
		/// </summary>
		/// <returns>A new configuration element</returns>
		protected override ConfigurationElement CreateNewElement()
		{
			return new TValue();
		}
		
		/// <summary>
		/// Creates the key for an element
		/// </summary>
		/// <param name="element">The element to create a key for</param>
		/// <returns>The requested key</returns>
		protected override object GetElementKey(ConfigurationElement element)
		{
			return (TValue)element;
		}
		
		//****************************************
		
		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		public void Add(TValue item)
		{
			this.BaseAdd(item);
		}
		
		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			this.BaseClear();
		}
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TValue item)
		{
			return this.BaseIndexOf(item) != -1;
		}
		
		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		public bool Remove(TValue item)
		{
			this.BaseRemove(item);
			
			return this.BaseIsRemoved(item);
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public new IEnumerator<TValue> GetEnumerator()
		{
			return new ConfigEnumerator<TValue>(this);
		}
		
		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TValue[] array, int arrayIndex)
		{
			base.CopyTo(array, arrayIndex);
		}
		
		//****************************************
		
		bool ICollection<TValue>.IsReadOnly
		{
			get { return this.IsReadOnly(); }
		}
		
		/// <inheritdoc />
		public override ConfigurationElementCollectionType CollectionType
		{
			get { return _CollectionType; }
		}
	}
}
#endif