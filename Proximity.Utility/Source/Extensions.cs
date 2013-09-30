/****************************************\
 Extensions.cs
 Created: 2013-04-30
\****************************************/
using System;
using System.Text;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// General API extensions
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Sanitises a string, ensuring it is within a valid display range
		/// </summary>
		/// <param name="inputText">The input string to sanitise</param>
		/// <returns>The input text, sanitised to valid display range</returns>
		/// <remarks>Replaces all characters below 32, except for carriage returns, linefeeds and tabs, with space</remarks>
		public static string SanitiseForDisplay(this string inputText)
		{
			foreach(var MyChar in inputText)
			{
				if (MyChar > 31)
					continue;
				
				if (MyChar == '\n' || MyChar == '\r' || MyChar == '\t')
					continue;
				
				// Invalid character found, check the whole string
				var MyBuilder = new StringBuilder(inputText.Length);
				
				foreach(var MyOutChar in inputText)
				{
					if (MyOutChar > 31 || MyOutChar == '\n' || MyOutChar == '\r' || MyOutChar == '\t')
						MyBuilder.Append(MyOutChar);
					else
						MyBuilder.Append(' ');
				}
				
				inputText = MyBuilder.ToString();
				
				break;
			}
			
			return inputText;
		}
	}
}
