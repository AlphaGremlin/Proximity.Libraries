using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Security;
using Proximity.Configuration;
using Proximity.Logging.Outputs;
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
