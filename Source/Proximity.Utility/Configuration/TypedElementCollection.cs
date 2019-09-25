using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Xml;
using System.Xml.Linq;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a collection of Typed Configuration Elements
	/// </summary>
	public class TypedElementCollection<TValue> : ConfigurationElement, ICollection<TValue> where TValue : TypedElement, new()
	{	//****************************************
		private readonly List<TValue> _Items = new List<TValue>();
		//****************************************

		/// <inheritdoc />
		public override bool Equals(object compareTo) => base.Equals(compareTo) && (compareTo is TypedElementCollection<TValue> ElementCollection) && CompareItemsWith(ElementCollection);

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

		//****************************************

		/// <inheritdoc />
		protected sealed override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			base.DeserializeElement(reader, serializeCollectionKey);
		}

		/// <inheritdoc />
		protected sealed override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
		{
			var TypeName = reader.GetAttribute("Type");

			var RawElement = XElement.Load(reader.ReadSubtree(), LoadOptions.None);

			// It still returns whitespace, even if we tell it not to
			foreach (var Node in RawElement.DescendantNodes().OfType<XText>().Where(text => string.IsNullOrWhiteSpace(text.Value)).ToArray())
				Node.Remove();

			var NewElement = new TValue
			{
				Type = TypeName,
				RawElement = RawElement
			};

			_Items.Add(NewElement);

			return true;
		}

		/// <inheritdoc />
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

		private bool CompareItemsWith(TypedElementCollection<TValue> other) => new HashSet<TValue>(_Items).SetEquals(other._Items);

		//****************************************

		/// <summary>
		/// Gets the number of items in this list
		/// </summary>
		public int Count => _Items.Count;

		bool ICollection<TValue>.IsReadOnly => IsReadOnly();
	}
}