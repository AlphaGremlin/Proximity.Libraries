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
	/// Describes a type providing terminal data
	/// </summary>
	public sealed class TerminalType
	{//****************************************
		private readonly Type _Type;
		private readonly string _Name;
		private readonly bool _IsDefault;
		
		private readonly Dictionary<string, TerminalCommandSet> _Commands = new Dictionary<string, TerminalCommandSet>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, TerminalVariable> _Variables = new Dictionary<string, TerminalVariable>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		internal TerminalType(TerminalRegistry registry, Type type, TerminalProviderAttribute provider)
		{
			_Type = type;
			
			_Name = provider.InstanceType;
			_IsDefault = provider.IsDefault;
			
			//****************************************
			
			foreach(var MyMethod in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				foreach(TerminalBindingAttribute MyBinding in MyMethod.GetCustomAttributes(typeof(TerminalBindingAttribute), true))
				{
					if (MyMethod.IsStatic)
					{
						registry.RegisterCommand(MyMethod, MyBinding);
						
						continue;
					}
					
					var MyName = MyBinding.Name ?? MyMethod.Name;
					TerminalCommandSet MyCommands;
					
					if (!_Commands.TryGetValue(MyName, out MyCommands))
						_Commands.Add(MyName, MyCommands = new TerminalCommandSet(MyName));
					
					MyCommands.AddOverload(MyMethod, MyBinding);
				}
			}
			
			foreach(var MyProperty in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				foreach(TerminalBindingAttribute MyBinding in MyProperty.GetCustomAttributes(typeof(TerminalBindingAttribute), true))
				{
					if (MyProperty.GetMethod.IsStatic)
					{
						registry.RegisterVariable(MyProperty, MyBinding);
						
						continue;
					}
					
					var MyName = MyBinding.Name ?? MyProperty.Name;
					
					if (_Variables.ContainsKey(MyName))
						Log.Warning("Ignoring duplicate property {0} in provider {1}", MyProperty.Name, type.FullName);
					else
						_Variables.Add(MyName, new TerminalVariable(MyProperty, MyBinding));
				}
			}
		}
		
		//****************************************
		
		public TerminalCommandSet FindCommand(string commandName)
		{	//****************************************
			TerminalCommandSet MyCommand;
			//****************************************
			
			if (_Commands.TryGetValue(commandName, out MyCommand))
				return MyCommand;
			
			return null;
		}
		
		public TerminalVariable FindVariable(string variableName)
		{	//****************************************
			TerminalVariable MyVariable;
			//****************************************
			
			if (_Variables.TryGetValue(variableName, out MyVariable))
				return MyVariable;
			
			return null;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the name to identify instances of this class registered with the Terminal
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
		
		/// <summary>
		/// Gets whether this Type is the default for this Name
		/// </summary>
		public bool IsDefault
		{
			get { return _IsDefault; }
		}
		
		/// <summary>
		/// Gets the collection of commands offered by this type
		/// </summary>
		public IEnumerable<TerminalCommandSet> Commands
		{
			get { return _Commands.Values; }
		}
		
		/// <summary>
		/// Gets the collection of variables offered by this type
		/// </summary>
		public IEnumerable<TerminalVariable> Variables
		{
			get { return _Variables.Values; }
		}
	}
}
