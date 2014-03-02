/****************************************\
 TerminalRegistry.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Manages a registry of terminal commands, variables, and instances
	/// </summary>
	public sealed class TerminalRegistry
	{	//****************************************
		private readonly object _LockObject = new object();
		
		private readonly Dictionary<string, TerminalCommandSet> _Commands = new Dictionary<string, TerminalCommandSet>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, TerminalVariable> _Variables = new Dictionary<string, TerminalVariable>(StringComparer.InvariantCultureIgnoreCase);
		
		private readonly ConcurrentDictionary<Type, TerminalType> _Types = new ConcurrentDictionary<Type, TerminalType>();
		private readonly ConcurrentDictionary<string, TerminalType> _DefaultTypes = new ConcurrentDictionary<string, TerminalType>(StringComparer.InvariantCultureIgnoreCase);
		private readonly ConcurrentDictionary<string, TerminalType> _InstanceTypes = new ConcurrentDictionary<string, TerminalType>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		internal TerminalRegistry()
		{
		}
		
		//****************************************
		
		public IEnumerable<TerminalType> Scan(Assembly assembly)
		{
			foreach(var NewType in assembly.GetTypes())
			{
				var MyType = Scan(NewType);
				
				if (MyType != null)
					yield return MyType;
			}
		}
		
		public TerminalType Scan(Type type)
		{	//****************************************
			var MyProvider = type.GetCustomAttribute<TerminalProviderAttribute>();
			TerminalType MyType;
			bool WasAdded;
			//****************************************
			
			if (MyProvider == null)
				return null;
			
			MyType = new TerminalType(type, MyProvider);
			
			
			
			MyType = _InstanceTypes.GetOrAdd(MyProvider.InstanceType, (typeName) => , out WasAdded);
			
			_Types.TryAdd(type, MyType);
			
			return MyType;
		}
		
		public void Add(object instance)
		{
			
		}
		
		public void Remove(object instance)
		{
			
		}
		
		//****************************************
		
		public TerminalCommandSet FindCommand(string commandName)
		{	//****************************************
			TerminalCommandSet MyCommand;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Commands.TryGetValue(commandName, out MyCommand))
					return MyCommand;
			}
			
			return null;
		}
		
		public TerminalVariable FindVariable(string variableName)
		{	//****************************************
			TerminalVariable MyVariable;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Variables.TryGetValue(variableName, out MyVariable))
					return MyVariable;
			}
			
			return null;
		}
		
		public TerminalType FindDefaultType(string typeName)
		{	//****************************************
			TerminalType MyType;
			//****************************************
			
			lock (_LockObject)
			{
				if (_InstanceTypes.TryGetValue(typeName, out MyType))
					return MyType;
			}
			
			return null;
		}
		
		public TerminalType FindInstanceType(string typeName)
		{	//****************************************
			TerminalType MyType;
			//****************************************
			
			lock (_LockObject)
			{
				if (_InstanceTypes.TryGetValue(typeName, out MyType))
					return MyType;
			}
			
			return null;
		}
		
		//****************************************
		
		//****************************************
		
		/// <summary>
		/// A global registry of terminal provider instances
		/// </summary>
		public static readonly TerminalRegistry Global = new TerminalRegistry();
	}
}
