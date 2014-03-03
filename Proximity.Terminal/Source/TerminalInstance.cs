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
	/// Description of TerminalInstance.
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
		
		public string Name
		{
			get { return _Name; }
		}
		
		public TerminalType Type
		{
			get { return _Type;	}
		}
		
		public object Target
		{
			get { return _Instance.Target; }
		}
	}
}
