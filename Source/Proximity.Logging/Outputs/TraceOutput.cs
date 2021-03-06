using System;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Xml;
using Proximity.Logging.Config;
using Proximity.Configuration;
//****************************************

namespace Proximity.Logging.Outputs
{
	/// <summary>
	/// Writes log entries to the Diagnostics Trace
	/// </summary>
	[TypedElement(typeof(TraceOutputElement))]
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
		
		//****************************************
		
		/// <inheritdoc />
		protected internal override void Configure(OutputElement config)
		{
			var MyConfig = (TraceOutputElement)config;
			
			_DebuggerOnly = MyConfig.DebuggerOnly;
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected internal override void Start()
		{
			_Source = new TraceSource((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name);
		}
		
		/// <inheritdoc />
		protected internal override void StartSection(LogSection newSection)
		{
			Trace.CorrelationManager.StartLogicalOperation(newSection.Text);
			
			Trace.IndentLevel = Target.Context.Count;
			Trace.WriteLine(newSection.Text);
		}

		/// <inheritdoc />
		protected internal override void Write(LogEntry newEntry)
		{
			if (newEntry is TraceLogEntry)
				return;
			
			Trace.IndentLevel = Target.Context.Count;
			Trace.WriteLine(newEntry.Text);
			
			if (newEntry is ExceptionLogEntry ExceptionEntry)
				foreach(string EntryLine in ExceptionEntry.Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
					Trace.WriteLine(EntryLine);

			/*			
			Console.WriteLine(newEntry.Text);
			
			if (newEntry is ExceptionLogEntry)
				foreach(string EntryLine in ((ExceptionLogEntry)newEntry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
					Console.WriteLine(EntryLine);
			*/
		}
		
		/// <inheritdoc />
		protected internal override void Flush()
		{
			Trace.Flush();
		}
		
		/// <inheritdoc />
		protected internal override void FinishSection(LogSection section)
		{
			Trace.IndentLevel = Target.Context.Count;

			if (Trace.CorrelationManager.LogicalOperationStack.Count != 0)
				Trace.CorrelationManager.StopLogicalOperation();
		}
		
		/// <inheritdoc />
		protected internal override void Finish()
		{
			_Source.Close();
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
