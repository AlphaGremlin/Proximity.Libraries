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
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" /></remarks>
		public static Task When(this Task task, CancellationToken token)
		{
			if (!token.CanBeCanceled || task.IsCompleted)
				return task;
			
			var MyCompletionSource = new TaskCompletionSource<VoidStruct>();
			
			task
				.ContinueWith(DoCompleteTaskSource, MyCompletionSource, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current)
				.ContinueWith(DoCancelTaskSource<VoidStruct>, MyCompletionSource, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
			
			return MyCompletionSource.Task;
		}
		
		/// <summary>
		/// Wraps the given task, waiting for it to complete or the given token to cancel
		/// </summary>
		/// <param name="task">The task to wait to complete</param>
		/// <param name="token">The cancellation token to abort waiting for the original task</param>
		/// <returns>A task returning the result of the original task, and cancelling if either the original task or the given token cancel</returns>
		/// <remarks>Will throw any exceptions from the original task as <see cref="AggregateException" /></remarks>
		public static Task<TResult> When<TResult>(this Task<TResult> task, CancellationToken token)
		{
			if (!token.CanBeCanceled || task.IsCompleted)
				return task;
			
			var MyCompletionSource = new TaskCompletionSource<TResult>();
			
			task
				.ContinueWith(DoCompleteTaskSource<TResult>, MyCompletionSource, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current)
				.ContinueWith(DoCancelTaskSource<TResult>, MyCompletionSource, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
			
			return MyCompletionSource.Task;
		}
		
		public static Task When(this Task task, int milliseconds)
		{
			return task.When(TimeSpan.FromMilliseconds(milliseconds), CancellationToken.None);
		}
		
		public static Task When(this Task task, TimeSpan timeout)
		{
			return task.When(timeout, CancellationToken.None);
		}
		
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
				MyCancelSource.CancelAfter(timeout);
			}
			else
			{
				// Can't cancel, just create a token source that times out
				MyCancelSource = new CancellationTokenSource(timeout);
			}
			
			task = task.When(MyCancelSource.Token);
			
			// Ensure we cleanup the token source
			task.ContinueWith(DoDisposeCancelSource, MyCancelSource, TaskContinuationOptions.ExecuteSynchronously);
			
			return task;
		}
		
		public static Task<TResult> When<TResult>(this Task<TResult> task, int milliseconds)
		{
			return task.When(TimeSpan.FromMilliseconds(milliseconds), CancellationToken.None);
		}
		
		public static Task<TResult> When<TResult>(this Task<TResult> task, TimeSpan timeout)
		{
			return task.When(timeout, CancellationToken.None);
		}
		
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
				MyCancelSource.CancelAfter(timeout);
			}
			else
			{
				// Can't cancel, just create a token source that times out
				MyCancelSource = new CancellationTokenSource(timeout);
			}
			
			task = task.When(MyCancelSource.Token);
			
			// Ensure we cleanup the token source
			task.ContinueWith(DoDisposeCancelSource, MyCancelSource, TaskContinuationOptions.ExecuteSynchronously);
			
			return task;
		}
		
		/// <summary>
		/// Interleaves an enumeration of tasks, returning them in the order they complete
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <returns>An enumeration that returns the tasks in order of completion</returns>
		public static IEnumerable<Task<TResult>> Interleave<TResult>(this IEnumerable<Task<TResult>> source)
		{
			return new Interleave<TResult>(source);
		}
		
		//****************************************
		
		private static void DoCompleteTaskSource(Task innerTask, object state)
		{
			var MySource = (TaskCompletionSource<VoidStruct>)state;
			
			if (innerTask.IsFaulted)
				MySource.SetException(innerTask.Exception.InnerException);
			else if (innerTask.IsCanceled)
				MySource.SetCanceled();
			else 
				MySource.SetResult(VoidStruct.Empty);
		}
		
		private static void DoCompleteTaskSource<TResult>(Task<TResult> innerTask, object state)
		{
			var MySource = (TaskCompletionSource<TResult>)state;
			
			if (innerTask.IsFaulted)
				MySource.SetException(innerTask.Exception.InnerException);
			else if (innerTask.IsCanceled)
				MySource.SetCanceled();
			else 
				MySource.SetResult(innerTask.Result);
		}
		
		private static void DoCancelTaskSource<TResult>(Task innerTask, object state)
		{
			var MySource = (TaskCompletionSource<TResult>)state;
			
			MySource.SetCanceled();
		}
		
		private static void DoDisposeCancelSource(Task innerTask, object state)
		{
			var MyCancelSource = (CancellationTokenSource)state;
			
			MyCancelSource.Dispose();
		}
	}
}
