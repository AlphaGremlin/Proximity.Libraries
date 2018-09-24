using System;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using Proximity.Utility.Configuration;
using System.Security;
//****************************************

namespace Proximity.Utility.Logging.Config
{
	/// <summary>
	/// An configuration entry defining a Logging Output
	/// </summary>
	[SecurityCritical]
	public class OutputElement : TypedElement
	{
		/// <summary>
		/// Creates a new Output Configuration element
		/// </summary>
		public OutputElement()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Transforms this output configuration into the appropriate configured logging output
		/// </summary>
		/// <param name="target">The target to receive from</param>
		/// <returns>A configured log output</returns>
		public LogOutput ToOutput(LogTarget target)
		{	//****************************************
			var NewOutput = (LogOutput)Activator.CreateInstance(InstanceType, new object[] { target });
			//****************************************

			NewOutput.Configure(this);
			
			return NewOutput;
		}
	}
}