using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal.Hosting
{
	internal sealed class TerminalLogProvider : ILoggerProvider
	{ //****************************************
		private readonly ITerminal _Terminal;
		//****************************************

		internal TerminalLogProvider(ITerminal terminal)
		{
			_Terminal = terminal;
		}

		//****************************************

		ILogger ILoggerProvider.CreateLogger(string categoryName) => _Terminal;

		void IDisposable.Dispose()
		{
		}
	}
}
