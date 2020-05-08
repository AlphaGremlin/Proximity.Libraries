using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	public interface ITerminal : ILogger
	{
		IReadOnlyList<TerminalRegistry> Registries { get; }
	}
}
