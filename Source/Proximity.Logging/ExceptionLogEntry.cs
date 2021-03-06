using System;
using System.Diagnostics;
//****************************************

namespace Proximity.Logging
{
	/// <summary>
	/// A log Entry representing an Exception
	/// </summary>
	public class ExceptionLogEntry : LogEntry
	{
		/// <summary>
		/// Creates a new Exception log entry
		/// </summary>
		/// <param name="source">The location the logging occurred</param>
		/// <param name="text">The text explaining the exception</param>
		/// <param name="exception">The exception that occurred</param>
		public ExceptionLogEntry(StackFrame source, string text, Exception exception) : base(source, Severity.Error, text)
		{
			Exception = exception;
		}

		//****************************************

		/// <inheritdoc />
		public override string ToString() => Text + "\n" + Exception.ToString();

		//****************************************

		/// <summary>
		/// Gets the Exception represented
		/// </summary>
		public Exception Exception { get; }
	}
}