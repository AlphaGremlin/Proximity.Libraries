using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a custom scope for a command for extra presentation information
	/// </summary>
	public sealed class TerminalScope
	{
		private TerminalScope(string name) => Name = name;

		//****************************************

		/// <summary>
		/// Gets the name of the scope
		/// </summary>
		public string Name { get; }

		//****************************************

		/// <summary>
		/// Represents a console command
		/// </summary>
		public static TerminalScope ConsoleCommand { get; } = new TerminalScope("Command");

		/// <summary>
		/// Represents a milestone entry
		/// </summary>
		public static TerminalScope Milestone { get; } = new TerminalScope("Milestone");
	}
}
