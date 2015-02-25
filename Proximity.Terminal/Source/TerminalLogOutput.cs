/****************************************\
 TerminalLogOutput.cs
 Created: 2014-03-03
\****************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Text;
using Proximity.Utility.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Captures logging output and sends it to the Terminal
	/// </summary>
	public sealed class TerminalLogOutput : LogOutput
	{	//****************************************
		private readonly List<ConsoleRecord> _History;
		private int _MaxHistoryRecords = 1024 * 4;
		
		private readonly ThreadLocal<StringBuilder> _OutputBuilder;
		//****************************************
		
		/// <summary>
		/// Creates a new terminal log outputter
		/// </summary>
		public TerminalLogOutput() : base()
		{
			_History = new List<ConsoleRecord>();
			_OutputBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder());
		}

		/// <summary>
		/// Creates a new terminal log outputter
		/// </summary>
		/// <param name="maxHistoryRecords">Determines how many records are kept as history</param>
		public TerminalLogOutput(int maxHistoryRecords)
			: this()
		{
			_MaxHistoryRecords = maxHistoryRecords;
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected override void Start()
		{
		}
		
		/// <inheritdoc />
		protected override void StartSection(LogSection newSection)
		{
			Write(newSection.Entry);
		}

		/// <inheritdoc />
		protected override void Write(LogEntry newEntry)
		{	//****************************************
			ConsoleRecord MyRecord;
			
			var MyBuilder = _OutputBuilder.Value;
			var IndentLevel = LogManager.SectionDepth;
			//****************************************

			if (newEntry is ConsoleLogEntry)
			{
				MyRecord = new ConsoleRecord((ConsoleLogEntry)newEntry);
			}
			else
			{
				MyBuilder.Append(' ', IndentLevel * 2);
				MyBuilder.Append(newEntry.Text);
				
				MyRecord = new ConsoleRecord(newEntry, MyBuilder.ToString(), IndentLevel);
				
				MyBuilder.Length = 0;
			}
			
			//****************************************
			
			ThresholdWrite(MyRecord);
			
			TerminalManager.WriteLine(MyRecord.Text, MyRecord.ConsoleColour);
			
			//****************************************
				
			if (newEntry is ExceptionLogEntry)
			{
				foreach(string EntryLine in ((ExceptionLogEntry)newEntry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
				{
					MyBuilder.Append(' ', IndentLevel * 2);
					MyBuilder.Append(EntryLine);
		
					//****************************************
					
					MyRecord = new ConsoleRecord(newEntry, MyBuilder.ToString(), IndentLevel);
					
					ThresholdWrite(MyRecord);
					
					TerminalManager.WriteLine(MyRecord.Text, MyRecord.ConsoleColour);
					
					MyBuilder.Length = 0;
				}
			}
		}
		
		/// <inheritdoc />
		protected override void Flush()
		{
		}
		
		/// <summary>
		/// Prints a line to the console, bypassing the log
		/// </summary>
		/// <param name="text">The text to write directly to the console</param>
		public void Print(string text)
		{	//****************************************
			ConsoleRecord MyRecord = new ConsoleRecord(text);
			//****************************************
			
			ThresholdWrite(MyRecord);
			
			TerminalManager.WriteLine(MyRecord.Text, MyRecord.ConsoleColour);
		}
		
		/// <inheritdoc />
		protected override void FinishSection(LogSection oldSection)
		{
		}
		
		/// <inheritdoc />
		protected override void Finish()
		{
			_History.Clear();
		}
		
		//****************************************
		
		/// <summary>
		/// Clears the console output (including history)
		/// </summary>
		public void Clear()
		{
			TerminalManager.Clear();
			
			lock (_History)
				_History.Clear();
		}
		
		/// <summary>
		/// Adds the most recent console records into the target list
		/// </summary>
		/// <param name="target">The list to add to</param>
		/// <param name="count">The number of records to add</param>
		/// <returns>The number of records written</returns>
		/// <remarks>Records will be added from oldest to newest. If there are not <paramref name="count" /> records in the history, will add every record</remarks>
		public int CopyTo(IList<ConsoleRecord> target, int count)
		{	//****************************************
			int Index, Length, Finish;
			//****************************************
			
			lock (_History)
			{
				Length = Math.Min(count, _History.Count);
				
				for (Index = _History.Count - Length, Finish = Index + Length; Index < Finish; Index++)
				{
					target.Add(_History[Index]);
				}
			}
				
			return Length;
		}
		
		//****************************************
		
		private void ThresholdWrite(ConsoleRecord newRecord)
		{
			if (_MaxHistoryRecords == 0)
				return;

			lock (_History)
			{
				if (_History.Count == _MaxHistoryRecords)
					_History.RemoveAt(0); // Moves the whole list down one element. Next addition will not cause an allocation
				
				_History.Add(newRecord);
			}
		}
	}
}
