using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Security;
using Proximity.Logging.Outputs;
using Proximity.Utility.Configuration;
//****************************************

namespace Proximity.Logging.Config
{
	/// <summary>
	/// Describes the configuration for a set of Logging Outputs
	/// </summary>
	public sealed class OutputCollection : TypedElementCollection<OutputElement>
	{
	}
}