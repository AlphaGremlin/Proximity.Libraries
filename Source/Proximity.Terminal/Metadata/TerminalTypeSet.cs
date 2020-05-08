using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
//****************************************

namespace Proximity.Terminal.Metadata
{
	/// <summary>
	/// Represents a set of types grouped by a type name
	/// </summary>
	public sealed class TerminalTypeSet : IComparable<TerminalTypeSet>
	{ //****************************************
		private readonly Dictionary<string, TerminalTypeInstance> _Instances = new Dictionary<string, TerminalTypeInstance>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		internal TerminalTypeSet(string typeName)
		{
			TypeName = typeName;
		}
			
		//****************************************
		
		/// <summary>
		/// Retrieves a registered Instance by name
		/// </summary>
		/// <param name="instanceName">The name of the Instance</param>
		/// <returns>The Terminal Instance registered with this name, or null if it doesn't exist</returns>
		public TerminalTypeInstance GetNamedInstance(string instanceName)
		{
			lock (_Instances)
			{
				if (_Instances.TryGetValue(instanceName, out var MyInstance))
					return MyInstance;
			}
			
			return null;
		}

		/// <inheritdoc />
		public override string ToString() => TypeName;

		/// <summary>
		/// Compares this type set to another for the purposes of sorting
		/// </summary>
		/// <param name="other">The type set to compare to</param>
		/// <returns>The result of the comparison</returns>
		public int CompareTo(TerminalTypeSet other) => TypeName.CompareTo(other.TypeName);

		//****************************************

		internal void AddNamedInstance(TerminalTypeInstance instance)
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
		public string TypeName { get; }

		/// <summary>
		/// Gets the default instance for this type set
		/// </summary>
		public TerminalTypeInstance Default { get; set; }

		/// <summary>
		/// Gets a list of currently known instance names
		/// </summary>
		public IReadOnlyCollection<string> Instances
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
		public bool HasInstance => _Instances.Count > 0 || Default != null;
	}
}
