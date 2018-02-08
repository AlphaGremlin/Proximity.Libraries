/****************************************\
 LogListener.cs
 Created: 5-06-2009
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Output debug messages to a file for .NetCF
	/// </summary>
	public class LogListener : TraceListener
	{	//****************************************
		private StringBuilder Buffer;
		
		private Severity _DefaultSeverity = Severity.Info;
		//****************************************
		
		/// <summary>
		/// Creates a new log listener
		/// </summary>
		public LogListener()
		{
			Buffer = new StringBuilder(128);
		}
		
		//****************************************
		
		/// <summary>
		/// Writes a message to the log file
		/// </summary>
		/// <param name="message">The message to write</param>
		public override void Write(string message)
		{
			Buffer.Append(message);
		}
		
		/// <summary>
		/// Writes a message line to the log file
		/// </summary>
		/// <param name="message">The message to write</param>
		public override void WriteLine(string message)
		{	//****************************************
			LogEntry NewEntry;
			//****************************************
			
			Buffer.Append(message);
			
			NewEntry = new TraceLogEntry(_DefaultSeverity, Buffer.ToString());
			
			Buffer.Length = 0;
			
			Log.Write(NewEntry);
		}
		
		//****************************************

		/// <summary>
		/// Write a trace data entry
		/// </summary>
		/// <param name="eventCache"></param>
		/// <param name="source"></param>
		/// <param name="eventType"></param>
		/// <param name="id"></param>
		/// <param name="data"></param>
		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
		{
			if (data is LogEntry)
				return;
			
			Log.Write(new TraceLogEntry(eventCache, source, eventType, data.ToString()));
		}
		
		/// <summary>
		/// Write a trace data entry
		/// </summary>
		/// <param name="eventCache"></param>
		/// <param name="source"></param>
		/// <param name="eventType"></param>
		/// <param name="id"></param>
		/// <param name="data"></param>
		public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
		{	//****************************************
			string FlatData;
			//****************************************
		
			FlatData = string.Join(" ", data.Select(obj => obj.ToString()));
			
			Log.Write(new TraceLogEntry(eventCache, source, eventType, FlatData));
		}
		
		/// <summary>
		/// Write a trace event entry
		/// </summary>
		/// <param name="eventCache"></param>
		/// <param name="source"></param>
		/// <param name="eventType"></param>
		/// <param name="id"></param>
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
		{
			Log.Write(new TraceLogEntry(eventCache, source, eventType, string.Empty));
		}
		
		/// <summary>
		/// Write a trace event entry
		/// </summary>
		/// <param name="eventCache"></param>
		/// <param name="source"></param>
		/// <param name="eventType"></param>
		/// <param name="id"></param>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			Log.Write(new TraceLogEntry(eventCache, source, eventType, string.Format(CultureInfo.InvariantCulture, format, args)));
		}
		
		/// <summary>
		/// Write a trace event entry
		/// </summary>
		/// <param name="eventCache"></param>
		/// <param name="source"></param>
		/// <param name="eventType"></param>
		/// <param name="id"></param>
		/// <param name="message"></param>
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
		{
			Log.Write(new TraceLogEntry(eventCache, source, eventType, message));
		}
	}
}
#endif