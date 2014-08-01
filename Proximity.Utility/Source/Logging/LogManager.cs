/****************************************\
 LogManager.cs
 Created: 3-06-2009
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Proximity.Utility.Collections;
using Proximity.Utility.Logging.Config;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Manages the logging framework
	/// </summary>
	public static class LogManager
	{	//****************************************
		private static ImmutableList<LogOutput> _Outputs = ImmutableList<LogOutput>.Empty;
		private static readonly ConcurrentDictionary<string, LogCategory> _Categories = new ConcurrentDictionary<string, LogCategory>();
		
		private static string _OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name);
		private static PrecisionTimer _Timer = new PrecisionTimer();
		private static bool _IsStarted;
		private static DateTime _StartTime = Process.GetCurrentProcess().StartTime;
		//****************************************

		/// <summary>
		/// Starts the logging framework
		/// </summary>
		public static void Start()
		{
			if (!Directory.Exists(_OutputPath))
				Directory.CreateDirectory(_OutputPath);
			
			//****************************************
			// Initialise Log Outputs
			
			_IsStarted = true;
			
			foreach(LogOutput MyOutput in _Outputs)
			{
				MyOutput.Start();
			}
			
			foreach (var MyConfig in LoggingConfig.OpenConfig().Outputs)
			{
				AddOutput(MyConfig.ToOutput());
			}
		}
		
		/// <summary>
		/// Adds a new logging output
		/// </summary>
		/// <param name="output">A log output to receive logging information</param>
		public static void AddOutput(LogOutput output)
		{	//****************************************
			ImmutableList<LogOutput> OldList, NewList;
			//****************************************
			
			do
			{
				OldList = _Outputs;
				
				NewList = OldList.Add(output);
			} while (Interlocked.CompareExchange(ref _Outputs, NewList, OldList) != OldList);
			
			//****************************************
			
			if (_IsStarted)
				output.Start();
		}
		
		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="newSection">The section to start</param>
		public static void StartSection(LogSection newSection)
		{	//****************************************
			var MyOutputs = _Outputs;
			//****************************************
			
			foreach(LogOutput MyOutput in _Outputs)
				MyOutput.StartSection(newSection);
			
			Context = Context.Push(newSection);
		}
		
		/// <summary>
		/// Resets the section context for this logical call, so future log operations are isolated
		/// </summary>
		public static void ClearContext()
		{
			Context = ImmutableCountedStack<LogSection>.Empty;
		}
		
		/// <summary>
		/// Finishes a logging section
		/// </summary>
		/// <param name="oldSection">The section to finish</param>
		public static void FinishSection(LogSection oldSection)
		{	//****************************************
			var MyContext = Context;
			var MyOutputs = _Outputs;
			//****************************************
			
			oldSection.IsDisposed = true;
			
			if (MyContext.IsEmpty || MyContext.Peek() != oldSection)
				return;
			
			Context = MyContext.Pop();
			
			foreach(LogOutput MyOutput in _Outputs)
				MyOutput.FinishSection(oldSection);
		}
		
		/// <summary>
		/// Ends the logging framework
		/// </summary>
		public static void Finish()
		{
			foreach(LogOutput MyOutput in _Outputs)
				MyOutput.Finish();
		}

		//****************************************
		
		/// <summary>
		/// Registers a new logging category
		/// </summary>
		/// <param name="newCategory">The new category to register</param>
		public static void AddCategory(LogCategory newCategory)
		{
			_Categories.TryAdd(newCategory.Name, newCategory);
		}
		
		/// <summary>
		/// Retrieves an existing logging category
		/// </summary>
		/// <param name="categoryName">The name of the category to retrieve</param>
		/// <returns>The requested category, or null if it does not exist</returns>
		public static LogCategory GetCategory(string categoryName)
		{	//****************************************
			LogCategory MyCategory;
			//****************************************
			
			if (_Categories.TryGetValue(categoryName, out MyCategory))
				return MyCategory;

			return null;
		}
		
		//****************************************
		
		internal static DateTime GetTimestamp()
		{
			if (_Timer == null)
				return DateTime.Now;

			return _Timer.GetTime();
		}
		
		//****************************************

		/// <summary>
		/// Gets a list of all the logging outputs
		/// </summary>
		public static IReadOnlyList<LogOutput> Outputs
		{
			get { return _Outputs; }
		}

		/// <summary>
		/// Gets/Sets the path logging outputs will place their results
		/// </summary>
		public static string OutputPath
		{
			get { return _OutputPath; }
			set { _OutputPath = value; }
		}
		
		/// <summary>
		/// Gets the time the logging framework was started
		/// </summary>
		public static DateTime StartTime
		{
			get { return _StartTime; }
		}
		
		/// <summary>
		/// Gets the current section stack for this logical context
		/// </summary>
		public static ImmutableCountedStack<LogSection> Context
		{
			get { return (CallContext.LogicalGetData("Logging.Context") as ImmutableCountedStack<LogSection>) ?? ImmutableCountedStack<LogSection>.Empty; }
			private set { CallContext.LogicalSetData("Logging.Context", value); }
		}
		
		/// <summary>
		/// Gets the current depth of the section stack
		/// </summary>
		public static int SectionDepth
		{
			get { return Context.Count; }
		}
	}
}
