/****************************************\
 TaskStreamSlim.cs
 Created: 2012-09-13
\****************************************/
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Manages the task of stringing together a sequence of tasks
	/// </summary>
	public sealed class TaskStream
	{	//****************************************
		private static readonly IStreamTask _CompletedTask = new CompletedTask();
		//****************************************
		private readonly TaskFactory _Factory;
		private IStreamTask _NextTask = _CompletedTask;
		
		private int _PendingActions;
		//****************************************
		
		/// <summary>
		/// Creates a new Task Stream with the default task factory
		/// </summary>
		public TaskStream() : this(Task.Factory)
		{
		}
		
		/// <summary>
		/// Creates a new Task Stream with a custom task factory
		/// </summary>
		/// <param name="factory">The target task factory to use</param>
		public TaskStream(TaskFactory factory)
		{
			_Factory = factory;
		}
		
		//****************************************
		
		/// <summary>
		/// Queues a task to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <returns>The task that was created</returns>
		public Task Queue(Action action)
		{
			return Queue(action, _Factory.CancellationToken);
		}
		
		/// <summary>
		/// Queues a task to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task Queue(Action action, CancellationToken cancellationToken)
		{
			var NewTask = new ActionTask<VoidStruct>(this, action, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask;
		}
		
		/// <summary>
		/// Queues a task that takes a value to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>The task that was created</returns>
		public Task Queue<TValue>(Action<TValue> action, TValue value)
		{
			return Queue(action, value, _Factory.CancellationToken);
		}
		
		/// <summary>
		/// Queues a task that takes a value to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task Queue<TValue>(Action<TValue> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new ActionTask<TValue>(this, action, value, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask;
		}
		
		/// <summary>
		/// Queues a task that returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TResult>(Func<TResult> action)
		{
			return Queue(action, _Factory.CancellationToken);
		}
		
		/// <summary>
		/// Queues a task that returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TResult>(Func<TResult> action, CancellationToken cancellationToken)
		{
			var NewTask = new FuncTask<VoidStruct, TResult>(this, action, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask;
		}
		
		/// <summary>
		/// Queues a task that takes a value and returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value)
		{
			return Queue(action, value, _Factory.CancellationToken);
		}
		
		/// <summary>
		/// Queues a task that takes a value and returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new FuncTask<TValue, TResult>(this, action, value, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask;
		}
		
		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask(Func<Task> action)
		{
			return QueueTask(action, _Factory.CancellationToken);
		}
		
		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask(Func<Task> action, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedTask<VoidStruct>(this, action, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask.Task;
		}
		
		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TResult>(Func<Task<TResult>> action)
		{
			return QueueTask(action, _Factory.CancellationToken);
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedResultTask<VoidStruct, TResult>(this, action, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask<TValue>(Func<TValue, Task> action, TValue value)
		{
			return QueueTask(action, value, _Factory.CancellationToken);
		}
		
		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask<TValue>(Func<TValue, Task> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedTask<TValue>(this, action, value, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask.Task;
		}
		
		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value)
		{
			return QueueTask(action, value, _Factory.CancellationToken);
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedResultTask<TValue, TResult>(this, action, value, cancellationToken);
			
			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange<IStreamTask>(ref _NextTask, NewTask).Queue(NewTask);
			
			return NewTask.Task;
		}

		/// <summary>
		/// Resets the stream, so future tasks will begin executing immediately (essentially starts a new stream)
		/// </summary>
		/// <remarks>This does not reset the pending actions counter</remarks>
		public void Reset()
		{
			Interlocked.Exchange(ref _NextTask, _CompletedTask);
		}

		/// <summary>
		/// Completes when all queued tasks have been completed
		/// </summary>
		public Task Complete()
		{
			return Complete(CancellationToken.None);
		}

		/// <summary>
		/// Completes when all queued tasks have been completed
		/// </summary>
		public Task Complete(CancellationToken token)
		{	//****************************************
			var CurrentTask = _NextTask;
			//****************************************

			if (object.ReferenceEquals(CurrentTask, _CompletedTask)) // Nothing has been queued or the stream has been reset
				return VoidStruct.EmptyTask;

			return CurrentTask.Task.When(token);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of actions that have yet to complete executing
		/// </summary>
		public int PendingActions
		{
			get { return _PendingActions; }
		}
		
		//****************************************
		
		private interface IStreamTask
		{
			void Queue(IStreamTask child);
			void Execute();
			
			TaskAwaiter GetAwaiter();

			Task Task { get; }
		}
		
		private sealed class CompletedTask : IStreamTask
		{
			void IStreamTask.Queue(IStreamTask child)
			{
				child.Execute();
			}
			
			void IStreamTask.Execute()
			{
				throw new InvalidOperationException("Cannot execute completed task");
			}
			
			TaskAwaiter IStreamTask.GetAwaiter()
			{
				throw new InvalidOperationException("Cannot await completed task");
			}

			Task IStreamTask.Task
			{
				get { return VoidStruct.EmptyTask; }
			}
		}
		
		//****************************************
		
		private sealed class ActionTask<TValue> : Task, IStreamTask
		{	//****************************************
			private readonly TaskStream _Owner;

			private IStreamTask _NextTask;
			//****************************************
			
			internal ActionTask(TaskStream owner, Action action, CancellationToken token) : base(action, token, owner._Factory.CreationOptions)
			{
				_Owner = owner;
			}
			
			internal ActionTask(TaskStream owner, Action<TValue> action, TValue value, CancellationToken token) : base(TaskValue<TValue>.Create(action, value), token, owner._Factory.CreationOptions)
			{
				_Owner = owner;
			}
			
			//****************************************
			
			void IStreamTask.Queue(IStreamTask child)
			{
				// Assign as the next task, only if there is none set
				var NextTask = Interlocked.CompareExchange<IStreamTask>(ref _NextTask, child, null);
				
				// If we're set to completed, just run it directly
				if (NextTask == _CompletedTask)
					child.Execute();
			}
			
			void IStreamTask.Execute()
			{
				// May be cancelled if the token has been set
				if (IsCanceled)
				{
					OnComplete();
					
					return;
				}
				
				// Try and start it
				try
				{
					Start(_Owner._Factory.Scheduler ?? TaskScheduler.Default);
					
					// Is it complete?
					if (IsCompleted)
						OnComplete();
					else
						// No, wait for it then
						this.ConfigureAwait(false).GetAwaiter().OnCompleted(OnComplete); // 2 (TaskCompletion and Action)
				}
				catch (InvalidOperationException)
				{
					OnComplete();
				}
			}

#if NET40
			TaskAwaiter IStreamTask.GetAwaiter()
			{
				return this.GetAwaiter();
			}
#endif

			//****************************************
			
			private void OnComplete()
			{
				// Mark us as completed
				var NextTask = Interlocked.CompareExchange<IStreamTask>(ref _NextTask, _CompletedTask, null);
				
				// Reduce the number of pending actions
				Interlocked.Decrement(ref _Owner._PendingActions);
				
				// If there's a task queued to run next, start it
				if (NextTask != null)
					NextTask.Execute();
			}

			//****************************************

			Task IStreamTask.Task
			{
				get { return this; }
			}
		}
	
		private sealed class FuncTask<TValue, TResult> : Task<TResult>, IStreamTask
		{	//****************************************
			private readonly TaskStream _Owner;
			private readonly Func<TValue, TResult> _Action;

			private IStreamTask _NextTask;
			//****************************************
			
			internal FuncTask(TaskStream owner, Func<TResult> action, CancellationToken token) : base(action, token, owner._Factory.CreationOptions)
			{
				_Owner = owner;
			}
			
			internal FuncTask(TaskStream owner, Func<TValue, TResult> action, TValue value, CancellationToken token) : base(TaskValue<TValue, TResult>.Create(action, value), token, owner._Factory.CreationOptions)
			{
				_Owner = owner;
				_Action = action;
			}
			
			//****************************************
			
			void IStreamTask.Queue(IStreamTask child)
			{
				// Assign as the next task, only if there is none set
				var NextTask = Interlocked.CompareExchange<IStreamTask>(ref _NextTask, child, null);

				// If we're set to completed, just run it directly
				if (NextTask == _CompletedTask)
					child.Execute();
			}
			
			void IStreamTask.Execute()
			{
				// May be cancelled if the token has been set
				if (IsCanceled)
				{
					OnComplete();
					
					return;
				}
				
				// Try and start it
				try
				{
					Start(_Owner._Factory.Scheduler ?? TaskScheduler.Default);
					
					// Is it complete?
					if (IsCompleted)
						OnComplete();
					else
						// No, wait for it then
						this.ConfigureAwait(false).GetAwaiter().OnCompleted(OnComplete); // 2 (TaskCompletion and Action)
				}
				catch (InvalidOperationException)
				{
					OnComplete();
				}
			}

#if NET40
			TaskAwaiter IStreamTask.GetAwaiter()
			{
				return ((Task)this).GetAwaiter();
			}
#endif

			//****************************************
			
			private void OnComplete()
			{
				// Mark us as completed
				var NextTask = Interlocked.CompareExchange<IStreamTask>(ref _NextTask, _CompletedTask, null);
				
				// Reduce the number of pending actions
				Interlocked.Decrement(ref _Owner._PendingActions);
				
				// If there's a task queued to run next, start it
				if (NextTask != null)
					NextTask.Execute();
			}

			//****************************************

			Task IStreamTask.Task
			{
				get { return this; }
			}
		}
		
		private class TaskValue<TValue>
		{	//****************************************
			private readonly Action<TValue> _Action;
			private readonly TValue _Value;
			//****************************************
			
			private TaskValue(Action<TValue> action, TValue value)
			{
				_Action = action;
				_Value = value;
			}
			
			//****************************************
			
			private void ExecWithValue()
			{
				_Action(_Value);
			}
			
			//****************************************
		
			internal static Action Create(Action<TValue> action, TValue value)
			{
				return new TaskValue<TValue>(action, value).ExecWithValue;
			}
		}
		
		private class TaskValue<TValue, TResult>
		{	//****************************************
			private readonly Func<TValue, TResult> _Action;
			private readonly TValue _Value;
			//****************************************
			
			private TaskValue(Func<TValue, TResult> action, TValue value)
			{
				_Action = action;
				_Value = value;
			}
			
			//****************************************
			
			private TResult ExecWithValue()
			{
				return _Action(_Value);
			}
			
			//****************************************
		
			internal static Func<TResult> Create(Func<TValue, TResult> action, TValue value)
			{
				return new TaskValue<TValue, TResult>(action, value).ExecWithValue;
			}
		}
		
		//****************************************
		
		private abstract class BaseWrappedTask<TValue, TResult, TFinalResult> : TaskCompletionSource<TFinalResult>, IStreamTask where TResult : Task
		{	//****************************************
			private readonly TaskStream _Owner;
			private readonly Action _OnComplete;
			
			private bool _WaitSecond;
			private Task _CurrentTask;

			private IStreamTask _NextTask;
			//****************************************
			
			protected BaseWrappedTask(TaskStream owner, Func<TResult> action, CancellationToken token)
			{
				_Owner = owner;
				
				_OnComplete = OnCompleteTask;
				_CurrentTask = new Task<TResult>(action, token, owner._Factory.CreationOptions);

				if (token.CanBeCanceled)
					_CurrentTask.ContinueWith(OnCancelled, TaskContinuationOptions.OnlyOnCanceled);
			}
			
			protected BaseWrappedTask(TaskStream owner, Func<TValue, TResult> action, TValue value, CancellationToken token)
			{
				_Owner = owner;
				
				_OnComplete = OnCompleteTask;
				_CurrentTask = new Task<TResult>(TaskValue<TValue, TResult>.Create(action, value), token, owner._Factory.CreationOptions);

				if (token.CanBeCanceled)
					_CurrentTask.ContinueWith(OnCancelled, TaskContinuationOptions.OnlyOnCanceled);
			}
			
			//****************************************
			
			void IStreamTask.Queue(IStreamTask child)
			{
				// Assign as the next task, only if there is none set
				var NextTask = Interlocked.CompareExchange<IStreamTask>(ref _NextTask, child, null);

				// If we're set to completed, just run it directly
				if (NextTask == _CompletedTask)
					child.Execute();
			}
			
			void IStreamTask.Execute()
			{
				// May be cancelled if the token has been set
				if (_CurrentTask.IsCanceled)
				{
					TrySetCanceled();
					
					OnComplete();
					
					return;
				}
				
				// Try and start it
				try
				{
					_CurrentTask.Start(_Owner._Factory.Scheduler ?? TaskScheduler.Default);

					// If we pass this point, we weren't cancelled and cannot be cancelled unless the returned task does so

					// Is it complete?
					if (_CurrentTask.IsCompleted)
						OnCompleteTask();
					else
						// No, wait for it then
						_CurrentTask.ConfigureAwait(false).GetAwaiter().OnCompleted(_OnComplete); // 2 (TaskCompletion and Action)
				}
				catch (InvalidOperationException)
				{
					// We were likely cancelled
					TrySetCanceled();

					OnComplete();
				}
				catch (Exception e)
				{
					TrySetException(e);

					OnComplete();
				}
			}
			
			TaskAwaiter IStreamTask.GetAwaiter()
			{
				return ((Task)Task).GetAwaiter();
			}
			
			protected abstract void ProcessResult(TResult resultTask);
			
			//****************************************

			private void OnCancelled(Task task)
			{
				// Our original task was cancelled. Cancel our external task, but don't queue the next one
				TrySetCanceled();
			}

			private void OnCompleteTask()
			{
				if (_WaitSecond)
				{
					OnComplete();
					
					ProcessResult((TResult)_CurrentTask);
					
					return;
				}
				
				var FirstTask = (Task<TResult>)_CurrentTask;
					
				if (FirstTask.IsFaulted)
				{
					SetException(FirstTask.Exception.InnerException);
					
					OnComplete();
				}
				else if (FirstTask.IsCanceled)
				{
					SetCanceled();
					
					OnComplete();
				}
				else
				{
					_CurrentTask = FirstTask.Result;
					_WaitSecond = true;
					
					// Is it complete?
					if (_CurrentTask.IsCompleted)
						OnCompleteTask();
					else
						// No, wait for it then
						_CurrentTask.ConfigureAwait(false).GetAwaiter().OnCompleted(_OnComplete); // 2 (TaskCompletion and Action)
				}
			}
			
			private void OnComplete()
			{
				// Mark us as completed
				var NextTask = Interlocked.CompareExchange<IStreamTask>(ref _NextTask, _CompletedTask, null);
				
				// Reduce the number of pending actions
				Interlocked.Decrement(ref _Owner._PendingActions);
				
				// If there's a task queued to run next, start it
				if (NextTask != null)
					NextTask.Execute();
			}

			//****************************************

			Task IStreamTask.Task
			{
				get { return base.Task; }
			}
		}
		
		private sealed class WrappedTask<TValue> : BaseWrappedTask<TValue, Task, VoidStruct>
		{
			internal WrappedTask(TaskStream owner, Func<Task> action, CancellationToken token) : base(owner, action, token)
			{
			}
			
			internal WrappedTask(TaskStream owner, Func<TValue, Task> action, TValue value, CancellationToken token) : base(owner, action, value, token)
			{
			}
			
			//****************************************
			
			protected override void ProcessResult(Task resultTask)
			{
				if (resultTask.IsFaulted)
					SetException(resultTask.Exception.InnerException);
				else if (resultTask.IsCanceled)
					SetCanceled();
				else
					SetResult(VoidStruct.Empty);
			}
		}
		
		private sealed class WrappedResultTask<TValue, TResult> : BaseWrappedTask<TValue, Task<TResult>, TResult>
		{
			internal WrappedResultTask(TaskStream owner, Func<Task<TResult>> action, CancellationToken token) : base(owner, action, token)
			{
			}
			
			internal WrappedResultTask(TaskStream owner, Func<TValue, Task<TResult>> action, TValue value, CancellationToken token) : base(owner, action, value, token)
			{
			}
			
			//****************************************
			
			protected override void ProcessResult(Task<TResult> resultTask)
			{
				if (resultTask.IsFaulted)
					SetException(resultTask.Exception.InnerException);
				else if (resultTask.IsCanceled)
					SetCanceled();
				else
					SetResult(resultTask.Result);
			}
		}
	}
}
