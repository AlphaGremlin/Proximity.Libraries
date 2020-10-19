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

				foreach (var Command in
					terminal.Registries.SelectMany(registry => registry.Commands)
					.Concat(terminal.Registries.SelectMany(registry => registry.DefaultInstances).SelectMany(instance => instance.Type.Commands))
					.OrderBy(command => command)
					)
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

				foreach (var Variable in
					terminal.Registries.SelectMany(registry => registry.Variables)
					.Concat(terminal.Registries.SelectMany(registry => registry.DefaultInstances).SelectMany(instance => instance.Type.Variables))
					.OrderBy(variable => variable))
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

					foreach (var Parameter in Command.ExternalParameters.Span)
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
		/// <param name="token">A token to pass to methods that accept it</param>
		/// <returns>A task representing the execution result. True if the command ran successfully, otherwise False</returns>
		public static ValueTask<bool> Execute(ITerminal terminal, string command, CancellationToken token = default) => InternalExecute(terminal, command, token);

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
			TerminalTypeInstance? TypeInstance;
			
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
					if (Registry.TryGetTypeSet(CommandText, out TypeSet))
						break;
				}
				
				// If the instance type doesn't match, return the partial command as is
				if (TypeSet == null)
					return Prefix.Concat(partialCommand);
				
				if (InstanceName == null)
				{
					TypeInstance = TypeSet.Default;
					CommandText = TypeSet.TypeName.AsSpan();
				}
				else
				{
					if (!TypeSet.TryGetNamedInstance(InstanceName, out TypeInstance))
						return Prefix.Concat(partialCommand);

					CommandText = $"{TypeSet.TypeName}.{TypeInstance.Name}".AsSpan();
				}
				
				// If the instance doesn't exist, return as is
				if (TypeInstance == null)
					return Prefix.Concat(partialCommand);
				
				// Add matching commands
				foreach (var MyCommand in TypeInstance.Type.Commands)
				{
					if (MyCommand.Name.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(CommandText.Concat(" ", MyCommand.Name));
				}
				
				// Add matching variables
				foreach (var MyVariable in TypeInstance.Type.Variables)
				{
					if (MyVariable.Name.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(CommandText.Concat(" ", MyVariable.Name));
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
						if (MyRegistry.TryGetTypeSet(CommandText, out TypeSet))
							break;
					}
					
					// If the instance type doesn't match, return the partial command as is
					if (TypeSet == null)
						return Prefix.Concat(partialCommand);
					
					foreach(var MyInstanceName in TypeSet.Instances)
					{
						if (MyInstanceName.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
							PartialMatches.Add(string.Format("{0}.{1}", TypeSet.TypeName, MyInstanceName));
					}
				}
				else
				{
					// No dot, we're parsing a partial Command/Variable/Instance Type
					foreach(var Registry in registries)
					{
						// Add matching commands
						foreach (var Command in Registry.Commands)
						{
							if (Command.Name.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
								PartialMatches.Add(Command.Name);
						}

						// Add matching variables (with an equals sign, so they can't be the same as commands)
						foreach (var Variable in Registry.Variables)
						{
							if (Variable.Name.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
								PartialMatches.Add(Variable.Name);
						}

						foreach (var Instance in Registry.DefaultInstances)
						{
							foreach (var Command in Instance.Type.Commands)
							{
								if (Command.Name.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
									PartialMatches.Add(Command.Name);
							}

							// Add matching variables (with an equals sign, so they can't be the same as commands)
							foreach (var Variable in Instance.Type.Variables)
							{
								if (Variable.Name.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
									PartialMatches.Add(Variable.Name);
							}

						}

						// Add matching type sets
						foreach (var Type in Registry.TypeSets)
						{
							// Only add ones that have an instance
							if (Type.TypeName.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase) && Type.HasInstance)
								PartialMatches.Add(Type.TypeName);
						}
					}
				}
			}
			
			//****************************************
			
			// Any results?
			if (PartialMatches.Count == 0)
				return "";
			
			// Sort them, so we can pick the next matching result
			PartialMatches.Sort();
			
			if (lastResult != null)
			{
				// Find one greater than our last match (user has requested the next one)
				foreach(var NextCommand in PartialMatches)
				{
					if (NextCommand.AsSpan().CompareTo(lastResult, StringComparison.OrdinalIgnoreCase) > 0)
						return Prefix.Concat(NextCommand);
				}
				// Nothing greater, go back to the start
			}
			
			return Prefix.Concat(PartialMatches[0]);
		}

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object? FindCommand(string command, params TerminalRegistry[] registries) => FindCommand(command.AsSpan(), (IEnumerable<TerminalRegistry>)registries);

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object? FindCommand(string command, IEnumerable<TerminalRegistry> registries) => FindCommand(command.AsSpan(), registries);

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object? FindCommand(ReadOnlySpan<char> command, params TerminalRegistry[] registries) => FindCommand(command, (IEnumerable<TerminalRegistry>)registries);

		/// <summary>
		/// Parses a command target (no arguments) and outputs the best match
		/// </summary>
		/// <param name="command">The command to parse</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalTypeInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object? FindCommand(ReadOnlySpan<char> command, IEnumerable<TerminalRegistry> registries)
		{
			if (!TryParse(command, registries, out var ParseResult))
				return null;

			if (ParseResult.CommandSet != null)
				return ParseResult.CommandSet;

			if (ParseResult.Variable != null)
				return ParseResult.Variable;

			if (ParseResult.Instance != null)
				return ParseResult.Instance;

			if (ParseResult.TypeSet != null)
				return ParseResult.TypeSet;

			return null;
		}

		/// <summary>
		/// Parses a terminal command
		/// </summary>
		/// <param name="command">The raw command text</param>
		/// <param name="registries">The terminal registries to lookup commands from</param>
		/// <param name="result">Receives the parsing result</param>
		/// <returns>True if the command matched something useful, otherwise False</returns>
		public static bool TryParse(ReadOnlySpan<char> command, IEnumerable<TerminalRegistry> registries, out TerminalParseResult result)
		{
			int CharIndex;

			ReadOnlySpan<char> InstanceType, InstanceName, Command, Arguments;

			CharIndex = command.IndexOf(' ');

			if (CharIndex != -1)
			{
				// Found a space - could be one of the following:
				// > Command
				// > Command Arguments...
				// > Variable=Value Continued
				// > InstanceType Command
				// > InstanceType Command Arguments...
				// > InstanceType Variable
				// > InstanceType Variable=Value
				// > InstanceType.InstanceName Command
				// > InstanceType.InstanceName Command Arguments...
				// > InstanceType.InstanceName Variable
				// > InstanceType.InstanceName Variable=Value

				Arguments = command.Slice(CharIndex + 1);
				Command = command.Slice(0, CharIndex);
			}
			else
			{
				// No space - could be one of the following:
				// > Command
				// > Variable
				// > Variable=Value
				// > InstanceType
				// > InstanceType.InstanceName

				Command = command;
				Arguments = default;
			}

			CharIndex = Command.IndexOf('.');

			if (CharIndex == -1)
			{
				// Did not find a dot - could be one of the following:
				// > Command
				// > Command Arguments...
				// > Variable
				// > Variable=Value
				// > Variable=Value Continued
				// > InstanceType
				// > InstanceType Command
				// > InstanceType Command Arguments...
				// > InstanceType Variable
				// > InstanceType Variable=Value

				// Is it a Variable set?
				CharIndex = Command.IndexOf('=');

				if (CharIndex != -1)
				{
					// Is one of:
					// > Variable=Value
					// > Variable=Value Continued

					// Fix up the arguments, in case Value is continued
					Command = Command.Slice(0, CharIndex);
					Arguments = command.Slice(CharIndex + 1);

					foreach (var Registry in registries)
					{
						if (Registry.TryGetVariable(Command, out var Variable))
						{
							result = new TerminalParseResult(null, null, Variable, default, Arguments);

							return true;
						}
					}

					result = default;

					return false;
				}

				// Is it a command?
				foreach (var Registry in registries)
				{
					if (Registry.TryGetCommandSet(Command, out var CommandSet))
					{
						// Is one of:
						// > Command
						// > Command Arguments...
						result = new TerminalParseResult(null, null, CommandSet, default, Arguments);

						return true;
					}

					foreach (var Instance in Registry.DefaultInstances)
					{
						if (Instance.Type.TryGetCommand(Command, out CommandSet))
						{
							// Is one of:
							// > Command (on global instance)
							// > Command Arguments (on global instance)...
							result = new TerminalParseResult(null, Instance, CommandSet, default, Arguments);

							return true;
						}
					}
				}

				// Is it a variable?
				if (Arguments.IsEmpty)
				{
					foreach (var Registry in registries)
					{
						if (Registry.TryGetVariable(Command, out var Variable))
						{
							// Is:
							// > Variable
							result = new TerminalParseResult(null, null, Variable, default, default);

							return true;
						}

						foreach (var Instance in Registry.DefaultInstances)
						{
							if (Instance.Type.TryGetVariable(Command, out Variable))
							{
								// Is:
								// > Variable (on global instance)
								result = new TerminalParseResult(null, Instance, Variable, default, Arguments);

								return true;
							}
						}
					}
				}

				// Is it an Instance Type?
				foreach (var Registry in registries)
				{
					if (Registry.TryGetTypeSet(Command, out var TypeSet))
					{
						// Are there any arguments?
						if (Arguments.Length == 0)
						{
							// Is: InstanceType
							result = new TerminalParseResult(TypeSet, null, default, default);

							return true;
						}

						// Yes - could be one of the following:
						// > InstanceType Command
						// > InstanceType Command Arguments...
						// > InstanceType Variable
						// > InstanceType Variable=Value
						var Type = TypeSet.Default?.Type;

						if (Type == null)
						{
							// Failed to parse, arguments supplied without a default instance
							result = new TerminalParseResult(TypeSet, null, default, default);

							return false;
						}

						return ParseType(TypeSet, Type, TypeSet.Default, Arguments, out result);
					}
				}

				result = new TerminalParseResult(null, null, Command, default);

				return false;
			}

			// Found a dot - could be one of the following:
			// > InstanceType.InstanceName
			// > InstanceType.InstanceName Command
			// > InstanceType.InstanceName Command Arguments...
			// > InstanceType.InstanceName Variable
			// > InstanceType.InstanceName Variable=Value

			InstanceType = Command.Slice(0, CharIndex);
			InstanceName = Command.Slice(CharIndex + 1);

			foreach (var Registry in registries)
			{
				if (Registry.TryGetTypeSet(InstanceType, out var TypeSet))
				{
					if (TypeSet.TryGetNamedInstance(InstanceName, out var Instance))
					{
						// Are there any arguments?
						if (Arguments.Length == 0)
						{
							// Is: InstanceType.InstanceName
							result = new TerminalParseResult(TypeSet, Instance, default, default);

							return true;
						}

						// Yes - could be one of the following:
						// > InstanceType.InstanceName Command
						// > InstanceType.InstanceName Command Arguments...
						// > InstanceType.InstanceName Variable
						// > InstanceType.InstanceName Variable=Value
						return ParseType(TypeSet, Instance.Type, Instance, Arguments, out result);
					}
				}
			}

			// Couldn't find the requested Instance
			foreach (var Registry in registries)
			{
				if (Registry.TryGetTypeSet(InstanceType, out var TypeSet))
				{
					result = new TerminalParseResult(TypeSet, default, InstanceName, Arguments);

					return false;
				}
			}

			result = new TerminalParseResult(default, default, Command, Arguments);

			return false;
		}

		//****************************************

		internal static ValueTask<bool> InternalExecute(ITerminal terminal, string command, CancellationToken token)
		{
			terminal.Log(LogLevel.Information, default, new ConsoleRecord(DateTimeOffset.Now, LogLevel.Information, command, scope: TerminalScope.ConsoleCommand), null, (record, exception) => record.Text);

			var Command = command.AsSpan();
			var IsHelp = false;

			if (Command.StartsWith("help".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
			{
				Command = Command.Slice(4);
				IsHelp = true;

				if (Command.IsEmpty || Command[0] != ' ')
				{
					terminal.LogInformation(HelpOn(terminal));

					return new ValueTask<bool>(true);
				}

				Command = Command.Slice(1);
			}

			if (!TryParse(Command, terminal.Registries, out var ParseResult))
			{
				if (ParseResult.TypeSet != null)
				{
					if (ParseResult.Instance != null)
					{
						if (ParseResult.Instance == ParseResult.TypeSet.Default)
							terminal.LogInformation("{0} {1} is not a Command or Variable", ParseResult.TypeSet.TypeName, ParseResult.Command.AsString());
						else
							terminal.LogInformation("{0}.{1} {2} is not a Command or Variable", ParseResult.TypeSet.TypeName, ParseResult.Instance.Name, ParseResult.Command.AsString());
					}
					else if (Command[ParseResult.TypeSet.TypeName.Length] == '.')
					{
						terminal.LogInformation("{0}.{1} is not a known Instance", ParseResult.TypeSet.TypeName, ParseResult.Command.AsString());
					}
					else
					{
						terminal.LogInformation("{0} {1} is not a Command or Variable", ParseResult.TypeSet.TypeName, ParseResult.Command.AsString());
					}
				}
				else if (ParseResult.Command.IndexOf('.') != -1)
				{
					terminal.LogInformation("{0} is not a known Type", ParseResult.Command.Slice(ParseResult.Command.IndexOf('.')).AsString());
				}
				else
				{
					terminal.LogInformation("{0} is not a Command or Variable ", ParseResult.Command.AsString());
				}

				return new ValueTask<bool>(false);
			}

			//****************************************

			object? TargetInstance = null;
			var TargetPath = "";

			// Get the Instance we're targeting (may be null if it's been GC'ed)
			if (ParseResult.Instance != null)
			{
				TargetInstance = ParseResult.Instance.Target;

				if (ParseResult.TypeSet != null)
				{
					if (TargetInstance == null)
					{
						if (ParseResult.Instance == ParseResult.TypeSet.Default)
							terminal.LogInformation("{0} is not a known Instance", ParseResult.TypeSet.TypeName);
						else
							terminal.LogInformation("{0}.{1} is not a known Instance", ParseResult.TypeSet.TypeName, ParseResult.Instance.Name);

						return new ValueTask<bool>(false);
					}

					if (ParseResult.Instance == ParseResult.TypeSet.Default)
						TargetPath = $"{ParseResult.TypeSet.TypeName} ";
					else
						TargetPath = $"{ParseResult.TypeSet.TypeName}.{ParseResult.Instance.Name} ";
				}
				else
				{
					if (TargetInstance == null)
					{
						// Instance was garbage collected
						terminal.LogInformation("{0} is not a Command or Variable ", ParseResult.Command.AsString());

						return new ValueTask<bool>(false);
					}
				}
			}

			//****************************************

			if (ParseResult.CommandSet == null && ParseResult.Variable == null)
			{
				// No Command or Variable, are we asking for star?
				if (ParseResult.Command.Length == 1 && ParseResult.Command[0] == '*')
				{
					if (ParseResult.Instance != null && TargetInstance != null)
					{
						foreach (var MyVar in ParseResult.Instance.Type.Variables)
						{
							terminal.LogInformation("{0}{1}={2}", TargetPath, MyVar.Name, MyVar.GetValue(TargetInstance));
						}
					}
					else
					{
						foreach (var Registry in terminal.Registries)
						{
							foreach (var Variable in Registry.Variables)
							{
								terminal.LogInformation("{0}={1}", Variable.Name, Variable.GetValue(null));
							}
						}
					}

					return new ValueTask<bool>(true);
				}

				// Do we have a target Instance?
				if (ParseResult.Instance != null)
				{
					terminal.LogInformation(HelpOn(ParseResult.Instance));

					return new ValueTask<bool>(true);
				}

				// Are we only targeting a Type?
				if (ParseResult.TypeSet != null)
				{
					terminal.LogInformation(HelpOn(ParseResult.TypeSet));

					return new ValueTask<bool>(true);
				}

				throw new InvalidOperationException("Failed to execute, no target specified");
			}

			//****************************************

			// Execute the Command
			if (ParseResult.CommandSet != null)
			{
				if (IsHelp)
				{
					terminal.LogInformation(HelpOn(ParseResult.CommandSet));

					return new ValueTask<bool>(true);
				}

				return ExecuteCommand(terminal, TargetPath, TargetInstance, ParseResult.CommandSet, ParseResult.Arguments.AsString(), token);
			}

			//****************************************

			if (ParseResult.Variable != null)
			{
				if (IsHelp)
				{
					terminal.LogInformation(HelpOn(ParseResult.Variable));

					return new ValueTask<bool>(true);
				}

				// Process the Variable
				if (ParseResult.Arguments.IsEmpty)
				{
					terminal.LogInformation("{0}{1}={2}", TargetPath, ParseResult.Variable.Name, ParseResult.Variable.GetValue(TargetInstance));

					return new ValueTask<bool>(true);
				}

				if (ParseResult.Variable.CanWrite)
				{
					if (ParseResult.Variable.SetValue(TargetInstance, ParseResult.Arguments))
						return new ValueTask<bool>(true);

					terminal.LogInformation("{0}{1} is of type {2}", TargetPath, ParseResult.Variable.Name, ParseResult.Variable.Type);
				}
				else
				{
					terminal.LogInformation("{0}{1} is not writeable", TargetPath, ParseResult.Variable.Name);
				}

				return new ValueTask<bool>(false);
			}

			//****************************************

			throw new InvalidOperationException("Failed to execute, no target specified");
		}

		//****************************************

		private static bool ParseType(TerminalTypeSet typeSet, TerminalType type, TerminalTypeInstance? instance, ReadOnlySpan<char> command, out TerminalParseResult result)
		{
			ReadOnlySpan<char> Command, Arguments;
			TerminalVariable? Variable;

			// Star command lists all variables
			if (command.Length == 1 && command[0] == '*')
			{
				result = new TerminalParseResult(typeSet, instance, command, default);

				return true;
			}

			// Is there an equals or space?
			var CharIndex = command.IndexOfAny(new[] { '=', ' ' });

			if (CharIndex == -1)
			{
				// Single word - could be one of the following:
				// > InstanceType Command
				// > InstanceType Variable
				// > InstanceType.InstanceName Command
				// > InstanceType.InstanceName Variable
				Command = command;
				Arguments = default;
			}
			else
			{
				Command = command.Slice(0, CharIndex);
				Arguments = command.Slice(CharIndex + 1);

				if (command[CharIndex] == '=')
				{
					// Equals Sign - could be one of the following:
					// > InstanceType Variable=Value
					// > InstanceType.InstanceName Variable=Value
					if (type.TryGetVariable(Command, out Variable))
					{
						result = new TerminalParseResult(typeSet, instance, Variable, default, Arguments);

						return true;
					}
					else
					{
						// Failed to parse, variable does not exist
						result = new TerminalParseResult(typeSet, instance, Command, Arguments);

						return false;
					}
				}
				else
				{
					// Space - could be one of the following:
					// > InstanceType Command Arguments...
					// > InstanceType.InstanceName Command Arguments...
				}
			}

			if (type.TryGetCommand(Command, out var CommandSet))
			{
				result = new TerminalParseResult(typeSet, instance, CommandSet, default, Arguments);

				return true;
			}

			if (Arguments.IsEmpty && type.TryGetVariable(Command, out Variable))
			{
				result = new TerminalParseResult(typeSet, instance, Variable, default, default);

				return true;
			}

			// Failed to parse, command or variable does not exist
			result = new TerminalParseResult(typeSet, instance, Command, Arguments);

			return false;
		}

		private static async ValueTask<bool> ExecuteCommand(ITerminal terminal, string path, object? instance, TerminalCommandSet commandSet, string arguments, CancellationToken token)
		{	//****************************************
			var RawParams = new List<string>();
			var AlteredText = arguments;
			int LastIndex = 0, CharIndex = 0, QuoteMode = 0;
			char CurrentChar;
			//****************************************
			
			if (arguments.Length > 0)
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
			if (!commandSet.PrepareCommand(RawParams.ToArray(), terminal, token, out var Command, out var OutParams))
			{
				// Failed, so try and pass the whole argument text as the first argument, no quoting
				if (RawParams.Count <= 1 || !commandSet.PrepareCommand(new[] { arguments }, terminal, token, out Command, out OutParams))
				{
					terminal.LogInformation("{0}{1} does not accept the given arguments", path, commandSet.Name);

					return false;
				}
			}

			//****************************************

			await Command.InvokeAsync(terminal, instance, OutParams);

			return true;
		}
	}
}
