/****************************************\
 TerminalParser.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Description of TerminalParser.
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
			
			TerminalType MyType = null;
			object MyInstance = null;
			Task<bool> MyResult;
			//****************************************
			
			// Find the first word (split on a space, equals, or dot)
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
					MyType = MyRegistry.FindType(CommandText);
					
					if (MyType != null)
					{
						MyInstance = MyType.GetNamedInstance(InstanceName);
						break;
					}
				}
				
				if (MyType == null)
				{
					Log.Error("{0} is not an Instance Type", CommandText);
					
					return Task.FromResult<bool>(false);
				}
				
				if (MyInstance == null)
				{
					Log.Error("{0} is not a known Instance of {1}", InstanceName, MyType.InstanceType);
					
					return Task.FromResult<bool>(false);
				}
				
				CurrentPath = string.Format("{0}.{1}!", MyType.InstanceType, InstanceName);
			}
			
			//****************************************
			
			if (MyType == null)
			{
				// Attempt to execute it as a command or variable
				MyResult = TryExecute(registries, CurrentPath, null, CommandText, ArgumentText);
				
				if (MyResult != null)
					return MyResult;
				
				// Perhaps it's an instance type, and we're calling the default instance
				foreach(var MyRegistry in registries)
				{
					MyType = MyRegistry.FindType(CommandText);
					
					if (MyType != null)
						break;
				}
				
				// Do we have a match at all?
				if (MyType == null)
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
				MyInstance = MyType.DefaultInstance;
				
				if (MyInstance == null)
				{
					Log.Error("{0} does not have a default instance", CommandText);
					
					return Task.FromResult<bool>(false);
				}
				
				CurrentPath = MyType.InstanceType + "!";
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
		
		//****************************************
		
		private static Task<bool> TryExecute(TerminalRegistry[] registries, string path, object instance, string commandText, string argumentText)
		{
			// Check if it's a variable first, and either get or set it
			foreach(var MyRegistry in registries)
			{
				var MyVariable = MyRegistry.FindVariable(commandText);
				
				if (MyVariable == null)
					continue;
				
				if (argumentText != null)
				{
					// TODO: Set Variable
					
				}
				else
				{
					Log.Info("{0}{1}={2}", path, MyVariable, MyVariable.GetValue(instance));
					
					return Task.FromResult<bool>(true);
				}
			}
			
			// Check if it's a command and attempt to execute it
			foreach(var MyRegistry in registries)
			{
				var MyCommand = MyRegistry.FindCommand(commandText);
				
				if (MyCommand != null)
					return ExecuteCommand(path, instance, MyCommand, argumentText);
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
				}
			}
			
			// Add the final parameter
			if (LastIndex != argumentText.Length)
				RawParams.Add(argumentText.Substring(LastIndex));
			
			//****************************************
			
			MyCommand = commandSet.FindCommand(RawParams.ToArray(), out OutParams);
			
			if (MyCommand == null)
				MyCommand = commandSet.FindCommand(new string[] { argumentText }, out OutParams);
				
			if (MyCommand == null)
			{
				Log.Error("{0}{1} does not accept the given arguments", path, commandSet.Name);
				
				return false;
			}
			
			await MyCommand.InvokeAsync(instance, OutParams);
			
			return true;
		}
	}
}
