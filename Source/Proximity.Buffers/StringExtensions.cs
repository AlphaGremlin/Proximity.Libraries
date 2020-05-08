using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
	public static class StringExtensions
	{
		public static string Concat(this string source, ReadOnlySpan<char> arg1) => source.AsSpan().Concat(arg1);

		public static string Concat(this ReadOnlySpan<char> source, ReadOnlySpan<char> arg1)
		{
			Span<char> Result = stackalloc char[source.Length + arg1.Length];
			Span<char> Remainder = Result;

			source.CopyTo(Remainder);
			Remainder = Remainder.Slice(source.Length);
			arg1.CopyTo(Remainder);

			return Result.AsString();
		}

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

		public static bool StartsWith(this string source, ReadOnlySpan<char> value, StringComparison comparisonType) => source.AsSpan().StartsWith(value, comparisonType);
	}
}
