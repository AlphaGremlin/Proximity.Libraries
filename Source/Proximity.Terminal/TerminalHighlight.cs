using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a custom scope providing additional highlight context
	/// </summary>
	public sealed class TerminalHighlight : IEnumerable<KeyValuePair<string, object>>
	{
		private TerminalHighlight(string name) => Name = name;

		//****************************************

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			// Enables capturing this scope as a property for other scope providers
			yield return new KeyValuePair<string, object>("TerminalHighlight", Name);
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();

		//****************************************

		/// <summary>
		/// Gets the name of the highlight
		/// </summary>
		public string Name { get; }

		//****************************************

		/// <summary>
		/// Represents a console command
		/// </summary>
		public static TerminalHighlight ConsoleCommand { get; } = new TerminalHighlight("Command");

		/// <summary>
		/// Represents a milestone entry
		/// </summary>
		public static TerminalHighlight Milestone { get; } = new TerminalHighlight("Milestone");

		/// <summary>
		/// Maps a name to a valid Terminal Highlight
		/// </summary>
		/// <param name="name">The name of the Highlight</param>
		/// <param name="scope">Receives the matching Terminal Highlight if successful</param>
		/// <returns>True if the name was valid, otherwise False</returns>
		public static bool FromName(string name,
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out TerminalHighlight scope)
		{
			switch (name)
			{
			case "Command":
				scope = ConsoleCommand;
				return true;

			case "Milestone":
				scope = Milestone;
				return true;

			default:
				scope = null!;
				return false;
			}
		}
	}
}
