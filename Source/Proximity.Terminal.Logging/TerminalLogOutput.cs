using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Proximity.Logging;

namespace Proximity.Terminal.Logging
{
	/// <summary>
	/// Provides a log output that routes to a terminal
	/// </summary>
	public sealed class TerminalLogOutput : LogOutput
	{
		/// <summary>
		/// Creates a new log output that routes to the default console
		/// </summary>
		public TerminalLogOutput() : this(LogManager.Default, TerminalConsole.View ?? throw new InvalidOperationException("The terminal must be initialised"))
		{
		}

		/// <summary>
		/// Creates a new log output
		/// </summary>
		/// <param name="target">The logging target to receive log entries from</param>
		/// <param name="logger">The logger to write output to</param>
		public TerminalLogOutput(LogTarget target, ILogger logger) : base(target)
		{
			Logger = logger;
		}

		//****************************************

		/// <inheritdoc />
		protected override void Finish()
		{
		}

		/// <inheritdoc />
		protected override void FinishSection(LogSection oldSection)
		{
		}

		/// <inheritdoc />
		protected override void Flush()
		{
		}

		/// <inheritdoc />
		protected override void Start()
		{
		}

		/// <inheritdoc />
		protected override void StartSection(LogSection newSection) => Write(newSection.Entry);

		/// <inheritdoc />
		protected override void Write(LogEntry entry)
		{
			TerminalScope? Scope = null;

			var Level = entry.Severity switch
			{
				Severity.Critical => LogLevel.Critical,
				Severity.Debug => LogLevel.Debug,
				Severity.Error => LogLevel.Error,
				Severity.Info => LogLevel.Information,
				Severity.Milestone => LogLevel.Information,
				Severity.Verbose => LogLevel.Trace,
				Severity.Warning => LogLevel.Warning,
				_ => LogLevel.None
			};

			if (entry.Severity == Severity.Milestone)
				Scope = TerminalScope.Milestone;
			else if (entry.Severity == Severity.None)
				Scope = TerminalScope.ConsoleCommand;

			if (entry is ExceptionLogEntry ExceptionEntry)
				Logger.Log(Level, default, new ConsoleRecord(entry.Timestamp, Level, entry.Text, Target.SectionDepth, Scope, ExceptionEntry.Exception), ExceptionEntry.Exception, Formatter);
			else
				Logger.Log(Level, default, new ConsoleRecord(entry.Timestamp, Level, entry.Text, Target.SectionDepth, Scope), null, Formatter);
		}

		//****************************************

		/// <summary>
		/// Gets the logger we will be writing ConsoleRecord entries to
		/// </summary>
		public ILogger Logger { get; }

		//****************************************

		private static string Formatter(ConsoleRecord record, Exception? exception) => record.Text;
	}
}
