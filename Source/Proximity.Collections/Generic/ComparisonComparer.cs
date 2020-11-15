using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
//****************************************

namespace System.Collections.Generic
{
	/// <summary>
	/// Provides an IComparer wrapper around a Comparison delegate
	/// </summary>
	/// <typeparam name="T">The type of value being compared</typeparam>
	public sealed class ComparisonComparer<T> : IComparer<T>
	{	//****************************************
		private readonly Comparison<T> _Comparison;
		//****************************************

		/// <summary>
		/// Creates a new Comparison Comparer
		/// </summary>
		/// <param name="comparison">The Comparison delegate to wrap</param>
		public ComparisonComparer(Comparison<T> comparison) => _Comparison = comparison;

		//****************************************

		/// <summary>
		/// Compares two values of the same type
		/// </summary>
		/// <param name="x">The first value to compare</param>
		/// <param name="y">The second value to compare</param>
		/// <returns>The result of the comparison</returns>
		public int Compare(T x, T y) => _Comparison(x, y);
	}
}
