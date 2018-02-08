/****************************************\
 LogSection.cs
 Created: 2-06-2009
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Security;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Represents a logical grouping for entries
	/// </summary>
	[Serializable]
	public sealed class LogSection : IDisposable
	{	//****************************************
		private LogEntry _Entry;
		
		private int _Priority;
		
		private bool _IsDisposed;
		//****************************************
		
		/// <summary>
		/// Creates a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		[SecuritySafeCritical]
		public LogSection(LogEntry entry)
		{
			_Entry = entry;
			
			LogManager.StartSection(this);
		}
		
		/// <summary>
		/// Creates a new logging section
		/// </summary>
		/// <param name="entry">The logging entry to start with</param>
		/// <param name="priority">The priority of this entry</param>
		[SecuritySafeCritical]
		public LogSection(LogEntry entry, int priority)
		{
			_Entry = entry;
			_Priority = priority;
			
			LogManager.StartSection(this);
		}
		
		//****************************************
		
		/// <summary>
		/// Ends the logging section
		/// </summary>
		[SecuritySafeCritical]
		public void Dispose()
		{
			LogManager.FinishSection(this);
		}
		
		//****************************************

		/// <summary>
		/// Gets the log entry written to start this section
		/// </summary>
		public LogEntry Entry
		{
			get { return _Entry; }
		}
		
		/// <summary>
		/// Gets the text describing this section
		/// </summary>
		public string Text
		{
			get { return _Entry.Text; }
		}
		
		/// <summary>
		/// Gets the priority of this section
		/// </summary>
		public int Priority
		{
			get { return _Priority; }
		}
		
		/// <summary>
		/// Gets whether this section has been disposed
		/// </summary>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
			internal set { _IsDisposed = value; }
		}
	}
}
#endif