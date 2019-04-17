using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides parsing functionality for the terminal input
	/// </summary>
	public static class TerminalParser
	{ //****************************************
		private static AsyncLocal<TerminalRegistry[]> _Context = new AsyncLocal<TerminalRegistry[]>();
		//****************************************

		/// <summary>
		/// Outputs usage information for a terminal object
		/// </summary>
		/// <param name="typeData">A <see cref="TerminalTypeSet"/>, <see cref="TerminalInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" /></param>
		public static void HelpOn(object typeData)
		{
			if (typeData is TerminalTypeSet MyTypeSet)
			{
				using (Log.VerboseSection("Usage information for '{0}':", MyTypeSet.TypeName))
				{
					if (MyTypeSet.Default?.Target != null)
					{
						if (MyTypeSet.Default.Type.Commands.Count() != 0)
						{
							using (Log.VerboseSection("Available Default Commands:"))
							{
								Log.Info(string.Join(", ", MyTypeSet.Default.Type.Commands.OrderBy(set => set)));
							}
						}
						
						if (MyTypeSet.Default.Type.Variables.Count() != 0)
						{
							using (Log.VerboseSection("Available Default Variables:"))
							{
								Log.Info(string.Join(", ", MyTypeSet.Default.Type.Variables.OrderBy(var => var)));
							}
						}
					}
					
					if (MyTypeSet.Instances.Count() != 0)
					{
						using (Log.VerboseSection("Available Instances:"))
						{
							Log.Info(string.Join(", ", MyTypeSet.Instances.OrderBy(name => name)));
						}
					}
				}
			}
			else if (typeData is TerminalInstance MyInstance)
			{
				Log.Info("Instance: {0}", MyInstance.Target);

				if (MyInstance.Type.Commands.Count() != 0)
				{
					using (Log.VerboseSection("Available Commands:"))
					{
						Log.Info(string.Join(", ", MyInstance.Type.Commands.OrderBy(set => set)));
					}
				}

				if (MyInstance.Type.Variables.Count() != 0)
				{
					using (Log.VerboseSection("Available Variables:"))
					{
						Log.Info(string.Join(", ", MyInstance.Type.Variables.OrderBy(var => var)));
					}
				}
			}
			else if (typeData is TerminalCommandSet MyCommandSet)
			{
				using (Log.VerboseSection("Available Overloads:"))
				{
					foreach (var MyCommand in MyCommandSet.Commands)
					{
						Log.Info("{0}({1})\t{2}", MyCommand.Name, string.Join(", ", MyCommand.Method.GetParameters().Select(param => string.Format("{0}: {1}", param.Name, param.ParameterType.Name))), MyCommand.Description);
					}
				}
			}
			else if (typeData is TerminalVariable MyVariable)
			{
				Log.Info("{0}: {1}\t{2}", MyVariable.Name, MyVariable.Type.Name, MyVariable.Description);
			}
		}

		/// <summary>
		/// Parses and executes a terminal command
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <param name="registry">The command registry to use</param>
		/// <returns>A task representing the execution result. True if the command ran successfully, otherwise False</returns>
		[SecurityCritical]
		public static Task<bool> Execute(string command, TerminalRegistry registry)
		{
			return InternalExecute(command, new TerminalRegistry[] { registry });
		}

		/// <summary>
		/// Parses and executes a terminal command
		/// </summary>
		/// <param name="command">The command to execute</param>
		/// <param name="registries">One or more command registries to use</param>
		/// <returns>A task representing the execution result. True if the command ran successfully, otherwise False</returns>
		[SecurityCritical]
		public static Task<bool> Execute(string command, params TerminalRegistry[] registries)
		{
			return InternalExecute(command, registries);
		}

		/// <summary>
		/// Finds the next command for auto completion
		/// </summary>
		/// <param name="partialCommand">The partial command being completed</param>
		/// <param name="lastResult">The last result returned. Null to get the first match</param>
		/// <param name="registries">The list of registries to search for matches</param>
		/// <returns>The next command matching the partial string</returns>
		public static string FindNextCommand(string partialCommand, string lastResult, params TerminalRegistry[] registries)
		{	//****************************************
			string CommandText, InstanceName = null, PartialText, Prefix = "";
			
			int CharIndex;
			
			TerminalTypeSet MyTypeSet = null;
			TerminalInstance MyInstance = null;
			
			var PartialMatches = new List<string>();
			//****************************************
			
			if (partialCommand.StartsWith("help ", StringComparison.InvariantCultureIgnoreCase))
			{
				partialCommand = partialCommand.Substring(5);
				
				Prefix = "Help ";
			}
			
			//****************************************
			
			// Find the first word (split on a space)
			CharIndex = partialCommand.IndexOf(' ');
			
			// If there's a space, we're parsing an Instance Type and optional Instance Name, with a partial Command/Variable
			if (CharIndex != -1)
			{
				CommandText = partialCommand.Substring(0, CharIndex);
				PartialText = partialCommand.Substring(CharIndex + 1);
				
				CharIndex = CommandText.IndexOf('.');
				
				// Split into Type and Name if necessary
				if (CharIndex != -1)
				{
					InstanceName = CommandText.Substring(CharIndex + 1);
					CommandText = CommandText.Substring(0, CharIndex);
				}
				
				foreach(var MyRegistry in registries)
				{
					MyTypeSet = MyRegistry.FindTypeSet(CommandText);

					if (MyTypeSet != null)
						break;
				}
				
				// If the instance type doesn't match, return the partial command as is
				if (MyTypeSet == null)
					return Prefix + partialCommand;
				
				if (InstanceName == null)
				{
					MyInstance = MyTypeSet.Default;
					InstanceName = MyTypeSet.TypeName;
				}
				else
				{
					MyInstance = MyTypeSet.GetNamedInstance(InstanceName);

					if (MyInstance == null)
						return Prefix + partialCommand;

					InstanceName = string.Format("{0}.{1}", MyTypeSet.TypeName, MyInstance.Name);
				}
				
				// If the instance doesn't exist, return as is
				if (MyInstance == null)
					return Prefix + partialCommand;
				
				// Add matching commands
				foreach (var MyCommand in MyInstance.Type.Commands)
				{
					if (MyCommand.Name.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(string.Format("{0} {1}", InstanceName, MyCommand.Name));
				}
				
				// Add matching variables
				foreach (var MyVariable in MyInstance.Type.Variables)
				{
					if (MyVariable.Name.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(string.Format("{0} {1}", InstanceName, MyVariable.Name));
				}
			}
			else
			{
				CharIndex = partialCommand.IndexOf('.');
				
				// If there's a dot, we're parsing an Instance Type, with a partial Instance Name
				if (CharIndex != -1)
				{
					CommandText = partialCommand.Substring(0, CharIndex);
					PartialText = partialCommand.Substring(CharIndex + 1);

					foreach(var MyRegistry in registries)
					{
						MyTypeSet = MyRegistry.FindTypeSet(CommandText);
					}
					
					// If the instance type doesn't match, return the partial command as is
					if (MyTypeSet == null)
						return Prefix + partialCommand;
					
					foreach(var MyInstanceName in MyTypeSet.Instances)
					{
						if (MyInstanceName.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
							PartialMatches.Add(string.Format("{0}.{1}", MyTypeSet.TypeName, MyInstanceName));
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
				foreach(string NextCommand in PartialMatches)
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
		/// <returns>A <see cref="TerminalTypeSet"/>, <see cref="TerminalInstance" />, <see cref="TerminalCommandSet" /> or <see cref="TerminalVariable" />, or null if no match was found</returns>
		public static object FindCommand(string command, params TerminalRegistry[] registries)
		{	//****************************************
			string CommandText, InstanceName = null, InstanceType;
			
			int CharIndex;
			
			TerminalTypeSet MyTypeSet = null;
			TerminalInstance MyInstance = null;
			
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
					MyTypeSet = MyRegistry.FindTypeSet(InstanceType);
				}
				
				if (MyTypeSet == null)
					return null;
				
				if (InstanceName == null)
				{
					MyInstance = MyTypeSet.Default;
				}
				else
				{
					MyInstance = MyTypeSet.GetNamedInstance(InstanceName);
				}
				
				// If the instance doesn't exist, return the type set
				if (MyInstance == null)
					return MyTypeSet;
				
				var MyVariable = MyInstance.Type.FindVariable(CommandText);
				
				if (MyVariable != null)
					return MyVariable;
				
				return MyInstance.Type.FindCommand(CommandText);
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
					MyTypeSet = MyRegistry.FindTypeSet(InstanceType);
				}
				
				if (MyTypeSet == null)
					return null;
				
				MyInstance = MyTypeSet.GetNamedInstance(CommandText);
				
				if (MyInstance != null)
					return MyInstance;
				
				return MyTypeSet;
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
				
				MyTypeSet = MyRegistry.FindTypeSet(command);
				
				if (MyTypeSet != null)
					return MyTypeSet;
			}
			
			return null;
		}

		//****************************************

		internal static Task<bool> InternalExecute(string command, params TerminalRegistry[] registries)
		{ //****************************************
			string CommandText, ArgumentText;
			string InstanceName, CurrentPath = "";

			int CharIndex;
			char WordDivider;

			TerminalTypeSet MyTypeSet = null;
			TerminalInstance MyInstance = null;
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
		
		//****************************************
		
		/// <summary>
		/// When executing a command, gets the registries that serve as context
		/// </summary>
		public static TerminalRegistry[] Context
		{
			get => _Context.Value;
			private set => _Context.Value = value;
		}
	}
}
