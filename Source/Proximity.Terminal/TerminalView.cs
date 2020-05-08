using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	public sealed class TerminalView : ITerminal, ITerminalListener
	{ //****************************************
		private readonly WeakHashSet<ITerminalListener> _Listeners = new WeakHashSet<ITerminalListener>();
		//****************************************

		public TerminalView(TerminalRegistry registry) : this((IEnumerable<TerminalRegistry>)new[] { registry })
		{
		}

		public TerminalView(params TerminalRegistry[] registries) : this((IEnumerable<TerminalRegistry>)registries)
		{
		}

		public TerminalView(IEnumerable<TerminalRegistry> registries)
		{
			Registries = registries.ToArray();
		}



		//****************************************

		IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Default;

		[CLSCompliant(false)]
		public bool IsEnabled(LogLevel logLevel)
		{
			throw new NotImplementedException();
		}

		[CLSCompliant(false)]
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
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

		public void Attach(ITerminalListener listener)
		{
			_Listeners.Add(listener);
		}

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
		/// Gets/Sets the maximum history to record for this View
		/// </summary>
		public int? MaxHistory { get; set; }

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
