using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Describes a Terminal Variable
	/// </summary>
	public sealed class TerminalVariable : IComparable<TerminalVariable>
	{ //****************************************
		private readonly PropertyInfo _Property;
		//****************************************

		internal TerminalVariable(PropertyInfo property, TerminalBindingAttribute binding)
		{
			Name = binding.Name ?? property.Name;
			_Property = property;
			Description = binding.Description;
		}

		//****************************************

		/// <summary>
		/// Gets the value of a variable
		/// </summary>
		/// <param name="instance">The instance object in question</param>
		/// <returns>The variable value</returns>
		public object GetValue(object instance) => _Property.GetValue(instance);

		/// <summary>
		/// Attempts to set the value of a variable
		/// </summary>
		/// <param name="instance">The instance object in question</param>
		/// <param name="argumentText">The value entered on the terminal</param>
		/// <returns>True if the property was set correctly, otherwise false</returns>
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

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <summary>
		/// Compares this variable with another
		/// </summary>
		/// <param name="other">The other variable to compare agains</param>
		/// <returns>Positive if this name is greater, negative if it's lesser, zero if they're identical</returns>
		public int CompareTo(TerminalVariable other) => Name.CompareTo(other.Name);

		//****************************************

		/// <summary>
		/// Gets the name of this Variable
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type of the value this Variable contains
		/// </summary>
		public Type Type => _Property.PropertyType;

		/// <summary>
		/// Gets a description of the Variable
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets whether this property can be written to
		/// </summary>
		public bool CanWrite => _Property.CanWrite;
	}
}
