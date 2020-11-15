using System;
using System.Drawing;
using Microsoft.Extensions.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a single entry on a terminal
	/// </summary>
	public sealed class ConsoleRecord
	{
		/// <summary>
		/// Creates a new console record
		/// </summary>
		/// <param name="timestamp">The time the entry was recorded</param>
		/// <param name="severity">The severity level</param>
		/// <param name="text">The text content</param>
		/// <param name="indentation">The indentation to display with</param>
		/// <param name="scope">The scope providing additional context</param>
		/// <param name="exception">An exception associated with the record</param>
		public ConsoleRecord(DateTimeOffset timestamp, LogLevel severity, string text, int indentation = 0, TerminalScope? scope = null, Exception? exception = null)
		{
			Timestamp = timestamp;
			Severity = severity;
			Scope = scope;
			Exception = exception;
			Text = text;
			Indentation = indentation;
		}

		//****************************************

		/// <summary>
		/// Gets the text displayed by this record
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the indentation to apply to this record
		/// </summary>
		public int Indentation { get; }

		/// <summary>
		/// Gets the time this record was created
		/// </summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>
		/// Gets the severity of this entry
		/// </summary>
		public LogLevel Severity { get; }

		/// <summary>
		/// Gets the scope of the record
		/// </summary>
		public TerminalScope? Scope { get; }

		/// <summary>
		/// Gets the exception associated with the record
		/// </summary>
		public Exception? Exception { get; }
	}
}
