/****************************************\
 TypedElement.cs
 Created: 2011-05-09
\****************************************/
using System;
using System.Configuration;
using System.Xml;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// The base class of an element within a <see cref="TypedElementCollection&lt;TValue&gt;" />
	/// </summary>
	public abstract class TypedElement : ConfigurationElement
	{
		/// <summary>
		/// Creates a typed element
		/// </summary>
		protected TypedElement() : base()
		{
		}
		
		//****************************************
		
		internal bool Serialise(XmlWriter writer, bool serializeCollectionKey)
		{
			return SerializeElement(writer, serializeCollectionKey);
		}
		
		internal void Deserialise(XmlReader reader, bool serializeCollectionKey)
		{
			DeserializeElement(reader, serializeCollectionKey);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the base Type of this Element
		/// </summary>
		[ConfigurationProperty("Type", IsRequired=true)]
		public string Type
		{
			get { return (string)base["Type"]; }
		}
	}
}
