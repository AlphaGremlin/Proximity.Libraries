﻿/****************************************\
 TraceOutputElement.cs
 Created: 2014-07-29
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Configuration;
using Proximity.Utility.Configuration;
using Proximity.Utility.Logging.Config;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Describes the configuration for the Trace Output Logger
	/// </summary>
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
#endif