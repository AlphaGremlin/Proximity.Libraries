/****************************************\
 TypedElement.cs
 Created: 2011-05-09
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Configuration;
using System.Reflection;
using System.Xml;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// The base class of an element within a <see cref="TypedElementCollection&lt;TValue&gt;" />
	/// </summary>
	public abstract class TypedElement : ConfigurationElement
	{	//****************************************
		private Type _InstanceType;
		//****************************************
		
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
			get
			{
				var Result = (string)base["Type"];
				
				// Will be empty if we've manually constructed this type (eg: testing)
				if (string.IsNullOrEmpty(Result))
				{
					var MyAttribute = (TypedElementAttribute)Attribute.GetCustomAttribute(GetType(), typeof(TypedElementAttribute), false);
					
					if (MyAttribute != null)
					{
						_InstanceType = MyAttribute.ConfigType;
						
						Result = _InstanceType.AssemblyQualifiedName;
						SetPropertyValue(Properties["Type"], Result, true);
					}
				}
				
				return Result;
			}
		}
		
		/// <summary>
		/// Gets the type resolved by the collection when using TypedElementAttribute
		/// </summary>
		public Type InstanceType
		{
			get
			{
				if (_InstanceType == null)
				{
					var MyAttribute = (TypedElementAttribute)Attribute.GetCustomAttribute(GetType(), typeof(TypedElementAttribute), false);
					
					if (MyAttribute != null)
					{
						_InstanceType = MyAttribute.ConfigType;
						
						SetPropertyValue(Properties["Type"], _InstanceType.AssemblyQualifiedName, true);
					}
				}
				
				return _InstanceType;
			}
			internal set { _InstanceType = value; }
		}
	}
}
#endif