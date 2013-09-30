/****************************************\
 FieldOptions.cs
 Created: 2011-08-16
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Provides methods for retrieving option codes associated with enumerations
	/// </summary>
	public static class FieldOptions
	{	//****************************************
		private static Dictionary<Type, OptionSet> _OptionSets = new Dictionary<Type, OptionSet>();
		//****************************************
		
		/// <summary>
		/// Retrieves the string code associated with an enumeration value
		/// </summary>
		/// <param name="enumValue">The enumeration value to look for</param>
		/// <returns>The associated string code, or null if it does not have a code/is not a valid value</returns>
		public static string ToCode<TEnum>(TEnum enumValue)
		{	//****************************************
			OptionSet MyOptionSet = null;
			//****************************************
			
			lock (_OptionSets)
			{
				_OptionSets.TryGetValue(typeof(TEnum), out MyOptionSet);
			}
			
			if (MyOptionSet == null)
			{
				MyOptionSet = new OptionSet(typeof(TEnum));
				
				lock (_OptionSets)
				{
					_OptionSets[typeof(TEnum)] = MyOptionSet;
				}
			}
			
			//****************************************
			
			return MyOptionSet.GetCode((Enum)(object)enumValue);
		}
		
		/// <summary>
		/// Retrieves the enumeration value associated with a string code
		/// </summary>
		/// <param name="codeValue">The string code to look for</param>
		/// <returns>The associated enumeration value, or the default value (zero) if the code is not valid </returns>
		public static TEnum ToValue<TEnum>(string codeValue)
		{	//****************************************
			OptionSet MyOptionSet = null;
			object MyValue;
			//****************************************
			
			lock (_OptionSets)
			{
				_OptionSets.TryGetValue(typeof(TEnum), out MyOptionSet);
			}
			
			if (MyOptionSet == null)
			{
				MyOptionSet = new OptionSet(typeof(TEnum));
				
				lock (_OptionSets)
				{
					_OptionSets[typeof(TEnum)] = MyOptionSet;
				}
			}
			
			//****************************************
			
			if (codeValue == null)
				return default(TEnum);
			
			MyValue = MyOptionSet.GetValue(codeValue);
			
			if (MyValue == null)
				return default(TEnum);
			
			return (TEnum)MyValue;
		}
		
		//****************************************
		
		private class OptionSet
		{	//****************************************
			private Dictionary<Enum, string> _FieldToCode = new Dictionary<Enum, string>();
			private Dictionary<string, Enum> _CodeToField = new Dictionary<string, Enum>();
			//****************************************
			
			public OptionSet(Type optionType)
			{	//****************************************
				Enum MyValue;
				//****************************************
				
				if (!optionType.IsEnum)
					throw new ArgumentException("Not an enumeration");
				
				foreach(var MyField in optionType.GetFields(BindingFlags.Public | BindingFlags.Static))
				{
					MyValue = (Enum)MyField.GetValue(null);
					
					foreach(OptionCodeAttribute MyAttrib in MyField.GetCustomAttributes(typeof(OptionCodeAttribute), false))
					{
						_FieldToCode[MyValue] = MyAttrib.Code;
						_CodeToField[MyAttrib.Code] = MyValue;
					}
				}
			}
			
			//****************************************
			
			public string GetCode(Enum fieldValue)
			{	//****************************************
				string OptionCode;
				//****************************************
				
				if (_FieldToCode.TryGetValue(fieldValue, out OptionCode))
					return OptionCode;
				
				return null;
			}
			
			public Enum GetValue(string codeValue)
			{	//****************************************
				Enum FieldValue;
				//****************************************
				
				if (_CodeToField.TryGetValue(codeValue, out FieldValue))
					return FieldValue;
				
				return null;
			}
		}
	}
}
