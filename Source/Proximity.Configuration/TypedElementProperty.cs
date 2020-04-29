using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Represents a Configuration Element that contains a single Typed Element
	/// </summary>
	/// <remarks>Allows using a Typed Element as a property rather than a collection. Adding properties to this class is not supported</remarks>
	public class TypedElementProperty<T> : ConfigurationElement where T : TypedElement, new()
	{
		private static readonly ConfigurationProperty ContentProperty = new ConfigurationProperty("__Content", typeof(T));

		/// <summary>
		/// Creates a new Configuration Element that contains a single Typed Element
		/// </summary>
		public TypedElementProperty() : base()
		{
		}
		
		//****************************************

		/// <inheritdoc />
		protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			var TypeName = reader.GetAttribute("Type");

			if (string.IsNullOrEmpty(TypeName))
				return;

			var RawElement = XElement.Load(reader.ReadSubtree(), LoadOptions.None);

			// It still returns whitespace, even if we tell it not to
			foreach (var Node in RawElement.DescendantNodes().OfType<XText>().Where(text => string.IsNullOrWhiteSpace(text.Value)).ToArray())
				Node.Remove();

			var NewElement = new T
			{
				Type = TypeName,
				RawElement = RawElement
			};

			SetPropertyValue(ContentProperty, NewElement, true);
		}

		/// <inheritdoc />
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			if (writer == null) // Null when it does a validation pass?
				return true;

			var ContentValue = (T)this[ContentProperty];

			// The library user has set Value to null, rather than the property itself
			if (ContentValue == null)
				return true;

			ContentValue.Serialise(writer, false);

			return true;
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the content of this element
		/// </summary>
		public T Content
		{
			get => (T)base[ContentProperty];
			set => SetPropertyValue(ContentProperty, value, false);
		}
	}
}
