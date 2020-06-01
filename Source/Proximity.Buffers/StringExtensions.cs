using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
	/// <summary>
	/// Provides extension methods for concatenating <see cref="string"/> and <see cref="ReadOnlySpan{Char}"/>
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Concatenates a string and a character span, returning a new string
		/// </summary>
		/// <param name="source">The string to start with</param>
		/// <param name="arg1">The character span to append</param>
		/// <returns>The combined string</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Concat(this string source, ReadOnlySpan<char> arg1) => source.AsSpan().Concat(arg1);

		/// <summary>
		/// Concatenates two character spans, returning a new string
		/// </summary>
		/// <param name="source">The character span to start with</param>
		/// <param name="arg1">The character span to append</param>
		/// <returns>The combined string</returns>
		public static string Concat(this ReadOnlySpan<char> source, ReadOnlySpan<char> arg1)
		{
			Span<char> Result = stackalloc char[source.Length + arg1.Length];
			Span<char> Remainder = Result;

			source.CopyTo(Remainder);
			Remainder = Remainder.Slice(source.Length);
			arg1.CopyTo(Remainder);

			return Result.AsString();
		}

		/// <summary>
		/// Concatenates a string and two character spans, returning a new string
		/// </summary>
		/// <param name="source">The string to start with</param>
		/// <param name="arg1">The character span to append first</param>
		/// <param name="arg2">The character span to append second</param>
		/// <returns>The combined string</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Concat(this string source, ReadOnlySpan<char> arg1, ReadOnlySpan<char> arg2) => source.AsSpan().Concat(arg1, arg2);

		/// <summary>
		/// Concatenates three character spans, returning a new string
		/// </summary>
		/// <param name="source">The character span to start with</param>
		/// <param name="arg1">The character span to append first</param>
		/// <param name="arg2">The character span to append second</param>
		/// <returns>The combined string</returns>
		public static string Concat(this ReadOnlySpan<char> source, ReadOnlySpan<char> arg1, ReadOnlySpan<char> arg2)
		{
			Span<char> Result = stackalloc char[source.Length + arg1.Length + arg2.Length];
			Span<char> Remainder = Result;

			source.CopyTo(Remainder);
			Remainder = Remainder.Slice(source.Length);
			arg1.CopyTo(Remainder);
			Remainder = Remainder.Slice(arg1.Length);
			arg2.CopyTo(Remainder);

			return Result.AsString();
		}

		/// <summary>
		/// Concatenates a string and three character spans, returning a new string
		/// </summary>
		/// <param name="source">The string to start with</param>
		/// <param name="arg1">The character span to append first</param>
		/// <param name="arg2">The character span to append second</param>
		/// <param name="arg3">The character span to append last</param>
		/// <returns>The combined string</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Concat(this string source, ReadOnlySpan<char> arg1, ReadOnlySpan<char> arg2, ReadOnlySpan<char> arg3) => source.AsSpan().Concat(arg1, arg2, arg3);

		/// <summary>
		/// Concatenates four character spans, returning a new string
		/// </summary>
		/// <param name="source">The character span to start with</param>
		/// <param name="arg1">The character span to append first</param>
		/// <param name="arg2">The character span to append second</param>
		/// <param name="arg3">The character span to append last</param>
		/// <returns>The combined string</returns>
		public static string Concat(this ReadOnlySpan<char> source, ReadOnlySpan<char> arg1, ReadOnlySpan<char> arg2, ReadOnlySpan<char> arg3)
		{
			Span<char> Result = stackalloc char[source.Length + arg1.Length + arg2.Length + arg3.Length];
			Span<char> Remainder = Result;

			source.CopyTo(Remainder);
			Remainder = Remainder.Slice(source.Length);
			arg1.CopyTo(Remainder);
			Remainder = Remainder.Slice(arg1.Length);
			arg2.CopyTo(Remainder);
			Remainder = Remainder.Slice(arg2.Length);
			arg3.CopyTo(Remainder);

			return Result.AsString();
		}

		/// <summary>
		/// Checks whether a string ends with a character span
		/// </summary>
		/// <param name="source">The string to check</param>
		/// <param name="value">The character span to check for</param>
		/// <param name="comparisonType">The string comparison type to apply</param>
		/// <returns>True if the string ends with the given character span, otherwise False</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EndsWith(this string source, ReadOnlySpan<char> value, StringComparison comparisonType) => source.AsSpan().EndsWith(value, comparisonType);

		/// <summary>
		/// Calculates a HashCode for a character span
		/// </summary>
		/// <param name="span">The span to calculate</param>
		/// <param name="comparisonType">The string comparison to use for the hash calculation</param>
		/// <returns>The matching hash code</returns>
		public static int GetHashCode(this ReadOnlySpan<char> span, StringComparison comparisonType)
		{
			// TODO: Implement a proper GetHashCode for .Net Standard
#if NETSTANDARD2_1
			return span.AsString().GetHashCode(comparisonType);
#elif NETSTANDARD2_0
			return (comparisonType switch
			{
				StringComparison.Ordinal => StringComparer.Ordinal,
				StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
				StringComparison.InvariantCulture => StringComparer.InvariantCulture,
				StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
				StringComparison.CurrentCulture => StringComparer.CurrentCulture,
				StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
				_ => throw new ArgumentOutOfRangeException(nameof(comparisonType))
			}).GetHashCode(span.AsString());
#else
			return string.GetHashCode(span, comparisonType);
#endif
		}

		/// <summary>
		/// Checks whether a string starts with a character span
		/// </summary>
		/// <param name="source">The string to check</param>
		/// <param name="value">The character span to check for</param>
		/// <param name="comparisonType">The string comparison type to apply</param>
		/// <returns>True if the string starts with the given character span, otherwise False</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWith(this string source, ReadOnlySpan<char> value, StringComparison comparisonType) => source.AsSpan().StartsWith(value, comparisonType);
	}
}
