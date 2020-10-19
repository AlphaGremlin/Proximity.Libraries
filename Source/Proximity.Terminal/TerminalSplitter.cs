using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	/// <summary>
	/// Routes terminal output and commands to multiple terminals and loggers
	/// </summary>
	public sealed class TerminalSplitter : ITerminal
	{
		/// <summary>
		/// Creates a terminal splitter that routes terminal output to a logger
		/// </summary>
		/// <param name="terminal">The underlying terminal</param>
		/// <param name="logger">An external logger to route to</param>
		public TerminalSplitter(ITerminal terminal, ILogger logger) : this(new[] { terminal }, new[] { logger })
		{
		}

		/// <summary>
		/// Creates a terminal splitter that routes terminal output to a logger
		/// </summary>
		/// <param name="terminals">The underlying terminasl</param>
		/// <param name="loggers">The external loggers to route to</param>
		public TerminalSplitter(IReadOnlyCollection<ITerminal> terminals, IReadOnlyCollection<ILogger> loggers)
		{
			Terminals = terminals;
			Loggers = loggers;
			Registries = terminals.SelectMany(terminal => terminal.Registries).Distinct().ToArray();
		}

		//****************************************

		IDisposable ILogger.BeginScope<TState>(TState state)
		{
			var Scopes = new List<IDisposable>();

			foreach (var Terminal in Terminals)
			{
				var NewScope = Terminal.BeginScope(state);

				if (NewScope != null)
					Scopes.Add(NewScope);
			}

			foreach (var Logger in Loggers)
			{
				var NewScope = Logger.BeginScope(state);

				if (NewScope != null)
					Scopes.Add(NewScope);
			}

			return new Scope(Scopes);
		}

		void ITerminal.Clear()
		{
			foreach (var Terminal in Terminals)
				Terminal.Clear();
		}

		bool ILogger.IsEnabled(LogLevel logLevel) => true; // We route everything onwards

		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			foreach (var Terminal in Terminals)
				Terminal.Log(logLevel, eventId, state, exception, formatter);

			foreach (var Logger in Loggers)
				Logger.Log(logLevel, eventId, state, exception, formatter);
		}

		//****************************************

		/// <summary>
		/// Gets the underlying terminal
		/// </summary>
		public IReadOnlyCollection<ITerminal> Terminals { get; }

		/// <summary>
		/// Gets the external logger to route to
		/// </summary>
		public IReadOnlyCollection<ILogger> Loggers { get; }

		/// <summary>
		/// Gets the command registries available to this terminal
		/// </summary>
		public IReadOnlyList<TerminalRegistry> Registries { get; }

		//****************************************

		private sealed class Scope : IDisposable
		{ //****************************************
			private readonly IReadOnlyCollection<IDisposable> _Scopes;
			//****************************************

			public Scope(IReadOnlyCollection<IDisposable> scopes)
			{
				_Scopes = scopes;
			}

			//****************************************

			void IDisposable.Dispose()
			{
				foreach (var Scope in _Scopes)
					Scope.Dispose();
			}
		}
	}
}
