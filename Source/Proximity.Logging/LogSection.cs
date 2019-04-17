using System;
using System.Security;
//****************************************

namespace Proximity.Logging
{
	/// <summary>
	/// Represents a logical grouping for entries
	/// </summary>
	public sealed class LogSection : IDisposable
	{
		internal LogSection(LogTarget target, LogEntry entry, int priority)
		{
			Target = target;
			Entry = entry;
			Priority = priority;
		}
		
		//****************************************
		
		/// <summary>
		/// Ends the logging section
		/// </summary>
		public void Dispose()
		{
			if (Target != null)
				Target.FinishSection(this);
		}

		//****************************************

		/// <summary>
		/// Gets the log entry written to start this section
		/// </summary>
		public LogEntry Entry { get; }

		/// <summary>
		/// The logging target we're associated with
		/// </summary>
		public LogTarget Target { get; }

		/// <summary>
		/// Gets the text describing this section
		/// </summary>
		public string Text => Entry?.Text;

		/// <summary>
		/// Gets the priority of this section
		/// </summary>
		public int Priority { get; }

		/// <summary>
		/// Gets whether this section has been disposed
		/// </summary>
		public bool IsDisposed { get; internal set; }

		//****************************************

		internal static readonly LogSection Null = new LogSection(null, null, 0);
	}
}