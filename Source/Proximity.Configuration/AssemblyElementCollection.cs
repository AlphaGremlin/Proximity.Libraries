using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Xml;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Represents a collection of Assembly Configuration Elements
	/// </summary>
	public class AssemblyElementCollection<TValue> : ConfigurationElement, ICollection<TValue> where TValue : AssemblyElement, new()
	{	//****************************************
		private readonly List<TValue> _Items = new List<TValue>();
		//****************************************
		
		/// <summary>
		/// Represents a collection of Assembly Configuration Elements
		/// </summary>
		public AssemblyElementCollection() : base()
		{
		}
		
		//****************************************
		
		/// <inheritdoc />
		public override bool Equals(object compareTo)
		{
			return base.Equals(compareTo) && (compareTo is AssemblyElementCollection<TValue> ElementCollection) && CompareItemsWith(ElementCollection);
		}
		
		/// <inheritdoc />
		public override int GetHashCode()
		{
			var hashCode = base.GetHashCode();

			unchecked
			{
				foreach (var MyItem in _Items)
				{
					if (MyItem != null)
						hashCode += MyItem.GetHashCode();
				}
			}

			return hashCode;
		}

		/// <inheritdoc />
		protected sealed override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			base.DeserializeElement(reader, serializeCollectionKey);
		}
		
		/// <inheritdoc />
		protected sealed override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
		{	//****************************************
			var AssemblyName = reader.GetAttribute("Assembly");
			Assembly TargetAssembly;
			TValue? NewElement = null;
			//****************************************
			
			TargetAssembly = Assembly.Load(AssemblyName);
			
			reader.MoveToElement();

			if (TargetAssembly != null)
			{
				foreach(AssemblyElementAttribute MyAttribute in TargetAssembly.GetCustomAttributes(typeof(AssemblyElementAttribute), true))
				{
					if (!typeof(TValue).IsAssignableFrom(MyAttribute.ConfigType))
						continue;
					
					NewElement = (TValue)Activator.CreateInstance(MyAttribute.ConfigType);
					
					break;
				}
				
				if (NewElement == null)
				{
					NewElement = new TValue();
				}
				
				NewElement.Deserialise(reader, false);
				
				_Items.Add(NewElement);
				
				return true;
			}
			
			return base.OnDeserializeUnrecognizedElement(elementName, reader);
		}
		
		/// <inheritdoc />
		protected sealed override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{	//****************************************
			var DataWritten = base.SerializeElement(writer, serializeCollectionKey);
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
		public void Add(TValue item) => _Items.Add(item);

		/// <summary>
		/// Removes all elements from the collection
		/// </summary>
		public void Clear() => _Items.Clear();

		/// <summary>
		/// Determines whether the collection contains a specific item
		/// </summary>
		/// <param name="item">The item to locate</param>
		/// <returns>True if the item is in the list, otherwise false</returns>
		public bool Contains(TValue item) => _Items.Contains(item);

		/// <summary>
		/// Copies the elements of the collection to a given array, starting at a specified index
		/// </summary>
		/// <param name="array">The destination array</param>
		/// <param name="arrayIndex">The index into the array to start writing</param>
		public void CopyTo(TValue[] array, int arrayIndex) => _Items.CopyTo(array, arrayIndex);

		/// <summary>
		/// Removes an element from the collection
		/// </summary>
		/// <param name="item">The element to remove</param>
		/// <returns>True if the item was removed, false if it was not in the collection</returns>
		public bool Remove(TValue item) => _Items.Remove(item);

		/// <summary>
		/// Returns an enumerator that iterates through the collection
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection</returns>
		public IEnumerator<TValue> GetEnumerator() => _Items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _Items.GetEnumerator();

		//****************************************

		private bool CompareItemsWith(AssemblyElementCollection<TValue> other) => _Items.Except(other._Items).Any();

		//****************************************

		/// <summary>
		/// Gets the number of items in this list
		/// </summary>
		public int Count => _Items.Count;

		bool ICollection<TValue>.IsReadOnly => IsReadOnly();
	}
}
