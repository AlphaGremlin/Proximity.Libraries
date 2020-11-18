using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
//****************************************

namespace Proximity.Terminal.Metadata
{
	/// <summary>
	/// Describes a set of commands grouped by name
	/// </summary>
	public sealed class TerminalCommandSet : IComparable<TerminalCommandSet>
	{ //****************************************
		private readonly List<TerminalCommand> _Commands = new List<TerminalCommand>();
		//****************************************
		
		internal TerminalCommandSet(string name)
		{
			Name = name;
		}
		
		//****************************************
		
		internal TerminalCommand AddOverload(MethodInfo method, TerminalBindingAttribute binding)
		{	//****************************************
			var NewCommand = new TerminalCommand(method, binding);
			//****************************************
			
			_Commands.Add(NewCommand);
			
			return NewCommand;
		}
		
		//****************************************
		
		/// <summary>
		/// Looks up a command based on its arguments and prepares the parameter values
		/// </summary>
		/// <param name="inArgs">The arguments to parse</param>
		/// <param name="terminal">The terminal to execute the command against</param>
		/// <param name="token">The cancellation token to pass to the command</param>
		/// <param name="command">Receives the command to execute</param>
		/// <param name="outArgs">Receives the resulting arguments for the command</param>
		/// <returns>True if a matching command was found, False if one could not be determined</returns>
		public bool PrepareCommand(string[] inArgs, ITerminal terminal, CancellationToken token,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TerminalCommand command,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out object[] outArgs)
		{
			// Try and find an overload that matches the provided arguments
			foreach(var MyCommand in _Commands)
			{
				var MethodParams = MyCommand.ExternalParameters;
				
				if (MethodParams.Length != inArgs.Length)
					continue;
				
				try
				{
					var ParamData = new object[MethodParams.Length + (MyCommand.TakesToken ? 1 : 0) + (MyCommand.TakesTerminal ? 1 : 0)];

					var ParamTarget = MyCommand.TakesTerminal ? ParamData.AsSpan(1) : ParamData.AsSpan(0);

					// Parameter count matches, try and convert the arguments to the expected types
					for(var Index = 0; Index < MethodParams.Length; Index++)
					{
						var MyConverter = TypeDescriptor.GetConverter(MethodParams.Span[Index].ParameterType);

						if (MyConverter == null)
							throw new NotSupportedException();

						ParamTarget[Index] = MyConverter.ConvertFromString(inArgs[Index]);
					}

					// Success, populate the optionals
					if (MyCommand.TakesTerminal)
						ParamData[0] = terminal;

					if (MyCommand.TakesToken)
						ParamData[ParamData.Length - 1] = token;

					outArgs = ParamData;
					
					command = MyCommand;

					return true;
				}
				catch(FormatException)
				{
					// Ignore exception and try again
				}
				catch (InvalidCastException)
				{
					// Ignore exception and try again
				}
				catch (NotSupportedException)
				{
					// Ignore exception and try again
				}
			}

			//****************************************

			command = null!;
			outArgs = null!;

			return false;
		}

		/// <inheritdoc />
		public override string ToString() => Name;

		/// <summary>
		/// Compares this command to another for the purposes of sorting
		/// </summary>
		/// <param name="other">The command to compare to</param>
		/// <returns>The result of the comparison</returns>
		public int CompareTo(TerminalCommandSet other) => Name.CompareTo(other.Name);

		//****************************************

		/// <summary>
		/// Gets the name of this command set
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the commands under this set
		/// </summary>
		public IReadOnlyCollection<TerminalCommand> Commands => _Commands;
	}
}
