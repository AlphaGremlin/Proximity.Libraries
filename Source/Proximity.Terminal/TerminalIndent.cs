using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Proximity.Terminal
{
	/// <summary>
	/// Represents text that should be indented
	/// </summary>
	public readonly struct TerminalIndent : IEnumerable<KeyValuePair<string, object>>
	{ //****************************************
		private static readonly AsyncLocal<int> _CurrentIndent = new();
		//****************************************
		private readonly int _PreviousLevels;
		//****************************************

		private TerminalIndent(int levels, int previous)
		{
			if (levels <= 0)
				throw new ArgumentOutOfRangeException(nameof(levels));

			Levels = levels;
			_PreviousLevels = previous;
		}

		//****************************************

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			// Enables capturing this scope as a property for other scope providers
			yield return new KeyValuePair<string, object>(ScopeProperty, Levels);
		}

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();

		internal void Reset() => _CurrentIndent.Value = _PreviousLevels;

		//****************************************

		/// <summary>
		/// Gets the number of indentation levels
		/// </summary>
		public int Levels { get; }

		/// <summary>
		/// Gets the property name returned for scope providers
		/// </summary>
		public const string ScopeProperty = "TerminalIndent";

		//****************************************

		/// <summary>
		/// Gets a scope for use with <see cref="ILogger.BeginScope{TerminalIndent}(TerminalIndent)"/> that captures and increases the indentation level
		/// </summary>
		public static TerminalIndent Increase()
		{
			var Value = _CurrentIndent.Value;

			return new TerminalIndent(_CurrentIndent.Value = Value + 1, Value);
		}

		/// <summary>
		/// Gets a scope for use with <see cref="ILogger.BeginScope{TerminalIndent}(TerminalIndent)"/> that captures and replaces the indentation level
		/// </summary>
		public static TerminalIndent Replace(int levels)
		{
			var Value = _CurrentIndent.Value;

			return new TerminalIndent(_CurrentIndent.Value = levels, Value);
		}

		//****************************************

		internal static int Current => _CurrentIndent.Value;
	}
}
