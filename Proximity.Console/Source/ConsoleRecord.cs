/****************************************\
 ConsoleRecord.cs
 Created: 6-06-2009
\****************************************/
using System;
using System.Drawing;
using Proximity.Utility.Logging;
//****************************************

namespace Proximity.Console
{
	/// <summary>
	/// Represents a single entry in the console
	/// </summary>
	public class ConsoleRecord
	{	//****************************************
		private DateTime _Timestamp;
		private Severity _Severity;
		private string _Text;
		
		private KnownColor _KnownColour;
		private ConsoleColor _ConsoleColour;
		//****************************************
				
		internal ConsoleRecord(string text)
		{
			_KnownColour = KnownColor.Green;
			_ConsoleColour = ConsoleColor.Green;
			_Timestamp = DateTime.Now;

			_Text = text;
			_Severity = Severity.None;
		}
		
		internal ConsoleRecord(ConsoleLogEntry entry)
		{
			_KnownColour = KnownColor.Green;
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
				_KnownColour = KnownColor.Red;
				_ConsoleColour = ConsoleColor.Red;
				break;
				
			case Severity.Error:
				_KnownColour = KnownColor.Orange;
				_ConsoleColour = ConsoleColor.Magenta;
				break;
				
			case Severity.Warning:
				_KnownColour = KnownColor.Yellow;
				_ConsoleColour = ConsoleColor.Yellow;
				break;
			
			case Severity.Milestone:
				_KnownColour = KnownColor.Teal;
				_ConsoleColour = ConsoleColor.Cyan;
				break;
				
			case Severity.Info:
				_KnownColour = KnownColor.White;
				_ConsoleColour = ConsoleColor.White;
				break;
				
			case Severity.Debug:
				_KnownColour = KnownColor.DarkGray;
				_ConsoleColour = ConsoleColor.DarkGray;
				break;
				
			case Severity.Verbose:
			default:
				_KnownColour = KnownColor.Gray;
				_ConsoleColour = ConsoleColor.Gray;
				break;
			}
			
			_Text = text;
			_Severity = entry.Severity;
			_Timestamp = entry.Timestamp;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the System.Drawing colour of this record
		/// </summary>
		public KnownColor KnownColour
		{
			get { return _KnownColour; }
		}
		
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
		/// <remarks>Determines the <see cref="KnownColour" /> and <see cref="ConsoleColour" /> values</remarks>
		public Severity Severity
		{
			get { return _Severity; }
		}
	}
}
