/****************************************\
 TypedPropertyElement.cs
 Created: 2014-02-11
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Represents a Configuration Element that can have properties with Typed Elements
	/// </summary>
	public abstract class TypedPropertyElement : ConfigurationElement
	{
		/// <summary>
		/// Creates a new Configuration Element that can have properties with Typed Elements
		/// </summary>
		public TypedPropertyElement() : base()
		{
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected sealed override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
		{	//****************************************
			string TypeName = reader.GetAttribute("Type");
			Type TargetType = Type.GetType(TypeName);
			object[] Attributes;
			TypedElement NewElement;
			//****************************************
			
			reader.MoveToElement();

			if (TargetType != null)
			{
				if (typeof(TypedElement).IsAssignableFrom(TargetType))
				{
					NewElement = (TypedElement)Activator.CreateInstance(TargetType);
					
					NewElement.Deserialise(reader, false);
					
					base[elementName] = NewElement;
					
					return true;
				}
				
				Attributes = TargetType.GetCustomAttributes(typeof(TypedElementAttribute), true);
				
				if (Attributes.Length != 0)
				{
					NewElement = (TypedElement)Activator.CreateInstance(((TypedElementAttribute)Attributes[0]).ConfigType);
					
					NewElement.Deserialise(reader, false);
					
					base[elementName] = NewElement;
					
					return true;
				}
			}
			
			return base.OnDeserializeUnrecognizedElement(elementName, reader);
		}
	}
}
#endif