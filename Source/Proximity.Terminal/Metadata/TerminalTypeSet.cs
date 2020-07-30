using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
//****************************************

namespace Proximity.Terminal.Metadata
{
	/// <summary>
	/// Represents a set of types grouped by a type name
	/// </summary>
	public sealed class TerminalTypeSet : ICommandTarget, IComparable<TerminalTypeSet>
	{ //****************************************
		private readonly StringKeyDictionary<TerminalTypeInstance> _Instances = new StringKeyDictionary<TerminalTypeInstance>(StringComparison.OrdinalIgnoreCase);
		private TerminalTypeInstance? _Default;

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
		/// <param name="instance">Receives the Terminal Instance registered with this name, or null if it doesn't exist</param>
		/// <returns>True if the instance was found, otherwise False</returns>
		public bool TryGetNamedInstance(ReadOnlySpan<char> instanceName, out TerminalTypeInstance instance)
		{
			lock (_Instances)
			{
				return _Instances.TryGetValue(instanceName, out instance!);
			}
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

		internal void AddDefault(TerminalTypeInstance instance) => Interlocked.Exchange(ref _Default, instance);

		internal void AddNamedInstance(TerminalTypeInstance instance)
		{
			if (instance.Name == null)
				throw new InvalidOperationException("Name is not set");

			lock (_Instances)
			{
				Cleanup();

				// Add the Instance under this name, or replace if it's already in use
				_Instances[instance.Name] = instance;
			}
		}

		internal void RemoveNamedInstance(string instanceName)
		{
			lock (_Instances)
			{
				Cleanup();

				_Instances.Remove(instanceName);
			}
		}

		internal void RemoveDefault(object instance)
		{
			TerminalTypeInstance? OldInstance;

			do
			{
				OldInstance = Volatile.Read(ref _Default);

				if (OldInstance == null || OldInstance.Target != instance)
					return;
			}
			while (Interlocked.CompareExchange(ref _Default, null, OldInstance) != OldInstance);
		}

		//****************************************

		private void Cleanup()
		{ //****************************************
			var OldInstances = new List<string>();
			//****************************************

			foreach (var MyInstance in _Instances.Values)
			{
				if (MyInstance.Target == null)
					OldInstances.Add(MyInstance.Name!); // All instances in here must be named
			}

			foreach (var MyOldInstance in OldInstances)
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
		public TerminalTypeInstance? Default => _Default;

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
