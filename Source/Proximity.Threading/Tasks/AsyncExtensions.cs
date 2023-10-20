using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading.Tasks
{
	/// <summary>
	/// Provides extensions for <see cref="IAsyncEnumerable{T}"/>
	/// </summary>
	public static class AsyncExtensions
	{
		/// <summary>
		/// Unwraps an <see cref="IEnumerable{T}"/> where T is a <see cref="Task{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of the task results</typeparam>
		/// <param name="source">The enumerable returning Tasks to unwrap</param>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> with the result of each task</returns>
		/// <remarks>Any failed tasks will cause the enumeration to throw</remarks>
		public static async IAsyncEnumerable<T> Unwrap<T>(this IEnumerable<Task<T>> source)
		{
			foreach (var Value in source)
			{
				yield return await Value;
			}
		}

		/// <summary>
		/// Unwraps an <see cref="IEnumerable{T}"/> where T is a <see cref="ValueTask{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of the task results</typeparam>
		/// <param name="source">The enumerable returning Tasks to unwrap</param>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> with the result of each task</returns>
		/// <remarks>Any failed tasks will cause the enumeration to throw</remarks>
		public static async IAsyncEnumerable<T> Unwrap<T>(this IEnumerable<ValueTask<T>> source)
		{
			foreach (var Value in source)
			{
				yield return await Value;
			}
		}

		/// <summary>
		/// Unwraps an <see cref="IAsyncEnumerable{T}"/> where T is a <see cref="Task{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of the task results</typeparam>
		/// <param name="source">The enumerable returning Tasks to unwrap</param>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> with the result of each task</returns>
		/// <remarks>Any failed tasks will cause the enumeration to throw</remarks>
		public static async IAsyncEnumerable<T> Unwrap<T>(this IAsyncEnumerable<Task<T>> source)
		{
			await foreach (var Value in source)
			{
				yield return await Value;
			}
		}

		/// <summary>
		/// Unwraps an <see cref="IAsyncEnumerable{T}"/> where T is a <see cref="ValueTask{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of the task results</typeparam>
		/// <param name="source">The enumerable returning Tasks to unwrap</param>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> with the result of each task</returns>
		/// <remarks>Any failed tasks will cause the enumeration to throw</remarks>
		public static async IAsyncEnumerable<T> Unwrap<T>(this IAsyncEnumerable<ValueTask<T>> source)
		{
			await foreach (var Value in source)
			{
				yield return await Value;
			}
		}
	}
}
