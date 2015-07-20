/****************************************\
 XmlReaderTryReadExtensions.cs
 Created: 2013-08-02
\****************************************/
using System;
using System.Globalization;
using System.Xml;
//****************************************

namespace Proximity.Utility.Xml
{
	/// <summary>
	/// XmlReader TryReadAttribute extensions
	/// </summary>
	public static class XmlReaderTryReadExtensions
	{
		/// <summary>
		/// Tries to read the value of an Attribute into a Boolean
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Boolean if successful, otherwise false</param>
		/// <returns>True if the attribute exists and was a valid Boolean, otherwise false</returns>
		public static bool TryReadAttributeAsBoolean(this XmlReader reader, string name, out bool result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = false;
				
				return false;
			}
			
			return bool.TryParse(AttributeValue, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Boolean
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Boolean if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Boolean. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsBoolean(this XmlReader reader, string name, out bool? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			bool MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = bool.TryParse(AttributeValue, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Boolean
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Boolean if successful, otherwise false</param>
		/// <returns>True if the attribute exists and was a valid Boolean, otherwise false</returns>
		public static bool TryReadAttributeAsBoolean(this XmlReader reader, string name, string namespaceUri, out bool result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = false;
				
				return false;
			}
			
			return bool.TryParse(AttributeValue, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Boolean
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Boolean if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Boolean. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsBoolean(this XmlReader reader, string name, string namespaceUri, out bool? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			bool MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = bool.TryParse(AttributeValue, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		//****************************************
		
		/// <summary>
		/// Tries to read the value of an Attribute into a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if successful, otherwise DateTime.MinValue</param>
		/// <returns>True if the attribute exists and was a valid DateTime, otherwise false</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, out DateTime result)
		{
			return reader.TryReadAttributeAsDateTime(name, DateTimeStyles.None, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid DateTime. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, out DateTime? result)
		{
			return reader.TryReadAttributeAsDateTime(name, DateTimeStyles.None, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if successful, otherwise DateTime.MinValue</param>
		/// <returns>True if the attribute exists and was a valid DateTime, otherwise false</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, string namespaceUri, out DateTime result)
		{
			return reader.TryReadAttributeAsDateTime(name, namespaceUri, DateTimeStyles.None, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid DateTime. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, string namespaceUri, out DateTime? result)
		{
			return reader.TryReadAttributeAsDateTime(name, namespaceUri, DateTimeStyles.None, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if successful, otherwise DateTime.MinValue</param>
		/// <returns>True if the attribute exists and was a valid DateTime, otherwise false</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, DateTimeStyles styles, out DateTime result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = DateTime.MinValue;
				
				return false;
			}
			
			return DateTime.TryParse(AttributeValue, CultureInfo.InvariantCulture, styles, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid DateTime. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, DateTimeStyles styles, out DateTime? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			DateTime MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = DateTime.TryParse(AttributeValue, CultureInfo.InvariantCulture, styles, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if successful, otherwise DateTime.MinValue</param>
		/// <returns>True if the attribute exists and was a valid DateTime, otherwise false</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, string namespaceUri, DateTimeStyles styles, out DateTime result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = DateTime.MinValue;
				
				return false;
			}
			
			return DateTime.TryParse(AttributeValue, CultureInfo.InvariantCulture, styles, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable DateTime
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as a DateTime if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid DateTime. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsDateTime(this XmlReader reader, string name, string namespaceUri, DateTimeStyles styles, out DateTime? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			DateTime MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = DateTime.TryParse(AttributeValue, CultureInfo.InvariantCulture, styles, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		//****************************************
		
		/// <summary>
		/// Tries to read the value of an Attribute into a Decimal
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Decimal if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Decimal, otherwise false</returns>
		public static bool TryReadAttributeAsDecimal(this XmlReader reader, string name, out decimal result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = 0;
				
				return false;
			}
			
			return decimal.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Decimal
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Decimal if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Decimal. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsDecimal(this XmlReader reader, string name, out decimal? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			decimal MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = decimal.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a Decimal
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Decimal if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Decimal, otherwise false</returns>
		public static bool TryReadAttributeAsDecimal(this XmlReader reader, string name, string namespaceUri, out decimal result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = 0;
				
				return false;
			}
			
			return decimal.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Decimal
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Decimal if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Decimal. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsDecimal(this XmlReader reader, string name, string namespaceUri, out decimal? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			decimal MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = decimal.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		//****************************************
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Enumeration
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Enum if successful, otherwise zero</param>
		/// <typeparam name="TEnum">The type of Enumeration to parse as</typeparam>
		/// <returns>True if the attribute exists and was a valid Enum, otherwise false</returns>
		public static bool TryReadAttributeAsEnum<TEnum>(this XmlReader reader, string name, out TEnum result) where TEnum : struct
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = default(TEnum);
				
				return false;
			}
			
			return Enum.TryParse<TEnum>(AttributeValue, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Enumeration
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Enum if set and valid, otherwise null</param>
		/// <typeparam name="TEnum">The type of Enumeration to parse as</typeparam>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Enum. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsEnum<TEnum>(this XmlReader reader, string name, out TEnum? result) where TEnum : struct
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			TEnum MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = Enum.TryParse<TEnum>(AttributeValue, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a Enumeration
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Enum if successful, otherwise zero</param>
		/// <typeparam name="TEnum">The type of Enumeration to parse as</typeparam>
		/// <returns>True if the attribute exists and was a valid Enum, otherwise false</returns>
		public static bool TryReadAttributeAsEnum<TEnum>(this XmlReader reader, string name, string namespaceUri, out TEnum result) where TEnum : struct
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = default(TEnum);
				
				return false;
			}
			
			return Enum.TryParse<TEnum>(AttributeValue, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Enumeration
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Enum if set and valid, otherwise null</param>
		/// <typeparam name="TEnum">The type of Enumeration to parse as</typeparam>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Enum. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsEnum<TEnum>(this XmlReader reader, string name, string namespaceUri, out TEnum? result) where TEnum : struct
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			TEnum MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = Enum.TryParse<TEnum>(AttributeValue, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		//****************************************
		
		/// <summary>
		/// Tries to read the value of an Attribute into a Guid
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Guid if successful, otherwise Guid.Empty</param>
		/// <returns>True if the attribute exists and was a valid Guid, otherwise false</returns>
		public static bool TryReadAttributeAsGuid(this XmlReader reader, string name, out Guid result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = Guid.Empty;
				
				return false;
			}
			
			return Guid.TryParse(AttributeValue, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Guid
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Guid if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Guid. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsGuid(this XmlReader reader, string name, out Guid? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			Guid MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = Guid.TryParse(AttributeValue, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a Guid
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Guid if successful, otherwise Guid.Empty</param>
		/// <returns>True if the attribute exists and was a valid Guid, otherwise false</returns>
		public static bool TryReadAttributeAsGuid(this XmlReader reader, string name, string namespaceUri, out Guid result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = Guid.Empty;
				
				return false;
			}
			
			return Guid.TryParse(AttributeValue, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Guid
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a Guid if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Guid. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsGuid(this XmlReader reader, string name, string namespaceUri, out Guid? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			Guid MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = Guid.TryParse(AttributeValue, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		//****************************************
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Int32
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int32 if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Int32, otherwise false</returns>
		public static bool TryReadAttributeAsInt32(this XmlReader reader, string name, out int result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = 0;
				
				return false;
			}
			
			return int.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Int32
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int32 if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Int32. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsInt32(this XmlReader reader, string name, out int? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			int IntResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = int.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out IntResult);
			
			if (ParseResult)
				result = IntResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Int32
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int32 if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Int32, otherwise false</returns>
		public static bool TryReadAttributeAsInt32(this XmlReader reader, string name, string namespaceUri, out int result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = 0;
				
				return false;
			}
			
			return int.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Int32
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int32 if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Int32. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsInt32(this XmlReader reader, string name, string namespaceUri, out int? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			int IntResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = int.TryParse(AttributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out IntResult);
			
			if (ParseResult)
				result = IntResult;
				
			return ParseResult;
		}
		
		//****************************************
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Int64, otherwise false</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, out long result)
		{
			return reader.TryReadAttributeAsInt64(name, NumberStyles.Any, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Int64. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, out long? result)
		{
			return reader.TryReadAttributeAsInt64(name, NumberStyles.Any, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Int64, otherwise false</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, string namespaceUri, out long result)
		{
			return reader.TryReadAttributeAsInt64(name, namespaceUri, NumberStyles.Any, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Int64. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, string namespaceUri, out long? result)
		{
			return reader.TryReadAttributeAsInt64(name, namespaceUri, NumberStyles.Any, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Int64, otherwise false</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, NumberStyles styles, out long result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = 0;
				
				return false;
			}
			
			return long.TryParse(AttributeValue, styles, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Int64. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, NumberStyles styles, out long? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			long LongResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = long.TryParse(AttributeValue, styles, CultureInfo.InvariantCulture, out LongResult);
			
			if (ParseResult)
				result = LongResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into an Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if successful, otherwise zero</param>
		/// <returns>True if the attribute exists and was a valid Int64, otherwise false</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, string namespaceUri, NumberStyles styles, out long result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = 0;
				
				return false;
			}
			
			return long.TryParse(AttributeValue, styles, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable Int64
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="styles">Style elements to expect when parsing the value</param>
		/// <param name="result">Contains the value of the attribute as an Int64 if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid Int64. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsInt64(this XmlReader reader, string name, string namespaceUri, NumberStyles styles, out long? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			long LongResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = long.TryParse(AttributeValue, styles, CultureInfo.InvariantCulture, out LongResult);
			
			if (ParseResult)
				result = LongResult;
				
			return ParseResult;
		}
		
		//****************************************
		
		/// <summary>
		/// Tries to read the value of an Attribute into a TimeSpan
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a TimeSpan if successful, otherwise TimeSpan.Zero</param>
		/// <returns>True if the attribute exists and was a valid TimeSpan, otherwise false</returns>
		public static bool TryReadAttributeAsTimeSpan(this XmlReader reader, string name, out TimeSpan result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = TimeSpan.Zero;
				
				return false;
			}
			
			return TimeSpan.TryParse(AttributeValue, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable TimeSpan
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a TimeSpan if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid TimeSpan. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsTimeSpan(this XmlReader reader, string name, out TimeSpan? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name);
			TimeSpan MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = TimeSpan.TryParse(AttributeValue, CultureInfo.InvariantCulture, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a TimeSpan
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as a TimeSpan if successful, otherwise TimeSpan.Zero</param>
		/// <returns>True if the attribute exists and was a valid TimeSpan, otherwise false</returns>
		public static bool TryReadAttributeAsTimeSpan(this XmlReader reader, string name, string namespaceUri, out TimeSpan result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			//****************************************
			
			if (string.IsNullOrEmpty(AttributeValue))
			{
				result = TimeSpan.Zero;
				
				return false;
			}
			
			return TimeSpan.TryParse(AttributeValue, CultureInfo.InvariantCulture, out result);
		}
		
		/// <summary>
		/// Tries to read the value of an Attribute into a nullable TimeSpan
		/// </summary>
		/// <param name="reader">The XmlReader to read from</param>
		/// <param name="name">The qualified name of the attribute</param>
		/// <param name="namespaceUri">The namespace URI of the attribute</param>
		/// <param name="result">Contains the value of the attribute as an TimeSpan if set and valid, otherwise null</param>
		/// <returns>True if the Attribue is not set. True if the Attribute was set and is a valid TimeSpan. False if the Attribute was set and is invalid.</returns>
		public static bool TryReadAttributeAsTimeSpan(this XmlReader reader, string name, string namespaceUri, out TimeSpan? result)
		{	//****************************************
			var AttributeValue = reader.GetAttribute(name, namespaceUri);
			TimeSpan MyResult;
			//****************************************
			
			result = null;
			
			if (string.IsNullOrEmpty(AttributeValue))
				return true;
			
			bool ParseResult = TimeSpan.TryParse(AttributeValue, CultureInfo.InvariantCulture, out MyResult);
			
			if (ParseResult)
				result = MyResult;
				
			return ParseResult;
		}
	}
}
