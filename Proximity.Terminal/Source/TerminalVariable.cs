/****************************************\
 TerminalVariable.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Description of TerminalVariable.
	/// </summary>
	public sealed class TerminalVariable
	{	//****************************************
		private readonly string _Name;
		private readonly PropertyInfo _Property;
		private readonly string _Description;
		//****************************************
		
		internal TerminalVariable(PropertyInfo property, TerminalBindingAttribute binding)
		{
			_Name = binding.Name ?? property.Name;
			_Property = property;
			_Description = binding.Description;
		}
		
		//****************************************
		
		public object GetValue(object instance)
		{
			return _Property.GetValue(instance);
		}
		
		public bool SetValue(object instance, string argumentText)
		{	//****************************************
			var MyConverter = TypeDescriptor.GetConverter(_Property.PropertyType);
			//****************************************
			
			if (MyConverter == null)
				return false;
			
			try
			{
				var MyValue = MyConverter.ConvertFromString(argumentText);
				
				_Property.SetValue(instance, MyValue);
				
				return true;
			}
			catch(FormatException)
			{
				return false;
			}
			catch (InvalidCastException)
			{
				return false;
			}
			catch (NotSupportedException)
			{
				return false;
			}
		}
		
		//****************************************
		
		public string Name
		{
			get { return _Name; }
		}
		
		public Type Type
		{
			get { return _Property.PropertyType; }
		}
		
		public string Description
		{
			get { return _Description; }
		}
	}
}
