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
	public sealed class TerminalTypeSet : IComparable<TerminalTypeSet>
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
		
		public override string ToString()
		{
			return _TypeName;
		}
		
		public int CompareTo(TerminalTypeSet other)
		{
			return _TypeName.CompareTo(other._TypeName);
		}
		
		//****************************************
		
		internal void AddNamedInstance(TerminalInstance instance)
		{
			lock (_Instances)
			{
				Cleanup();
				
				_Instances.Add(instance.Name, instance);
			}
		}
		
		internal void Remove(string instanceName)
		{
			lock (_Instances)
			{
				Cleanup();
				
				_Instances.Remove(instanceName);
			}
		}
		
		//****************************************
		
		private void Cleanup()
		{	//****************************************
			var OldInstances = new List<string>();
			//****************************************
			
			foreach(var MyInstance in _Instances.Values)
			{
				if (MyInstance.Target == null)
					OldInstances.Add(MyInstance.Name);
			}
			
			foreach(var MyOldInstance in OldInstances)
			{
				_Instances.Remove(MyOldInstance);
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
				{
					Cleanup();
					
					return _Instances.Keys.ToArray();
				}
			}
		}
	}
}
