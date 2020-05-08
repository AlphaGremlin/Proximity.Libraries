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
		internal ConsoleRecord(DateTimeOffset timestamp, LogLevel severity, string text)
		{
			Timestamp = timestamp;
			Severity = severity;
			Text = text;
		}

		//****************************************

		/// <summary>
		/// Gets the text displayed by this record
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the time this record was created
		/// </summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>
		/// Gets the severity of this entry
		/// </summary>
		public LogLevel Severity { get; }
	}
}
