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
	public sealed class TerminalVariable : IComparable<TerminalVariable>
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
			TypeConverter MyConverter;
			object MyValue;
			//****************************************
			
			if (!_Property.CanWrite)
				return false;
			
			MyConverter = TypeDescriptor.GetConverter(_Property.PropertyType);
			
			if (MyConverter == null)
				return false;
			
			try
			{
				MyValue = MyConverter.ConvertFromString(argumentText);
				
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
		
		public override string ToString()
		{
			return _Name;
		}
		
		public int CompareTo(TerminalVariable other)
		{
			return _Name.CompareTo(other._Name);
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
		
		public bool CanWrite
		{
			get { return _Property.CanWrite; }
		}
	}
}
