using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Security;
using Proximity.Utility.Configuration;
using Proximity.Utility.Logging.Outputs;
//****************************************

namespace Proximity.Utility.Logging.Config
{
	/// <summary>
	/// Describes the configuration for a set of Logging Outputs
	/// </summary>
	public sealed class OutputCollection : TypedElementCollection<OutputElement>
	{
		/// <summary>
		/// Creates a new Logging Output Collection
		/// </summary>
		public OutputCollection()
		{
		}
		
		//****************************************
		
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