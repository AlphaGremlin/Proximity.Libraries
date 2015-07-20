/****************************************\
 AssemblyElementAttribute.cs
 Created: 2011-08-03
\****************************************/
#if !MOBILE && !PORTABLE
using System;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Describes the custom configuration element to use for an Assembly
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public class AssemblyElementAttribute : Attribute
	{	//****************************************
		private Type _ConfigType;
		//****************************************
		
		/// <summary>
		/// Describes the custom configuration element to use for an Assembly
		/// </summary>
		/// <param name="configType">Identifies the Type of the Configuration Element to use</param>
		public AssemblyElementAttribute(Type configType) : base()
		{
			_ConfigType = configType;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the Type of the Configuration Element to use
		/// </summary>
		public Type ConfigType
		{
			get { return _ConfigType; }
		}
	}
}
#endif