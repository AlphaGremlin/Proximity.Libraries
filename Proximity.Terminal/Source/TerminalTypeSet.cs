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
		
		/// <summary>
		/// Retrieves a registered Instance by name
		/// </summary>
		/// <param name="instanceName">The name of the Instance</param>
		/// <returns>The Terminal Instance registered with this name, or null if it doesn't exist</returns>
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
				
				// Add the Instance under this name, or replace if it's already in use
				_Instances[instance.Name] = instance;
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
		
		/// <summary>
		/// Gets the name of the type this set belongs to
		/// </summary>
		public string TypeName
		{
			get { return _TypeName; }
		}
		
		/// <summary>
		/// Gets the default instance for this type set
		/// </summary>
		public TerminalInstance Default
		{
			get { return _DefaultInstance; }
			set
			{
				_DefaultInstance = value;
			}
		}
		
		/// <summary>
		/// Gets a list of currently known instance names
		/// </summary>
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
