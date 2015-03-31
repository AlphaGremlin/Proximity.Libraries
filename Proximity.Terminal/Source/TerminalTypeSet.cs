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
	/// Represents a set of types grouped by a type name
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
		
		/// <inheritdoc />
		public override string ToString()
		{
			return _TypeName;
		}
		
		/// <summary>
		/// Compares this type set to another for the purposes of sorting
		/// </summary>
		/// <param name="other">The type set to compare to</param>
		/// <returns>The result of the comparison</returns>
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
		
		/// <summary>
		/// Gets whether there is any Instance associated with this type set
		/// </summary>
		public bool HasInstance
		{
			get { return _Instances.Count > 0 || _DefaultInstance != null; }
		}
	}
}
