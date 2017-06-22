/****************************************\
 TerminalCommand.cs
 Created: 2014-02-28
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a Terminal Command
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

		/// <summary>
		/// Synchronously invokes a command
		/// </summary>
		/// <param name="instance">The instance this command should be called on, if any</param>
		/// <param name="arguments">The arguments to pass to the command</param>
		[SecurityCritical]
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

		/// <summary>
		/// Asynchronously invokes a command
		/// </summary>
		/// <param name="instance">The instance this command should be called on, if any</param>
		/// <param name="arguments">The arguments to pass to the command</param>
		/// <returns>A task that completes with the result of the command</returns>
		[SecurityCritical]
		public Task InvokeAsync(object instance, object[] arguments)
		{
			return InternalInvokeAsync(instance, arguments);
		}

		//****************************************

		internal async Task InternalInvokeAsync(object instance, object[] arguments)
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
				catch (TargetInvocationException e)
				{
					Log.Exception(e.InnerException, "Failure running command");
				}
				catch (AggregateException e)
				{
					Log.Exception(e.InnerException, "Failure running command");
				}
				catch (Exception e)
				{
					Log.Exception(e, "Internal failure running command");
				}
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the name of this Command
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
		
		/// <summary>
		/// Gets the underlying Method this Command invokes
		/// </summary>
		public MethodInfo Method
		{
			get { return _Method; }
		}
		
		/// <summary>
		/// Gets a description of this Command
		/// </summary>
		public string Description
		{
			get { return _Description; }
		}
	}
}
