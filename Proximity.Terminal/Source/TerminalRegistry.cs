/****************************************\
 TerminalRegistry.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Proximity.Utility;
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
		
		// Global Commands and Variables
		private readonly Dictionary<string, TerminalCommandSet> _Commands = new Dictionary<string, TerminalCommandSet>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, TerminalVariable> _Variables = new Dictionary<string, TerminalVariable>(StringComparer.InvariantCultureIgnoreCase);
		
		// Maps Types to Terminal Types
		private readonly ConcurrentDictionary<Type, TerminalType> _Types = new ConcurrentDictionary<Type, TerminalType>();
		
		// Maps Type Names to Terminal Type Sets
		private readonly ConcurrentDictionary<string, TerminalTypeSet> _TypeSets = new ConcurrentDictionary<string, TerminalTypeSet>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		internal TerminalRegistry()
		{
		}
		
		//****************************************
		
		public void ScanLoaded()
		{
			foreach(Assembly MyAssembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach(Type NewType in MyAssembly.GetTypes())
					{
						Scan(NewType);
					}
				}
				catch (ReflectionTypeLoadException)
				{
				}
			}
		}
		
		public void ScanOnLoad()
		{
			AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
		}
		
		public IEnumerable<TerminalType> Scan(Assembly assembly)
		{
			var MyMatches = new List<TerminalType>();
			
			foreach(var NewType in assembly.GetTypes())
			{
				var MyType = Scan(NewType);
				
				if (MyType != null)
					MyMatches.Add(MyType);
			}
			
			return MyMatches;
		}
		
		public TerminalType Scan(Type type)
		{	//****************************************
			var MyProvider = type.GetCustomAttribute<TerminalProviderAttribute>();
			TerminalType MyType;
			TerminalTypeSet MyTypeSet;
			//****************************************
			
			if (MyProvider == null)
				return null;
			
			MyType = new TerminalType(this, type, MyProvider);
			
			// Register this type
			_Types.TryAdd(type, MyType);
			
			// If defined, add a matching Type Set
			if (MyType.Name != null)
			{
				MyTypeSet = _TypeSets.AddOrUpdate(MyType.Name, (typeName) => new TerminalTypeSet(typeName), (typeName, typeSet) => typeSet);
			}
			
			return MyType;
		}
		
		public TerminalInstance Add(string name, object instance)
		{	//****************************************
			TerminalType MyType;
			TerminalTypeSet MyTypeSet;
			TerminalInstance MyInstance;
			//****************************************
			
			if (!_Types.TryGetValue(instance.GetType(), out MyType))
				throw new ArgumentException("Unknown Instance Type");
			
			if (!_TypeSets.TryGetValue(MyType.Name, out MyTypeSet))
				throw new InvalidOperationException("Missing type set");
			
			//****************************************
			
			MyInstance = new TerminalInstance(name, MyType, instance);
			
			if (MyType.IsDefault)
				MyTypeSet.Default = MyInstance;
			else
				MyTypeSet.AddNamedInstance(MyInstance);
			
			//****************************************
			
			return MyInstance;
		}
		
		public void Remove(string name, object instance)
		{	//****************************************
			TerminalType MyType;
			TerminalTypeSet MyTypeSet;
			//****************************************
			
			if (!_Types.TryGetValue(instance.GetType(), out MyType))
				throw new ArgumentException("Unknown Instance Type");
			
			if (!_TypeSets.TryGetValue(MyType.Name, out MyTypeSet))
				throw new InvalidOperationException("Missing type set");
			
			//****************************************
			
			MyTypeSet.Remove(name);
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
		
		public TerminalTypeSet FindTypeSet(string typeName)
		{	//****************************************
			TerminalTypeSet MyType;
			//****************************************
			
			lock (_LockObject)
			{
				if (_TypeSets.TryGetValue(typeName, out MyType))
					return MyType;
			}
			
			return null;
		}
		
		//****************************************
		
		internal TerminalCommand RegisterCommand(MethodInfo method, TerminalBindingAttribute binding)
		{	//****************************************
			var MyName = binding.Name ?? method.Name;
			TerminalCommandSet MyCommands;
			//****************************************
			
			if (!_Commands.TryGetValue(MyName, out MyCommands))
				_Commands.Add(MyName, MyCommands = new TerminalCommandSet(MyName));
			
			return MyCommands.AddOverload(method, binding);
		}
		
		internal TerminalVariable RegisterVariable(PropertyInfo property, TerminalBindingAttribute binding)
		{	//****************************************
			var MyName = binding.Name ?? property.Name;
			TerminalVariable MyVariable;
			//****************************************
			
			if (_Variables.ContainsKey(MyName))
			{
				Log.Warning("Ignoring duplicate property {0} in provider {1}", property.Name, property.DeclaringType.FullName);
				
				return null;
			}
			
			MyVariable = new TerminalVariable(property, binding);
				
			_Variables.Add(MyName, MyVariable);
			
			return MyVariable;
		}
		
		//****************************************
		
		private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			if (args.LoadedAssembly.ManifestModule.Assembly is AssemblyBuilder)
				return;
			
			try
			{
				foreach(Type NewType in args.LoadedAssembly.GetTypes())
				{
					Scan(NewType);
				}
			}
			catch (ReflectionTypeLoadException)
			{
			}
		}
		
		//****************************************
		
		public IEnumerable<string> Commands
		{
			get
			{
				lock (_LockObject)
					return _Commands.Keys.ToArray();
			}
		}
		
		public IEnumerable<string> Variables
		{
			get
			{
				lock (_LockObject)
					return _Variables.Keys.ToArray();
			}
		}
		
		public IEnumerable<string> TypeSets
		{
			get { return _TypeSets.Keys; }
		}
		
		/// <summary>
		/// A global registry of terminal provider instances
		/// </summary>
		public static readonly TerminalRegistry Global = new TerminalRegistry();
	}
}
