/****************************************\
 TraceOutput.cs
 Created: 2-06-2009
\****************************************/
using System;
using System.Xml;
using System.Diagnostics;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Writes log entries to the Diagnostics Trace
	/// </summary>
	public sealed class TraceOutput : LogOutput
	{	//****************************************
		private bool _DebuggerOnly;
		
		private TraceSource _Source;
		//****************************************
		
		/// <summary>
		/// Creates a new Trace Log Output
		/// </summary>
		public TraceOutput()
		{
		}
		
		/// <summary>
		/// Creates a new Trace Log Output
		/// </summary>
		/// <param name="reader">An XmlReader describing the settings for the Log Output</param>
		public TraceOutput(XmlReader reader) : base(reader)
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Starts the logging output process
		/// </summary>
		protected internal override void Start()
		{
			_Source = new TraceSource(System.Reflection.Assembly.GetEntryAssembly().GetName().Name);
		}
		
		/// <summary>
		/// Starts a logging section for this thread
		/// </summary>
		/// <param name="newSection">The details of the new logging section</param>
		protected internal override void StartSection(LogSection newSection)
		{
			Trace.CorrelationManager.StartLogicalOperation(newSection.Text);
			Trace.WriteLine(newSection.Text);

			Trace.Indent();
		}

		/// <summary>
		/// Writes an entry to the log
		/// </summary>
		/// <param name="newEntry">The log entry to write</param>
		protected internal override void Write(LogEntry newEntry)
		{
			if (newEntry is TraceLogEntry)
				return;
			
			Trace.WriteLine(newEntry.Text);
			
			if (newEntry is ExceptionLogEntry)
				foreach(string EntryLine in ((ExceptionLogEntry)newEntry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
					Trace.WriteLine(EntryLine);

			/*			
			Console.WriteLine(newEntry.Text);
			
			if (newEntry is ExceptionLogEntry)
				foreach(string EntryLine in ((ExceptionLogEntry)newEntry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
					Console.WriteLine(EntryLine);
			*/
		}
		
		/// <summary>
		/// Ends a logging section for this thread
		/// </summary>
		protected internal override void FinishSection()
		{
			Trace.Unindent();

			Trace.CorrelationManager.StopLogicalOperation();
		}
		
		/// <summary>
		/// Ends the logging output process
		/// </summary>
		protected internal override void Finish()
		{
			_Source.Close();
		}
		
		//****************************************
		
		/// <summary>
		/// Reads an attribute from the configuration
		/// </summary>
		/// <param name="name">The name of the attribute</param>
		/// <param name="value">The attribute's value</param>
		/// <returns>True if the Attribute is known, otherwise False</returns>
		protected override bool ReadAttribute(string name, string value)
		{
			switch (name)
			{
			case "DebuggerOnly":
				bool.TryParse(value, out _DebuggerOnly);
				break;
				
			default:
				return base.ReadAttribute(name, value);
			}
			
			return true;
		}
		
		//****************************************
		
		private static TraceEventType MapToTraceType(Severity severity)
		{
			switch (severity)
			{
			case Severity.Critical:
				return TraceEventType.Critical;

			case Severity.Error:
				return TraceEventType.Error;

			case Severity.Verbose:
				return TraceEventType.Verbose;

			case Severity.Warning:
				return TraceEventType.Warning;

			case Severity.Info:
			case Severity.Milestone:
			default:
				return TraceEventType.Information;
			}
		}
		
		//****************************************

		/// <summary>
		/// Gets/Sets whether trace output sends to the Debug source or Trace source
		/// </summary>
		public bool DebuggerOnly
		{
			get { return _DebuggerOnly; }
			set { _DebuggerOnly = value; }
		}
	}
}
