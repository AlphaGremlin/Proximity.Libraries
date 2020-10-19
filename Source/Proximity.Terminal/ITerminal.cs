using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents the interface for a terminal
	/// </summary>
	public interface ITerminal : ILogger
	{
		/// <summary>
		/// Clears the terminal
		/// </summary>
		void Clear();

		//****************************************

		/// <summary>
		/// Gets the command registries available to this terminal
		/// </summary>
		IReadOnlyList<TerminalRegistry> Registries { get; }
	}
}
