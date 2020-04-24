using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System
{
	/// <summary>
	/// Provides some useful extensions related to ReadOnlySpan
	/// </summary>
	public static class SpanExtensions
	{
		/// <summary>
		/// Converts a character span to a string
		/// </summary>
		/// <param name="span">The span to convert</param>
		/// <returns>A new string with the contents of the span</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETSTANDARD2_0
		public static string AsString(this ReadOnlySpan<char> span) => span.IsEmpty ? string.Empty : span.ToString();
#else
		public static string AsString(this ReadOnlySpan<char> span) => span.IsEmpty ? string.Empty : new string(span);
#endif
	}
}
