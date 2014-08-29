/****************************************\
 UnsafeTask.cs
 Created: 2014-08-07
\****************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
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
			
			ThreadPool.UnsafeQueueUserWorkItem(ExecuteAction, new TaskDef<Task>(action, MyTaskSource));
			
			return MyTaskSource.Task.Unwrap();
		}
		
		/// <summary>
		/// Queue an task-method to run via UnsafeQueueUserWorkItem
		/// </summary>
		/// <param name="action">The code to run</param>
		/// <returns>A task that represents the action execution</returns>
		public static Task Run<TResult>(Func<Task<TResult>> action)
		{
			var MyTaskSource = new TaskCompletionSource<Task<TResult>>();
			
			ThreadPool.UnsafeQueueUserWorkItem(ExecuteAction, new TaskDef<Task<TResult>>(action, MyTaskSource));
			
			return MyTaskSource.Task.Unwrap();
		}
		
		/// <summary>
		/// Queue an action to run via UnsafeQueueUserWorkItem
		/// </summary>
		/// <param name="action">The code to run</param>
		/// <returns>A task that represents the action execution</returns>
		public static Task Run<TResult>(Func<TResult> action)
		{
			var MyTaskSource = new TaskCompletionSource<TResult>();
			
			ThreadPool.UnsafeQueueUserWorkItem(ExecuteAction, new TaskDef<TResult>(action, MyTaskSource));
			
			return MyTaskSource.Task;
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
	}
}
