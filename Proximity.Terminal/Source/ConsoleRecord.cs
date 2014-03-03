/****************************************\
 ConsoleRecord.cs
 Created: 6-06-2009
\****************************************/
using System;
using System.Drawing;
using Proximity.Utility.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a single entry in the console
	/// </summary>
	public sealed class ConsoleRecord
	{	//****************************************
		private DateTime _Timestamp;
		private Severity _Severity;
		private string _Text;
		
		private ConsoleColor _ConsoleColour;
		//****************************************
				
		internal ConsoleRecord(string text)
		{
			_ConsoleColour = ConsoleColor.Green;
			_Timestamp = DateTime.Now;

			_Text = text;
			_Severity = Severity.None;
		}
		
		internal ConsoleRecord(ConsoleLogEntry entry)
		{
			_ConsoleColour = ConsoleColor.Green;
			_Timestamp = entry.Timestamp;

			_Text = entry.Text;
			_Severity = Severity.None;
		}

		internal ConsoleRecord(LogEntry entry, string text)
		{
			switch(entry.Severity)
			{
			case Severity.Critical:
				_ConsoleColour = ConsoleColor.Red;
				break;
				
			case Severity.Error:
				_ConsoleColour = ConsoleColor.Magenta;
				break;
				
			case Severity.Warning:
				_ConsoleColour = ConsoleColor.Yellow;
				break;
			
			case Severity.Milestone:
				_ConsoleColour = ConsoleColor.Cyan;
				break;
				
			case Severity.Info:
				_ConsoleColour = ConsoleColor.White;
				break;
				
			case Severity.Debug:
				_ConsoleColour = ConsoleColor.DarkGray;
				break;
				
			case Severity.Verbose:
			default:
				_ConsoleColour = ConsoleColor.Gray;
				break;
			}
			
			_Text = text;
			_Severity = entry.Severity;
			_Timestamp = entry.Timestamp;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the console colour of this record
		/// </summary>
		public ConsoleColor ConsoleColour
		{
			get { return _ConsoleColour; }
		}
		
		/// <summary>
		/// Gets the text displayed by this record
		/// </summary>
		public string Text
		{
			get { return _Text; }
		}
		
		/// <summary>
		/// Gets the time this record was created
		/// </summary>
		public DateTime Timestamp
		{
			get { return _Timestamp; }
		}
		
		/// <summary>
		/// Gets the severity of this entry
		/// </summary>
		/// <remarks>Determines the <see cref="ConsoleColour" /> value</remarks>
		public Severity Severity
		{
			get { return _Severity; }
		}
	}
}
