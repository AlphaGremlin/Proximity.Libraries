/****************************************\
 TypedElementProperty.cs
 Created: 2014-02-11
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a Configuration Element that contains a single Typed Element
	/// </summary>
	/// <remarks>Allows using a Typed Element as a property rather than a collection. Adding properties to this class is not supported</remarks>
	public class TypedElementProperty<TValue> : ConfigurationElement where TValue : TypedElement
	{
		private static ConfigurationProperty _ContentProperty = new ConfigurationProperty("__Content", typeof(TValue));

		/// <summary>
		/// Creates a new Configuration Element that contains a single Typed Element
		/// </summary>
		public TypedElementProperty() : base()
		{
		}
		
		//****************************************

		/// <inheritdoc />
		protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{	//****************************************
			string TypeName = reader.GetAttribute("Type");
			Type TargetType;
			object[] Attributes;
			TValue NewElement;
			//****************************************

			// Will be null if we serialised a blank element
			if (string.IsNullOrEmpty(TypeName))
				return;

			TargetType = ResolveType(TypeName);

			if (TargetType != null)
			{
				if (typeof(TValue).IsAssignableFrom(TargetType))
				{
					NewElement = (TValue)Activator.CreateInstance(TargetType);

					NewElement.InstanceType = TargetType;

					NewElement.Deserialise(reader, false);

					SetPropertyValue(_ContentProperty, NewElement, true);

					return;
				}

				Attributes = TargetType.GetCustomAttributes(typeof(TypedElementAttribute), true);

				if (Attributes.Length != 0)
				{
					NewElement = (TValue)Activator.CreateInstance(((TypedElementAttribute)Attributes[0]).ConfigType);

					NewElement.InstanceType = TargetType;

					NewElement.Deserialise(reader, false);

					SetPropertyValue(_ContentProperty, NewElement, true);

					return;
				}
			}

			try
			{
				NewElement = Activator.CreateInstance<TValue>();

				NewElement.InstanceType = TargetType;

				NewElement.Deserialise(reader, false);

				SetPropertyValue(_ContentProperty, NewElement, true);

				return;
			}
			catch (MissingMethodException)
			{
			}

			throw new ConfigurationErrorsException("Failed to create an instance of the Typed Element");
		}

		/// <inheritdoc />
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			if (writer == null) // Null when it does a validation pass?
				return true;

			TValue ContentValue = (TValue)this[_ContentProperty];

			// The library user has set Value to null, rather than the property itself
			if (ContentValue == null)
				return true;

			ContentValue.Serialise(writer, false);

			return true;
		}

		/// <summary>
		/// Resolves a type name into a .Net Type
		/// </summary>
		/// <param name="typeName">The name of the type</param>
		/// <returns>A .Net Type, or null if the name could not be resolved</returns>
		protected virtual Type ResolveType(string typeName)
		{
			return Type.GetType(typeName);
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