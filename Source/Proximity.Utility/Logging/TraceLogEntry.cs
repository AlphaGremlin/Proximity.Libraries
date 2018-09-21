using System;
using System.Diagnostics;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// A log entry coming from the Diagnostics Trace
	/// </summary>
	public class TraceLogEntry : LogEntry
	{
		/// <summary>
		/// Creates a new trace entry from the trace event outputs
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="source"></param>
		/// <param name="eventType"></param>
		/// <param name="text"></param>
		public TraceLogEntry(TraceEventCache cache, string source, TraceEventType eventType, string text) : base(null, MapFromTraceType(eventType), text)
		{
		}
		
		/// <summary>
		/// Creates a new trace entry
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="text"></param>
		public TraceLogEntry(Severity severity, string text) : base(null, severity, text)
		{
		}

		//****************************************

		private static Severity MapFromTraceType(TraceEventType eventType)
		{
			switch (eventType)
			{
			case TraceEventType.Critical:
				return Severity.Critical;

			case TraceEventType.Error:
				return Severity.Error;

			case TraceEventType.Verbose:
				return Severity.Verbose;

			case TraceEventType.Warning:
				return Severity.Warning;

			case TraceEventType.Information:
			default:
				return Severity.Info;
			}
		}
	}
}