using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Proximity.Terminal.Tests
{
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
			Assert.IsTrue(TerminalParser.TryParse("IntVariable".AsSpan(), _Registries, out var Result));

			Assert.IsNull(Result.TypeSet);
			Assert.IsNull(Result.Instance);

			Assert.IsNotNull(Result.Variable);
			Assert.AreEqual("IntVariable", Result.Variable.Name);

			Assert.IsTrue(Result.Arguments.IsEmpty);
		}

		[Test]
		public void StaticIntVariableSet()
		{
			Assert.IsTrue(TerminalParser.TryParse("IntVariable=1234".AsSpan(), _Registries, out var Result));

			Assert.IsNull(Result.TypeSet);
			Assert.IsNull(Result.Instance);

			Assert.IsNotNull(Result.Variable);
			Assert.AreEqual("IntVariable", Result.Variable.Name);

			Assert.IsFalse(Result.Arguments.IsEmpty);
			Assert.AreEqual("1234", Result.Arguments.AsString());
		}

		[Test]
		public void StaticCommandNoArgs()
		{
			Assert.IsTrue(TerminalParser.TryParse("CommandNoArgs".AsSpan(), _Registries, out var Result));

			Assert.IsNull(Result.TypeSet);
			Assert.IsNull(Result.Instance);

			Assert.IsNotNull(Result.CommandSet);
			Assert.AreEqual("CommandNoArgs", Result.CommandSet.Name);

			Assert.IsTrue(Result.Arguments.IsEmpty);
		}

		[Test]
		public void StaticCommandStringArgs()
		{
			Assert.IsTrue(TerminalParser.TryParse("CommandStringArg ABC".AsSpan(), _Registries, out var Result));

			Assert.IsNull(Result.TypeSet);
			Assert.IsNull(Result.Instance);

			Assert.IsNotNull(Result.CommandSet);
			Assert.AreEqual("Command", Result.CommandSet.Name);

			Assert.IsTrue(Result.Arguments.IsEmpty);
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
}
