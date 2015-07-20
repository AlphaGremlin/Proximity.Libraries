/****************************************\
 OptionCodeAttribute.cs
 Created: 2011-08-16
\****************************************/
using System;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Describes the short text code associated with an enumeration value
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public sealed class OptionCodeAttribute : Attribute
	{	//****************************************
		private string _Code;
		private bool _IsDefault;
		//****************************************

		/// <summary>
		/// Describes the short text code associated with an enumeration value
		/// </summary>
		public OptionCodeAttribute(string code)
		{
			_Code = code;
		}

		//****************************************

		/// <summary>
		/// Gets the short text code associated with this enumeration value
		/// </summary>
		public string Code
		{
			get { return _Code; }
		}

		/// <summary>
		/// Gets/Sets whether this code is the default (used for enum -> code), if multiple codes map to one enumeration value
		/// </summary>
		public bool IsDefault
		{
			get { return _IsDefault; }
			set { _IsDefault = value; }
		}
	}
}
