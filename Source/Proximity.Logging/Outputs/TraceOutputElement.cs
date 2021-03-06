using System;
using System.Configuration;
using System.Security;
using Proximity.Logging.Config;
using Proximity.Configuration;
//****************************************

namespace Proximity.Logging.Outputs
{
	/// <summary>
	/// Describes the configuration for the Trace Output Logger
	/// </summary>
	[SecurityCritical]
	[TypedElement(typeof(TraceOutput))]
	public class TraceOutputElement : OutputElement
	{
		/// <summary>
		/// Creates a new Trace Output configuration element
		/// </summary>
		public TraceOutputElement()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets whether to output to the Debug listeners rather than Trace
		/// </summary>
		[ConfigurationProperty("DebuggerOnly", IsRequired=false, DefaultValue=false)]
		public bool DebuggerOnly
		{
			get { return (bool)base["DebuggerOnly"]; }
			set { base["DebuggerOnly"] = value; }
		}
	}
}
