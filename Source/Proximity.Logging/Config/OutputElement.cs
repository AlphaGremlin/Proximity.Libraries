using System;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;
using Proximity.Utility.Configuration;
using System.Security;
//****************************************

namespace Proximity.Logging.Config
{
	/// <summary>
	/// An configuration entry defining a Logging Output
	/// </summary>
	public class OutputElement : TypedElement
	{
		/// <summary>
		/// Transforms this output configuration into the appropriate configured logging output
		/// </summary>
		/// <returns>A configured log output</returns>
		public LogOutput ToOutput()
		{	//****************************************
			var NewOutput = (LogOutput)Activator.CreateInstance(InstanceType);
			//****************************************

			NewOutput.Configure(this);
			
			return NewOutput;
		}
	}
}