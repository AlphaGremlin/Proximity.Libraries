/****************************************\
 TypedElementCollection.cs
 Created: 2011-05-09
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a collection of Typed Configuration Elements
	/// </summary>
	public class TypedElementCollection<TValue> : ConfigurationElement, ICollection<TValue> where TValue : TypedElement
	{	//****************************************
		private List<TValue> _Items = new List<TValue>();
		//****************************************
		
		/// <summary>
		/// Represents a collection of Typed Configuration Elements
		/// </summary>
		public TypedElementCollection() : base()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="serializeCollectionKey"></param>
		protected sealed override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			base.DeserializeElement(reader, serializeCollectionKey);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="elementName"></param>
		/// <param name="reader"></param>
		/// <returns></returns>
		protected sealed override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
		{	//****************************************
			string TypeName = reader.GetAttribute("Type");
			Type TargetType = Type.GetType(TypeName);
			object[] Attributes;
			TValue NewElement;
			//****************************************
			
			reader.MoveToElement();

			if (TargetType != null)
			{
				if (typeof(TValue).IsAssignableFrom(TargetType))
				{
					NewElement = (TValue)Activator.CreateInstance(TargetType);
					
					NewElement.Deserialise(reader, false);
					
					_Items.Add(NewElement);
					
					return true;
				}
				
				Attributes = TargetType.GetCustomAttributes(typeof(TypedElementAttribute), true);
				
				if (Attributes.Length != 0)
				{
					NewElement = (TValue)Activator.CreateInstance(((TypedElementAttribute)Attributes[0]).ConfigType);
					
					NewElement.Deserialise(reader, false);
					
					_Items.Add(NewElement);
					
					return true;
				}
			}
			
			try
			{
				NewElement = Activator.CreateInstance<TValue>();
				
				NewElement.Deserialise(reader, false);
				
				_Items.Add(NewElement);
				
				return true;
			}
			catch (MissingMethodException)
			{
			}
			
			return base.OnDeserializeUnrecognizedElement(elementName, reader);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="serializeCollectionKey"></param>
		/// <returns></returns>
		protected sealed override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{	//****************************************
			bool DataWritten = base.SerializeElement(writer, serializeCollectionKey);
			//****************************************
			
			foreach(var Item in _Items)
			{
				DataWritten |= Item.Serialise(writer, serializeCollectionKey);
			}
			
			return DataWritten;
		}
		
		//****************************************
		
		/// <summary>
		/// Adds an element to the collection
		/// </summary>
		/// <param name="item">The element to add</param>
		public void Add(TValue item)
		{
			_Items.Add(item);
		}
		
		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear()
		{
			_Items.Clear();
		}
		
		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TValue item)
		{
			return _Items.Contains(item);
		}
		
		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TValue[] array, int arrayIndex)
		{
			_Items.CopyTo(array, arrayIndex);
		}
		
		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		public bool Remove(TValue item)
		{
			return _Items.Remove(item);
		}
		
		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TValue> GetEnumerator()
		{
			return _Items.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return _Items.GetEnumerator();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of items in this list
		/// </summary>
		public int Count
		{
			get { return _Items.Count; }
		}
		
		bool ICollection<TValue>.IsReadOnly
		{
			get { return this.IsReadOnly(); }
		}
	}
}
