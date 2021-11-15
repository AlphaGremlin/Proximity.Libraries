using System;
using System.Collections.Generic;
using System.Collections.ReadOnly;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Collections
{
#if NET40
	/// <summary>
	/// Provides static empty arrays
	/// </summary>
	public static class Empty
	{
		/// <summary>
		/// Returns an empty array of element type T
		/// </summary>
		/// <typeparam name="T">The type of array elements</typeparam>
		/// <returns>An empty array of element type T</returns>
		public static T[] Array<T>() => Empty<T>.Array;

		/// <summary>
		/// Returns an empty array of element type T
		/// </summary>
		/// <typeparam name="T">The type of array elements</typeparam>
		/// <returns>An empty array of element type T</returns>
		public static IReadOnlyList<T> ReadOnlyList<T>() => Empty<T>.ReadOnlyList;
	}

	internal static class Empty<T>
	{
		public static T[] Array { get; } = new T[0];

		public static IReadOnlyList<T> ReadOnlyList { get; } = new ReadOnlyList<T>(Array);
	}
#else
	/// <summary>
	/// Shim to allow easier .Net 4.0 compatibility, should be a noop in other runtime targets
	/// </summary>
	internal static class Empty
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Array<T>() => System.Array.Empty<T>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IReadOnlyList<T> ReadOnlyList<T>() => System.Array.Empty<T>();
	}
#endif
}
