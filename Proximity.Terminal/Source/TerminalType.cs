/****************************************\
 TerminalType.cs
 Created: 2014-02-28
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
	/// Description of TerminalType.
	/// </summary>
	public sealed class TerminalType
	{//****************************************
		private readonly Type _Type;
		private readonly string _InstanceType;
		private readonly bool _IsDefault;
		
		private WeakReference _DefaultInstance;
		private readonly WeakDictionary<string, object> _Instances = new WeakDictionary<string, object>();
		
		private readonly Dictionary<string, TerminalCommandSet> _Commands = new Dictionary<string, TerminalCommandSet>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, TerminalVariable> _Variables = new Dictionary<string, TerminalVariable>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		internal TerminalType(Type type, TerminalProviderAttribute provider)
		{
			_Type = type;
			
			_InstanceType = provider.InstanceType;
			_IsDefault = provider.IsDefault;
			
			//****************************************
			
			foreach(var MyMethod in type.GetMethods())
			{
				foreach(TerminalBindingAttribute MyBinding in MyMethod.GetCustomAttributes(typeof(TerminalBindingAttribute), true))
				{
					var MyName = MyBinding.Name ?? MyMethod.Name;
					TerminalCommandSet MyCommands;
					
					if (!_Commands.TryGetValue(MyName, out MyCommands))
						_Commands.Add(MyName, MyCommands = new TerminalCommandSet(this, MyName));
					
					MyCommands.AddOverload(MyMethod, MyBinding);
				}
			}
			
			foreach(var MyProperty in type.GetProperties())
			{
				foreach(TerminalBindingAttribute MyBinding in MyProperty.GetCustomAttributes(typeof(TerminalBindingAttribute), true))
				{
					var MyName = MyBinding.Name ?? MyProperty.Name;
					
					if (_Variables.ContainsKey(MyName))
						Log.Warning("Ignoring duplicate property {0} in provider {1}", MyProperty.Name, type.FullName);
					else
						_Variables.Add(MyName, new TerminalVariable(MyProperty, MyBinding));
				}
			}
		}
		
		//****************************************
		
		public object GetNamedInstance(string instance)
		{
			object MyInstance;
			
			if (_Instances.TryGetValue(instance, out MyInstance))
				return MyInstance;
			
			return null;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the name to identify instances of this class registered with the Terminal
		/// </summary>
		public string InstanceType
		{
			get { return _InstanceType; }
		}
		
		public object DefaultInstance
		{
			get { return _DefaultInstance != null ? _DefaultInstance.Target : null; }
		}
	}
}
