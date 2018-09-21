using System;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Describes the short text code associated with an enumeration value
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public sealed class OptionCodeAttribute : Attribute
	{
		/// <summary>
		/// Describes the short text code associated with an enumeration value
		/// </summary>
		public OptionCodeAttribute(string code)
		{
			Code = code;
		}

		//****************************************

		/// <summary>
		/// Gets the short text code associated with this enumeration value
		/// </summary>
		public string Code { get; }

		/// <summary>
		/// Gets/Sets whether this code is the default (used for enum -> code), if multiple codes map to one enumeration value
		/// </summary>
		public bool IsDefault { get; set; }
	}
}
