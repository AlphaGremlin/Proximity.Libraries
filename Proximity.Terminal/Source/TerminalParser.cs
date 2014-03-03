﻿/****************************************\
 TerminalParser.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides parsing functionality for the terminal input
	/// </summary>
	public static class TerminalParser
	{
		public static Task<bool> Execute(string command, TerminalRegistry registry)
		{
			return Execute(command, new TerminalRegistry[] { registry });
		}
		
		public static Task<bool> Execute(string command, params TerminalRegistry[] registries)
		{	//****************************************
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
			CharIndex = command.IndexOfAny(new char[] {' ', '='});
			
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
				foreach(var MyRegistry in registries)
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
					Log.Error("{0} is not an Instance Type", CommandText);
					
					return Task.FromResult<bool>(false);
				}
				
				if (MyInstance == null)
				{
					Log.Error("{0} is not a known Instance of {1}", InstanceName, MyTypeSet.TypeName);
					
					return Task.FromResult<bool>(false);
				}
				
				CurrentPath = string.Format("{0}.{1}!", MyTypeSet.TypeName, InstanceName);
			}
			
			//****************************************
			
			if (MyTypeSet == null)
			{
				// Attempt to execute it as a command or variable
				MyResult = TryExecute(registries, CurrentPath, null, CommandText, ArgumentText);
				
				if (MyResult != null)
					return MyResult;
				
				// Perhaps it's an instance type, and we're calling the default instance
				foreach(var MyRegistry in registries)
				{
					MyTypeSet = MyRegistry.FindTypeSet(CommandText);
					
					if (MyTypeSet != null)
						break;
				}
				
				// Do we have a match at all?
				if (MyTypeSet == null)
				{
					Log.Error("{0} is not a Command, Variable, or Instance Type", CommandText);
					
					return Task.FromResult<bool>(false);
				}
				
				// If there are no arguments, we should list all the instances that match
				if (ArgumentText == null)
				{
					// TODO: List Named Instances
					
					return Task.FromResult<bool>(true);
				}
				
				// We have arguments, so is there a default instance?
				MyInstance = MyTypeSet.Default;
				
				if (MyInstance == null)
				{
					Log.Error("{0} does not have a default instance", MyTypeSet.TypeName);
					
					return Task.FromResult<bool>(false);
				}
				
				CurrentPath = MyTypeSet.TypeName + "!";
			}
			
			//****************************************
			
			// We're calling an Instance Type, are there any arguments?
			if (ArgumentText == null)
			{
				// TODO: display the instance details (ToString, available commands and variables)
				
				return Task.FromResult<bool>(true);
			}
			
			// Repeat the argument process on the arguments themselves
			CharIndex = ArgumentText.IndexOfAny(new char[] {' ', '='});
			
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
			
			Log.Error("{0}{1} is not a Command or Variable", CurrentPath, CommandText);
			
			return Task.FromResult<bool>(false);
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
			string CommandText, InstanceName = null, PartialText;
			
			int CharIndex;
			
			TerminalTypeSet MyTypeSet = null;
			TerminalInstance MyInstance = null;
			
			var PartialMatches = new List<string>();
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
				}
				
				// If the instance type doesn't match, return the partial command as is
				if (MyTypeSet == null)
					return partialCommand;
				
				if (InstanceName == null)
				{
					MyInstance = MyTypeSet.Default;
				}
				else
				{
					MyInstance = MyTypeSet.GetNamedInstance(InstanceName);
				}
				
				// If the instance doesn't exist, return as is
				if (MyInstance == null)
					return partialCommand;
				
				// Add matching commands
				foreach (string commmandName in MyInstance.Type.Commands)
				{
					if (commmandName.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(string.Format("{0}.{1} {2}", MyTypeSet.TypeName, MyInstance.Name, commmandName));
				}
				
				// Add matching variables
				foreach (string variableName in MyInstance.Type.Variables)
				{
					if (variableName.StartsWith(PartialText, StringComparison.InvariantCultureIgnoreCase))
						PartialMatches.Add(string.Format("{0}.{1} {2}", MyTypeSet.TypeName, MyInstance.Name, variableName));
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
						return partialCommand;
					
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
						foreach (string commmandName in MyRegistry.Commands)
						{
							if (commmandName.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
								PartialMatches.Add(commmandName);
						}
						
						// Add matching properties (with an equals sign, so they can't be the same as commands)
						foreach (string propertyName in MyRegistry.Variables)
						{
							if (propertyName.StartsWith(partialCommand, StringComparison.InvariantCultureIgnoreCase))
								PartialMatches.Add(propertyName + "=");
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
						return NextCommand;
				}
				// Nothing greater, go back to the start
			}
			
			return PartialMatches[0];
		}
		
		public static object FindCommand(string command, params TerminalRegistry[] registries)
		{	//****************************************
			string CommandText, InstanceName = null, InstanceType;
			
			int CharIndex;
			
			TerminalTypeSet MyTypeSet = null;
			TerminalInstance MyInstance = null;
			
			var PartialMatches = new List<string>();
			//****************************************
			
			// Find the first word (split on a space)
			CharIndex = command.IndexOf(' ');
			
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
					Log.Error("{0} is not a known Instance of {1}", instance.Name, instance.Type.Name);
					
					return Task.FromResult<bool>(false);
				}
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
					if (!MyVariable.SetValue(MyInstance, argumentText))
						Log.Error("{0}{1} is of type {2}", path, MyVariable, MyVariable.Type);
				}
				else
				{
					Log.Info("{0}{1}={2}", path, MyVariable, MyVariable.GetValue(MyInstance));
					
					return Task.FromResult<bool>(true);
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
			int LastIndex = 0, CharIndex = 0, QuoteMode = 0;
			TerminalCommand MyCommand;
			object[] OutParams;
			//****************************************
			
			if (argumentText != null)
			{
				for(; CharIndex < argumentText.Length; CharIndex++)
				{
					switch (argumentText[CharIndex])
					{
					case ' ':
						if (QuoteMode != 0)
							continue;
						
						if (CharIndex != LastIndex)
							RawParams.Add(argumentText.Substring(LastIndex, CharIndex - LastIndex));
						
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
							
							RawParams.Add(argumentText.Substring(LastIndex, CharIndex - LastIndex));
							
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
							
							RawParams.Add(argumentText.Substring(LastIndex, CharIndex - LastIndex));
							
							LastIndex = CharIndex + 1;
						}
						break;
						
					case '\\': // Skip the next character
						argumentText = argumentText.Remove(CharIndex, 1);
						break;
					}
				}
				
				// Add the final parameter
				if (LastIndex != argumentText.Length)
					RawParams.Add(argumentText.Substring(LastIndex));
			}
			
			//****************************************
			
			// Try with the broken up arguments
			MyCommand = commandSet.FindCommand(RawParams.ToArray(), out OutParams);
			
			// If that fails, try and pass the whole argument text as the first argument
			if (MyCommand == null)
				MyCommand = commandSet.FindCommand(new string[] { argumentText }, out OutParams);
				
			if (MyCommand == null)
			{
				Log.Error("{0}{1} does not accept the given arguments", path, commandSet.Name);
				
				return false;
			}
			
			//****************************************
			
			await MyCommand.InvokeAsync(instance, OutParams);
			
			return true;
		}
		
		//****************************************
		
		public static TerminalRegistry[] Context
		{
			get { return (TerminalRegistry[])CallContext.LogicalGetData("Terminal.Parser.Context"); }
			private set { CallContext.LogicalSetData("Terminal.Parser.Context", value); }
		}
	}
}
