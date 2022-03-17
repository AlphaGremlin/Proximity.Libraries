using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents an active indentation scope
	/// </summary>
	public readonly struct TerminalIndentLevel : IDisposable
	{ //****************************************
		private readonly TerminalIndent _Indent;
		private readonly IDisposable _Scope;
		//****************************************

		private TerminalIndentLevel(ILogger logger)
		{
			_Indent = TerminalIndent.Increase();
			_Scope = logger.BeginScope(_Indent);
		}

		//****************************************

		/// <summary>
		/// Ends the indentation scope
		/// </summary>
		public void Dispose()
		{
			_Scope.Dispose();
			_Indent.Reset();
		}

		//****************************************

		/// <summary>
		/// Begins a scope with <see cref="ILogger.BeginScope{TerminalIndent}(TerminalIndent)"/> that increases the indentation level
		/// </summary>
		public static TerminalIndentLevel Increase(ILogger logger) => new TerminalIndentLevel(logger);
	}
}
