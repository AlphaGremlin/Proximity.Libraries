using System;
using System.Diagnostics;
using Proximity.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a log entry written as part of console processing
	/// </summary>
	public sealed class ConsoleLogEntry : LogEntry
	{
		/// <summary>
		/// Creates a new console log entry
		/// </summary>
		/// <param name="text">The text that was entered into the console</param>
		public ConsoleLogEntry(string text) : base(null, Severity.Info, text)
		{
		}
	}
}
