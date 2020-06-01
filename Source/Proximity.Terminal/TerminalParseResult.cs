using System;
using System.Collections.Generic;
using System.Text;
using Proximity.Terminal.Metadata;

namespace Proximity.Terminal
{
	public readonly ref struct TerminalParseResult
	{
		internal TerminalParseResult(TerminalTypeSet? typeSet, TerminalTypeInstance? instance, ReadOnlySpan<char> command, ReadOnlySpan<char> arguments)
		{
			TypeSet = typeSet;
			Instance = instance;
			CommandSet = null;
			Variable = null;
			Command = command;
			Arguments = arguments;
		}

		internal TerminalParseResult(TerminalTypeSet? typeSet, TerminalTypeInstance? instance, TerminalCommandSet? commandSet, ReadOnlySpan<char> command, ReadOnlySpan<char> arguments)
		{
			TypeSet = typeSet;
			Instance = instance;
			CommandSet = commandSet;
			Variable = null;
			Command = command;
			Arguments = arguments;
		}

		internal TerminalParseResult(TerminalTypeSet? typeSet, TerminalTypeInstance? instance,  TerminalVariable? variable, ReadOnlySpan<char> command, ReadOnlySpan<char> arguments)
		{
			TypeSet = typeSet;
			Instance = instance;
			CommandSet = null;
			Variable = variable;
			Command = command;
			Arguments = arguments;
		}

		//****************************************

		public TerminalTypeSet? TypeSet{ get; }

		public TerminalTypeInstance? Instance { get; }

		public TerminalCommandSet? CommandSet { get; }

		public TerminalVariable? Variable { get; }

		public ReadOnlySpan<char> Command { get; }

		public ReadOnlySpan<char> Arguments { get; }
	}
}
