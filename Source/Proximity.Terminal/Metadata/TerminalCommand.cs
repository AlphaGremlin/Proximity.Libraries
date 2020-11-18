using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Threading;
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

			var Parameters = method.GetParameters();

			if (Parameters.Length > 0)
			{
				TakesTerminal = Parameters[0].ParameterType.IsAssignableFrom(typeof(ITerminal));

				TakesToken = Parameters[Parameters.Length - 1].ParameterType == typeof(CancellationToken);

				if (TakesTerminal)
				{
					if (TakesToken)
						ExternalParameters = Parameters.AsMemory(1, Parameters.Length - 2);
					else
						ExternalParameters = Parameters.AsMemory(1);
				}
				else
				{
					if (TakesToken)
						ExternalParameters = Parameters.AsMemory(0, Parameters.Length - 1);
					else
						ExternalParameters = Parameters.AsMemory();
				}
			}
		}

		//****************************************

		/// <summary>
		/// Synchronously invokes a command
		/// </summary>
		/// <param name="terminal">The terminal to log any failures to</param>
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
		/// <param name="terminal">The terminal to log any failures to</param>
		/// <param name="instance">The instance this command should be called on, if any</param>
		/// <param name="arguments">The arguments to pass to the command</param>
		/// <returns>A task that completes with the result of the command</returns>
		public async ValueTask InvokeAsync(ITerminal terminal, object? instance, object[] arguments)
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
		public string? Description { get; }

		/// <summary>
		/// Gets whether the first parameter is a Terminal
		/// </summary>
		public bool TakesTerminal { get; }

		/// <summary>
		/// Gets whether the last parameter is a Terminal
		/// </summary>
		public bool TakesToken { get; }

		/// <summary>
		/// Gets the required external parameters
		/// </summary>
		public ReadOnlyMemory<ParameterInfo> ExternalParameters { get; }
	}
}
