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
using Proximity.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Contains static methods for console commands
	/// </summary>
	[TerminalProvider()]
	public static class StandardCommands
	{	//****************************************
		
		//****************************************
		// Standard console commands
		
		/// <summary>
		/// Outputs base-level command help
		/// </summary>
		[TerminalBinding("Output a list of available commands, variables and instances")]
		public static void Help()
		{	//****************************************
			var MyCommands = new List<TerminalCommandSet>();
			var MyVariables = new List<TerminalVariable>();
			var MyTypes = new List<TerminalTypeSet>();
			//****************************************
			
			foreach(var MyRegistry in TerminalParser.Context)
			{
				MyCommands.AddRange(MyRegistry.Commands);
				
				MyVariables.AddRange(MyRegistry.Variables);
				
				MyTypes.AddRange(MyRegistry.TypeSets);
			}
			
			MyCommands.Sort();
			
			MyVariables.Sort();
			
			MyTypes.Sort();
			
			//****************************************
			
			if (MyCommands.Count > 0)
			{
				using (Log.VerboseSection("Available Commands:"))
				{
					Log.Info(string.Join(", ", MyCommands));
				}
			}
			
			if (MyVariables.Count > 0)
			{
				using (Log.VerboseSection("Available Variables:"))
				{
					Log.Info(string.Join(", ", MyVariables));
				}
			}
			
			if (MyTypes.Count > 0)
			{
				using (Log.VerboseSection("Available Types:"))
				{
					Log.Info(string.Join(", ", MyTypes.Where(type => type.HasInstance)));
				}
			}
		}
		
		/// <summary>
		/// Outputs help on a specific command
		/// </summary>
		/// <param name="command">The command text</param>
		[TerminalBinding("Output brief information about a command, variable or instance")]
		public static void Help(string command)
		{	//****************************************
			var MyTypeData = TerminalParser.FindCommand(command, TerminalParser.Context);
			//****************************************
			
			if (MyTypeData == null)
			{
				Log.Warning("{0} is not a valid command, variable, or instance", command);
				
				return;
			}
			
			TerminalParser.HelpOn(MyTypeData);
		}
		
		//********************
	
		/// <summary>
		/// Outputs the loaded assemblies
		/// </summary>
		[TerminalBinding("Displays the full names of every loaded assembly")]
		public static void Assemblies()
		{
			using(Log.InfoSection("Currently loaded Assemblies:"))
			{
				foreach(Assembly MyAssembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					Log.Info(MyAssembly.FullName);
				}
			}
		}
		
		/// <summary>
		/// Outputs platform details
		/// </summary>
		[TerminalBinding("Displays information about the current platform")]
		public static void Platform()
		{
			Log.Info("CLR Version: {0}", Environment.Version);
			Log.Info("CLR Implementation: {0}", Type.GetType("Mono.Runtime") != null ? "Mono" : "Microsoft.Net");
			Log.Info("Host Platform: {0}", Environment.OSVersion);
			Log.Info("Platform Bits: {0}-bit", IntPtr.Size * 8);
		}
		
		//********************
		
		/// <summary>
		/// Outputs text to the console
		/// </summary>
		/// <param name="text">The text to print</param>
		/// <remarks>Used for Exec scripts</remarks>
		[TerminalBinding("Prints text to the log and console")]
		public static void Echo(string text)
		{
			Log.Info(text);
		}
		
		//********************
		
		/// <summary>
		/// Executes a file containing a set of commands
		/// </summary>
		/// <param name="fileName">The local file path</param>
		/// <returns>A task representing execution progress</returns>
		[TerminalBinding("Execute a list of terminal commands")]
		public static async Task Exec(string fileName)
		{
			var MyContext = TerminalParser.Context;
			
			foreach (var MyLine in File.ReadLines(fileName))
			{
				if (MyLine == "" || MyLine.StartsWith("#"))
					continue;
				
				await TerminalParser.InternalExecute(MyLine, MyContext);
			}
		}
		
		//****************************************
		// Resource Management
		
		/// <summary>
		/// Outputs memory statistics
		/// </summary>
		[TerminalBinding("Displays statistics about the current memory usage")]
		[SecuritySafeCritical]
		public static void MemStatus()
		{	//****************************************
			Process MyProcess = Process.GetCurrentProcess();
			//****************************************
			
			Log.Info("Runtime Memory: {0:N}", GC.GetTotalMemory(false));
			Log.Info("Total Unshared Memory: {0:N}", MyProcess.PrivateMemorySize64);
			Log.Info("Total Virtual Memory: {0:N}", MyProcess.VirtualMemorySize64);
		}
				
		/// <summary>
		/// Outputs CPU statistics
		/// </summary>
		[TerminalBinding("Displays statistics about the current CPU usage")]
		[SecuritySafeCritical]
		public static void CpuStatus()
		{	//****************************************
			Process MyProcess = Process.GetCurrentProcess();
			long Affinity = MyProcess.ProcessorAffinity.ToInt64();
			int Index, CpuCount = 0;
			int MaxWorkerThreads, MaxIOThreads, FreeWorkerThreads, FreeIOThreads;
			//****************************************
			
			for (Index = 0; Index < Environment.ProcessorCount; Index++)
			{
				if ((Affinity & (1 << Index)) != 0)
					CpuCount++;
			}
			
			Log.Info("Current Priority: {0}", MyProcess.PriorityClass);
			Log.Info("Permitted Processors: {0}", CpuCount);
			Log.Info("Total CPU Time Used: {0}", MyProcess.TotalProcessorTime);
			Log.Info("% time spent in kernel: {0:F2}", MyProcess.PrivilegedProcessorTime.TotalMilliseconds / MyProcess.TotalProcessorTime.TotalMilliseconds * 100);
			
			ThreadPool.GetMaxThreads(out MaxWorkerThreads, out MaxIOThreads);
			ThreadPool.GetAvailableThreads(out FreeWorkerThreads, out FreeIOThreads);
			
			Log.Info("Thread Pool: {0} of {1} workers free, {2} of {3} IO free", FreeWorkerThreads, MaxWorkerThreads, FreeIOThreads, MaxIOThreads);
		}
		
		/// <summary>
		/// Outputs process details
		/// </summary>
		[TerminalBinding("Displays statistics about the current Process")]
		[SecuritySafeCritical]
		public static void ProcessStatus()
		{	//****************************************
			Process MyProcess = Process.GetCurrentProcess();
			//****************************************

			Log.Info("Process: {0} ({1})", MyProcess.ProcessName, MyProcess.Id);
			Log.Info("Commandline Arguments: {0}", MyProcess.StartInfo.Arguments);
			Log.Info("Working Directory: {0}", MyProcess.StartInfo.WorkingDirectory);
			Log.Info("Current Threads: {0}", MyProcess.Threads.Count);
			Log.Info("Current Uptime: {0}", DateTime.Now.Subtract(MyProcess.StartTime));
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
		[SecuritySafeCritical]
		public static void Clear()
		{
			TerminalManager.Clear();
		}
	}
}
