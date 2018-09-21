/****************************************\
 UnsafeTask.cs
 Created: 2014-08-07
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides methods for queueing a task on the ThreadPool without the call context
	/// </summary>
	[SecurityCritical]
	public static class UnsafeTask
	{
		/// <summary>
		/// Queue an action to run via UnsafeQueueUserWorkItem
		/// </summary>
		/// <param name="action">The code to run</param>
		/// <returns>A task that represents the action execution</returns>
		public static Task Run(Action action)
		{
			var MyTaskSource = new TaskCompletionSource<VoidStruct>();
			
			ThreadPool.UnsafeQueueUserWorkItem(ExecuteAction, new TaskDef(action, MyTaskSource));
			
			return MyTaskSource.Task;
		}
		
		/// <summary>
		/// Queue a task-method to run via UnsafeQueueUserWorkItem
		/// </summary>
		/// <param name="action">The code to run</param>
		/// <returns>A task that represents the task execution</returns>
		public static Task Run(Func<Task> action)
		{
			var MyTaskSource = new TaskCompletionSource<Task>();

			ThreadPool.UnsafeQueueUserWorkItem(ExecuteFunc<Task>, new TaskDef<Task>(action, MyTaskSource));
			
			return MyTaskSource.Task.Unwrap();
		}
		
		/// <summary>
		/// Queue an task-method to run via UnsafeQueueUserWorkItem
		/// </summary>
		/// <param name="action">The code to run</param>
		/// <returns>A task that represents the action execution</returns>
		public static Task<TResult> Run<TResult>(Func<Task<TResult>> action)
		{
			var MyTaskSource = new TaskCompletionSource<Task<TResult>>();

			ThreadPool.UnsafeQueueUserWorkItem(ExecuteFunc<Task<TResult>>, new TaskDef<Task<TResult>>(action, MyTaskSource));
			
			return MyTaskSource.Task.Unwrap();
		}
		
		/// <summary>
		/// Queue an action to run via UnsafeQueueUserWorkItem
		/// </summary>
		/// <param name="action">The code to run</param>
		/// <returns>A task that represents the action execution</returns>
		public static Task<TResult> Run<TResult>(Func<TResult> action)
		{
			var MyTaskSource = new TaskCompletionSource<TResult>();

			ThreadPool.UnsafeQueueUserWorkItem(ExecuteFunc<TResult>, new TaskDef<TResult>(action, MyTaskSource));
			
			return MyTaskSource.Task;
		}

		/// <summary>
		/// Yields the thread in an unsafe way, breaking the execution context
		/// </summary>
		/// <returns>An awaitable that when awaited, continues in an unsafe context</returns>
		public static UnsafeYieldAwaitable Yield()
		{
			return new UnsafeYieldAwaitable();
		}

		//****************************************
		
		private static void ExecuteAction(object state)
		{	//****************************************
			var Definition = (TaskDef)state;
			//****************************************
			
			try
			{
				Definition._Action();
				
				Definition._TaskSource.SetResult(VoidStruct.Empty);
			}
			catch (OperationCanceledException)
			{
				Definition._TaskSource.SetCanceled();
			}
			catch (Exception e)
			{
				Definition._TaskSource.SetException(e);
			}
		}
		
		private static void ExecuteFunc<TResult>(object state)
		{	//****************************************
			var Definition = (TaskDef<TResult>)state;
			//****************************************
			
			try
			{
				var MyResult = Definition._Action();
				
				Definition._TaskSource.SetResult(MyResult);
			}
			catch (OperationCanceledException)
			{
				Definition._TaskSource.SetCanceled();
			}
			catch (Exception e)
			{
				Definition._TaskSource.SetException(e);
			}
		}
		
		//****************************************
		
		private struct TaskDef
		{	//****************************************
			public Action _Action;
			public TaskCompletionSource<VoidStruct> _TaskSource;
			//****************************************
			
			public TaskDef(Action action, TaskCompletionSource<VoidStruct> taskSource)
			{
				_Action = action;
				_TaskSource = taskSource;
			}
		}
		
		private struct TaskDef<TResult>
		{	//****************************************
			public Func<TResult> _Action;
			public TaskCompletionSource<TResult> _TaskSource;
			//****************************************
			
			public TaskDef(Func<TResult> action, TaskCompletionSource<TResult> taskSource)
			{
				_Action = action;
				_TaskSource = taskSource;
			}
		}

		//****************************************

		/// <summary>
		/// Provides an awaitable object that breaks execution context
		/// </summary>
		public struct UnsafeYieldAwaitable : INotifyCompletion, ICriticalNotifyCompletion
		{
			/// <summary>
			/// Gets the awaitable
			/// </summary>
			/// <returns>The unsafe awaiter</returns>
			public UnsafeYieldAwaitable GetAwaiter()
			{
				return this;
			}

			/// <summary>
			/// Retrieves the result of the awaitable
			/// </summary>
			public void GetResult()
			{
			}

			[SecuritySafeCritical]
			void INotifyCompletion.OnCompleted(Action continuation)
			{
				throw new SecurityException("Cannot yield unsafe");
			}

			/// <summary>
			/// Queues the continuation on the threadpool in an unsafe manner
			/// </summary>
			/// <param name="continuation">The continuation to queue</param>
			[SecurityCritical]
			public void UnsafeOnCompleted(Action continuation)
			{
				ThreadPool.UnsafeQueueUserWorkItem(OnContinue, continuation);
			}

			//****************************************

			private static void OnContinue(object state)
			{
				((Action)state)();
			}

			//****************************************

			/// <summary>
			/// Gets whether the awaitable is completed. Always false
			/// </summary>
			public bool IsCompleted
			{
				get { return false; }
			}
		}
	}
}
#endif