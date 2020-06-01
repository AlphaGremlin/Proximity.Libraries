using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	public interface ITerminal : ILogger
	{
		void Clear();

		//****************************************

		IReadOnlyList<TerminalRegistry> Registries { get; }
	}
}
