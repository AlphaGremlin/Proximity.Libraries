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
		/// <inheritdoc />
		protected override Type ResolveType(string typeName)
		{
			if (typeName.IndexOf(',') == -1)
			{
				// No Assembly Definition, just a Type
				if (typeName.IndexOf('.') == -1)
				{
					// No namespace either. Add the default namespace
					typeName = typeof(FileOutput).Namespace + System.Type.Delimiter + typeName;
				}
			
				return typeof(FileOutput).Assembly.GetType(typeName);
			}
			
			return Type.GetType(typeName);
		}
	}
}