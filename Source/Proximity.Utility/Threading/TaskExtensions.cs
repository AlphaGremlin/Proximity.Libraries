/****************************************\
 TaskExtensions.cs
 Created: 2011-08-02
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
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
		/// <returns>A task that completes with the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task When(this Task task, CancellationToken token)
		{
			if (!token.CanBeCanceled || task.IsCompleted)
				return task;

			var MySource = new TargetTask<VoidStruct>(task);

			// If the token cancels, we need to abort the task source
			MySource.Registration = token.Register(DoCancelTaskSource<VoidStruct>, MySource);

			// If the task completes, we need to pass the result (if any) to the task source
			// If the token cancels first, DoCancelTaskSource will take care of observing any potential exceptions
			task.ContinueWith(DoCompleteTaskSource, MySource, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

			return MySource.Task;
		}
		
		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task<TResult> When<TResult>(this Task<TResult> task, CancellationToken token)
		{
			if (!token.CanBeCanceled || task.IsCompleted)
				return task;

			var MySource = new TargetTask<TResult>(task);

			// If the token cancels, we need to abort the task source
			MySource.Registration = token.Register(DoCancelTaskSource<TResult>, MySource);

			// If the task completes, we need to pass the result (if any) to the task source
			// If the token cancels first, DoCancelTaskSource will take care of observing any potential exceptions
			task.ContinueWith(DoCompleteTaskSource, MySource, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

			return MySource.Task;
		}

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given timeout to occur
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="milliseconds">The number of millisecond before we abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task When(this Task task, int milliseconds) => task.When(TimeSpan.FromMilliseconds(milliseconds), CancellationToken.None);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given timeout to occur
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="timeout">The time before we abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task When(this Task task, TimeSpan timeout) => task.When(timeout, CancellationToken.None);

		/// <summary>
		/// Wraps the given task, waiting for it to complete, the given timeout to occur, or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="timeout">The time before we abort waiting for the original task</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task When(this Task task, TimeSpan timeout, CancellationToken token)
		{	//****************************************
			CancellationTokenSource MyCancelSource;
			//****************************************
			
			if (task.IsCompleted)
				return task;
			
			// If the token can cancel, wrap it with one that times out as well
			if (token.CanBeCanceled)
			{
				MyCancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);
			}
			else
			{
				// Can't cancel, just create a token source that times out
				MyCancelSource = new CancellationTokenSource();
			}

			MyCancelSource.CancelAfter(timeout);
			
			task = task.When(MyCancelSource.Token);
			
			// Ensure we cleanup the token source
			task.ContinueWith(DoDisposeCancelSource, MyCancelSource, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			
			return task;
		}

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given timeout to occur
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="milliseconds">The number of millisecond before we abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task<TResult> When<TResult>(this Task<TResult> task, int milliseconds) => task.When(TimeSpan.FromMilliseconds(milliseconds), CancellationToken.None);

		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given timeout to occur
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="timeout">The time before we abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task<TResult> When<TResult>(this Task<TResult> task, TimeSpan timeout) => task.When(timeout, CancellationToken.None);

		/// <summary>
		/// Wraps the given task, waiting for it to complete, the given timeout to occur, or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="timeout">The time before we abort waiting for the original task</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" />, unless the token cancels first. In which case, exceptions will be silently ignored (no UnhandledTaskException).</remarks>
		public static Task<TResult> When<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken token)
		{	//****************************************
			CancellationTokenSource MyCancelSource;
			//****************************************
			
			if (task.IsCompleted)
				return task;
			
			// If the token can cancel, wrap it with one that times out as well
			if (token.CanBeCanceled)
			{
				MyCancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);
			}
			else
			{
				// Can't cancel, just create a token source that times out
				MyCancelSource = new CancellationTokenSource();
			}
			MyCancelSource.CancelAfter(timeout);
			
			task = task.When(MyCancelSource.Token);
			
			// Ensure we cleanup the token source
			task.ContinueWith(DoDisposeCancelSource, MyCancelSource, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			
			return task;
		}

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <returns>An enumeration that returns the tasks in order of completion</returns>
		public static IEnumerable<Task> Interleave(this IEnumerable<Task> source) => new Interleave(source);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to cancel the interleaving tasks</param>
		/// <returns>An enumeration that returns the tasks in order of completion</returns>
		public static IEnumerable<Task> Interleave(this IEnumerable<Task> source, CancellationToken token) => new Interleave(source, token);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <returns>An enumeration that returns the tasks in order of completion</returns>
		public static IEnumerable<Task<TResult>> Interleave<TResult>(this IEnumerable<Task<TResult>> source) => new Interleave<TResult>(source);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to cancel the interleaving tasks</param>
		/// <returns>An enumeration that returns the tasks in order of completion</returns>
		public static IEnumerable<Task<TResult>> Interleave<TResult>(this IEnumerable<Task<TResult>> source, CancellationToken token) => new Interleave<TResult>(source, token);

		/// <summary>
		/// Waits until all given ValueTasks have completed
		/// </summary>
		/// <param name="tasks">The Tasks to wait on</param>
		/// <returns>A ValueTask that completes when all given Tasks complete</returns>
		public static ValueTask WhenAll(this IEnumerable<ValueTask> tasks)
		{
			if (tasks is null)
				throw new ArgumentNullException(nameof(tasks));

			if (tasks is ValueTask[] ArrayTasks)
				return InternalWhenAll(ArrayTasks);

			if (tasks is IReadOnlyCollection<ValueTask> ReadOnlyCollection)
			{
				if (ReadOnlyCollection.Count == 0)
					return default;

				var Index = 0;

				ArrayTasks = new ValueTask[ReadOnlyCollection.Count];

				foreach (var CurrentTask in tasks)
					ArrayTasks[Index++] = CurrentTask;

				return InternalWhenAll(ArrayTasks);
			}

			if (tasks is ICollection<ValueTask> Collection)
			{
				if (Collection.Count == 0)
					return default;

				var Index = 0;

				ArrayTasks = new ValueTask[Collection.Count];

				foreach (var CurrentTask in tasks)
					ArrayTasks[Index++] = CurrentTask;

				return InternalWhenAll(ArrayTasks);
			}

			var ListTasks = new List<ValueTask>(tasks);

			if (ListTasks.Count == 0)
				return default;

			return InternalWhenAll(ListTasks);
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

		//****************************************

		internal static ValueTask AsValueTask(this Task task) => new ValueTask(task);

		internal static ValueTask<TResult> AsValueTask<TResult>(this Task<TResult> task) => new ValueTask<TResult>(task);

		//****************************************

		private static void DoCompleteTaskSource(Task innerTask, object state)
		{
			var MySource = (TargetTask<VoidStruct>)state;
			
			if (innerTask.IsFaulted)
				MySource.TrySetException(innerTask.Exception.InnerException);
			else if (innerTask.IsCanceled)
				MySource.TrySetCanceled();
			else
				MySource.TrySetResult(VoidStruct.Empty);

			MySource.Registration.Dispose();
		}
		
		private static void DoCompleteTaskSource<TResult>(Task<TResult> innerTask, object state)
		{
			var MySource = (TargetTask<TResult>)state;
			
			if (innerTask.IsFaulted)
				MySource.TrySetException(innerTask.Exception.InnerException);
			else if (innerTask.IsCanceled)
				MySource.TrySetCanceled();
			else
				MySource.TrySetResult(innerTask.Result);

			MySource.Registration.Dispose();
		}
		
		private static void DoCancelTaskSource<TResult>(object state)
		{
			var MySource = (TargetTask<TResult>)state;

			if (MySource.TrySetCanceled())
			{
				// We cancelled our listener. The danger here is, if the wrapped task then throws an exception, it'll go unobserved if there's no other continuations.
				// Since we want When to be a drop-in replacement for async methods that don't take CancellationToken, we attach a new task to observe the fault, just in case it throws
				MySource.Target.ContinueWith(DoObserveFault, TaskContinuationOptions.OnlyOnFaulted);
			}
		}
		
		private static void DoDisposeCancelSource(Task innerTask, object state)
		{
			var MyCancelSource = (CancellationTokenSource)state;
			
			MyCancelSource.Dispose();
		}

		private static void DoObserveFault(Task innerTask)
		{
			var MyException = innerTask.Exception;
		}

		private static async ValueTask InternalWhenAll(IReadOnlyList<ValueTask> tasks)
		{
			List<Exception> Exceptions = null;

			for (var Index = 0; Index < tasks.Count; Index++)
			{
				try
				{
					await tasks[Index];
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

		private static async ValueTask<T[]> InternalWhenAll<T>(IReadOnlyList<ValueTask<T>> tasks)
		{
			var Results = new T[tasks.Count];
			List<Exception> Exceptions = null;

			for (var Index = 0; Index < tasks.Count; Index++)
			{
				try
				{
					Results[Index] = await tasks[Index];
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

		//****************************************

		private sealed class TargetTask<TResult> : TaskCompletionSource<TResult>
		{
			public TargetTask(Task task) => Target = task;

			//****************************************

			public Task Target { get; }

			public CancellationTokenRegistration Registration { get; set; }
		}
	}
}
