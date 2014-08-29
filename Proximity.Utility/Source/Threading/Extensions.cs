/****************************************\
 Extensions.cs
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
	/// Contains extension methods for Threading support
	/// </summary>
	public static class Extensions
	{	//****************************************
		private static ConditionalWeakTable<WaitHandle, Task<bool>> _AsyncWaits = new ConditionalWeakTable<WaitHandle, Task<bool>>();
		//****************************************
		
		/// <summary>
		/// Open a Read Lock for the duration of the Using Block
		/// </summary>
		/// <param name="readLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static ReadLock UsingReader(this ReaderWriterLockSlim readLock)
		{
			return ReadLock.From(readLock);
		}
		
		/// <summary>
		/// Open a Write Lock for the duration of the Using Block
		/// </summary>
		/// <param name="writeLock">The <see cref="ReaderWriterLockSlim" /> to lock</param>
		/// <returns>An IDisposable structure to pass to a Using block</returns>
		public static WriteLock UsingWriter(this ReaderWriterLockSlim writeLock)
		{
			return WriteLock.From(writeLock);
		}
		
		/// <summary>
		/// Gets a <see cref="Task" /> used to await this <see cref="WaitHandle" />, with a timeout
		/// </summary>
		/// <param name="waitHandle">The <see cref="WaitHandle" /> to wait on</param>
		/// <param name="timeout">The time to wait. Use <see cref="Timeout.Infinite" /> for no timeout (wait forever)</param>
		/// <returns>A <see cref="Task" /> returning true if the <see cref="WaitHandle" /> received a signal, or false if it timed out</returns>
		/// <remarks>This method ensures multiple waits on a single WaitHandle are valid</remarks>
		public static Task<bool> WaitAsync(this WaitHandle waitHandle, int timeout)
		{
			if (waitHandle == null)
				throw new ArgumentNullException("waitHandle");
			
			lock (waitHandle)
			{
				return _AsyncWaits.GetValue(
					waitHandle,
					(innerHandle) =>
					{
						var MyTaskSource = new TaskCompletionSource<bool>();
						var MyRegisteredWait = ThreadPool.RegisterWaitForSingleObject(innerHandle, (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut), MyTaskSource, timeout, true);
						var MyTask = MyTaskSource.Task;
						
						MyTask.ContinueWith((ancestor, state) => ((RegisteredWaitHandle)state).Unregister(null), MyRegisteredWait);
						
						return MyTask;
					}
				);
			}
		}
		
		/// <summary>
		/// Gets a <see cref="Task" /> used to await this <see cref="WaitHandle" />
		/// </summary>
		/// <param name="waitHandle">The <see cref="WaitHandle" /> to wait on</param>
		/// <returns>A <see cref="Task" /> that completes when the <see cref="WaitHandle" /> is signalled</returns>
		public static Task WaitAsync(this WaitHandle waitHandle)
		{
			return waitHandle.WaitAsync(Timeout.Infinite);
		}
		
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
			
			task.ContinueWith(
				(innerTask, state) =>
				{
					var MySource = (TaskCompletionSource<VoidStruct>)state;
					
					if (innerTask.IsFaulted)
						MySource.SetException(innerTask.Exception.InnerException);
					else
						MySource.SetResult(VoidStruct.Empty);
				},
				MyCompletionSource, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current
			);
			
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
			
			task.ContinueWith(
				(innerTask, state) =>
				{
					var MySource = (TaskCompletionSource<TResult>)state;
					
					if (innerTask.IsFaulted)
						MySource.SetException(innerTask.Exception.InnerException);
					else if (innerTask.IsCanceled)
						MySource.SetCanceled();
					else 
						MySource.SetResult(innerTask.Result);
				},
				MyCompletionSource, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current
			);
			
			return MyCompletionSource.Task;
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
	}
}
