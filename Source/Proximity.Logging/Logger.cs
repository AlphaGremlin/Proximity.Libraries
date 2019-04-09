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
		public void Debug(string text) => Debug(1, text);

		internal void Debug(int depth, string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Debug, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at debugging level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Debug(string text, params object[] args) => Debug(1, text, args);

		internal void Debug(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Debug, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Verbose(string text) => Verbose(1, text);

		internal void Verbose(int depth, string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Verbose, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the verbose level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Verbose(string text, params object[] args) => Verbose(1, text, args);

		internal void Verbose(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Verbose, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection VerboseSection(string text) => VerboseSection(1, text);

		internal LogSection VerboseSection(int depth, string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Verbose, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the verbose level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection VerboseSection(string text, params object[] args) => VerboseSection(1, text, args);

		internal LogSection VerboseSection(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Verbose, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Info(string text) => Info(1, text);

		internal void Info(int depth, string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Info, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the information level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Info(string text, params object[] args) => Info(1, text, args);

		internal void Info(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Info, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection InfoSection(string text) => InfoSection(1, text);

		internal LogSection InfoSection(int depth, string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Info, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the information level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection InfoSection(string text, params object[] args) => InfoSection(1, text, args);

		internal LogSection InfoSection(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Info, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Milestone(string text) => Milestone(1, text);

		internal void Milestone(int depth, string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Milestone, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the milestone level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Milestone(string text, params object[] args) => Milestone(1, text, args);

		internal void Milestone(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Milestone, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection MilestoneSection(string text) => MilestoneSection(1, text);

		internal LogSection MilestoneSection(int depth, string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Milestone, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the milestone level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection MilestoneSection(string text, params object[] args) => MilestoneSection(1, text, args);

		internal LogSection MilestoneSection(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Milestone, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Warning(string text) => Warning(1, text);

		internal void Warning(int depth, string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Warning, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the warning level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Warning(string text, params object[] args) => Warning(1, text, args);

		internal void Warning(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Warning, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection WarningSection(string text) => WarningSection(1, text);

		internal LogSection WarningSection(int depth, string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Warning, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the warning level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection WarningSection(string text, params object[] args) => WarningSection(1, text, args);

		internal LogSection WarningSection(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Warning, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Error(string text) => Error(1, text);

		internal void Error(int depth, string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Error, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the error level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Error(string text, params object[] args) => Error(1, text, args);

		internal void Error(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Error, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		public LogSection ErrorSection(string text) => ErrorSection(1, text);

		internal LogSection ErrorSection(int depth, string text)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Error, text));

			return LogSection.Null;
		}

		/// <summary>
		/// Begins a new section at the error level
		/// </summary>
		/// <param name="text">The text to write describing the section</param>
		/// <param name="args">The arguments to format the text with</param>
		public LogSection ErrorSection(string text, params object[] args) => ErrorSection(1, text, args);

		internal LogSection ErrorSection(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				return Target.StartSection(new LogEntry(new StackFrame(depth), Severity.Error, string.Format(CultureInfo.InvariantCulture, text, args)));

			return LogSection.Null;
		}

		//****************************************

		/// <summary>
		/// Target.Writes an exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <remarks>Automatically calls <see cref="LogTarget.Flush" /></remarks>
		public void Exception(Exception newException, string text) => Exception(1, newException, text);

		internal void Exception(int depth, Exception newException, string text)
		{
			if (Target.IsStarted)
				Target.Write(new ExceptionLogEntry(new StackFrame(depth), text, newException));
		}

		/// <summary>
		/// Target.Writes a formatted exception
		/// </summary>
		/// <param name="newException">The exception to write</param>
		/// <param name="text">The text to write explaining the exception</param>
		/// <param name="args">The arguments to format the text with</param>
		/// <remarks>Automatically calls <see cref="LogTarget.Flush" /></remarks>
		public void Exception(Exception newException, string text, params object[] args) => Exception(1, newException, text, args);

		internal void Exception(int depth, Exception newException, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new ExceptionLogEntry(new StackFrame(depth), string.Format(CultureInfo.InvariantCulture, text, args), newException));
		}

		//****************************************

		/// <summary>
		/// Target.Writes an entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		public void Critical(string text) => Critical(1, text);

		internal void Critical(int depth, string text)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Critical, text));
		}

		/// <summary>
		/// Target.Writes a formatted entry at the critical level
		/// </summary>
		/// <param name="text">The text to write</param>
		/// <param name="args">The arguments to format the text with</param>
		public void Critical(string text, params object[] args) => Critical(1, text, args);

		internal void Critical(int depth, string text, object[] args)
		{
			if (Target.IsStarted)
				Target.Write(new LogEntry(new StackFrame(depth), Severity.Critical, string.Format(CultureInfo.InvariantCulture, text, args)));
		}

		//****************************************

		/// <summary>
		/// Gets the logging target being written to
		/// </summary>
		public LogTarget Target { get; }
	}
}
