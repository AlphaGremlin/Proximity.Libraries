using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
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
	{ //****************************************
		private readonly static LogTarget _Default = new LogTarget();

		private readonly static PrecisionTimer _Timer = new PrecisionTimer();
		//****************************************

		/// <summary>
		/// Starts the logging framework
		/// </summary>
		public static void Start() => _Default.Start(LoggingConfig.OpenConfig());

		/// <summary>
		/// Starts the logging framework
		/// </summary>
		public static void Start(LoggingConfig config) => _Default.Start(config);

		/// <summary>
		/// Adds a new logging output
		/// </summary>
		/// <param name="output">A log output to receive logging information</param>
		public static void AddOutput(LogOutput output) => _Default.AddOutput(output);

		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		/// <returns>A new logging section</returns>
		public static LogSection StartSection(LogEntry entry) => _Default.StartSection(entry, 0);

		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		/// <param name="priority">The priority of this entry</param>
		/// <returns>A new logging section</returns>
		public static LogSection StartSection(LogEntry entry, int priority) => _Default.StartSection(entry, priority);

		/// <summary>
		/// Resets the section context for this logical call, so future log operations are isolated
		/// </summary>
		public static void ClearContext() => _Default.ClearContext();

		/// <summary>
		/// Flushes all outputs, ensuring all previously written log messages have been stored
		/// </summary>
		public static void Flush() => _Default.Flush();

		/// <summary>
		/// Ends the logging framework
		/// </summary>
		[SecurityCritical]
		public static void Finish() => _Default.Finish();

		//****************************************

		internal static DateTime GetTimestamp() => _Timer == null ? DateTime.Now : _Timer.GetTime();

		//****************************************

		/// <summary>
		/// Gets a list of all the logging outputs
		/// </summary>
		public static IReadOnlyCollection<LogOutput> Outputs => _Default.Outputs;

		/// <summary>
		/// Gets/Sets the path logging outputs will place their results
		/// </summary>
		public static string OutputPath
		{
			get => _Default.OutputPath;
			set => _Default.OutputPath = value;
		}

		/// <summary>
		/// Gets the time the logging framework was started
		/// </summary>
		public static DateTime StartTime => _Default.StartTime;

		/// <summary>
		/// Gets the current section stack for this logical context
		/// </summary>
		public static ImmutableCountedStack<LogSection> Context => _Default.Context;

		/// <summary>
		/// Gets the current depth of the section stack
		/// </summary>
		public static int SectionDepth => _Default.SectionDepth;

		/// <summary>
		/// Gets the default 
		/// </summary>
		public static LogTarget Default => _Default;
	}
}