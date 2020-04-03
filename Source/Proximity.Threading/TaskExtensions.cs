﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
		/// Interleaves an enumeration of tasks, returning the results in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <returns>An enumeration that returns the task results in order of completion</returns>
		public static IAsyncEnumerable<TResult> Interleave<TResult>(this IEnumerable<Task<TResult>> source) => new InterleaveTask<TResult>(source, default);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to cancel the enumeration</param>
		/// <returns>An enumeration that returns the task result and the index of the original task in order of completion</returns>
		public static IAsyncEnumerable<(TResult result, int index)> InterleaveIndex<TResult>(this IEnumerable<Task<TResult>> source, CancellationToken token) => new InterleaveTask<TResult>(source, token);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning the results in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <returns>An enumeration that returns the task results in order of completion</returns>
		public static IAsyncEnumerable<TResult> Interleave<TResult>(this IEnumerable<ValueTask<TResult>> source) => new InterleaveValueTask<TResult>(source, default);

		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to cancel the enumeration</param>
		/// <returns>An enumeration that returns the task result and the index of the original task in order of completion</returns>
		public static IAsyncEnumerable<(TResult result, int index)> InterleaveIndex<TResult>(this IEnumerable<ValueTask<TResult>> source, CancellationToken token) => new InterleaveValueTask<TResult>(source, token);

		//****************************************

		private static void DoCompleteTaskSource(Task innerTask, object state)
		{
			var MySource = (TargetTask<VoidStruct>)state;
			
			if (innerTask.IsFaulted)
				MySource.TrySetException(innerTask.Exception.InnerException);
			else if (innerTask.IsCanceled)
				MySource.TrySetCanceled();
			else
				MySource.TrySetResult(default);

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
			_ = innerTask.Exception;
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
