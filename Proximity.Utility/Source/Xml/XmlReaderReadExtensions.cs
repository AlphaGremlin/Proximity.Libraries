/****************************************\
 XmlReaderReadExtensions.cs
 Created: 2013-08-02
\****************************************/
using System;
using System.Globalization;
using System.Xml;
//****************************************

namespace Proximity.Utility.Xml
{
	/// <summary>
	/// XmlReader ReadAttribute extensions
	/// </summary>
	public static class XmlReaderReadExtensions
	{
		/// <summary>
		/// Reads the value of an attribute as a Boolean
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <returns>The value of the attribute as an Boolean, or null if the attribute doesn't exist</returns>
		public static bool? ReadAttributeAsBoolean(this XmlReader reader, string name)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return bool.Parse(AttributeValue);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a Boolean
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <returns>The value of the attribute as an Boolean, or null if the attribute doesn't exist</returns>
		public static bool? ReadAttributeAsBoolean(this XmlReader reader, string name, string namespaceUri)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return bool.Parse(AttributeValue);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <returns>The value of the attribute as a DateTime, or null if the attribute doesn't exist</returns>
		public static DateTime? ReadAttributeAsDateTime(this XmlReader reader, string name)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return DateTime.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <returns>The value of the attribute as a DateTime, or null if the attribute doesn't exist</returns>
		public static DateTime? ReadAttributeAsDateTime(this XmlReader reader, string name, string namespaceUri)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return DateTime.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <returns>The value of the attribute as a DateTime, or null if the attribute doesn't exist</returns>
		public static DateTime? ReadAttributeAsDateTime(this XmlReader reader, string name, DateTimeStyles styles)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return DateTime.Parse(AttributeValue, CultureInfo.InvariantCulture, styles);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <returns>The value of the attribute as a DateTime, or null if the attribute doesn't exist</returns>
		public static DateTime? ReadAttributeAsDateTime(this XmlReader reader, string name, string namespaceUri, DateTimeStyles styles)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return DateTime.Parse(AttributeValue, CultureInfo.InvariantCulture, styles);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a Decimal
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <returns>The value of the attribute as a Decimal, or null if the attribute doesn't exist</returns>
		public static decimal? ReadAttributeAsDecimal(this XmlReader reader, string name)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return decimal.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a Decimal
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <returns>The value of the attribute as a Decimal, or null if the attribute doesn't exist</returns>
		public static decimal? ReadAttributeAsDecimal(this XmlReader reader, string name, string namespaceUri)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return decimal.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as an Enumeration
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <typeparam name="TEnum">The type of Enumeration to parse as</typeparam>
		/// <returns>The value of the attribute as a Decimal, or null if the attribute doesn't exist</returns>
		public static TEnum? ReadAttributeAsEnum<TEnum>(this XmlReader reader, string name) where TEnum : struct
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return (TEnum)Enum.Parse(typeof(TEnum), AttributeValue);
		}
		
		/// <summary>
		/// Reads the value of an attribute as an Enumeration
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <typeparam name="TEnum">The type of Enumeration to parse as</typeparam>
		/// <returns>The value of the attribute as a Decimal, or null if the attribute doesn't exist</returns>
		public static TEnum? ReadAttributeAsEnum<TEnum>(this XmlReader reader, string name, string namespaceUri) where TEnum : struct
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return (TEnum)Enum.Parse(typeof(TEnum), AttributeValue);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a Guid
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <returns>The value of the attribute as a Guid, or null if the attribute doesn't exist</returns>
		public static Guid? ReadAttributeAsGuid(this XmlReader reader, string name)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return Guid.Parse(AttributeValue);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a Guid
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <returns>The value of the attribute as a Guid, or null if the attribute doesn't exist</returns>
		public static Guid? ReadAttributeAsGuid(this XmlReader reader, string name, string namespaceUri)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return Guid.Parse(AttributeValue);
		}
		
		/// <summary>
		/// Reads the value of an attribute as an Int32
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <returns>The value of the attribute as an Int32, or null if the attribute doesn't exist</returns>
		public static int? ReadAttributeAsInt32(this XmlReader reader, string name)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return int.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as an Int32
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <returns>The value of the attribute as an Int32, or null if the attribute doesn't exist</returns>
		public static int? ReadAttributeAsInt32(this XmlReader reader, string name, string namespaceUri)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return int.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as an Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <returns>The value of the attribute as an Int64, or null if the attribute doesn't exist</returns>
		public static long? ReadAttributeAsInt64(this XmlReader reader, string name)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return long.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as an Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <returns>The value of the attribute as an Int64, or null if the attribute doesn't exist</returns>
		public static long? ReadAttributeAsInt64(this XmlReader reader, string name, string namespaceUri)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return long.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a TimeSpan
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <returns>The value of the attribute as a TimeSpan, or null if the attribute doesn't exist</returns>
		public static TimeSpan? ReadAttributeAsTimeSpan(this XmlReader reader, string name)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return TimeSpan.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads the value of an attribute as a TimeSpan
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The local name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <returns>The value of the attribute as a TimeSpan, or null if the attribute doesn't exist</returns>
		public static TimeSpan? ReadAttributeAsTimeSpan(this XmlReader reader, string name, string namespaceUri)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
				return null;
			
			return TimeSpan.Parse(AttributeValue, CultureInfo.InvariantCulture);
		}
	}
}
