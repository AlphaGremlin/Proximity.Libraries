/****************************************\
 LogEntry.cs
 Created: 2-06-2009
\****************************************/
#if !MOBILE && !PORTABLE
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
		private static int CurrentIndex = 1;
		//****************************************
		private StackFrame _Source;
		
		private DateTime _Timestamp;

		private Severity _Severity;
		private int _ThreadId;
		//private int _Priority;
		private long _Index;
		private string _Text;
		//****************************************
		
		/// <summary>
		/// Creates a new Log Entry
		/// </summary>
		/// <param name="source">The location the logging occurred</param>
		/// <param name="severity">The severity of the log entry</param>
		/// <param name="text">The text of the entry</param>
		[SecuritySafeCritical]
		public LogEntry(StackFrame source, Severity severity, string text)
		{
			_Index = Interlocked.Increment(ref CurrentIndex);
			_Timestamp = LogManager.GetTimestamp();
			
			_ThreadId = Thread.CurrentThread.ManagedThreadId;
			
			_Source = source;
			_Severity = severity;
			_Text = text;
		}
		
		//****************************************
		
		/// <summary>
		/// Retrieves the text of this log entry
		/// </summary>
		/// <returns>The text of the log entry</returns>
		public override string ToString()
		{
			return _Text;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the severity of this log entry
		/// </summary>
		public Severity Severity
		{
			get { return _Severity; }
		}
		
		/// <summary>
		/// Gets the ThreadId of the thread this entry was logged on
		/// </summary>
		public int ThreadId
		{
			get { return _ThreadId; }
		}
		
		/*
		public int Priority
		{
			get { return _Priority; }
		}
		*/
		
		/// <summary>
		/// Gets the index of this log entry. Always increments
		/// </summary>
		public long Index
		{
			get { return _Index; }
		}
		
		/// <summary>
		/// Gets the text of this log entry
		/// </summary>
		public string Text
		{
			get { return _Text; }
		}
		
		/// <summary>
		/// Gets the time this log entry occurred
		/// </summary>
		public DateTime Timestamp
		{
			get { return _Timestamp; }
		}
		
		/// <summary>
		/// Gets the amount of elapsed time since the program started
		/// </summary>
		public TimeSpan RelativeTime
		{
			[SecuritySafeCritical]
			get { return _Timestamp.Subtract(LogManager.StartTime); }
		}
		
		/// <summary>
		/// Gets the source method that logged this entry
		/// </summary>
		public MethodBase Source
		{
			get
			{
				if (_Source == null)
					return null;
				else
					return _Source.GetMethod();
			}
		}
	}
}
#endif