/****************************************\
 TerminalTypeSet.cs
 Created: 2014-03-03
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Description of TerminalTypeSet.
	/// </summary>
	public sealed class TerminalTypeSet
	{	//****************************************
		private readonly string _TypeName;
		
		private TerminalInstance _DefaultInstance;
		private readonly Dictionary<string, TerminalInstance> _Instances = new Dictionary<string, TerminalInstance>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		internal TerminalTypeSet(string typeName)
		{
			_TypeName = typeName;
		}
			
		//****************************************
		
		internal void AddNamedInstance(TerminalInstance instance)
		{
			lock (_Instances)
			{
				_Instances.Add(instance.Name, instance);
			}
		}
		
		public TerminalInstance GetNamedInstance(string instanceName)
		{	//****************************************
			TerminalInstance MyInstance;
			//****************************************
			
			lock (_Instances)
			{
				if (_Instances.TryGetValue(instanceName, out MyInstance))
					return MyInstance;
			}
			
			return null;
		}
		
		internal void Remove(string instanceName)
		{
			lock (_Instances)
			{
				_Instances.Remove(instanceName);
			}
		}
		
		//****************************************
		
		public string TypeName
		{
			get { return _TypeName; }
		}
		
		public TerminalInstance Default
		{
			get { return _DefaultInstance; }
			set
			{
				_DefaultInstance = value;
			}
		}
		
		public IEnumerable<string> Instances
		{
			get
			{
				lock (_Instances)
					return _Instances.Keys.ToArray();
			}
		}
	}
}
