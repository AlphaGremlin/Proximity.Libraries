using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Collections.Generic
{
#if NET40
	/// <summary>
	/// Constructs comparers around a delegate
	/// </summary>
	public static class DelegateComparer
	{
		/// <summary>
		/// Creates an IComparer instance
		/// </summary>
		/// <typeparam name="T">The type to compare</typeparam>
		/// <param name="comparer">The comparison method</param>
		/// <returns>A comparer that calls the given delgate</returns>
		public static IComparer<T> Create<T>(Comparison<T> comparer) => new DelegateComparer<T>(comparer);
	}

	internal sealed class DelegateComparer<T> : IComparer<T>
	{ //****************************************
		private readonly Comparison<T> _Comparer;
		//****************************************

		public DelegateComparer(Comparison<T> comparer) => _Comparer = comparer;

		//****************************************

		int IComparer<T>.Compare(T x, T y) => _Comparer(x, y);
	}

#else
	/// <summary>
	/// Shim to allow easier .Net 4.0 compatibility, should be a noop in other runtime targets
	/// </summary>
	internal static class DelegateComparer
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IComparer<T> Create<T>(Comparison<T> comparer) => Comparer<T>.Create(comparer);
	}
#endif
}
