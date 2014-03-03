﻿/****************************************\
 TerminalLogOutput.cs
 Created: 2014-03-03
\****************************************/
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Proximity.Utility.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Captures logging output and sends it to the Terminal
	/// </summary>
	public sealed class TerminalLogOutput : LogOutput
	{	//****************************************
		private List<ConsoleRecord> _History;
		private int _MaxHistoryRecords = 1024 * 4;
		
		[ThreadStatic()] private static int IndentLevel;
		[ThreadStatic()] private static StringBuilder OutputBuilder;
		//****************************************
		
		public TerminalLogOutput() : base()
		{
			_History = new List<ConsoleRecord>();
		}
		
		public TerminalLogOutput(int maxHistoryRecords) : base()
		{
			_History = new List<ConsoleRecord>();
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
			
			IndentLevel++;
		}

		/// <inheritdoc />
		protected override void Write(LogEntry newEntry)
		{	//****************************************
			ConsoleRecord MyRecord;
			//****************************************

			if (newEntry is ConsoleLogEntry)
			{
				MyRecord = new ConsoleRecord((ConsoleLogEntry)newEntry);
			}
			else
			{
				if (OutputBuilder == null)
					OutputBuilder = new StringBuilder();
				
				OutputBuilder.Append(' ', IndentLevel * 2);
				OutputBuilder.Append(newEntry.Text);
				
				MyRecord = new ConsoleRecord(newEntry, OutputBuilder.ToString());
				
				OutputBuilder.Length = 0;
			}
			
			//****************************************
			
			ThresholdWrite(MyRecord);
			
			TerminalManager.WriteLine(MyRecord.Text, MyRecord.ConsoleColour);
			
			//****************************************
				
			if (newEntry is ExceptionLogEntry)
			{
				foreach(string EntryLine in ((ExceptionLogEntry)newEntry).Exception.ToString().Split(new string[] {Environment.NewLine}, StringSplitOptions.None))
				{
					OutputBuilder.Append(' ', IndentLevel * 2);
					OutputBuilder.Append(EntryLine);
		
					//****************************************
					
					MyRecord = new ConsoleRecord(newEntry, OutputBuilder.ToString());
					
					ThresholdWrite(MyRecord);
					
					TerminalManager.WriteLine(MyRecord.Text, MyRecord.ConsoleColour);
					
					OutputBuilder.Length = 0;
				}
			}
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
		protected override void FinishSection()
		{
			IndentLevel--;
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
			lock (_History)
			{
				if (_History.Count == _MaxHistoryRecords)
					_History.RemoveAt(0); // Moves the whole list down one element. Next addition will not cause an allocation
				
				_History.Add(newRecord);
			}
		}
	}
}
