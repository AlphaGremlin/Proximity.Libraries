/****************************************\
 TerminalVariable.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
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
		//****************************************
		
		public TerminalVariable(PropertyInfo property, TerminalBindingAttribute binding)
		{
			_Name = binding.Name ?? property.Name;
			_Property = property;
		}
		
		//****************************************
		
		public object GetValue(object instance)
		{
			return _Property.GetValue(instance);
		}
	}
}
