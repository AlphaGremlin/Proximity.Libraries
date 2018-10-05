using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// A logger to write entries to a Log Target
	/// </summary>
	public struct Logger
	{
		internal Logger(LogTarget target)
		{
			Target = target;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at debugging level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Debug(string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Debug, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at debugging level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Debug(string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Debug, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Verbose(string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Verbose, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Verbose(string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Verbose, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection VerboseSection(string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Verbose, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection VerboseSection(string text, params object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Verbose, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Info(string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Info, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Info(string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Info, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection InfoSection(string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Info, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection InfoSection(string text, params object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Info, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Milestone(string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Milestone, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Milestone(string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Milestone, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection MilestoneSection(string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Milestone, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection MilestoneSection(string text, params object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Milestone, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Warning(string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Warning, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Warning(string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Warning, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection WarningSection(string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Warning, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection WarningSection(string text, params object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Warning, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Error(string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Error, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Error(string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Error, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection ErrorSection(string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Error, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection ErrorSection(string text, params object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(1), Severity.Error, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <remarks>Automatically calls <see cref="LogTarget.Flush" /></remarks>
		public void Exception(Exception newException, string text)
		{
			if (Target.IsStarted)
				Target.Write(new ExceptionLogEntry(new StackFrame(1), text, newException));
		}

		/// <summary>
		/// Target.Writes a formatted exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <param name="args">The arguments to format the text with</param>
		/// <remarks>Automatically calls <see cref="LogTarget.Flush" /></remarks>
		public void Exception(Exception newException, string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new ExceptionLogEntry(new StackFrame(1), string.Format(CultureInfo.InvariantCulture, text, args), newException));
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Critical(string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Critical, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Critical(string text, params object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(1), Severity.Critical, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		//****************************************

		/// <summary>
		/// Gets the logging target being written to
		/// </summary>
		public LogTarget Target { get; }
	}
}
