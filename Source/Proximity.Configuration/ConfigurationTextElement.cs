using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Xml;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Represents a configuration element within a configuration file that also contains a text node
	/// </summary>
	public class ConfigurationTextElement : ConfigurationElement
	{	//****************************************
		private static readonly ConfigurationProperty _ContentProperty = new ConfigurationProperty("__Content", typeof(string));
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

				var MyValue = MyProperty.Converter.ConvertFromString(reader.Value);

				SetPropertyValue(MyProperty, MyValue, true);
			}
			
			reader.MoveToElement();
			
			if (reader.IsEmptyElement)
			{
				SetPropertyValue(_ContentProperty, null, true);
			}
			else
			{
				reader.Read();

				SetPropertyValue(_ContentProperty, reader.ReadContentAsString(), true);
			}
		}
		
		/// <inheritdoc />
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			if (writer == null) // Null when it does a validation pass?
				return true;
			
			object? ContentValue = null;
			
			foreach(ConfigurationProperty MyProperty in Properties)
			{
				var MyResult = this[MyProperty];
				
				if (MyProperty.Name == _ContentProperty.Name)
					ContentValue = MyResult;
				else if (MyResult != null)
					writer.WriteAttributeString(MyProperty.Name, MyProperty.Converter.ConvertToInvariantString(MyResult));
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
			get => (string)base[_ContentProperty];
			set => SetPropertyValue(_ContentProperty, value, false);
		}
	}
	
	/// <summary>
	/// Represents a configuration element within a configuration file that also contains a strongly typed text node
	/// </summary>
	public class ConfigurationTextElement<T> : ConfigurationElement
	{	//****************************************
		private static readonly ConfigurationProperty _ContentProperty = new ConfigurationProperty("__Content", typeof(T));
		private static readonly TypeConverter _Converter = TypeDescriptor.GetConverter(typeof(T));
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

				var MyValue = MyProperty.Converter.ConvertFromString(reader.Value);

				SetPropertyValue(MyProperty, MyValue, true);
			}
			
			reader.MoveToElement();
			
			if (reader.IsEmptyElement)
			{
				SetPropertyValue(_ContentProperty, default(T)!, true);
			}
			else
			{
				reader.Read();

				var TempContent = reader.ReadContentAsString();
				
				SetPropertyValue(_ContentProperty, _Converter.ConvertFromInvariantString(TempContent), true);
			}
		}
		
		/// <inheritdoc />
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			if (writer == null) // Null when it does a validation pass?
				return true;
			
			T ContentValue = default!;
			
			foreach (ConfigurationProperty MyProperty in Properties)
			{
				var MyResult = this[MyProperty];
				
				if (MyProperty.Name == _ContentProperty.Name)
					ContentValue = (T)MyResult;
				else if (MyResult != null)
					writer.WriteAttributeString(MyProperty.Name, MyProperty.Converter.ConvertToInvariantString(MyResult));
			}

			if (!EqualityComparer<T>.Default.Equals(ContentValue, default!))
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
		public T Content
		{
			get => (T)base[_ContentProperty];
			set => SetPropertyValue(_ContentProperty, value, false);
		}
	}
}
