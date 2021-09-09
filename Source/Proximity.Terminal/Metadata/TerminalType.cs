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
		private readonly StringKeyDictionary<TerminalCommandSet> _Commands = new(StringComparison.OrdinalIgnoreCase);
		private readonly StringKeyDictionary<TerminalVariable> _Variables = new(StringComparison.OrdinalIgnoreCase);
		//****************************************
		
		internal TerminalType(TerminalRegistry registry, Type type, TerminalProviderAttribute provider)
		{
			Name = provider.TypeSet;
			IsDefault = provider.IsDefault;
			
			//****************************************
			
			foreach(var Method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				foreach(TerminalBindingAttribute Binding in Method.GetCustomAttributes(typeof(TerminalBindingAttribute), true))
				{
					if (Method.IsStatic)
					{
						registry.RegisterCommand(Method, Binding);
						
						continue;
					}
					
					var Name = Binding.Name ?? Method.Name;
					
					if (!_Commands.TryGetValue(Name, out var MyCommands))
						_Commands.Add(Name, MyCommands = new TerminalCommandSet(Name));
					
					MyCommands.AddOverload(Method, Binding);
				}
			}
			
			foreach(var Property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				foreach(TerminalBindingAttribute Binding in Property.GetCustomAttributes(typeof(TerminalBindingAttribute), true))
				{
					if (Property.GetMethod.IsStatic)
					{
						registry.RegisterVariable(Property, Binding);
						
						continue;
					}
					
					var Name = Binding.Name ?? Property.Name;
					
					if (!_Variables.ContainsKey(Name))
						_Variables.Add(Name, new TerminalVariable(Property, Binding));
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
		public string? Name { get; }

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
