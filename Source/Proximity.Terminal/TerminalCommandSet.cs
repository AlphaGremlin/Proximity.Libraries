using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
//****************************************

namespace Proximity.Terminal
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
		/// Looks up a command based on its arguments
		/// </summary>
		/// <param name="inArgs">The arguments to parse</param>
		/// <param name="outArgs">The resulting properly typed arguments</param>
		/// <returns>The command that best matches, or none if it could not be determined</returns>
		public TerminalCommand FindCommand(string[] inArgs, out object[] outArgs)
		{	//****************************************
			var ParamData = new object[inArgs.Length];
			ParameterInfo[] MethodParams;
			TypeConverter MyConverter;
			//****************************************
			
			// Try and find an overload that matches the provided arguments
			foreach(var MyCommand in _Commands)
			{
				MethodParams = MyCommand.Method.GetParameters();
				
				if (MethodParams.Length != inArgs.Length)
					continue;
				
				try
				{
					// Parameter count matches, try and convert the arguments to the expected types
					for(int Index = 0; Index < MethodParams.Length; Index++)
					{
						MyConverter = TypeDescriptor.GetConverter(MethodParams[Index].ParameterType);
							
						if (MyConverter == null)
							throw new NotSupportedException();
						
						ParamData[Index] = MyConverter.ConvertFromString(inArgs[Index]);
					}
					
					// Success!
					outArgs = ParamData;
					
					return MyCommand;
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
			
			outArgs = null;
			
			return null;
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
