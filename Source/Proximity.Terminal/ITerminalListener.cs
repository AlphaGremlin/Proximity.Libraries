using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Terminal
{
	public interface ITerminalListener
	{
		void Clear();

		void Log(ConsoleRecord record);
	}
}
