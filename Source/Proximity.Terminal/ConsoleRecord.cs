using System;
using System.Drawing;
using Proximity.Logging;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a single entry in the console
	/// </summary>
	public sealed class ConsoleRecord
	{
		internal ConsoleRecord(string text)
		{
			ConsoleColour = ConsoleColor.Green;
			Timestamp = DateTime.Now;

			Text = text;
			Severity = Severity.None;
		}
		
		internal ConsoleRecord(ConsoleLogEntry entry)
		{
			ConsoleColour = ConsoleColor.Green;
			Timestamp = entry.Timestamp;

			Text = entry.Text;
			Severity = Severity.None;
		}

		internal ConsoleRecord(LogEntry entry, string text, int indentLevel)
		{
			switch(entry.Severity)
			{
			case Severity.Critical:
				ConsoleColour = ConsoleColor.Red;
				break;
				
			case Severity.Error:
				ConsoleColour = ConsoleColor.Red;
				break;
				
			case Severity.Warning:
				ConsoleColour = ConsoleColor.Yellow;
				break;
			
			case Severity.Milestone:
				ConsoleColour = ConsoleColor.Cyan;
				break;
				
			case Severity.Info:
				ConsoleColour = ConsoleColor.White;
				break;
				
			case Severity.Debug:
				ConsoleColour = ConsoleColor.DarkGray;
				break;
				
			case Severity.Verbose:
			default:
				ConsoleColour = ConsoleColor.Gray;
				break;
			}
			
			Text = text;
			Severity = entry.Severity;
			Timestamp = entry.Timestamp;
			IndentLevel = indentLevel;
		}

		//****************************************

		/// <summary>
		/// Gets the console colour of this record
		/// </summary>
		public ConsoleColor ConsoleColour { get; }

		/// <summary>
		/// Gets the text displayed by this record
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the time this record was created
		/// </summary>
		public DateTime Timestamp { get; }

		/// <summary>
		/// Gets the severity of this entry
		/// </summary>
		/// <remarks>Determines the <see cref="ConsoleColour" /> value</remarks>
		public Severity Severity { get; }

		/// <summary>
		/// Gets the indentation level
		/// </summary>
		public int IndentLevel { get; }
	}
}
