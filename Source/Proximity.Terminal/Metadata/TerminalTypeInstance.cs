using System;
using System.Collections.Generic;
using System.Reflection;
//****************************************

namespace Proximity.Terminal.Metadata
{
	/// <summary>
	/// Describes a named Instance
	/// </summary>
	public sealed class TerminalTypeInstance
	{ //****************************************
		private readonly WeakReference _Instance;
		//****************************************
		
		internal TerminalTypeInstance(string? name, TerminalType type, object instance)
		{
			Name = name;
			Type = type;
			_Instance = new WeakReference(instance);
		}

		//****************************************

		/// <summary>
		/// Gets the name of this Instance
		/// </summary>
		public string? Name { get; }

		/// <summary>
		/// Gets the type this Instance is of
		/// </summary>
		public TerminalType Type { get; }

		/// <summary>
		/// Gets the Instance object
		/// </summary>
		public object Target => _Instance.Target;
	}
}
