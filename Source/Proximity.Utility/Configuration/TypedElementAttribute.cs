using System;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Describes the custom configuration element to use for a Type
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class TypedElementAttribute : Attribute
	{
		/// <summary>
		/// Describes the custom configuration element to use for a Type
		/// </summary>
		/// <param name="configType">Identifies the Type of the Configuration Element to use</param>
		public TypedElementAttribute(Type configType) : base()
		{
			ConfigType = configType;
		}

		//****************************************

		/// <summary>
		/// Gets the Type of the Configuration Element to use
		/// </summary>
		public Type ConfigType { get; }
	}
}