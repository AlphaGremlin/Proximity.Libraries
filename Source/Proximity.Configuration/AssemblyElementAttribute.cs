using System;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Describes the custom configuration element to use for an Assembly
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public class AssemblyElementAttribute : Attribute
	{
		/// <summary>
		/// Describes the custom configuration element to use for an Assembly
		/// </summary>
		/// <param name="configType">Identifies the Type of the Configuration Element to use</param>
		public AssemblyElementAttribute(Type configType) : base()
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
