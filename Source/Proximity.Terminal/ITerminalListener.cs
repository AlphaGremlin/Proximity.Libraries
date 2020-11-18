using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents a receiver of terminal actions
	/// </summary>
	public interface ITerminalListener
	{
		/// <summary>
		/// Instructs the receiver to clear any history
		/// </summary>
		void Clear();

		/// <summary>
		/// Instructs the receiver to process or display a new record
		/// </summary>
		/// <param name="record">The record sent to the terminal</param>
		void Log(ConsoleRecord record);
	}
}
