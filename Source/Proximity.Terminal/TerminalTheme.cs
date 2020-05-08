using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	/// <summary>
	/// Manages the appearance of the local terminal
	/// </summary>
	[CLSCompliant(false)]
	public abstract class TerminalTheme
	{
		/// <summary>
		/// Gets the default terminal theme
		/// </summary>
		public static TerminalTheme Default { get; } = new DefaultTheme();

		//****************************************

		/// <summary>
		/// Gets the colour associated with a particular logging level
		/// </summary>
		/// <param name="level">The relevant logging level</param>
		/// <returns>The associated colour</returns>
		public abstract ConsoleColor GetColour(LogLevel level);

		//****************************************

		/// <summary>
		/// Gets the text to use for the prompt
		/// </summary>
		public abstract string Prompt { get; }

		/// <summary>
		/// Gets the colour to use for the prompt
		/// </summary>
		public abstract ConsoleColor PromptColour { get; }

		//****************************************

		private sealed class DefaultTheme : TerminalTheme
		{
			public override ConsoleColor GetColour(LogLevel level)
			{
				return level switch
				{
					LogLevel.Critical => ConsoleColor.DarkMagenta,
					LogLevel.Error => ConsoleColor.Red,
					LogLevel.Warning => ConsoleColor.Yellow,
					LogLevel.None => ConsoleColor.Cyan,
					LogLevel.Information => ConsoleColor.White,
					LogLevel.Debug => ConsoleColor.Blue,
					_ => ConsoleColor.Gray,
				};
			}

			public override string Prompt => "> ";

			public override ConsoleColor PromptColour => ConsoleColor.Green;
		}
	}
}
