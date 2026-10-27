using System;
using System.Collections.Generic;
using System.Text;
using Proximity.Terminal.Metadata;

namespace Proximity.Terminal;

/// <summary>
/// Represents the results of parsing a terminal command
/// </summary>
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

	/// <summary>
	/// Gets the set of type instances being referenced
	/// </summary>
	public TerminalTypeSet? TypeSet { get; }

	/// <summary>
	/// Gest the referenced type instance
	/// </summary>
	public TerminalTypeInstance? Instance { get; }

	/// <summary>
	/// Gets the set of commands being referenced
	/// </summary>
	public TerminalCommandSet? CommandSet { get; }

	/// <summary>
	/// Gets the referenced variable
	/// </summary>
	public TerminalVariable? Variable { get; }

	/// <summary>
	/// Gets the name of the command
	/// </summary>
	public ReadOnlySpan<char> Command { get; }

	/// <summary>
	/// Gets the arguments to the command
	/// </summary>
	public ReadOnlySpan<char> Arguments { get; }
}
