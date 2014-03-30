/****************************************\
 LogManager.cs
 Created: 3-06-2009
\****************************************/
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using Proximity.Utility.Logging.Config;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Manages the logging framework
	/// </summary>
	public static class LogManager
	{	//****************************************
		private static List<LogOutput> _Outputs = new List<LogOutput>();
		private static Dictionary<string, LogCategory> _Categories = new Dictionary<string, LogCategory>();
		[ThreadStatic()] private static Stack<LogSection> _Sections;

		private static string _OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name);
		private static PrecisionTimer _Timer = new PrecisionTimer();
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
			
			foreach(LogOutput MyOutput in _Outputs)
			{
				MyOutput.Start();
			}
			
			foreach (OutputConfig MyConfig in LoggingConfig.OpenConfig().Outputs)
			{
				MyConfig.Output.Start();
				
				_Outputs.Add(MyConfig.Output);
			}
		}
		
		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="newSection">The section to start</param>
		public static void StartSection(LogSection newSection)
		{
			if (_Sections == null)
				_Sections = new Stack<LogSection>(16);
			
			_Sections.Push(newSection);
			
			for(int Index = 0; Index < _Outputs.Count; Index++)
				_Outputs[Index].StartSection(newSection);
		}
		
		internal static DateTime GetTimestamp()
		{
			if (_Timer == null)
				return DateTime.Now;

			return _Timer.GetTime();
		}
		
		/// <summary>
		/// Finishes a logging section
		/// </summary>
		/// <param name="oldSection">The section to finish</param>
		public static void FinishSection(LogSection oldSection)
		{
			for(int Index = 0; Index < _Outputs.Count; Index++)
				_Outputs[Index].FinishSection();			
			
			if (_Sections.Pop() == oldSection)
				return;
			
			throw new InvalidOperationException("Closed section is not the latest section");
		}
		
		/// <summary>
		/// Ends the logging framework
		/// </summary>
		public static void Finish()
		{
			foreach(LogOutput MyOutput in _Outputs)
			{
				MyOutput.Finish();
			}
		}

		//****************************************
		
		/// <summary>
		/// Registers a new logging category
		/// </summary>
		/// <param name="newCategory">The new category to register</param>
		public static void AddCategory(LogCategory newCategory)
		{
			_Categories.Add(newCategory.Name, newCategory);
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

		/// <summary>
		/// Gets a list of all the logging outputs
		/// </summary>
		public static IList<LogOutput> Outputs
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
	}
}
