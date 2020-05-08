using System;
using System.Collections.Generic;
using System.Reflection;
//****************************************

namespace Proximity.Terminal.Metadata
{
	/// <summary>
	/// Describes a type providing terminal data
	/// </summary>
	public sealed class TerminalType
	{//****************************************
		private readonly Type _Type;
		private readonly Dictionary<string, TerminalCommandSet> _Commands = new Dictionary<string, TerminalCommandSet>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, TerminalVariable> _Variables = new Dictionary<string, TerminalVariable>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		internal TerminalType(TerminalRegistry registry, Type type, TerminalProviderAttribute provider)
		{
			_Type = type;
			
			Name = provider.InstanceType;
			IsDefault = provider.IsDefault;
			
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
					
					if (!_Commands.TryGetValue(MyName, out var MyCommands))
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
		
		/// <summary>
		/// Looks up a command by name
		/// </summary>
		/// <param name="commandName">The name to lookup</param>
		/// <returns>The command set matching this name</returns>
		public TerminalCommandSet FindCommand(string commandName)
		{
			if (_Commands.TryGetValue(commandName, out var MyCommand))
				return MyCommand;
			
			return null;
		}
		
		/// <summary>
		/// Looks up a variable by name
		/// </summary>
		/// <param name="variableName">The name to lookup</param>
		/// <returns>The variable matching this name</returns>
		public TerminalVariable FindVariable(string variableName)
		{
			if (_Variables.TryGetValue(variableName, out var MyVariable))
				return MyVariable;
			
			return null;
		}

		//****************************************

		/// <summary>
		/// Gets the name to identify instances of this class registered with the Terminal
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets whether this Type is the default for this Name
		/// </summary>
		public bool IsDefault { get; }

		/// <summary>
		/// Gets the collection of commands offered by this type
		/// </summary>
		public IReadOnlyCollection<TerminalCommandSet> Commands => _Commands.Values;

		/// <summary>
		/// Gets the collection of variables offered by this type
		/// </summary>
		public IReadOnlyCollection<TerminalVariable> Variables => _Variables.Values;
	}
}
