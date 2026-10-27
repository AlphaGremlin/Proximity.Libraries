using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Terminal.Tests;

[TestFixture]
public class TerminalParserTests
{ //****************************************
	private TerminalRegistry[] _Registries;
	//****************************************

	[OneTimeSetUp]
	public void Setup()
	{
		var Registry = new TerminalRegistry();

		Registry.Scan(typeof(StaticProvider));

		_Registries = new[] { Registry };
	}

	//****************************************

	[Test]
	public void StaticIntVariableGet()
	{
		Assert.That(TerminalParser.TryParse("IntVariable".AsSpan(), _Registries, out var Result), Is.True);

		Assert.That(Result.TypeSet, Is.Null);
		Assert.That(Result.Instance, Is.Null);

		Assert.That(Result.Variable, Is.Not.Null);
		Assert.That(Result.Variable.Name, Is.EqualTo("IntVariable"));

		Assert.That(Result.Arguments.IsEmpty, Is.True);
	}

	[Test]
	public void StaticIntVariableSet()
	{
		Assert.That(TerminalParser.TryParse("IntVariable=1234".AsSpan(), _Registries, out var Result), Is.True);

		Assert.That(Result.TypeSet, Is.Null);
		Assert.That(Result.Instance, Is.Null);

		Assert.That(Result.Variable, Is.Not.Null);
		Assert.That(Result.Variable.Name, Is.EqualTo("IntVariable"));

		Assert.That(Result.Arguments.IsEmpty, Is.False);
		Assert.That(Result.Arguments.AsString(), Is.EqualTo("1234"));
	}

	[Test]
	public void StaticCommandNoArgs()
	{
		Assert.That(TerminalParser.TryParse("CommandNoArgs".AsSpan(), _Registries, out var Result), Is.True);

		Assert.That(Result.TypeSet, Is.Null);
		Assert.That(Result.Instance, Is.Null);

		Assert.That(Result.CommandSet, Is.Not.Null);
		Assert.That(Result.CommandSet.Name, Is.EqualTo("CommandNoArgs"));

		Assert.That(Result.Arguments.IsEmpty, Is.True);
	}

	[Test]
	public void StaticCommandStringArgs()
	{
		Assert.That(TerminalParser.TryParse("CommandStringArg ABC".AsSpan(), _Registries, out var Result), Is.True);

		Assert.That(Result.TypeSet, Is.Null);
		Assert.That(Result.Instance, Is.Null);

		Assert.That(Result.CommandSet, Is.Not.Null);
		Assert.That(Result.CommandSet.Name, Is.EqualTo("CommandStringArg"));

		Assert.That(Result.Arguments.AsString(), Is.EqualTo("ABC"));
	}

	//****************************************

	[TerminalProvider]
	private static class StaticProvider
	{
		[TerminalBinding]
		public static int IntVariable { get; set; }

		[TerminalBinding]
		public static void CommandNoArgs()
		{
		}

		[TerminalBinding]
		public static void CommandStringArg(ITerminal terminal, string text)
		{
		}
	}
}
