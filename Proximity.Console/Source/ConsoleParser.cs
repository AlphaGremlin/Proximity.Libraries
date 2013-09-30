/****************************************\
 ConsoleParser.cs
 Created: 30-01-2008
\****************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Proximity.Utility;
//****************************************

namespace Proximity.Console
{
	/// <summary>
	/// Parses console commands and executes them
	/// </summary>
	public static class ConsoleParser
	{	//****************************************
		private static Dictionary<string, CommandSet> CommandList = new Dictionary<string, CommandSet>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<string, PropertyInfo> PropertyList = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
		//****************************************
		
		/// <summary>
		/// Initialises the console Parser
		/// </summary>
		/// <remarks>This must be called before <see cref="Execute" /> will work</remarks>
		public static void Init()
		{
			AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
			
			foreach(Assembly MyAssembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach(Type NewType in MyAssembly.GetTypes())
					{
						OnProcessConsole(NewType);
					}
				}
				catch (ReflectionTypeLoadException)
				{
				}
			}
		}
		
		private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			if (args.LoadedAssembly.ManifestModule.Assembly is AssemblyBuilder)
				return;
			
			try
			{
				foreach(Type NewType in args.LoadedAssembly.GetTypes())
				{
					OnProcessConsole(NewType);
				}
			}
			catch (ReflectionTypeLoadException)
			{
			}
		}		
		
		private static void OnProcessConsole(Type newType)
		{	//****************************************
			object[] Attribs;
			//****************************************
			
			Attribs = newType.GetCustomAttributes(typeof(ConsoleProviderAttribute), false);
			
			if (Attribs.Length == 0)
				return;
			
			foreach(MethodInfo MyMethod in newType.GetMethods())
			{
				Attribs = MyMethod.GetCustomAttributes(typeof(ConsoleBindingAttribute), false);
				
				if (Attribs.Length == 0)
					continue;
				
				if (CommandList.ContainsKey(MyMethod.Name))
					CommandList[MyMethod.Name].AddOverload(MyMethod);
				else
					CommandList.Add(MyMethod.Name, new CommandSet(MyMethod));
			}
			
			foreach(PropertyInfo MyProperty in newType.GetProperties())
			{
				Attribs = MyProperty.GetCustomAttributes(typeof(ConsoleBindingAttribute), false);
				
				if (Attribs.Length == 0)
					continue;
				
				if (PropertyList.ContainsKey(MyProperty.Name))
					Log.Warning("Duplicate property {0} in provider {1}", MyProperty.Name, newType.FullName);
				else
					PropertyList.Add(MyProperty.Name, MyProperty);
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Parses and executes a console command
		/// </summary>
		/// <param name="cmdLine">The command to execute</param>
		/// <remarks>Commands will be executed on the current thread</remarks>
		public static void Execute(string cmdLine)
		{	//****************************************
			string CommandName;
			object Value;

			int CharIndex, LastIndex;
			int QuoteMode;
			
			CommandSet MyCommand;
			
			List<string> RawParams;
			//****************************************
			
			Log.Write(new ConsoleLogEntry(string.Format("> {0}", cmdLine)));
			
			CharIndex = cmdLine.IndexOfAny(new char[] {' ', '='});
			
			if (CharIndex == -1)
				CommandName = cmdLine;
			else
				CommandName = cmdLine.Substring(0, CharIndex);
			
			//****************************************
			// Identify the command being used
			
			if(!CommandList.TryGetValue(CommandName, out MyCommand))
			{
				if (CharIndex == 0 && PropertyList.ContainsKey(CommandName))
				{
					if (!ConsoleParser.GetVariable(CommandName, out Value))
						return;
					
					if (Value == null)
						Log.Info("Variable has no value");
					else
						Log.Info(Value.ToString());
				}
				
				if (CharIndex > 0 && cmdLine.Substring(CharIndex, 1) == "=")
				{
					ConsoleParser.SetVariable(CommandName, cmdLine.Substring(CharIndex + 1));
					
					return;
				}
				
				Log.Warning("Unknown command '{0}', please use 'help' for assistance", CommandName);
				
				return;
			}
			
			//****************************************
			// Do we have any parameters?
			
			if (CharIndex == -1)
			{
				if (!MyCommand.AttemptInvoke(null))
					StandardCommands.Help(CommandName);
				
				return;
			}
			
			//****************************************
			// First, build a parameter list
			
			RawParams = new List<string>();
			
			LastIndex = CharIndex + 1;
			QuoteMode = 0;
			
			for(LastIndex = CharIndex; CharIndex < cmdLine.Length; CharIndex++)
			{
				switch (cmdLine[CharIndex])
				{
				case ' ':
					if (QuoteMode != 0)
						continue;
					
					if (CharIndex != LastIndex)
						RawParams.Add(cmdLine.Substring(LastIndex, CharIndex - LastIndex));
					
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
						
						RawParams.Add(cmdLine.Substring(LastIndex, CharIndex - LastIndex));
						
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
						
						RawParams.Add(cmdLine.Substring(LastIndex, CharIndex - LastIndex));
						
						LastIndex = CharIndex + 1;
					}
					break;
				}
			}
			
			// Add the final parameter
			
			if (LastIndex != cmdLine.Length)
				RawParams.Add(cmdLine.Substring(LastIndex));
			
			//****************************************
			// Invoke the method
			
			if (MyCommand.AttemptInvoke(RawParams.ToArray()))
				return;
			
			//****************************************
			// Final attempt, pass entire command line
			
			if (MyCommand.AttemptInvoke(new string[] {cmdLine.Substring(CommandName.Length + 1)}))
				return;
			
			//****************************************
			
			StandardCommands.Help(CommandName);
		}

		/// <summary>
		/// Finds the next command for auto completion
		/// </summary>
		/// <param name="partialName">The partial command being completed</param>
		/// <param name="lastResult">The last result returned. Null to get the first match</param>
		/// <returns>The next command matching the partial string</returns>
		public static string FindNextCommand(string partialName, string lastResult)
		{	//****************************************
			List<string> PartialMatches = new List<string>();
			//****************************************
			
			// Add matching commands
			foreach (string commmandName in CommandList.Keys)
			{
				if (commmandName.StartsWith(partialName, StringComparison.InvariantCultureIgnoreCase))
					PartialMatches.Add(commmandName);
			}
			
			// Add matching properties (with an equals sign, so they can't be the same as commands)
			foreach (string propertyName in PropertyList.Keys)
			{
				if (propertyName.StartsWith(partialName, StringComparison.InvariantCultureIgnoreCase))
					PartialMatches.Add(propertyName + "=");
			}
			
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
		
		//****************************************

		internal static IList<string> FindCommand(string commandName)
		{	//***************************************
			CommandSet MyCommand;
			//****************************************
			
			if(!CommandList.TryGetValue(commandName, out MyCommand))
				return new string[] {};
			
			return MyCommand.ListOverloads();
		}
		
		internal static List<string> ListCommands()
		{	//****************************************
			List<string> CommandNames;
			//****************************************
			
			CommandNames = new List<string>(CommandList.Count);
			
			CommandNames.AddRange(CommandList.Keys);
			
			CommandNames.Sort();
			
			return CommandNames;
		}
		
		internal static void SetVariable(string varName, string newValue)
		{	//****************************************
			PropertyInfo MyVariable;
			
			object Value;
			//****************************************
						
			if (!PropertyList.TryGetValue(varName, out MyVariable))
			{
				Log.Warning("Variable '{0}' is unknown", varName);
				
				return;
			}
			
			//****************************************
			
			if (!MyVariable.CanWrite)
			{
				Log.Warning("Variable '{0}' cannot be changed", MyVariable.Name);
				
				return;
			}
			
			//****************************************
			
			try
			{	
				Value = Convert.ChangeType(newValue, MyVariable.PropertyType);
				
				MyVariable.SetValue(null, Value, null);
			}
			catch(FormatException)
			{
				Log.Warning("Invalid value. Variable '{0}' is of type {1}", MyVariable.Name, MyVariable.PropertyType.Name);
			}
			catch (TargetInvocationException x)
			{
				Log.Exception(x.InnerException, "Failed to set Variable");
			}
		}
		
		internal static void ClearVariable(string varName)
		{	//****************************************
			PropertyInfo MyVariable;
			//****************************************
						
			if (!PropertyList.TryGetValue(varName, out MyVariable))
			{
				Log.Warning("Variable '{0}' is unknown", varName);
				
				return;
			}
						
			//****************************************
			
			if (!MyVariable.CanWrite)
			{
				Log.Warning("Variable '{0}' cannot be changed", MyVariable.Name);
				
				return;
			}
			
			//****************************************
			
			try
			{	
				MyVariable.SetValue(null, null, null);
			}
			catch (TargetException)
			{
				Log.Warning("Invalid value. Variable '{0}' cannot be cleared", MyVariable.Name);
			}
			catch (TargetInvocationException x)
			{
				Log.Exception(x.InnerException, "Failed to set Variable");
			}			
		}
		
		internal static bool GetVariable(string varName, out object oldValue)
		{	//****************************************
			PropertyInfo MyVariable;
			//****************************************
			
			if (!PropertyList.TryGetValue(varName, out MyVariable))
			{
				Log.Warning("Unknown variable '{0}'", varName);
				
				oldValue = null;
				
				return false;
			}
			
			//****************************************
			
			oldValue = MyVariable.GetValue(null, null);
			
			return true;
		}
		
		internal static List<string> ListVariables()
		{	//****************************************
			List<string> VariableNames;
			//****************************************
			
			VariableNames = new List<string>(PropertyList.Count);
			
			VariableNames.AddRange(PropertyList.Keys);
			
			VariableNames.Sort();
			
			return VariableNames;
		}
		
		//****************************************
		
		internal static PropertyInfo[] RawListVariables()
		{	//****************************************
			PropertyInfo[] VariableData;
			//****************************************
			
			VariableData = new PropertyInfo[PropertyList.Count];
			
			PropertyList.Values.CopyTo(VariableData, 0);
			
			return VariableData;
		}
		
		internal static PropertyInfo RawGetVariable(string varName)
		{	//****************************************
			PropertyInfo MyVariable;
			//****************************************
						
			if (PropertyList.TryGetValue(varName, out MyVariable))
				return MyVariable;
			else
				return null;
		}
		
		//****************************************
		
		private class CommandSet
		{	//****************************************
			private MethodInfo[] MyMethods;
			//****************************************
			
			public CommandSet(MethodInfo newMethod)
			{
				MyMethods = new MethodInfo[] {newMethod};
			}
			
			//****************************************
			
			public void AddOverload(MethodInfo newOverload)
			{
				Array.Resize<MethodInfo>(ref MyMethods, MyMethods.Length + 1);
				
				MyMethods[MyMethods.Length - 1] = newOverload;
			}
			
			public IList<string> ListOverloads()
			{	//****************************************
				List<string> MySignatures;
				
				StringBuilder Signature = new StringBuilder();
				
				object[] Attribs;
				ConsoleBindingAttribute MethodDetails;
				//****************************************
			
				MySignatures = new List<string>(MyMethods.Length);
				
				foreach(MethodInfo MyMethod in MyMethods)
				{	
					Signature.Remove(0, Signature.Length).Append(MyMethod.Name);
					
					foreach(ParameterInfo MyParam in MyMethod.GetParameters())
					{
						Signature.Append(' ').Append(MyParam.Name);
					}
					
					Attribs = MyMethod.GetCustomAttributes(typeof(ConsoleBindingAttribute), false);
					MethodDetails = (ConsoleBindingAttribute)Attribs[0];
					
					Signature.Append(" - ").Append(MethodDetails.Description);
					
					MySignatures.Add(Signature.ToString());
				}
				
				MySignatures.Sort();
				
				return MySignatures.ToArray();
			}
			
			//****************************************
			
			public bool AttemptInvoke(string[] paramSet)
			{	//****************************************
				ParameterInfo[] MethodParams;
				object[] ParamData;
				TypeConverter MyConverter;
				//****************************************
				
				if (paramSet == null)
				{
					foreach(MethodInfo MyMethod in MyMethods)
					{
						if (MyMethod.GetParameters().Length == 0)
						{
							ConsoleInvoke(MyMethod, null);
							
							return true;
						}
					}

					return false;
				}
				
				//****************************************
				// Find a method matching the params
					
				ParamData = new object[paramSet.Length];
				
				foreach(MethodInfo MyMethod in MyMethods)
				{
					MethodParams = MyMethod.GetParameters();
					
					if (MethodParams.Length != ParamData.Length)
						continue;
					
					try
					{
						for(int Index = 0; Index < MethodParams.Length; Index++)
						{
							MyConverter = TypeDescriptor.GetConverter(MethodParams[Index].ParameterType);
								
							if (MyConverter == null)
								throw new InvalidCastException();
							
							ParamData[Index] = MyConverter.ConvertFromString(paramSet[Index]);
							//ParamData[Index] = Convert.ChangeType(paramSet[Index], MethodParams[Index].ParameterType);
						}
						
						ConsoleInvoke(MyMethod, ParamData);
						
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
				}
				
				return false;
			}
			
			public void ConsoleInvoke(MethodInfo myMethod, object[] myParams)
			{
				if (Debugger.IsAttached)
				{
					myMethod.Invoke(null, myParams);
				}
				else
				{
					try
					{
						myMethod.Invoke(null, myParams);
					}
					catch (TargetInvocationException x)
					{
						Log.Exception(x.InnerException, "Failure running command");
					}
				}
			}
		}
	}
}
