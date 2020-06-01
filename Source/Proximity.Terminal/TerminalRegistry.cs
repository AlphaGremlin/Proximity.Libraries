using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Proximity.Terminal.Metadata;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Manages a registry of terminal commands, variables, and instances
	/// </summary>
	public sealed class TerminalRegistry
	{	//****************************************
		private bool _IsLoaded = false;
		
		private readonly object _LockObject = new object();
		
		// Global Commands and Variables
		private readonly StringKeyDictionary<TerminalCommandSet> _Commands = new StringKeyDictionary<TerminalCommandSet>(StringComparison.OrdinalIgnoreCase);
		private readonly StringKeyDictionary<TerminalVariable> _Variables = new StringKeyDictionary<TerminalVariable>(StringComparison.OrdinalIgnoreCase);
		
		// Maps Types to Terminal Types
		private readonly ConcurrentDictionary<Type, TerminalType> _Types = new ConcurrentDictionary<Type, TerminalType>();
		
		// Maps Type Names to Terminal Type Sets
		private readonly StringKeyDictionary<TerminalTypeSet> _TypeSets = new StringKeyDictionary<TerminalTypeSet>(StringComparison.OrdinalIgnoreCase);
		//****************************************
		
		internal TerminalRegistry()
		{
		}

		//****************************************

		/// <summary>
		/// Scans all assemblies loaded in this App Domain for Terminal Providers
		/// </summary>
		public void ScanLoaded()
		{
			foreach(Assembly MyAssembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach(var NewType in MyAssembly.GetTypes())
					{
						if (NewType != null)
							Scan(NewType);
					}
				}
				catch (ReflectionTypeLoadException e)
				{
					foreach (var NewType in e.Types)
					{
						if (NewType != null)
							Scan(NewType);
					}
				}
			}
		}

		/// <summary>
		/// Attaches this Registry to the <see cref="AppDomain.AssemblyLoad" /> event, scanning new assemblies for Terminal Providers
		/// </summary>
		public void ScanOnLoad()
		{
			AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
		}

		/// <summary>
		/// Scans an assembly for Terminal Providers
		/// </summary>
		/// <param name="assembly">The Assembly to scan</param>
		/// <returns>A list of Terminal Types within this Assembly</returns>
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

		/// <summary>
		/// Scans a type, checking if it's a Terminal Provider
		/// </summary>
		/// <param name="type">The type to scan</param>
		/// <returns>A Terminal Type definition, or null if this type is not a Terminal Provider</returns>
		public TerminalType Scan(Type type)
		{	//****************************************
			var MyProvider = type.GetCustomAttribute<TerminalProviderAttribute>();
			TerminalType MyType;
			TerminalTypeSet MyTypeSet;
			//****************************************
			
			if (MyProvider == null)
				return null;
			
			// We've scanned at least one type, so mark us as Loaded
			_IsLoaded = true;
			
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
		
		/// <summary>
		/// Registers a Terminal Instance with this Registry
		/// </summary>
		/// <param name="name">The unique name to assign this Instance</param>
		/// <param name="instance">The instance itself</param>
		/// <returns>A new Terminal Instance describing this Instance</returns>
		public TerminalTypeInstance Add(string name, object instance)
		{	//****************************************
			TerminalTypeInstance MyInstance;
			//****************************************
			
			if (!_IsLoaded)
				return null;
			
			if (!_Types.TryGetValue(instance.GetType(), out var MyType) && (MyType = Scan(instance.GetType())) == null)
				throw new ArgumentException("Unknown Instance Type");
			
			if (!_TypeSets.TryGetValue(MyType.Name, out var MyTypeSet))
				throw new InvalidOperationException("Missing type set");
			
			//****************************************
			
			MyInstance = new TerminalTypeInstance(name, MyType, instance);
			
			if (MyType.IsDefault)
				MyTypeSet.Default = MyInstance;
			else
				MyTypeSet.AddNamedInstance(MyInstance);
			
			//****************************************
			
			return MyInstance;
		}
		
		/// <summary>
		/// Unregisters a Terminal Instance previously registered via <see cref="Add" />
		/// </summary>
		/// <param name="name"></param>
		/// <param name="instance"></param>
		/// <remarks>Instances are held with weak references, so this method is not necessary to call. It does, however, improve performance</remarks>
		public void Remove(string name, object instance)
		{
			if (!_IsLoaded)
				return;
			
			if (!_Types.TryGetValue(instance.GetType(), out var MyType))
				throw new ArgumentException("Unknown Instance Type");
			
			if (!_TypeSets.TryGetValue(MyType.Name, out var MyTypeSet))
				throw new InvalidOperationException("Missing type set");
			
			//****************************************
			
			MyTypeSet.Remove(name);
		}
		
		//****************************************
		
		/// <summary>
		/// Looks up a global command set
		/// </summary>
		/// <param name="commandName">The global command set to find</param>
		/// <returns>The named command set, or null if it doesn't exist</returns>
		public bool TryGetCommandSet(ReadOnlySpan<char> commandName, out TerminalCommandSet commandSet)
		{
			lock (_LockObject)
			{
				return _Commands.TryGetValue(commandName, out commandSet);
			}
		}
		
		/// <summary>
		/// Looks up a global variable
		/// </summary>
		/// <param name="variableName">The global variable to find</param>
		/// <returns>The named variable, or null if it doesn't exist</returns>
		public bool TryGetVariable(ReadOnlySpan<char> variableName, out TerminalVariable variable)
		{
			lock (_LockObject)
			{
				return _Variables.TryGetValue(variableName, out variable);
			}
		}
		
		/// <summary>
		/// Looks up a type set
		/// </summary>
		/// <param name="typeName">The type set to find</param>
		/// <returns>The named type set, or null if it doesn't exist</returns>
		public bool TryGetTypeSet(ReadOnlySpan<char> typeName, out TerminalTypeSet typeSet)
		{
			lock (_LockObject)
			{
				return _TypeSets.TryGetValue(typeName, out typeSet);
			}
		}
		
		//****************************************
		
		internal TerminalCommand RegisterCommand(MethodInfo method, TerminalBindingAttribute binding)
		{	//****************************************
			var MyName = binding.Name ?? method.Name;
			//****************************************
			
			if (!_Commands.TryGetValue(MyName, out var MyCommands))
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
			if (args.LoadedAssembly.ManifestModule.Assembly.IsDynamic)
				return;
			
			try
			{
				foreach(Type NewType in args.LoadedAssembly.GetTypes())
				{
					Scan(NewType);
				}
			}
			catch (ReflectionTypeLoadException e)
			{
				foreach (Type NewType in e.Types)
				{
					if (NewType != null)
						Scan(NewType);
				}
			}
		}

		//****************************************

		/// <summary>
		/// Gets a collection of all global commands
		/// </summary>
		public IReadOnlyCollection<TerminalCommandSet> Commands
		{
			get
			{
				lock (_LockObject)
					return _Commands.Values.ToArray();
			}
		}

		/// <summary>
		/// Gets a collection of all global variables
		/// </summary>
		public IReadOnlyCollection<TerminalVariable> Variables
		{
			get
			{
				lock (_LockObject)
					return _Variables.Values.ToArray();
			}
		}

		/// <summary>
		/// Gets a collection of all type sets
		/// </summary>
		public ICollection<TerminalTypeSet> TypeSets => _TypeSets.Values;

		//****************************************

		/// <summary>
		/// A global registry of terminal provider instances
		/// </summary>
		public static readonly TerminalRegistry Global = new TerminalRegistry();
	}
}
