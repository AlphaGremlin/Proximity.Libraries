/****************************************\
 TerminalInstance.cs
 Created: 2014-03-03
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Describes a named Instance
	/// </summary>
	public sealed class TerminalInstance
	{	//****************************************
		private readonly string _Name;
		private readonly TerminalType _Type;
		
		private readonly WeakReference _Instance;
		//****************************************
		
		internal TerminalInstance(string name, TerminalType type, object instance)
		{
			_Name = name;
			_Type = type;
			_Instance = new WeakReference(instance);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the name of this Instance
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
		
		/// <summary>
		/// Gets the type this Instance is of
		/// </summary>
		public TerminalType Type
		{
			get { return _Type;	}
		}
		
		/// <summary>
		/// Gets the Instance object
		/// </summary>
		public object Target
		{
			get { return _Instance.Target; }
		}
	}
}
