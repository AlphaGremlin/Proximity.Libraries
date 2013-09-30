/****************************************\
 StandardCommands.cs
 Created: 31-01-2008
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Proximity.Utility;
//****************************************

namespace Proximity.Console
{
	/// <summary>
	/// Contains static methods for console commands
	/// </summary>
	[ConsoleProvider()]
	public static class StandardCommands
	{	//****************************************
		
		//****************************************
		// Standard console commands
		
		[ConsoleBinding("Output a list of available commands")]
		public static void Help()
		{	//****************************************
			string[] Commands = ConsoleParser.ListCommands().ToArray();
			StringBuilder MyBuilder;
			//****************************************
			
			using (Log.VerboseSection("Available Commands:"))
			{
				if (ConsoleManager.IsCommandLine)
				{
					// Try and split the list of commands across multiple lines
					try
					{
						int Width = System.Console.WindowWidth;
						
						if (Width > 0)
						{
							MyBuilder = new StringBuilder();
							
							for(int Index = 0; Index < Commands.Length; Index++)
							{
								if (MyBuilder.Length + Commands[Index].Length > Width && MyBuilder.Length > 0)
								{
									Log.Verbose(MyBuilder.ToString());
									
									MyBuilder.Clear();
								}
								
								if (MyBuilder.Length != 0)
									MyBuilder.Append(", ");
								
								MyBuilder.Append(Commands[Index]);
							}
							
							if (MyBuilder.Length > 0)
								Log.Verbose(MyBuilder.ToString());
							
							return;
						}
					}
					catch (IOException) // Unable to get the width. Console might not fully exist, or we're running under Mono
					{
					}
				}
				
				Log.Verbose(string.Join(", ", Commands));
			}
		}
		
		[ConsoleBinding("Output brief information about a command")]
		public static void Help(string commandName)
		{	//****************************************
			IList<string> Results = ConsoleParser.FindCommand(commandName);
			//****************************************
			
			if (Results.Count == 0)
			{
				Log.Warning("The command '{0}' does not exist", commandName);
				
				return;
			}
			
			using (Log.VerboseSection("Usage information for '{0}':", commandName))
			{
				foreach(string Result in Results)
				{
					Log.Verbose(Result);
				}
			}
		}
		
		//********************
	
		[ConsoleBinding("Displays the full names of every loaded assembly")]
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
		
		[ConsoleBinding("Displays information about the current platform")]
		public static void Platform()
		{
			Log.Info("CLR Version: {0}", Environment.Version);
			Log.Info("CLR Implementation: {0}", Type.GetType("Mono.Runtime") != null ? "Mono" : "Microsoft.Net");
			Log.Info("Host Platform: {0}", Environment.OSVersion);
			Log.Info("Platform Bits: {0}-bit", IntPtr.Size * 8);
		}
		
		//********************
		
		[ConsoleBinding("Prints text to the log and console")]
		public static void Echo(string text)
		{
			Log.Info(text);
		}
		
		[ConsoleBinding("Clears the console history")]
		public static void Clear()
		{
			if (ConsoleManager.Output != null)
				ConsoleManager.Output.Clear();			
		}
		
		//********************
				
		[ConsoleBinding("Execute a list of console commands")]
		public static void Exec(string fileName)
		{
			
		}
		
		//****************************************
		// Resource Management
		
		[ConsoleBinding("Displays statistics about the current memory usage")]
		public static void MemStatus()
		{	//****************************************
			Process MyProcess = Process.GetCurrentProcess();
			//****************************************
			
			Log.Info("Runtime Memory: {0:N}", GC.GetTotalMemory(false));
			Log.Info("Total Unshared Memory: {0:N}", MyProcess.PrivateMemorySize64);
			Log.Info("Total Virtual Memory: {0:N}", MyProcess.VirtualMemorySize64);
		}
				
		[ConsoleBinding("Displays statistics about the current CPU usage")]
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
		
		[ConsoleBinding("Displays statistics about the current Process")]
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
				
		[ConsoleBinding("Throws an exception")]
		public static void Throw()
		{
			throw new ApplicationException("Requested Exception");
		}		
		
		//****************************************
		// Variable Management Commands
		
		[ConsoleBinding("Sets a variable. Use quotes for text longer than 1 word")]
		public static void Set(string varName, string newValue)
		{
			ConsoleParser.SetVariable(varName, newValue);
		}
		
		[ConsoleBinding("Prints out a variable to the console")]
		public static void Get(string varName)
		{	//****************************************
			object Value;
			//****************************************
			
			if (!ConsoleParser.GetVariable(varName, out Value))
				return;
			
			if (Value == null)
				Log.Info("Variable has no value");
			else
				Log.Info(Value.ToString());
			
		}
		
		[ConsoleBinding("Lists the variables available")]
		public static void List()
		{	//****************************************
			string[] Variables = ConsoleParser.ListVariables().ToArray();
			//****************************************
			
			using (Log.VerboseSection("Available Variables:"))
			{
				for(int Index = 0; Index < Variables.Length; Index += 4)
				{
					Log.Verbose(string.Join(", ", Variables, Index, Math.Min(4, Variables.Length - Index)));
				}
			}
		}
	}
}
