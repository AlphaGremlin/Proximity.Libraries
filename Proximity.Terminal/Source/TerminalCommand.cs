/****************************************\
 TerminalCommand.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Description of TerminalCommand.
	/// </summary>
	public sealed class TerminalCommand
	{	//****************************************
		private readonly string _Name;
		private readonly MethodInfo _Method;
		private readonly bool _IsTask;
		
		private readonly string _Description;
		//****************************************
		
		internal TerminalCommand(MethodInfo method, TerminalBindingAttribute binding)
		{
			_Name = binding.Name ?? method.Name;
			_Method = method;
			
			_Description = binding.Description;
			
			if (method.ReturnType == typeof(Task))
				_IsTask = true;
			else if (method.ReturnType != typeof(void))
				throw new FormatException("Return type must be Void or Task");
		}
		
		//****************************************
		
		public void Invoke(object instance, object[] arguments)
		{
			if (instance != null && !_Method.DeclaringType.IsInstanceOfType(instance))
				throw new ArgumentException("Instance is invalid for this Command");
			
			if (Debugger.IsAttached)
			{
				if (_IsTask)
					((Task)_Method.Invoke(instance, arguments)).Wait();
				else
					_Method.Invoke(instance, arguments);
			}
			else
			{
				try
				{
					if (_IsTask)
						((Task)_Method.Invoke(instance, arguments)).Wait();
					else
						_Method.Invoke(instance, arguments);
				}
				catch (TargetInvocationException x)
				{
					Log.Exception(x.InnerException, "Failure running command");
				}
			}
		}
		
		public async Task InvokeAsync(object instance, object[] arguments)
		{
			if (instance != null && !_Method.DeclaringType.IsInstanceOfType(instance))
				throw new ArgumentException("Instance is invalid for this Command");
			
			if (Debugger.IsAttached)
			{
				if (_IsTask)
					await (Task)_Method.Invoke(instance, arguments);
				else
					await Task.Run(() => _Method.Invoke(instance, arguments));
			}
			else
			{
				try
				{
					if (_IsTask)
						await (Task)_Method.Invoke(instance, arguments);
					else
						await Task.Run(() => _Method.Invoke(instance, arguments));
				}
				catch (TargetInvocationException x)
				{
					Log.Exception(x.InnerException, "Failure running command");
				}
			}
		}
		
		//****************************************
		
		public string Name
		{
			get { return _Name; }
		}
		
		public MethodInfo Method
		{
			get { return _Method; }
		}
		
		public string Description
		{
			get { return _Description; }
		}
		
	}
}
