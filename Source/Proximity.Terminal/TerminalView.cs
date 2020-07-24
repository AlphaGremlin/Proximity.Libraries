using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	public sealed class TerminalView : ITerminal, ITerminalListener
	{ //****************************************
		private ImmutableHashSet<ITerminalListener> _Listeners = ImmutableHashSet<ITerminalListener>.Empty;

		private readonly HashSet<LogLevel> _IsEnabled = new HashSet<LogLevel>();
		//****************************************

		public TerminalView() : this(TerminalRegistry.Global)
		{
		}

		public TerminalView(TerminalRegistry registry) : this((IEnumerable<TerminalRegistry>)new[] { registry })
		{
		}

		public TerminalView(params TerminalRegistry[] registries) : this((IEnumerable<TerminalRegistry>)registries)
		{
		}

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
		[CLSCompliant(false)]
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
		[CLSCompliant(false)]
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
		[CLSCompliant(false)] public bool IsEnabled(LogLevel logLevel) => _IsEnabled.Contains(logLevel);

		/// <inheritdoc />
		[CLSCompliant(false)]
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

			var Record = new ConsoleRecord(DateTimeOffset.Now, logLevel, Message);

			((ITerminalListener)this).Log(Record);
		}

		/// <summary>
		/// Clear this Terminal View
		/// </summary>
		public void Clear()
		{
			foreach (var Listener in _Listeners)
				Listener.Clear();
		}

		/// <summary>
		/// Attach a listener to this Terminal
		/// </summary>
		/// <param name="listener">The listener that will receive events from this Terminal</param>
		/// <remarks>This keeps a strong reference to the listener, so care must be taken to detach</remarks>
		public void Attach(ITerminalListener listener) => ImmutableInterlockedEx.Add(ref _Listeners, listener);

		/// <summary>
		/// Removes a listener from this Terminal
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns>True if the listener was found and removed</returns>
		public bool Detach(ITerminalListener listener) => ImmutableInterlockedEx.Remove(ref _Listeners, listener);

		//****************************************

		void ITerminalListener.Clear()
		{
			// A View cannot be cleared by an upstream View
		}

		void ITerminalListener.Log(ConsoleRecord record)
		{
			foreach (var Listener in _Listeners)
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
			internal static readonly NullScope Default = new NullScope();

			void IDisposable.Dispose()
			{
			}
		}
	}
}
