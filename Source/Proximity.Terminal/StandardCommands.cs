using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proximity.Terminal.Metadata;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides some standard console commands
	/// </summary>
	[TerminalProvider, CLSCompliant(false)]
	public static class StandardCommands
	{
		/// <summary>
		/// Outputs base-level command help
		/// </summary>
		/// <param name="terminal">The terminal to write help to</param>
		[TerminalBinding("Output a list of available commands, variables and instances")]
		public static void Help(ITerminal terminal)
		{
			terminal.LogInformation(TerminalParser.HelpOn(terminal.Registries));
		}

		/// <summary>
		/// Outputs help on a specific command
		/// </summary>
		/// <param name="terminal">The terminal to write help to</param>
		/// <param name="command">The command text</param>
		[TerminalBinding("Output brief information about a command, variable or instance")]
		public static void Help(ITerminal terminal, string command)
		{	//****************************************
			var MyTypeData = TerminalParser.FindCommand(command, terminal.Registries);
			//****************************************
			
			if (MyTypeData == null)
			{
				terminal.LogWarning("{0} is not a valid command, variable, or instance", command);
				
				return;
			}

			terminal.LogInformation(TerminalParser.HelpOn(MyTypeData));
		}

		//********************

		/// <summary>
		/// Outputs the loaded assemblies
		/// </summary>
		/// <param name="logger">The logger to write help to</param>
		[TerminalBinding("Displays the full names of every loaded assembly")]
		public static void Assemblies(ILogger logger)
		{
			logger.LogInformation("Currently loaded Assemblies:");

			foreach (Assembly MyAssembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				logger.LogInformation("\t{FullName}", MyAssembly.FullName);
			}
		}

		/// <summary>
		/// Outputs platform details
		/// </summary>
		/// <param name="logger">The logger to write help to</param>
		[TerminalBinding("Displays information about the current platform")]
		public static void Platform(ILogger logger)
		{
			logger.LogInformation("CLR Version: {0}", Environment.Version);
			logger.LogInformation("CLR Implementation: {0}", Type.GetType("Mono.Runtime") != null ? "Mono" : "Microsoft.Net");
			logger.LogInformation("Host Platform: {0}", Environment.OSVersion);
			logger.LogInformation("Platform Bits: {0}-bit", IntPtr.Size * 8);
		}

		//********************

		/// <summary>
		/// Outputs text to the console
		/// </summary>
		/// <param name="logger">The logger to write help to</param>
		/// <param name="text">The text to print</param>
		/// <remarks>Used for Exec scripts</remarks>
		[TerminalBinding("Prints text to the log and console")]
		public static void Echo(ILogger logger, string text)
		{
			logger.LogInformation(text);
		}

		//********************

		/// <summary>
		/// Executes a file containing a set of commands
		/// </summary>
		/// <param name="terminal">The terminal to write help to</param>
		/// <param name="fileName">The local file path</param>
		/// <param name="token">The cancellation token to abort execution</param>
		/// <returns>A task representing execution progress</returns>
		[TerminalBinding("Execute a list of terminal commands")]
		public static async ValueTask Exec(ITerminal terminal, string fileName, CancellationToken token)
		{
			foreach (var MyLine in File.ReadLines(fileName))
			{
				if (MyLine == "" || MyLine.StartsWith("#"))
					continue;

				await TerminalParser.InternalExecute(terminal, MyLine, token);
			}
		}

		//****************************************
		// Resource Management

		/// <summary>
		/// Outputs memory statistics
		/// </summary>
		[TerminalBinding("Displays statistics about the current memory usage")]
		public static void MemStatus(ILogger logger)
		{	//****************************************
			var CurrentProcess = Process.GetCurrentProcess();
			//****************************************

			logger.LogInformation("Runtime Memory: {0:N}", GC.GetTotalMemory(false));
			logger.LogInformation("Total Unshared Memory: {0:N}", CurrentProcess.PrivateMemorySize64);
			logger.LogInformation("Total Virtual Memory: {0:N}", CurrentProcess.VirtualMemorySize64);
		}

		/// <summary>
		/// Outputs CPU statistics
		/// </summary>
		[TerminalBinding("Displays statistics about the current CPU usage")]
		public static void CpuStatus(ILogger logger)
		{	//****************************************
			var MyProcess = Process.GetCurrentProcess();
			var Affinity = MyProcess.ProcessorAffinity.ToInt64();
			int Index, CpuCount = 0;
			var TotalProcessors = Environment.ProcessorCount;
			//****************************************
			
			for (Index = 0; Index < TotalProcessors; Index++)
			{
				if ((Affinity & (1 << Index)) != 0)
					CpuCount++;
			}
			
			logger.LogInformation("Current Priority: {0}", MyProcess.PriorityClass);
			logger.LogInformation("Processors: {0} allowed/{1} total", CpuCount, TotalProcessors);
			logger.LogInformation("Total CPU Time Used: {0}", MyProcess.TotalProcessorTime);
			logger.LogInformation("% time spent in kernel: {0:F2}", MyProcess.PrivilegedProcessorTime.TotalMilliseconds / MyProcess.TotalProcessorTime.TotalMilliseconds * 100);
			
			ThreadPool.GetMaxThreads(out var MaxWorkerThreads, out var MaxIOThreads);
			ThreadPool.GetAvailableThreads(out var FreeWorkerThreads, out var FreeIOThreads);

			logger.LogInformation("Thread Pool: {0} of {1} workers free, {2} of {3} IO free", FreeWorkerThreads, MaxWorkerThreads, FreeIOThreads, MaxIOThreads);
		}
		
		/// <summary>
		/// Outputs process details
		/// </summary>
		[TerminalBinding("Displays statistics about the current Process")]
		public static void ProcessStatus(ILogger logger)
		{	//****************************************
			var CurrentProcess = Process.GetCurrentProcess();
			//****************************************

			logger.LogInformation("Process: {0} ({1})", CurrentProcess.ProcessName, CurrentProcess.Id);
			logger.LogInformation("Commandline Arguments: {0}", CurrentProcess.StartInfo.Arguments);
			logger.LogInformation("Working Directory: {0}", CurrentProcess.StartInfo.WorkingDirectory);
			logger.LogInformation("Current Threads: {0}", CurrentProcess.Threads.Count);
			logger.LogInformation("Current Uptime: {0}", DateTime.Now.Subtract(CurrentProcess.StartTime));
		}

		//****************************************

		/// <summary>
		/// Throws an exception to be immediately caught by the parser
		/// </summary>
		[TerminalBinding("Throws an exception")]
		public static void Throw()
		{
			throw new ApplicationException("Requested Exception");
		}

		/// <summary>
		/// Clears the local terminal
		/// </summary>
		[TerminalBinding("Clears the local terminal")]
		public static void Clear(ITerminal terminal)
		{
			terminal.Clear();
		}
	}
}
