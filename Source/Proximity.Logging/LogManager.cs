using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using Proximity.Logging.Config;
//****************************************

namespace Proximity.Logging
{
	/// <summary>
	/// Manages the logging framework
	/// </summary>
	public static class LogManager
	{ //****************************************
		private readonly static Stopwatch _Timer = new Stopwatch();
		private readonly static DateTime _StartTime;
		//****************************************

		static LogManager()
		{
			_StartTime = DateTime.Now;
			_Timer.Start();

			Default = new LogTarget();
		}

		//****************************************

		/// <summary>
		/// Starts the logging framework
		/// </summary>
		public static void Start() => Default.Start(LoggingConfig.OpenConfig());

		/// <summary>
		/// Starts the logging framework
		/// </summary>
		public static void Start(LoggingConfig config) => Default.Start(config);

		/// <summary>
		/// Adds a new logging output
		/// </summary>
		/// <param name="output">A log output to receive logging information</param>
		public static void AddOutput(LogOutput output) => Default.AddOutput(output);

		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		/// <returns>A new logging section</returns>
		public static LogSection StartSection(LogEntry entry) => Default.StartSection(entry, 0);

		/// <summary>
		/// Starts a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		/// <param name="priority">The priority of this entry</param>
		/// <returns>A new logging section</returns>
		public static LogSection StartSection(LogEntry entry, int priority) => Default.StartSection(entry, priority);

		/// <summary>
		/// Resets the section context for this logical call, so future log operations are isolated
		/// </summary>
		public static void ClearContext() => Default.ClearContext();

		/// <summary>
		/// Flushes all outputs, ensuring all previously written log messages have been stored
		/// </summary>
		public static void Flush() => Default.Flush();

		/// <summary>
		/// Ends the logging framework
		/// </summary>
		[SecurityCritical]
		public static void Finish() => Default.Finish();

		//****************************************

		internal static DateTime GetTimestamp() => _StartTime + _Timer.Elapsed;

		//****************************************

		/// <summary>
		/// Gets a list of all the logging outputs
		/// </summary>
		public static IReadOnlyCollection<LogOutput> Outputs => Default.Outputs;

		/// <summary>
		/// Gets/Sets the path logging outputs will place their results
		/// </summary>
		public static string OutputPath
		{
			get => Default.OutputPath;
			set => Default.OutputPath = value;
		}

		/// <summary>
		/// Gets the time the logging framework was started
		/// </summary>
		public static DateTime StartTime => Default.StartTime;

		/// <summary>
		/// Gets the current section stack for this logical context
		/// </summary>
		public static ImmutableCountedStack<LogSection> Context => Default.Context;

		/// <summary>
		/// Gets the current depth of the section stack
		/// </summary>
		public static int SectionDepth => Default.SectionDepth;

		/// <summary>
		/// Gets the default 
		/// </summary>
		public static LogTarget Default { get; }
	}
}
