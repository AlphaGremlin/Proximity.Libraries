using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		private readonly object _LockObject = new object();
		
		// Global Commands and Variables
		private readonly StringKeyDictionary<TerminalCommandSet> _Commands = new StringKeyDictionary<TerminalCommandSet>(StringComparison.OrdinalIgnoreCase);
		private readonly StringKeyDictionary<TerminalVariable> _Variables = new StringKeyDictionary<TerminalVariable>(StringComparison.OrdinalIgnoreCase);
		
		// Maps Types to Terminal Types
		private readonly ConcurrentDictionary<Type, TerminalType> _Types = new ConcurrentDictionary<Type, TerminalType>();
		
		// Maps Type Names to Terminal Type Sets
		private readonly StringKeyDictionary<TerminalTypeSet> _TypeSets = new StringKeyDictionary<TerminalTypeSet>(StringComparison.OrdinalIgnoreCase);

		private readonly ConcurrentDictionary<TerminalType, TerminalTypeInstance> _DefaultInstances = new ConcurrentDictionary<TerminalType, TerminalTypeInstance>();
		//****************************************

		/// <summary>
		/// Defines a new Registry
		/// </summary>
		public TerminalRegistry()
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
		public TerminalType? Scan(Type type)
		{	//****************************************
			var MyProvider = type.GetCustomAttribute<TerminalProviderAttribute>();
			TerminalType MyType;
			//****************************************
			
			if (MyProvider == null)
				return null;
			
			// We've scanned at least one type, so mark us as Loaded
			MyType = new TerminalType(this, type, MyProvider);
			
			// Register this type
			_Types.TryAdd(type, MyType);
			
			// If defined, add a matching Type Set
			if (MyType.Name != null)
			{
				lock (_LockObject)
				{
					if (!_TypeSets.ContainsKey(MyType.Name))
						_TypeSets[MyType.Name] = new TerminalTypeSet(MyType.Name);
				}
			}
			
			return MyType;
		}
		
		/// <summary>
		/// Registers a Terminal Instance with this Registry
		/// </summary>
		/// <param name="name">The unique name to assign this Instance</param>
		/// <param name="instance">The instance itself</param>
		/// <returns>A new Terminal Instance describing this Instance</returns>
		public TerminalTypeInstance? Add(string name, object instance)
		{
			if (!_Types.TryGetValue(instance.GetType(), out TerminalType? TargetType))
			{
				TargetType = Scan(instance.GetType());

				if (TargetType == null)
					throw new ArgumentException("Unknown Instance Type");
			}

			if (TargetType.IsDefault)
				throw new InvalidOperationException("Default type cannot be named");

			if (TargetType.Name == null)
				throw new InvalidOperationException("Missing Type Set Name");

			if (!_TypeSets.TryGetValue(TargetType.Name, out var TypeSet))
				throw new InvalidOperationException("Missing Type Set");

			//****************************************

			var NewInstance = new TerminalTypeInstance(name, TargetType, instance);

			TypeSet.AddNamedInstance(NewInstance);

			return NewInstance;
		}

		/// <summary>
		/// Registers a default Terminal Instance with this Registry
		/// </summary>
		/// <param name="instance">The instance itself</param>
		/// <returns>A new Terminal Instance describing this Instance</returns>
		public TerminalTypeInstance? Add(object instance)
		{
			if (!_Types.TryGetValue(instance.GetType(), out TerminalType? TargetType))
			{
				TargetType = Scan(instance.GetType());

				if (TargetType == null)
					throw new ArgumentException("Unknown Instance Type");
			}

			if (!TargetType.IsDefault)
				throw new InvalidOperationException("Type is not a default");

			//****************************************

			var NewInstance = new TerminalTypeInstance(null, TargetType, instance);

			if (string.IsNullOrEmpty(TargetType.Name))
			{
				_DefaultInstances[TargetType] = NewInstance;
			}
			else
			{
				if (!_TypeSets.TryGetValue(TargetType.Name!, out var TypeSet))
					throw new InvalidOperationException("Missing Type Set");

				TypeSet.AddDefault(NewInstance);
			}

			//****************************************

			return NewInstance;
		}

		/// <summary>
		/// Unregisters a Terminal Instance previously registered via <see cref="Add(string, object)" />
		/// </summary>
		/// <param name="name"></param>
		/// <param name="instance"></param>
		/// <remarks>Instances are held with weak references, so this method is not necessary to call. It does, however, improve performance</remarks>
		public void Remove(string name, object instance)
		{
			if (!_Types.TryGetValue(instance.GetType(), out var TargetType))
				throw new ArgumentException("Unknown Instance Type");

			if (TargetType.IsDefault)
				throw new InvalidOperationException("Default type cannot be named");

			if (TargetType.Name == null)
				throw new InvalidOperationException("Missing Type Set Name");

			if (!_TypeSets.TryGetValue(TargetType.Name, out var TypeSet))
				throw new InvalidOperationException("Missing Type Set");

			//****************************************

			TypeSet.RemoveNamedInstance(name);
		}

		/// <summary>
		/// Unregisters a Terminal Instance previously registered via <see cref="Add(object)" />
		/// </summary>
		/// <param name="instance"></param>
		/// <remarks>Instances are held with weak references, so this method is not necessary to call. It does, however, improve performance</remarks>
		public void Remove(object instance)
		{
			if (!_Types.TryGetValue(instance.GetType(), out var TargetType))
				throw new ArgumentException("Unknown Instance Type");

			if (!TargetType.IsDefault)
				throw new InvalidOperationException("Type is not a default");

			//****************************************

			if (string.IsNullOrEmpty(TargetType.Name))
			{
				if (_DefaultInstances.TryGetValue(TargetType, out var CurrentInstance) && CurrentInstance.Target == instance)
					_DefaultInstances.Remove(TargetType, CurrentInstance);
			}
			else
			{
				if (!_TypeSets.TryGetValue(TargetType.Name!, out var TypeSet))
					throw new InvalidOperationException("Missing Type Set");

				TypeSet.RemoveDefault(instance);
			}
		}

		//****************************************

		/// <summary>
		/// Looks up a global command set
		/// </summary>
		/// <param name="commandName">The global command set to find</param>
		/// <param name="commandSet">The named command set, or null if it doesn't exist</param>
		/// <returns>Returns True if the Command Set was found, otherwise False</returns>
		public bool TryGetCommandSet(ReadOnlySpan<char> commandName,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out TerminalCommandSet commandSet)
		{
			lock (_LockObject)
			{
				return _Commands.TryGetValue(commandName, out commandSet!);
			}
		}

		/// <summary>
		/// Looks up a global variable
		/// </summary>
		/// <param name="variableName">The global variable to find</param>
		/// <param name="variable">The named variable, or null if it doesn't exist</param>
		/// <returns>Returns True if the Variable was found, otherwise False</returns>
		public bool TryGetVariable(ReadOnlySpan<char> variableName,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out TerminalVariable variable)
		{
			lock (_LockObject)
			{
				return _Variables.TryGetValue(variableName, out variable!);
			}
		}

		/// <summary>
		/// Looks up a type set
		/// </summary>
		/// <param name="typeName">The type set to find</param>
		/// <param name="typeSet">The named type set, or null if it doesn't exist</param>
		/// <returns>Returns True if the Type Set was found, otherwise False</returns>
		public bool TryGetTypeSet(ReadOnlySpan<char> typeName,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
		out TerminalTypeSet typeSet)
		{
			lock (_LockObject)
			{
				return _TypeSets.TryGetValue(typeName, out typeSet!);
			}
		}
		
		//****************************************
		
		internal void RegisterCommand(MethodInfo method, TerminalBindingAttribute binding)
		{	//****************************************
			var Name = binding.Name ?? method.Name;
			//****************************************
			
			if (!_Commands.TryGetValue(Name, out var Commands))
				_Commands.Add(Name, Commands = new TerminalCommandSet(Name));
			
			Commands.AddOverload(method, binding);
		}
		
		internal void RegisterVariable(PropertyInfo property, TerminalBindingAttribute binding)
		{	//****************************************
			var Name = binding.Name ?? property.Name;
			//****************************************
			
			if (_Variables.ContainsKey(Name))
				return;
			
			_Variables.Add(Name, new TerminalVariable(property, binding));
		}

		//****************************************

		private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			if (args.LoadedAssembly.ManifestModule.Assembly.IsDynamic)
				return;
			
			try
			{
				foreach(var NewType in args.LoadedAssembly.GetTypes())
				{
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

		/// <summary>
		/// Gets a collection of all the default instances
		/// </summary>
		public ICollection<TerminalTypeInstance> DefaultInstances => _DefaultInstances.Values;

		//****************************************

		/// <summary>
		/// A global registry of terminal provider instances
		/// </summary>
		public static readonly TerminalRegistry Global = new TerminalRegistry();
	}
}
