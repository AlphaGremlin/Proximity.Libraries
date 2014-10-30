/****************************************\
 ConfigurationTextElement.cs
 Created: 2014-06-13
\****************************************/
using System;
using System.Configuration;
using System.Xml;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a configuration element within a configuration file that also contains a text node
	/// </summary>
	public class ConfigurationTextElement : ConfigurationElement
	{	//****************************************
		private string _Content;
		//****************************************
		
		/// <summary>
		/// Creates a new configuration text element
		/// </summary>
		public ConfigurationTextElement() : base()
		{
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			while (reader.MoveToNextAttribute())
			{
				var MyProperty = Properties[reader.LocalName];
				
				SetPropertyValue(MyProperty, reader.Value, true);
			}
			
			reader.MoveToElement();
			
			if (reader.IsEmptyElement)
			{
				_Content = null;
			}
			else
			{
				_Content = reader.ReadElementContentAsString();
			}
		}
		
		/// <inheritdoc />
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			foreach(ConfigurationProperty MyProperty in Properties)
			{
				var MyResult = this[MyProperty];
				
				if (MyResult != null)
					writer.WriteAttributeString(MyProperty.Name, MyResult.ToString());
			}
			
			if (_Content != null)
				writer.WriteString(_Content);
			
			return true;
		}
		
		/// <inheritdoc />
		public override bool Equals(object compareTo)
		{
			return base.Equals(compareTo) && (compareTo is ConfigurationTextElement) && ((ConfigurationTextElement)compareTo)._Content == _Content;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the text content of this element
		/// </summary>
		public string Content
		{
			get { return _Content; }
			set { _Content = value; }
		}
	}
}
