/****************************************\
 Log.cs
 Created: 4-06-2009
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using Proximity.Utility.Logging;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Writes entries to the Logging Infrastructure
	/// </summary>
	[SecuritySafeCritical]
	public static class Log
	{
		/// <summary>
		/// Writes an entry at debugging level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Debug(string text)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Debug, text));
		}
		
		/// <summary>
		/// Writes a formatted entry at debugging level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Debug(string text, params object[] args)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Debug, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		//****************************************
		
		/// <summary>
		/// Writes an entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Verbose(string text)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Verbose, text));
		}
		
		/// <summary>
		/// Writes a formatted entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Verbose(string text, params object[] args)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Verbose, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection VerboseSection(string text)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Verbose, text));
		}
		
		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection VerboseSection(string text, params object[] args)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Verbose, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		//****************************************

		/// <summary>
		/// Writes an entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Info(string text)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Info, text));
		}
		
		/// <summary>
		/// Writes a formatted entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Info(string text, params object[] args)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Info, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection InfoSection(string text)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Info, text));
		}
		
		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection InfoSection(string text, params object[] args)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Info, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		//****************************************
		
		/// <summary>
		/// Writes an entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Milestone(string text)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Milestone, text));
		}
		
		/// <summary>
		/// Writes a formatted entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Milestone(string text, params object[] args)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Milestone, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection MilestoneSection(string text)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Milestone, text));
		}
		
		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection MilestoneSection(string text, params object[] args)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Milestone, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		//****************************************

		/// <summary>
		/// Writes an entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Warning(string text)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Warning, text));	
		}
				
		/// <summary>
		/// Writes a formatted entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Warning(string text, params object[] args)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Warning, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection WarningSection(string text)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Warning, text));
		}

		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection WarningSection(string text, params object[] args)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Warning, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		//****************************************
		
		/// <summary>
		/// Writes an entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Error(string text)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Error, text));
		}
		
		/// <summary>
		/// Writes a formatted entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Error(string text, params object[] args)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Error, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public static LogSection ErrorSection(string text)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Error, text));
		}

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public static LogSection ErrorSection(string text, params object[] args)
		{
			return new LogSection(new LogEntry(new StackFrame(1), Severity.Error, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		//****************************************
		
		/// <summary>
		/// Writes an exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <remarks>Automatically calls <see cref="LogManager.Flush" /></remarks>
		public static void Exception(Exception newException, string text)
		{
			Write(new ExceptionLogEntry(new StackFrame(1), text, newException));
		}
		
		/// <summary>
		/// Writes a formatted exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <param name="args">The arguments to format the text with</param>
		/// <remarks>Automatically calls <see cref="LogManager.Flush" /></remarks>
		public static void Exception(Exception newException, string text, params object[] args)
		{
			Write(new ExceptionLogEntry(new StackFrame(1), string.Format(CultureInfo.InvariantCulture, text, args), newException));
		}
		
		//****************************************

		/// <summary>
		/// Writes an entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		public static void Critical(string text)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Critical, text));
		}
		
		/// <summary>
		/// Writes a formatted entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public static void Critical(string text, params object[] args)
		{
			Write(new LogEntry(new StackFrame(1), Severity.Critical, string.Format(CultureInfo.InvariantCulture, text, args)));
		}
		
		//****************************************
		
		/// <summary>
		/// Resets the section context for this logical call, so future log operations are isolated
		/// </summary>
		public static void ClearContext()
		{
			LogManager.ClearContext();
		}
		
		/// <summary>
		/// Writes an entry to the log
		/// </summary>
		/// <param name="newEntry">The entry to write</param>
		public static void Write(LogEntry newEntry)
		{
			foreach(var MyOutput in LogManager.Outputs)
				MyOutput.Write(newEntry);
		}
		
		/// <summary>
		/// Flushes all outputs, ensuring all previously written log messages have been stored
		/// </summary>
		public static void Flush()
		{
			LogManager.Flush();
		}
	}
}
#endif