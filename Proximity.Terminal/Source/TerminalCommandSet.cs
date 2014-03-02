/****************************************\
 TerminalVariable.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Description of TerminalCommandSet.
	/// </summary>
	public sealed class TerminalCommandSet
	{	//****************************************
		private readonly TerminalType _Parent;
		private readonly string _Name;
		private readonly List<TerminalCommand> _Commands = new List<TerminalCommand>();
		//****************************************
		
		internal TerminalCommandSet(TerminalType parent, string name)
		{
			_Parent = parent;
			_Name = name;
		}
		
		//****************************************
		
		public TerminalCommand AddOverload(MethodInfo method, TerminalBindingAttribute binding)
		{	//****************************************
			var NewCommand = new TerminalCommand(method, binding);
			//****************************************
			
			_Commands.Add(NewCommand);
			
			return NewCommand;
		}
		
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
							throw new InvalidCastException();
						
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
			}
			
			//****************************************
			
			outArgs = null;
			
			return null;
		}
		
		//****************************************
		
		public string Name
		{
			get { return _Name; }
		}
	}
}
