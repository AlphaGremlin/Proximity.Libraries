/****************************************\
 ConfigurationTextElement.cs
 Created: 2014-06-13
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private static ConfigurationProperty _ContentProperty = new ConfigurationProperty("__Content", typeof(string));
		//****************************************
		
		/// <summary>
		/// Creates a new configuration text element
		/// </summary>
		public ConfigurationTextElement() : base()
		{
			Properties.Add(_ContentProperty);
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
				SetPropertyValue(_ContentProperty, null, true);
			}
			else
			{
				SetPropertyValue(_ContentProperty, reader.ReadElementContentAsString(), true);
			}
		}
		
		/// <inheritdoc />
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			if (writer == null) // Null when it does a validation pass?
				return true;
			
			object ContentValue = null;
			
			foreach(ConfigurationProperty MyProperty in Properties)
			{
				var MyResult = this[MyProperty];
				
				if (MyProperty.Name == _ContentProperty.Name)
					ContentValue = MyResult;
				else if (MyResult != null)
					writer.WriteAttributeString(MyProperty.Name, MyResult.ToString());
			}
			
			if (ContentValue != null)
				writer.WriteString(ContentValue.ToString());
			
			return true;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the text content of this element
		/// </summary>
		public string Content
		{
			get { return (string)base[_ContentProperty]; }
			set { SetPropertyValue(_ContentProperty, value, false); }
		}
	}
	
	/// <summary>
	/// Represents a configuration element within a configuration file that also contains a strongly typed text node
	/// </summary>
	public class ConfigurationTextElement<TValue> : ConfigurationElement
	{	//****************************************
		private static ConfigurationProperty _ContentProperty = new ConfigurationProperty("__Content", typeof(TValue));
		private static TypeConverter _Converter = TypeDescriptor.GetConverter(typeof(TValue));
		//****************************************
		
		/// <summary>
		/// Creates a new configuration text element
		/// </summary>
		public ConfigurationTextElement() : base()
		{
			Properties.Add(_ContentProperty);
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
				SetPropertyValue(_ContentProperty, default(TValue), true);
			}
			else
			{
				var TempContent = reader.ReadElementContentAsString();
				
				SetPropertyValue(_ContentProperty, _Converter.ConvertFromInvariantString(TempContent), true);
			}
		}
		
		/// <inheritdoc />
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			if (writer == null) // Null when it does a validation pass?
				return true;
			
			TValue ContentValue = default(TValue);
			
			foreach (ConfigurationProperty MyProperty in Properties)
			{
				var MyResult = this[MyProperty];
				
				if (MyProperty.Name == _ContentProperty.Name)
					ContentValue = (TValue)MyResult;
				else if (MyResult != null)
					writer.WriteAttributeString(MyProperty.Name, MyResult.ToString());
			}

			if (!EqualityComparer<TValue>.Default.Equals(ContentValue, default(TValue)))
			{
				var TempContent = _Converter.ConvertToInvariantString(ContentValue);

				writer.WriteString(TempContent);
			}

			return true;
		}

		//****************************************
		
		/// <summary>
		/// Gets/Sets the content of this element
		/// </summary>
		public TValue Content
		{
			get { return (TValue)base[_ContentProperty]; }
			set { SetPropertyValue(_ContentProperty, value, false); }
		}
	}
}
#endif