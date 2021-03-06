using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	/// <summary>
	/// Manages the appearance of the local terminal
	/// </summary>
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
		/// <param name="level">The relevant logging level, or null if this is a reported console command </param>
		/// <param name="scope">The custom scope for the entry being logged</param>
		/// <returns>The associated colour</returns>
		public abstract ConsoleColor GetColour(LogLevel? level, TerminalScope? scope);

		//****************************************

		/// <summary>
		/// Gets the text to use for the prompt
		/// </summary>
		public abstract string Prompt { get; }

		/// <summary>
		/// Gets the colour to use for the prompt
		/// </summary>
		public abstract ConsoleColor PromptColour { get; }

		/// <summary>
		/// Gets whether to echo entered console commands back to the terminal
		/// </summary>
		public abstract bool EchoCommands { get; }

		/// <summary>
		/// Gets the text to use for each indentation level (spaces/tabs/etc)
		/// </summary>
		public abstract string Indentation { get; }

		//****************************************

		private sealed class DefaultTheme : TerminalTheme
		{
			public override ConsoleColor GetColour(LogLevel? level, TerminalScope? scope)
			{
				if (scope == TerminalScope.ConsoleCommand)
					return ConsoleColor.Green;

				if (scope == TerminalScope.Milestone)
					return ConsoleColor.Cyan;

				return level switch
				{
					LogLevel.Critical => ConsoleColor.DarkMagenta,
					LogLevel.Error => ConsoleColor.Red,
					LogLevel.Warning => ConsoleColor.Yellow,
					LogLevel.None => ConsoleColor.Cyan,
					LogLevel.Information => ConsoleColor.White,
					LogLevel.Debug => ConsoleColor.Blue,
					null => PromptColour,
					_ => ConsoleColor.Gray,
				};
			}

			public override string Prompt => "> ";

			public override ConsoleColor PromptColour => ConsoleColor.Green;

			public override bool EchoCommands => true;

			public override string Indentation => "  ";
		}
	}
}
