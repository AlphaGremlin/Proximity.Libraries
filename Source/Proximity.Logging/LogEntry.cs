using System;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Security;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// A single entry in the Log
	/// </summary>
	public class LogEntry
	{	//****************************************
		private static long CurrentIndex = 1;
		//****************************************
		private readonly StackFrame _Source;
		//****************************************

		/// <summary>
		/// Creates a new Log Entry
		/// </summary>
		/// <param name="source">The location the logging occurred</param>
		/// <param name="severity">The severity of the log entry</param>
		/// <param name="text">The text of the entry</param>
		public LogEntry(StackFrame source, Severity severity, string text)
		{
			Index = Interlocked.Increment(ref CurrentIndex);
			Timestamp = LogManager.GetTimestamp();
			
			ThreadId = Thread.CurrentThread.ManagedThreadId;
			
			_Source = source;
			Severity = severity;
			Text = text;
		}

		/// <summary>
		/// Clones an existing Log Entry
		/// </summary>
		/// <param name="source">The source Log Entry</param>
		public LogEntry(LogEntry source)
		{
			Index = source.Index;
			Timestamp = source.Timestamp;

			ThreadId = source.ThreadId;

			_Source = null; // We don't pass the source StackFrame, since it can result in assembly loading when crossing app-domains
			Severity = source.Severity;
			Text = source.ToString();
		}

		//****************************************

		/// <inheritdoc />
		public override string ToString() => Text;

		//****************************************

		/// <summary>
		/// Gets the severity of this log entry
		/// </summary>
		public Severity Severity { get; }

		/// <summary>
		/// Gets the ThreadId of the thread this entry was logged on
		/// </summary>
		public int ThreadId { get; }

		/// <summary>
		/// Gets the index of this log entry. Always increments
		/// </summary>
		public long Index { get; }

		/// <summary>
		/// Gets the text of this log entry
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the time this log entry occurred
		/// </summary>
		public DateTime Timestamp { get; }

		/// <summary>
		/// Gets the source method that logged this entry
		/// </summary>
		public MethodBase Source => _Source?.GetMethod();
	}
}