using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Threading.Tasks
{
	/// <summary>
	/// Contains extension methods for <see cref="ValueTask" /> and <see cref="ValueTask{TResult}"/>
	/// </summary>
	public static class ValueTaskExtensions
	{
		/// <summary>
		/// Interleaves an enumeration of tasks, returning the results in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <returns>An enumeration that returns the tasks in order of completion</returns>
		public static IAsyncEnumerable<ValueTask<TResult>> Interleave<TResult>(this IEnumerable<ValueTask<TResult>> source) => new InterleaveTask<TResult>(source, default);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to cancel the enumeration</param>
		/// <returns>An enumeration that returns the task and the index of the original task in order of completion</returns>
		public static IAsyncEnumerable<(ValueTask<TResult> result, int index)> InterleaveIndex<TResult>(this IEnumerable<ValueTask<TResult>> source, CancellationToken token) => new InterleaveTask<TResult>(source, token);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public static ValueTask When(this ValueTask task, CancellationToken token) => task.When(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given timeout to occur
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="milliseconds">The number of millisecond before we abort waiting for the original task</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public static ValueTask When(this ValueTask task, int milliseconds, CancellationToken token = default) => task.When(new TimeSpan(milliseconds * TimeSpan.TicksPerMillisecond), token);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="timeout">The time before we abort waiting for the original task</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task that completes with the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static ValueTask When(this ValueTask task, TimeSpan timeout, CancellationToken token = default)
		{
			if ((!token.CanBeCanceled && timeout == Timeout.InfiniteTimeSpan) || task.IsCompleted)
				return task;

			var Instance = TaskCancelInstance<VoidStruct>.GetOrCreate(token, timeout);

			Instance.Attach(task);

			return new ValueTask(Instance, Instance.Version);
		}

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public static ValueTask<TResult> When<TResult>(this ValueTask<TResult> task, CancellationToken token) => task.When(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given timeout to occur
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="milliseconds">The number of millisecond before we abort waiting for the original task</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public static ValueTask<TResult> When<TResult>(this ValueTask<TResult> task, int milliseconds, CancellationToken token = default) => task.When(TimeSpan.FromMilliseconds(milliseconds), token);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="timeout">The time before we abort waiting for the original task</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public static ValueTask<TResult> When<TResult>(this ValueTask<TResult> task, TimeSpan timeout, CancellationToken token = default)
		{
			if ((!token.CanBeCanceled && timeout == Timeout.InfiniteTimeSpan) || task.IsCompleted)
				return task;

			var Instance = TaskCancelInstance<TResult>.GetOrCreate(token, timeout);

			Instance.Attach(task);

			return new ValueTask<TResult>(Instance, Instance.Version);
		}

		/// <summary>
		/// Waits until all given ValueTasks have completed
		/// </summary>
		/// <param name="tasks">The Tasks to wait on</param>
		/// <returns>A ValueTask that completes when all given Tasks complete</returns>
		public static async ValueTask WhenAll(this IEnumerable<ValueTask> tasks)
		{
			if (tasks is null)
				throw new ArgumentNullException(nameof(tasks));

			List<Exception>? Exceptions = null;

			foreach (var Task in tasks)
			{
				try
				{
					await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);
		}

		/// <summary>
		/// Waits until all given ValueTasks have completed
		/// </summary>
		/// <typeparam name="T">The type of the result from each Task</typeparam>
		/// <param name="tasks">The Tasks to wait on</param>
		/// <returns>A ValueTask that completes when all given Tasks complete</returns>
		public static ValueTask<T[]> WhenAll<T>(this IEnumerable<ValueTask<T>> tasks)
		{
			if (tasks is null)
				throw new ArgumentNullException(nameof(tasks));

			if (tasks is ValueTask<T>[] ArrayTasks)
				return InternalWhenAll(ArrayTasks);

			if (tasks is IReadOnlyCollection<ValueTask<T>> ReadOnlyCollection)
			{
				if (ReadOnlyCollection.Count == 0)
					return new ValueTask<T[]>(Array.Empty<T>());

				var Index = 0;

				ArrayTasks = new ValueTask<T>[ReadOnlyCollection.Count];

				foreach (var CurrentTask in tasks)
					ArrayTasks[Index++] = CurrentTask;

				return InternalWhenAll(ArrayTasks);
			}

			if (tasks is ICollection<ValueTask<T>> Collection)
			{
				if (Collection.Count == 0)
					return new ValueTask<T[]>(Array.Empty<T>());

				var Index = 0;

				ArrayTasks = new ValueTask<T>[Collection.Count];

				foreach (var CurrentTask in tasks)
					ArrayTasks[Index++] = CurrentTask;

				return InternalWhenAll(ArrayTasks);
			}

			var ListTasks = new List<ValueTask<T>>(tasks);

			if (ListTasks.Count == 0)
				return new ValueTask<T[]>(Array.Empty<T>());

			return InternalWhenAll(ListTasks);
		}

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter ThenWaitOn(this ValueTask task1, ValueTask task2) => new ValueTaskWaiter(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T> ThenWaitOn<T>(this ValueTask<T> task1, ValueTask task2) => new ValueTaskWaiter<T>(task2, task1);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T> ThenWaitOn<T>(this ValueTask task1, ValueTask<T> task2) => new ValueTaskWaiter<T>(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T> ThenWaitOn<T>(this ValueTask<T> task1, ValueTask<T> task2) => new ValueTaskWaiter<T>(task1, task2);

		/// <summary>
		/// Waits on two or more <see cref="ValueTask"/> instances
		/// </summary>
		/// <param name="task1">The first task to wait on</param>
		/// <param name="task2">The second task to wait on</param>
		/// <returns>A new waiter that waits on all the supplied tasks</returns>
		public static ValueTaskWaiter<T1,T2> ThenWaitOn<T1,T2>(this ValueTask<T1> task1, ValueTask<T2> task2) => new ValueTaskWaiter<T1, T2>(task1, task2);

		//****************************************

		/// <summary>
		/// Transforms a Task to a ValueTask
		/// </summary>
		/// <param name="task">The Task to transform</param>
		/// <returns>The resulting ValueTask</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ValueTask AsValueTask(this Task task) => new ValueTask(task);

		/// <summary>
		/// Transforms a Task with a result to a ValueTask
		/// </summary>
		/// <typeparam name="TResult">The type of result</typeparam>
		/// <param name="task">The Task to transform</param>
		/// <returns>The resulting ValueTask</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ValueTask<TResult> AsValueTask<TResult>(this Task<TResult> task) => new ValueTask<TResult>(task);

		//****************************************

		private static async ValueTask<T[]> InternalWhenAll<T>(IReadOnlyCollection<ValueTask<T>> tasks)
		{
			var Results = new T[tasks.Count];
			List<Exception>? Exceptions = null;
			var Index = 0;

			foreach (var Task in tasks)
			{
				try
				{
					Results[Index++] = await Task;
				}
				catch (Exception e)
				{
					if (Exceptions == null)
						Exceptions = new List<Exception>();

					Exceptions.Add(e);
				}
			}

			if (Exceptions != null)
				throw new AggregateException(Exceptions);

			return Results;
		}
	}
}
