using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Terminal.Metadata
{
	/// <summary>
	/// Represents a Terminal Command
	/// </summary>
	public sealed class TerminalCommand
	{ //****************************************
		private readonly bool _IsTask;
		//****************************************

		internal TerminalCommand(MethodInfo method, TerminalBindingAttribute binding)
		{
			Name = binding.Name ?? method.Name;
			Method = method;
			
			Description = binding.Description;
			
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
			if (instance != null && !Method.DeclaringType.IsInstanceOfType(instance))
				throw new ArgumentException("Instance is invalid for this Command");
			
			if (Debugger.IsAttached)
			{
				if (_IsTask)
					((Task)Method.Invoke(instance, arguments)).Wait();
				else
					Method.Invoke(instance, arguments);
			}
			else
			{
				try
				{
					if (_IsTask)
						((Task)Method.Invoke(instance, arguments)).Wait();
					else
						Method.Invoke(instance, arguments);
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
		public ValueTask InvokeAsync(object instance, object[] arguments)
		{
			return InternalInvokeAsync(instance, arguments);
		}

		//****************************************

		internal async ValueTask InternalInvokeAsync(object instance, object[] arguments)
		{
			if (instance != null && !Method.DeclaringType.IsInstanceOfType(instance))
				throw new ArgumentException("Instance is invalid for this Command");
			
			if (Debugger.IsAttached)
			{
				if (_IsTask)
					await (Task)Method.Invoke(instance, arguments);
				else
					await Task.Run(() => Method.Invoke(instance, arguments));
			}
			else
			{
				try
				{
					if (_IsTask)
						await (Task)Method.Invoke(instance, arguments);
					else
						await Task.Run(() => Method.Invoke(instance, arguments));
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
		public string Name { get; }

		/// <summary>
		/// Gets the underlying Method this Command invokes
		/// </summary>
		public MethodInfo Method { get; }

		/// <summary>
		/// Gets a description of this Command
		/// </summary>
		public string Description { get; }
	}
}
