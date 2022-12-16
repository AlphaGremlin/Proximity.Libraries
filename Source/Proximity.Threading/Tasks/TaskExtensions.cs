using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Interleave;
using Proximity.Threading;
//****************************************

namespace System.Threading.Tasks
{
	/// <summary>
	/// Contains extension methods for <see cref="Task" />
	/// </summary>
	public static class TaskExtensions
	{
		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public static Task When(this Task task, CancellationToken token) => task.When(Timeout.InfiniteTimeSpan, token);

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
		public static Task When(this Task task, int milliseconds, CancellationToken token = default) => task.When(new TimeSpan(milliseconds * TimeSpan.TicksPerMillisecond), token);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="timeout">The time before we abort waiting for the original task</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task that completes with the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task When(this Task task, TimeSpan timeout, CancellationToken token = default)
		{
			if ((!token.CanBeCanceled && timeout == Timeout.InfiniteTimeSpan) || task.IsCompleted)
				return task;

			var Instance = TaskCancelInstance<VoidStruct>.GetOrCreate();

			Instance.Attach(task, token, timeout);

			return new ValueTask(Instance, Instance.Version).AsTask();
		}

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public static Task<TResult> When<TResult>(this Task<TResult> task, CancellationToken token) => task.When(Timeout.InfiniteTimeSpan, token);

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
		public static Task<TResult> When<TResult>(this Task<TResult> task, int milliseconds, CancellationToken token = default) => task.When(new TimeSpan(milliseconds * TimeSpan.TicksPerMillisecond), token);

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
		public static Task<TResult> When<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken token = default)
		{
			if ((!token.CanBeCanceled && timeout == Timeout.InfiniteTimeSpan) || task.IsCompleted)
				return task;

			var Instance = TaskCancelInstance<TResult>.GetOrCreate();

			Instance.Attach(task, token, timeout);

			return new ValueTask<TResult>(Instance, Instance.Version).AsTask();
		}

		/// <summary>
		/// Interleaves an enumeration of tasks, returning the results in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to cancel the enumeration</param>
		/// <returns>An enumeration that returns the tasks in order of completion</returns>
		public static InterleaveTaskAsyncEnumerable<TResult> Interleave<TResult>(this IEnumerable<Task<TResult>> source, CancellationToken token = default) => new(source, token);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to cancel the enumeration</param>
		/// <returns>An enumeration that returns the task and the original index of the task in order of completion</returns>
		public static InterleaveTaskIndexAsyncEnumerable<TResult> InterleaveIndex<TResult>(this IEnumerable<Task<TResult>> source, CancellationToken token = default) => new(source, token);
	}
}
