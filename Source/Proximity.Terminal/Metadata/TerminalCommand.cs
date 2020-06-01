using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
//****************************************

namespace Proximity.Terminal.Metadata
{
	/// <summary>
	/// Represents a Terminal Command
	/// </summary>
	public sealed class TerminalCommand
	{
		internal TerminalCommand(MethodInfo method, TerminalBindingAttribute binding)
		{
			Name = binding.Name ?? method.Name;
			Method = method;
			
			Description = binding.Description;
			
			if (method.ReturnType != typeof(Task) && method.ReturnType != typeof(void) && method.ReturnType != typeof(ValueTask))
				throw new FormatException($"Command Return Type for {method.DeclaringType.FullName}.{method.Name} must be Void or Task/ValueTask");
		}

		//****************************************

		/// <summary>
		/// Synchronously invokes a command
		/// </summary>
		/// <param name="instance">The instance this command should be called on, if any</param>
		/// <param name="arguments">The arguments to pass to the command</param>
		public void Invoke(ITerminal terminal, object instance, object[] arguments)
		{
			if (instance != null && !Method.DeclaringType.IsInstanceOfType(instance))
				throw new ArgumentException("Instance is invalid for this Command");
			
			if (Debugger.IsAttached)
			{
				if (Method.ReturnType == typeof(Task))
					((Task)Method.Invoke(instance, arguments)).Wait();
				else if (Method.ReturnType == typeof(ValueTask))
					((ValueTask)Method.Invoke(instance, arguments)).AsTask().Wait();
				else
					Method.Invoke(instance, arguments);
			}
			else
			{
				try
				{
					if (Method.ReturnType == typeof(Task))
						((Task)Method.Invoke(instance, arguments)).Wait();
					else if (Method.ReturnType == typeof(ValueTask))
						((ValueTask)Method.Invoke(instance, arguments)).AsTask().Wait();
					else
						Method.Invoke(instance, arguments);
				}
				catch (TargetInvocationException x)
				{
					terminal.LogError(x.InnerException, "Failure running command");
				}
			}
		}

		/// <summary>
		/// Asynchronously invokes a command
		/// </summary>
		/// <param name="instance">The instance this command should be called on, if any</param>
		/// <param name="arguments">The arguments to pass to the command</param>
		/// <returns>A task that completes with the result of the command</returns>
		public async ValueTask InvokeAsync(ITerminal terminal, object instance, object[] arguments)
		{
			if (instance != null && !Method.DeclaringType.IsInstanceOfType(instance))
				throw new ArgumentException("Instance is invalid for this Command");

			if (Debugger.IsAttached)
			{
				if (Method.ReturnType == typeof(Task))
					await(Task)Method.Invoke(instance, arguments);
				else if (Method.ReturnType == typeof(ValueTask))
					await(ValueTask)Method.Invoke(instance, arguments);
				else
					Method.Invoke(instance, arguments);
			}
			else
			{
				try
				{
					if (Method.ReturnType == typeof(Task))
						await(Task)Method.Invoke(instance, arguments);
					else if (Method.ReturnType == typeof(ValueTask))
						await(ValueTask)Method.Invoke(instance, arguments);
					else
						Method.Invoke(instance, arguments);
				}
				catch (TargetInvocationException e)
				{
					terminal.LogError(e.InnerException, "Failure running command");
				}
				catch (AggregateException e)
				{
					terminal.LogError(e.InnerException, "Failure running command");
				}
				catch (Exception e)
				{
					terminal.LogError(e, "Internal failure running command");
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
