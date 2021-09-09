using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	/// <summary>
	/// Describes a terminal that receives text data and accepts a set of commands
	/// </summary>
	public sealed class TerminalView : TerminalRouter, ITerminal, ITerminalListener
	{ //****************************************
		private readonly HashSet<LogLevel> _IsEnabled = new();
		//****************************************

		/// <summary>
		/// Creates a new Terminal View over the global command registry
		/// </summary>
		public TerminalView() : this(TerminalRegistry.Global)
		{
		}

		/// <summary>
		/// Creates a new Terminal View
		/// </summary>
		/// <param name="registry">The command registry</param>
		public TerminalView(TerminalRegistry registry) : this((IEnumerable<TerminalRegistry>)new[] { registry })
		{
		}

		/// <summary>
		/// Creates a new Terminal View
		/// </summary>
		/// <param name="registries">The set of command registries</param>
		public TerminalView(params TerminalRegistry[] registries) : this((IEnumerable<TerminalRegistry>)registries)
		{
		}

		/// <summary>
		/// Creates a new Terminal View
		/// </summary>
		/// <param name="registries">The set of command registries</param>
		public TerminalView(IEnumerable<TerminalRegistry> registries)
		{
			Registries = registries.ToArray();
			SetMinimum(LogLevel.Trace);
		}

		//****************************************

		IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Default;

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
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (state is ConsoleRecord StateRecord)
			{
				((ITerminalListener)this).Log(StateRecord);

				return;
			}

			if (!IsEnabled(logLevel))
				return;

			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));

			var Message = formatter(state, exception);

			if (string.IsNullOrEmpty(Message) && exception == null)
				return;

			var Record = new ConsoleRecord(DateTimeOffset.Now, logLevel, Message, exception: exception);

			((ITerminalListener)this).Log(Record);
		}

		/// <summary>
		/// Clear this Terminal View
		/// </summary>
		public void Clear()
		{
			foreach (var Listener in Listeners)
				Listener.Clear();
		}

		//****************************************

		void ITerminalListener.Clear()
		{
			// A View cannot be cleared by an upstream View
		}

		void ITerminalListener.Log(ConsoleRecord record)
		{
			foreach (var Listener in Listeners)
				Listener.Log(record);
		}

		//****************************************

		/// <summary>
		/// Gets the command registries used by this view, in order of processing
		/// </summary>
		public IReadOnlyList<TerminalRegistry> Registries { get; }

		//****************************************

		private sealed class NullScope : IDisposable
		{
			internal static readonly NullScope Default = new();

			void IDisposable.Dispose()
			{
			}
		}
	}
}
