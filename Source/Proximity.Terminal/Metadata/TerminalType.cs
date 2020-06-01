using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		private readonly StringKeyDictionary<TerminalCommandSet> _Commands = new StringKeyDictionary<TerminalCommandSet>(StringComparison.OrdinalIgnoreCase);
		private readonly StringKeyDictionary<TerminalVariable> _Variables = new StringKeyDictionary<TerminalVariable>(StringComparison.OrdinalIgnoreCase);
		//****************************************
		
		internal TerminalType(TerminalRegistry registry, Type type, TerminalProviderAttribute provider)
		{
			_Type = type;
			
			Name = provider.TypeName;
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
					
					if (!_Variables.ContainsKey(MyName))
						_Variables.Add(MyName, new TerminalVariable(MyProperty, MyBinding));
				}
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Looks up a command by name
		/// </summary>
		/// <param name="commandName">The name to lookup</param>
		/// <param name="commandSet">The command set matching this name</param>
		/// <returns>True if the command set was found, otherwise False</returns>
		public bool TryGetCommand(ReadOnlySpan<char> commandName,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out TerminalCommandSet commandSet)
		{
			return _Commands.TryGetValue(commandName, out commandSet!);
		}
		
		/// <summary>
		/// Looks up a variable by name
		/// </summary>
		/// <param name="variableName">The name to lookup</param>
		/// <param name="variable">Receives the variable matching this name</param>
		/// <returns>True if the variable was found, otherwise False</returns>
		public bool TryGetVariable(ReadOnlySpan<char> variableName,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out TerminalVariable variable)
		{
			return _Variables.TryGetValue(variableName, out variable!);
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
