using System;
using System.Configuration;
using System.Xml;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Represents an element within an <see cref="AssemblyElementCollection&lt;TValue&gt;" />
	/// </summary>
	public class AssemblyElement : ConfigurationElement
	{
		/// <summary>
		/// Creates a Assembly element
		/// </summary>
		public AssemblyElement() : base()
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
		[ConfigurationProperty("Assembly", IsRequired = true)]
		public string Assembly => (string)base["Assembly"];
	}
}
