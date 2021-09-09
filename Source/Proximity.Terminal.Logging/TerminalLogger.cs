using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Proximity.Logging;

namespace Proximity.Terminal.Logging
{
	/// <summary>
	/// Provides an ILogger implementation that routes to Proximity Logging, until we refactor to use Serilog
	/// </summary>
	public sealed class TerminalLogger : ILogger, ITerminalListener
	{
		/// <summary>
		/// Gets the default ILogger implementation
		/// </summary>
		public static TerminalLogger Default { get; } = new TerminalLogger();

		//****************************************
		private readonly LogTarget _Target;
		private readonly HashSet<LogLevel> _IsEnabled = new();
		//****************************************

		/// <summary>
		/// Creates a new Terminal Logger
		/// </summary>
		/// <param name="minimum">The minimum log level to start with</param>
		public TerminalLogger(LogLevel minimum = LogLevel.Trace) : this(LogManager.Default, minimum)
		{
		}

		/// <summary>
		/// Creates a new Terminal Logger
		/// </summary>
		/// <param name="target">The logging target</param>
		/// <param name="minimum">The minimum log level to start with</param>
		public TerminalLogger(LogTarget target, LogLevel minimum = LogLevel.Trace)
		{
			if (minimum == LogLevel.None)
				throw new ArgumentOutOfRangeException(nameof(minimum));

			_Target = target;

			SetMinimum(minimum);
		}

		//****************************************

		IDisposable ILogger.BeginScope<TState>(TState state) => LoggerScope.Default;

		/// <summary>
		/// Resets the minimum enabled log level
		/// </summary>
		/// <param name="minimum">The minimum log level</param>
		public void SetMinimum(LogLevel minimum)
		{
			_IsEnabled.Clear();

			if (minimum != LogLevel.None)
			{
				foreach (LogLevel Value in Enum.GetValues(typeof(LogLevel)))
					if (Value >= minimum && Value != LogLevel.None)
						_IsEnabled.Add(Value);
			}
		}

		/// <summary>
		/// Sets the enabled state of a particular logging level
		/// </summary>
		/// <param name="logLevel">The target log level</param>
		/// <param name="enabled">Whether to enable or disable this level</param>
		public void IsEnabled(LogLevel logLevel, bool enabled)
		{
			if (logLevel == LogLevel.None)
				throw new ArgumentOutOfRangeException(nameof(logLevel));

			if (enabled)
				_IsEnabled.Add(logLevel);
			else
				_IsEnabled.Remove(logLevel);
		}

		/// <inheritdoc />
		public bool IsEnabled(LogLevel logLevel) => _IsEnabled.Contains(logLevel);

		/// <inheritdoc />
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!_IsEnabled.Contains(logLevel))
				return;

			if (state is LogEntry Entry)
			{
				_Target.Write(Entry);

				return;
			}

			var Level = logLevel switch
			{
				LogLevel.Critical => Severity.Critical,
				LogLevel.Debug => Severity.Debug,
				LogLevel.Error => Severity.Error,
				LogLevel.Information => Severity.Info,
				LogLevel.None => Severity.None,
				LogLevel.Trace => Severity.Verbose,
				LogLevel.Warning => Severity.Warning,
				_ => Severity.None
			};

			if (state is ConsoleRecord Record)
			{
				if (Record.Scope == TerminalScope.Milestone)
					Level = Severity.Milestone;
				else if (Record.Scope == TerminalScope.ConsoleCommand)
					Level = Severity.None;

				if (exception != null)
					_Target.Write(new ExceptionLogEntry(new System.Diagnostics.StackFrame(1), Record.Text, exception));
				else
					_Target.Write(new LogEntry(new System.Diagnostics.StackFrame(1), Level, Record.Text));

				return;
			}

			var Content = formatter(state, exception);

			if (Content == null)
				return;

			if (exception != null)
				_Target.Write(new ExceptionLogEntry(new System.Diagnostics.StackFrame(1), Content, exception));
			else
				_Target.Write(new LogEntry(new System.Diagnostics.StackFrame(1), Level, Content));
		}

		//****************************************

		void ITerminalListener.Clear()
		{
		}

		void ITerminalListener.Log(ConsoleRecord record) => Log(record.Severity, default, record, record.Exception, Formatter);

		//****************************************

		private static string Formatter(ConsoleRecord record, Exception? exception) => record.Text;

		//****************************************

		private sealed class LoggerScope : IDisposable
		{
			internal static LoggerScope Default { get; } = new LoggerScope();

			//****************************************

			void IDisposable.Dispose()
			{
			}
		}
	}
}
