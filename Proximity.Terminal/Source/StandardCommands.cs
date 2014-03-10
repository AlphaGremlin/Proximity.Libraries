/****************************************\
 StandardCommands.cs
 Created: 2014-03-03
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Proximity.Utility;
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
					Log.Info(string.Join(", ", MyTypes));
				}
			}
		}
		
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
		
		[TerminalBinding("Displays information about the current platform")]
		public static void Platform()
		{
			Log.Info("CLR Version: {0}", Environment.Version);
			Log.Info("CLR Implementation: {0}", Type.GetType("Mono.Runtime") != null ? "Mono" : "Microsoft.Net");
			Log.Info("Host Platform: {0}", Environment.OSVersion);
			Log.Info("Platform Bits: {0}-bit", IntPtr.Size * 8);
		}
		
		//********************
		
		[TerminalBinding("Prints text to the log and console")]
		public static void Echo(string text)
		{
			Log.Info(text);
		}
		
		//********************
		
		[TerminalBinding("Execute a list of terminal commands")]
		public static void Exec(string fileName)
		{
			var MyContext = TerminalParser.Context;
			
			foreach (var MyLine in File.ReadLines(fileName))
			{
				if (MyLine == "" || MyLine.StartsWith("#"))
					continue;
				
				TerminalParser.Execute(MyLine, MyContext);
			}
		}
		
		//****************************************
		// Resource Management
		
		[TerminalBinding("Displays statistics about the current memory usage")]
		public static void MemStatus()
		{	//****************************************
			Process MyProcess = Process.GetCurrentProcess();
			//****************************************
			
			Log.Info("Runtime Memory: {0:N}", GC.GetTotalMemory(false));
			Log.Info("Total Unshared Memory: {0:N}", MyProcess.PrivateMemorySize64);
			Log.Info("Total Virtual Memory: {0:N}", MyProcess.VirtualMemorySize64);
		}
				
		[TerminalBinding("Displays statistics about the current CPU usage")]
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
		
		[TerminalBinding("Displays statistics about the current Process")]
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
		
		[TerminalBinding("Throws an exception")]
		public static void Throw()
		{
			throw new ApplicationException("Requested Exception");
		}
		
		public static void Clear()
		{
			TerminalManager.Clear();
		}
	}
}
