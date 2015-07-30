/****************************************\
 FieldOptions.cs
 Created: 2011-08-16
\****************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Proximity.Utility.Reflection;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Provides methods for retrieving option codes associated with enumerations
	/// </summary>
	public static class FieldOptions
	{
		/// <summary>
		/// Retrieves the string code associated with an enumeration value
		/// </summary>
		/// <param name="enumValue">The enumeration value to look for</param>
		/// <returns>The associated string code, or null if it does not have a code/is not a valid value</returns>
		public static string ToCode<TEnum>(TEnum enumValue)
		{	//****************************************
			var MyOptionSet = OptionContainer<TEnum>._Options;
			//****************************************
			
			if (MyOptionSet == null)
				Interlocked.CompareExchange<OptionSet>(ref OptionContainer<TEnum>._Options, MyOptionSet = new OptionSet(typeof(TEnum)), null);
			
			return MyOptionSet.GetCode((Enum)(object)enumValue);
		}
		
		/// <summary>
		/// Retrieves the enumeration value associated with a string code
		/// </summary>
		/// <param name="codeValue">The string code to look for</param>
		/// <returns>The associated enumeration value, or the default value (zero) if the code is not valid </returns>
		public static TEnum ToValue<TEnum>(string codeValue)
		{	//****************************************
			var MyOptionSet = OptionContainer<TEnum>._Options;
			object MyValue;
			//****************************************
			
			if (MyOptionSet == null)
				Interlocked.CompareExchange<OptionSet>(ref OptionContainer<TEnum>._Options, MyOptionSet = new OptionSet(typeof(TEnum)), null);
			
			if (codeValue == null)
				return default(TEnum);
			
			MyValue = MyOptionSet.GetValue(codeValue);
			
			if (MyValue == null)
				return default(TEnum);
			
			return (TEnum)MyValue;
		}
		
		//****************************************
		
		private static class OptionContainer<TEnum>
		{
			internal static OptionSet _Options;
		}
		
		private class OptionSet
		{	//****************************************
			private readonly Dictionary<Enum, string> _FieldToCode = new Dictionary<Enum, string>();
			private readonly Dictionary<string, Enum> _CodeToField = new Dictionary<string, Enum>();
			//****************************************
			
			public OptionSet(Type optionType)
			{	//****************************************
				Enum MyValue;
				//****************************************
				
#if PORTABLE
				if (!optionType.GetTypeInfo().IsEnum)
#else
				if (!optionType.IsEnum)
#endif
				throw new ArgumentException("Not an enumeration");
				
				foreach(var MyField in optionType.GetRuntimeFields())
				{
					if (!MyField.IsStatic)
						continue;

					MyValue = (Enum)MyField.GetValue(null);
					
					foreach(OptionCodeAttribute MyAttrib in MyField.GetCustomAttributes(typeof(OptionCodeAttribute), false))
					{
						if (!_FieldToCode.ContainsKey(MyValue) || MyAttrib.IsDefault)
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
