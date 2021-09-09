using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace System.Threading.Tasks
{
	/// <summary>
	/// Provides methods for queueing a task on the ThreadPool without the call context
	/// </summary>
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
			
			ThreadPool.UnsafeQueueUserWorkItem(ExecuteAction, Tuple.Create(action, MyTaskSource));

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

			ThreadPool.UnsafeQueueUserWorkItem(ExecuteFunc<Task>, Tuple.Create(action, MyTaskSource));
			
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

			ThreadPool.UnsafeQueueUserWorkItem(ExecuteFunc<Task<TResult>>, Tuple.Create(action, MyTaskSource));
			
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

			ThreadPool.UnsafeQueueUserWorkItem(ExecuteFunc<TResult>, Tuple.Create(action, MyTaskSource));
			
			return MyTaskSource.Task;
		}

		/// <summary>
		/// Yields the thread in an unsafe way, breaking the execution context
		/// </summary>
		/// <returns>An awaitable that when awaited, continues in an unsafe context</returns>
		public static UnsafeYieldAwaitable Yield() => new();

		//****************************************

		private static void ExecuteAction(object state)
		{	//****************************************
			var Definition = (Tuple<Action, TaskCompletionSource<VoidStruct>>)state;
			//****************************************
			
			try
			{
				Definition.Item1();
				
				Definition.Item2.SetResult(default);
			}
			catch (OperationCanceledException)
			{
				Definition.Item2.SetCanceled();
			}
			catch (Exception e)
			{
				Definition.Item2.SetException(e);
			}
		}
		
		private static void ExecuteFunc<TResult>(object state)
		{	//****************************************
			var Definition = (Tuple<Func<TResult>, TaskCompletionSource<TResult>>)state;
			//****************************************
			
			try
			{
				var MyResult = Definition.Item1();
				
				Definition.Item2.SetResult(MyResult);
			}
			catch (OperationCanceledException)
			{
				Definition.Item2.SetCanceled();
			}
			catch (Exception e)
			{
				Definition.Item2.SetException(e);
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
			public UnsafeYieldAwaitable GetAwaiter() => this;

			/// <summary>
			/// Retrieves the result of the awaitable
			/// </summary>
			public void GetResult()
			{
			}

			/// <summary>
			/// Queues the continuation on the threadpool in an unsafe manner
			/// </summary>
			/// <param name="continuation">The continuation to queue</param>
			public void OnCompleted(Action continuation) => ThreadPool.UnsafeQueueUserWorkItem(OnContinue, continuation);

			/// <summary>
			/// Queues the continuation on the threadpool in an unsafe manner
			/// </summary>
			/// <param name="continuation">The continuation to queue</param>
			public void UnsafeOnCompleted(Action continuation) => ThreadPool.UnsafeQueueUserWorkItem(OnContinue, continuation);

			//****************************************

			/// <summary>
			/// Gets whether the awaitable is completed. Always false
			/// </summary>
			public bool IsCompleted => false;

			//****************************************

			private static void OnContinue(object state) => ((Action)state)();
		}
	}
}
