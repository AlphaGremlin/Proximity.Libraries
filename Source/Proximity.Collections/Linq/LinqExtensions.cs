using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq
{
	/// <summary>
	/// A delegate describing a SelectWhere implementation
	/// </summary>
	/// <typeparam name="TInput">The type of input to the select</typeparam>
	/// <typeparam name="TOutput">The type of output from the select</typeparam>
	/// <param name="input">The input value</param>
	/// <param name="output">The output value</param>
	/// <returns>True to select this value, otherwise False</returns>
	public delegate bool SelectWherePredicate<in TInput, TOutput>(TInput input, out TOutput output);

	/// <summary>
	/// Provides useful Linq-style methods
	/// </summary>
	public static class LinqExtensions
	{
		/// <summary>
		/// Performs a Select-Where operation
		/// </summary>
		/// <typeparam name="TInput">The type of input to the select</typeparam>
		/// <typeparam name="TOutput">The type of output from the select</typeparam>
		/// <param name="input">The input enumeration</param>
		/// <param name="predicate">A predicate that performs the select-where</param>
		/// <returns>An enumeration of outputs where the SelectWhere predicate returned True</returns>
		public static IEnumerable<TOutput> SelectWhere<TInput, TOutput>(this IEnumerable<TInput> input, SelectWherePredicate<TInput, TOutput> predicate)
		{
			foreach (var InRecord in input)
			{
				if (predicate(InRecord, out var OutRecord))
					yield return OutRecord;
			}
		}

		/// <summary>
		/// Performs a Select-Where operation
		/// </summary>
		/// <typeparam name="TInput">The type of input to the select</typeparam>
		/// <typeparam name="TOutput">The type of output from the select</typeparam>
		/// <typeparam name="TResult">The type of output from the result selector</typeparam>
		/// <param name="input">The input enumeration</param>
		/// <param name="predicate">A predicate that performs the select-where</param>
		/// <param name="resultSelector">A selector that generates the result of the operation</param>
		/// <returns>An enumeration of outputs where the SelectWhere predicate returned True</returns>
		public static IEnumerable<TResult> SelectWhere<TInput, TOutput, TResult>(this IEnumerable<TInput> input, SelectWherePredicate<TInput, TOutput> predicate, Func<TInput, TOutput, TResult> resultSelector)
		{
			foreach (var InRecord in input)
			{
				if (predicate(InRecord, out var OutRecord))
					yield return resultSelector(InRecord, OutRecord);
			}
		}
	}
}
