using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proximity.Terminal.Metadata;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides parsing functionality for the terminal input
	/// </summary>
	public static class TerminalParser
	{
		/// <summary>
		/// Generates usage information for a terminal
		/// </summary>
		public static string HelpOn(ITerminal terminal)
		{ //****************************************
			var HasCommands = false;
			var HasVariables = false;
			var HasTypes = false;
			var Builder = new StringBuilder();
			bool IsFirst;
			//****************************************

			foreach (var Registry in terminal.Registries)
			{
				HasCommands |= Registry.Commands.Count > 0;
				HasVariables |= Registry.Variables.Count > 0;
				HasTypes |= Registry.TypeSets.Count > 0;
			}

			if (HasCommands)
			{
				Builder.AppendLine("Available Commands:").Append("\t");

				IsFirst = true;

				foreach (var Command in terminal.Registries.SelectMany(registry => registry.Commands).OrderBy(command => command))
				{
					if (IsFirst)
						IsFirst = false;
					else
						Builder.Append(", ");

					Builder.Append(Command.ToString());
				}
			}

			if (HasVariables)
			{
				if (Builder.Length > 0)
					Builder.AppendLine();

				Builder.AppendLine("Available Variables:").Append("\t");

				IsFirst = true;

				foreach (var Variable in terminal.Registries.SelectMany(registry => registry.Variables).OrderBy(variable => variable))
				{
					if (IsFirst)
						IsFirst = false;
					else
						Builder.Append(", ");

					Builder.Append(Variable.ToString());
				}
			}

			if (HasTypes)
			{
				if (Builder.Length > 0)
					Builder.AppendLine();

				Builder.AppendLine("Available Types:").Append("\t");

				IsFirst = true;

				foreach (var TypeSet in terminal.Registries.SelectMany(registry => registry.TypeSets).Where(type => type.HasInstance).OrderBy(type => type))
				{
					if (IsFirst)
						IsFirst = false;
					else
						Builder.Append(", ");

					Builder.Append(TypeSet.ToString());
				}
			}

			return Builder.ToString();
		}

		/// <summary>
		/// Generates usage information for a terminal object
		/// </summary>
		/// <param name="typeData">A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" /></param>
		public static string HelpOn(object typeData)
		{
			var Builder = new StringBuilder();
			bool IsFirst;

			if (typeData is TerminalTypeSet TypeSet)
			{
				Builder.AppendFormat("Usage information for Type: {0}", TypeSet.TypeName);

				if (TypeSet.Default?.Target != null)
				{
					if (TypeSet.Default.Type.Commands.Count != 0)
					{
						Builder.AppendLine().AppendLine("\tAvailable Default Commands:").Append("\t\t");

						IsFirst = true;

						foreach (var Command in TypeSet.Default.Type.Commands.OrderBy(set => set))
						{
							if (IsFirst)
								IsFirst = false;
							else
								Builder.Append(", ");

							Builder.Append(Command.ToString());
						}
					}
						
					if (TypeSet.Default.Type.Variables.Count != 0)
					{
						Builder.AppendLine().AppendLine("\tAvailable Default Variables:").Append("\t\t");

						IsFirst = true;

						foreach (var Variable in TypeSet.Default.Type.Variables.OrderBy(var => var))
						{
							if (IsFirst)
								IsFirst = false;
							else
								Builder.Append(", ");

							Builder.Append(Variable.ToString());
						}
					}
				}
					
				if (TypeSet.Instances.Count != 0)
				{
					Builder.AppendLine().AppendLine("\tAvailable Instances:").Append("\t\t");

					IsFirst = true;

					foreach (var InstanceName in TypeSet.Instances.OrderBy(name => name))
					{
						if (IsFirst)
							IsFirst = false;
						else
							Builder.Append(", ");

						Builder.Append(InstanceName);
					}
				}
			}
			else if (typeData is TerminalTypeInstance Instance)
			{
				Builder.AppendFormat("Usage information for Instance: {0}", Instance.Target);

				if (Instance.Type.Commands.Count != 0)
				{
					Builder.AppendLine().AppendLine("\tAvailable Commands:").Append("\t\t");

					IsFirst = true;

					foreach (var Command in Instance.Type.Commands.OrderBy(set => set))
					{
						if (IsFirst)
							IsFirst = false;
						else
							Builder.Append(", ");

						Builder.Append(Command.ToString());
					}
				}

				if (Instance.Type.Variables.Count != 0)
				{
					Builder.AppendLine().AppendLine("\tAvailable Variables:").Append("\t\t");

					IsFirst = true;

					foreach (var Variable in Instance.Type.Variables.OrderBy(var => var))
					{
						if (IsFirst)
							IsFirst = false;
						else
							Builder.Append(", ");

						Builder.Append(Variable.ToString());
					}
				}
			}
			else if (typeData is TerminalCommandSet CommandSet)
			{
				Builder.AppendFormat("Usage information for Command: {0}", CommandSet.Name);

				foreach (var Command in CommandSet.Commands)
				{
					Builder.AppendLine().Append(Command.Name).Append('(');

					IsFirst = true;

					foreach (var Parameter in Command.Method.GetParameters())
					{
						if (IsFirst)
							IsFirst = false;
						else
							Builder.Append(", ");

						Builder.AppendFormat("{0}: {1}", Parameter.Name, Parameter.ParameterType.Name);
					}

					Builder.Append(")\t").Append(Command.Description);
				}
			}
			else if (typeData is TerminalVariable Variable)
			{
				Builder.AppendFormat("{0}: {1}\t{2}", Variable.Name, Variable.Type.Name, Variable.Description);
			}

			return Builder.ToString();
		}

		/// <summary>
		/// Parses and executes a terminal command
		/// </summary>
		/// <param name="terminal">The terminal where any output from the command will appear</param>
		/// <param name="command">The command to execute</param>
		/// <returns>A task representing the execution result. True if the command ran successfully, otherwise False</returns>
		public static ValueTask<bool> Execute(ITerminal terminal, string command) => InternalExecute(terminal, command);

		/// <summary>
		/// Finds the next command for auto completion
		/// </summary>
		/// <param name="partialCommand">The partial command being completed</param>
		/// <param name="lastResult">The last result returned. Null to get the first match</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>The next command matching the partial string</returns>
		public static string FindNextCommand(string partialCommand, string lastResult, params TerminalRegistry[] registries) => FindNextCommand(partialCommand.AsSpan(), lastResult.AsSpan(), (IEnumerable<TerminalRegistry>)registries);

		/// <summary>
		/// Finds the next command for auto completion
		/// </summary>
		/// <param name="partialCommand">The partial command being completed</param>
		/// <param name="lastResult">The last result returned. Null to get the first match</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>The next command matching the partial string</returns>
		public static string FindNextCommand(string partialCommand, string lastResult, IEnumerable<TerminalRegistry> registries) => FindNextCommand(partialCommand.AsSpan(), lastResult.AsSpan(), registries);

		/// <summary>
		/// Finds the next command for auto completion
		/// </summary>
		/// <param name="partialCommand">The partial command being completed</param>
		/// <param name="lastResult">The last result returned. Null to get the first match</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>The next command matching the partial string</returns>
		public static string FindNextCommand(ReadOnlySpan<char> partialCommand, ReadOnlySpan<char> lastResult, params TerminalRegistry[] registries) => FindNextCommand(partialCommand, lastResult, (IEnumerable<TerminalRegistry>)registries);

		/// <summary>
		/// Finds the next command for auto completion
		/// </summary>
		/// <param name="partialCommand">The partial command being completed</param>
		/// <param name="lastResult">The last result returned. Null to get the first match</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>The next command matching the partial string</returns>
		public static string FindNextCommand(ReadOnlySpan<char> partialCommand, ReadOnlySpan<char> lastResult, IEnumerable<TerminalRegistry> registries)
		{ //****************************************
			ReadOnlySpan<char> CommandText, PartialText;
			ReadOnlySpan<char> Prefix = default;
			ReadOnlySpan<char> InstanceName = default;

			int CharIndex;
			
			TerminalTypeSet? TypeSet = null;
			TerminalTypeInstance? TypeInstance = null;
			
			var PartialMatches = new List<string>();
			//****************************************
			
			if (partialCommand.StartsWith("help ".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
			{
				partialCommand = partialCommand.Slice(5);
				
				Prefix = "Help ".AsSpan();
			}
			
			//****************************************
			
			// Find the first word (split on a space)
			CharIndex = partialCommand.IndexOf(' ');
			
			// If there's a space, we're parsing an Instance Type and optional Instance Name, with a partial Command/Variable
			if (CharIndex != -1)
			{
				CommandText = partialCommand.Slice(0, CharIndex);
				PartialText = partialCommand.Slice(CharIndex + 1);
				
				CharIndex = CommandText.IndexOf('.');
				
				// Split into Type and Name if necessary
				if (CharIndex != -1)
				{
					InstanceName = CommandText.Slice(CharIndex + 1);
					CommandText = CommandText.Slice(0, CharIndex);
				}
				
				foreach(var Registry in registries)
				{
					TypeSet = Registry.FindTypeSet(CommandText);

					if (TypeSet != null)
						break;
				}
				
				// If the instance type doesn't match, return the partial command as is
				if (TypeSet == null)
					return Prefix.Concat(partialCommand);
				
				if (InstanceName == null)
				{
					TypeInstance = TypeSet.Default;
					InstanceName = TypeSet.TypeName;
				}
				else
				{
					TypeInstance = TypeSet.GetNamedInstance(InstanceName);

					if (TypeInstance == null)
						return Prefix.Concat(partialCommand);

					InstanceName = $"{TypeSet.TypeName}.{TypeInstance.Name}";
				}
				
				// If the instance doesn't exist, return as is
				if (TypeInstance == null)
					return Prefix.Concat(partialCommand);
				
				// Add matching commands
				foreach (var MyCommand in TypeInstance.Type.Commands)
				{
					if (MyCommand.Name.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(InstanceName.Concat(" ", MyCommand.Name));
				}
				
				// Add matching variables
				foreach (var MyVariable in TypeInstance.Type.Variables)
				{
					if (MyVariable.Name.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(InstanceName.Concat(" ", MyCommand.Name)string.Format("{0} {1}", InstanceName, MyVariable.Name));
				}
			}
			else
			{
				CharIndex = partialCommand.IndexOf('.');
				
				// If there's a dot, we're parsing an Instance Type, with a partial Instance Name
				if (CharIndex != -1)
				{
					CommandText = partialCommand.Slice(0, CharIndex);
					PartialText = partialCommand.Slice(CharIndex + 1);

					foreach(var MyRegistry in registries)
					{
						TypeSet = MyRegistry.FindTypeSet(CommandText);
					}
					
					// If the instance type doesn't match, return the partial command as is
					if (TypeSet == null)
						return Prefix + partialCommand;
					
					foreach(var MyInstanceName in TypeSet.Instances)
					{
						if (MyInstanceName.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
							PartialMatches.Add(string.Format("{0}.{1}", TypeSet.TypeName, MyInstanceName));
					}
				}
				else
				{
					// No dot, we're parsing a partial Command/Variable/Instance Type
					foreach(var MyRegistry in registries)
					{
						// Add matching commands
						foreach (var MyCommand in MyRegistry.Commands)
						{
							if (MyCommand.Name.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
								PartialMatches.Add(MyCommand.Name);
						}
						
						// Add matching variables (with an equals sign, so they can't be the same as commands)
						foreach (var MyVariable in MyRegistry.Variables)
						{
							if (MyVariable.Name.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
								PartialMatches.Add(MyVariable.Name);
						}
						
						// Add matching type sets
						foreach (var MyType in MyRegistry.TypeSets)
						{
							// Only add ones that have an instance
							if (MyType.TypeName.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase) && MyType.HasInstance)
								PartialMatches.Add(MyType.TypeName);
						}
					}
				}
			}
			
			//****************************************
			
			// Any results?
			if (PartialMatches.Count == 0)
				return null;
			
			// Sort them, so we can pick the next matching result
			PartialMatches.Sort();
			
			if (lastResult != null)
			{
				// Find one greater than our last match (user has requested the next one)
				foreach(var NextCommand in PartialMatches)
				{
					if (NextCommand.CompareTo(lastResult) > 0)
						return Prefix + NextCommand;
				}
				// Nothing greater, go back to the start
			}
			
			return Prefix + PartialMatches[0];
		}

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object FindCommand(string command, params TerminalRegistry[] registries) => FindCommand(command.AsSpan(), (IEnumerable<TerminalRegistry>)registries);

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object FindCommand(string command, IEnumerable<TerminalRegistry> registries) => FindCommand(command.AsSpan(), registries);

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object FindCommand(ReadOnlySpan<char> command, params TerminalRegistry[] registries) => FindCommand(command, (IEnumerable<TerminalRegistry>)registries);

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object FindCommand(ReadOnlySpan<char> command, IEnumerable<TerminalRegistry> registries)
		{	//****************************************
			string CommandText, InstanceName = null, InstanceType;
			
			int CharIndex;
			
			TerminalTypeSet? TypeSet = null;
			TerminalTypeInstance? TypeInstance = null;
			
			var PartialMatches = new List<string>();
			//****************************************
			
			// Find the first word (split on a space/exclamation)
			CharIndex = command.IndexOfAny(new char[] { ' ', '!' });
			
			// If there's a space, we're parsing an Instance Type and optional Instance Name, with a Command/Variable
			if (CharIndex != -1)
			{
				InstanceType = command.Substring(0, CharIndex);
				CommandText = command.Substring(CharIndex + 1);
				
				CharIndex = InstanceType.IndexOf('.');
				
				// Split into Type and Name if necessary
				if (CharIndex != -1)
				{
					InstanceName = InstanceType.Substring(CharIndex + 1);
					InstanceType = InstanceType.Substring(0, CharIndex);
				}
				
				foreach(var MyRegistry in registries)
				{
					TypeSet = MyRegistry.FindTypeSet(InstanceType);
				}
				
				if (TypeSet == null)
					return null;
				
				if (InstanceName == null)
				{
					TypeInstance = TypeSet.Default;
				}
				else
				{
					TypeInstance = TypeSet.GetNamedInstance(InstanceName);
				}
				
				// If the instance doesn't exist, return the type set
				if (TypeInstance == null)
					return TypeSet;
				
				var MyVariable = TypeInstance.Type.FindVariable(CommandText);
				
				if (MyVariable != null)
					return MyVariable;
				
				return TypeInstance.Type.FindCommand(CommandText);
			}
			
			//****************************************
			
			CharIndex = command.IndexOf('.');
				
			// If there's a dot, we're parsing an Instance Type plus an Instance Name
			if (CharIndex != -1)
			{
				InstanceType = command.Substring(0, CharIndex);
				CommandText = command.Substring(CharIndex + 1);

				foreach(var MyRegistry in registries)
				{
					TypeSet = MyRegistry.FindTypeSet(InstanceType);
				}
				
				if (TypeSet == null)
					return null;
				
				TypeInstance = TypeSet.GetNamedInstance(CommandText);
				
				if (TypeInstance != null)
					return TypeInstance;
				
				return TypeSet;
			}
			
			//****************************************
			
			// No dot, we're parsing a Command/Variable/Instance Type
			foreach(var MyRegistry in registries)
			{
				var MyVariable = MyRegistry.FindVariable(command);
				
				if (MyVariable != null)
					return MyVariable;
				
				var MyCommand = MyRegistry.FindCommand(command);
				
				if (MyCommand != null)
					return MyCommand;
				
				TypeSet = MyRegistry.FindTypeSet(command);
				
				if (TypeSet != null)
					return TypeSet;
			}
			
			return null;
		}

		//****************************************

		internal static ValueTask<bool> InternalExecute(ITerminal terminal, string command)
		{ //****************************************
			string CommandText, ArgumentText;
			string InstanceName, CurrentPath = "";

			int CharIndex;
			char WordDivider;

			TerminalTypeSet MyTypeSet = null;
			TerminalTypeInstance MyInstance = null;
			Task<bool> MyResult;
			//****************************************

			Context = registries;

			Log.Write(new ConsoleLogEntry(string.Format("> {0}", command)));

			// Find the first word (split on a space, equals)
			CharIndex = command.IndexOfAny(new char[] { ' ', '=' });

			// Split off the arguments (if any)
			if (CharIndex == -1)
			{
				CommandText = command;
				ArgumentText = null;
				WordDivider = '\0';
			}
			else
			{
				CommandText = command.Substring(0, CharIndex);
				ArgumentText = command.Substring(CharIndex + 1);
				WordDivider = command[CharIndex];
			}

			//****************************************

			// Is there a dot divider identifying an instance path?
			CharIndex = CommandText.IndexOf('.');

			if (CharIndex != -1 && WordDivider != '=')
			{
				InstanceName = CommandText.Substring(CharIndex + 1);
				CommandText = CommandText.Substring(0, CharIndex);

				// Split into Type and Instance
				foreach (var MyRegistry in registries)
				{
					MyTypeSet = MyRegistry.FindTypeSet(CommandText);

					if (MyTypeSet != null)
					{
						MyInstance = MyTypeSet.GetNamedInstance(InstanceName);
						break;
					}
				}

				if (MyTypeSet == null)
				{
					Log.Info("{0} is not an Instance Type", CommandText);

					return Task.FromResult(false);
				}

				if (MyInstance == null)
				{
					Log.Info("{0} is not a known Instance of {1}", InstanceName, MyTypeSet.TypeName);

					return Task.FromResult(false);
				}

				CurrentPath = string.Format("{0}.{1}!", MyTypeSet.TypeName, MyInstance.Name);
			}

			//****************************************

			if (MyTypeSet == null)
			{
				// Attempt to execute it as a command or variable
				MyResult = TryExecute(registries, CurrentPath, null, CommandText, ArgumentText);

				if (MyResult != null)
					return MyResult;

				// Perhaps it's an instance type, and we're calling the default instance
				foreach (var MyRegistry in registries)
				{
					MyTypeSet = MyRegistry.FindTypeSet(CommandText);

					if (MyTypeSet != null)
						break;
				}

				// Do we have a match at all?
				if (MyTypeSet == null)
				{
					Log.Info("{0} is not a Command, Variable, or Instance Type", CommandText);

					return Task.FromResult(false);
				}

				// If there are no arguments, we should list all the instances that match
				if (ArgumentText == null)
				{
					TerminalParser.HelpOn(MyTypeSet);

					return Task.FromResult(true);
				}

				// We have arguments, so is there a default instance?
				MyInstance = MyTypeSet.Default;

				if (MyInstance == null)
				{
					Log.Info("{0} does not have a default instance", MyTypeSet.TypeName);

					return Task.FromResult(false);
				}

				CurrentPath = MyTypeSet.TypeName + " ";
			}

			//****************************************

			// We're calling an Instance Type, are there any arguments?
			if (ArgumentText == null)
			{
				// Display the instance details (ToString, available commands and variables)
				TerminalParser.HelpOn(MyInstance);

				return Task.FromResult(true);
			}

			// Repeat the argument process on the arguments themselves
			CharIndex = ArgumentText.IndexOfAny(new char[] { ' ', '=' });

			// Split off the arguments (if any)
			if (CharIndex == -1)
			{
				CommandText = ArgumentText;
				ArgumentText = null;
				WordDivider = '\0';
			}
			else
			{
				CommandText = ArgumentText.Substring(0, CharIndex);
				WordDivider = ArgumentText[CharIndex];
				ArgumentText = ArgumentText.Substring(CharIndex + 1);
			}

			//****************************************

			// Attempt to execute it as a command or variable
			MyResult = TryExecute(registries, CurrentPath, MyInstance, CommandText, ArgumentText);

			if (MyResult != null)
				return MyResult;

			Log.Info("{0}{1} is not a Command or Variable", CurrentPath, CommandText);

			return Task.FromResult(false);
		}

		private static Task<bool> TryExecute(TerminalRegistry[] registries, string path, TerminalInstance instance, string commandText, string argumentText)
		{	//****************************************
			TerminalVariable MyVariable = null;
			TerminalCommandSet MyCommand = null;
			object MyInstance = null;
			//****************************************
			
			if (instance != null)
			{
				MyInstance = instance.Target;
				
				if (MyInstance == null)
				{
					Log.Info("{0} is not a known Instance of {1}", instance.Name, instance.Type.Name);
					
					return Task.FromResult(false);
				}
			}
			
			//****************************************
			
			if (commandText == "*")
			{
				if (instance != null)
				{
					foreach(var MyVar in instance.Type.Variables)
					{
						Log.Info("{0}{1}={2}", path, MyVar.Name, MyVar.GetValue(MyInstance));
					}
				}
				else
				{
					foreach(var MyRegistry in registries)
					{
						foreach(var MyVar in MyRegistry.Variables)
						{
							Log.Info("{0}{1}={2}", path, MyVar.Name, MyVar.GetValue(null));
						}
					}
				}
				
				return Task.FromResult(true);
			}
			
			//****************************************
			
			// Check if it's a variable first, and either get or set it
			if (instance != null)
				MyVariable = instance.Type.FindVariable(commandText);
			else
			{
				foreach(var MyRegistry in registries)
				{
					MyVariable = MyRegistry.FindVariable(commandText);
					
					if (MyVariable != null)
						break;
				}
			}
			
			if (MyVariable != null)
			{
				if (argumentText != null)
				{
					if (!MyVariable.CanWrite)
					{
						Log.Info("{0}{1} is not writeable", path, MyVariable.Name);
						
						return Task.FromResult(false);
					}
					
					if (!MyVariable.SetValue(MyInstance, argumentText))
					{
						Log.Info("{0}{1} is of type {2}", path, MyVariable.Name, MyVariable.Type);
						
						return Task.FromResult(false);
					}
					
					return Task.FromResult(true);
				}
				else
				{
					Log.Info("{0}{1}={2}", path, MyVariable.Name, MyVariable.GetValue(MyInstance));
					
					return Task.FromResult(true);
				}
			}
			
			//****************************************
			
			// Check if it's a command and attempt to execute it
			if (instance != null)
				MyCommand = instance.Type.FindCommand(commandText);
			else
			{
				foreach(var MyRegistry in registries)
				{
					MyCommand = MyRegistry.FindCommand(commandText);
					
					if (MyCommand != null)
						break;
				}
			}
			
			if (MyCommand != null)
			{
				return ExecuteCommand(path, MyInstance, MyCommand, argumentText);
			}
			
			return null;
		}
		
		private static async Task<bool> ExecuteCommand(string path, object instance, TerminalCommandSet commandSet, string argumentText)
		{	//****************************************
			var RawParams = new List<string>();
			string AlteredText = argumentText;
			int LastIndex = 0, CharIndex = 0, QuoteMode = 0;
			TerminalCommand MyCommand;
			char CurrentChar;
			//****************************************
			
			if (argumentText != null)
			{
				for(; CharIndex < AlteredText.Length; CharIndex++)
				{
					CurrentChar = AlteredText[CharIndex];
					
					switch (CurrentChar)
					{
					case ' ':
						if (QuoteMode != 0)
							continue;
						
						if (CharIndex != LastIndex)
							RawParams.Add(AlteredText.Substring(LastIndex, CharIndex - LastIndex));
						
						LastIndex = CharIndex + 1;
						
						break;
						
					case '"':
						if (QuoteMode == 0)
						{
							QuoteMode = 1;
							LastIndex = CharIndex + 1;
						}
						else if (QuoteMode == 1)
						{
							QuoteMode = 0;
							
							RawParams.Add(AlteredText.Substring(LastIndex, CharIndex - LastIndex));
							
							LastIndex = CharIndex + 1;
						}
						break;
						
					case '\'':
						if (QuoteMode == 0)
						{
							QuoteMode = 2;
							LastIndex = CharIndex + 1;
						}
						else if (QuoteMode == 2)
						{
							QuoteMode = 0;
							
							RawParams.Add(AlteredText.Substring(LastIndex, CharIndex - LastIndex));
							
							LastIndex = CharIndex + 1;
						}
						break;
						
					case '\\': // Skip the next character if it's a quote or space
						if (CharIndex < AlteredText.Length - 1) // Make sure it's not the last character
						{
							CurrentChar = AlteredText[CharIndex];
							
							if (CurrentChar != '\'' && CurrentChar != '"' && CurrentChar != ' ')
								AlteredText = AlteredText.Remove(CharIndex, 1);
						}
						break;
					}
				}
				
				// Add the final parameter
				if (LastIndex != AlteredText.Length)
					RawParams.Add(AlteredText.Substring(LastIndex));
			}
			
			//****************************************
			
			// Try with the broken up arguments
			MyCommand = commandSet.FindCommand(RawParams.ToArray(), out var OutParams);
			
			// If that fails, try and pass the whole argument text as the first argument, no quoting
			if (MyCommand == null)
				MyCommand = commandSet.FindCommand(new string[] { argumentText }, out OutParams);
				
			if (MyCommand == null)
			{
				Log.Info("{0}{1} does not accept the given arguments", path, commandSet.Name);
				
				return false;
			}
			
			//****************************************
			
			await MyCommand.InternalInvokeAsync(instance, OutParams);
			
			return true;
		}
	}
}
