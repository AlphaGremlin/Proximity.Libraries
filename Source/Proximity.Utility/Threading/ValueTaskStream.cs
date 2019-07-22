using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Manages the task of stringing together a sequence of tasks
	/// </summary>
	public sealed class ValueTaskStream
	{ //****************************************
		private static readonly IStreamTask _CompletedTask = new CompletedTask();
		private static readonly Action<object> _CompletedContinuation = (state) => throw new Exception("Should never be called");
		//****************************************
		private IStreamTask _NextTask = _CompletedTask;

		private int _PendingActions;
		//****************************************

		/// <summary>
		/// Creates a new Task Stream with the default task factory
		/// </summary>
		public ValueTaskStream()
		{
		}

		//****************************************

		/// <summary>
		/// Queues a task to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <returns>The task that was created</returns>
		public ValueTask Queue(Action action) => Queue(action, CancellationToken.None);

		/// <summary>
		/// Queues a task to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask Queue(Action action, CancellationToken cancellationToken)
		{
			var NewTask = new ActionTask(this, action, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a task that takes a value to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>The task that was created</returns>
		public ValueTask Queue<TValue>(Action<TValue> action, TValue value) => Queue(action, value, CancellationToken.None);

		/// <summary>
		/// Queues a task that takes a value to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask Queue<TValue>(Action<TValue> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new ActionTask<TValue>(this, action, value, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a task that returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <returns>The task that was created</returns>
		public ValueTask<TResult> Queue<TResult>(Func<TResult> action) => Queue(action, CancellationToken.None);

		/// <summary>
		/// Queues a task that returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask<TResult> Queue<TResult>(Func<TResult> action, CancellationToken cancellationToken)
		{
			var NewTask = new FuncTask<TResult>(this, action, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a task that takes a value and returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>The task that was created</returns>
		public ValueTask<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value) => Queue(action, value, CancellationToken.None);

		/// <summary>
		/// Queues a task that takes a value and returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new FuncTask<TValue, TResult>(this, action, value, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask(Func<Task> action) => QueueTask(action, CancellationToken.None);

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask(Func<Task> action, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedTask(this, action, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TResult>(Func<Task<TResult>> action) => QueueTask(action, CancellationToken.None);

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedFuncTask<TResult>(this, action, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask<TValue>(Func<TValue, Task> action, TValue value) => QueueTask(action, value, CancellationToken.None);

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask<TValue>(Func<TValue, Task> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedTask<TValue>(this, action, value, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value) => QueueTask(action, value, CancellationToken.None);

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedFuncTask<TValue, TResult>(this, action, value, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueValueTask(Func<ValueTask> action) => QueueValueTask(action, CancellationToken.None);

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueValueTask(Func<ValueTask> action, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedValueTask(this, action, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueValueTask<TResult>(Func<ValueTask<TResult>> action) => QueueValueTask(action, CancellationToken.None);

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueValueTask<TResult>(Func<ValueTask<TResult>> action, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedFuncValueTask<TResult>(this, action, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueValueTask<TValue>(Func<TValue, ValueTask> action, TValue value) => QueueValueTask(action, value, CancellationToken.None);

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueValueTask<TValue>(Func<TValue, ValueTask> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedValueTask<TValue>(this, action, value, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueValueTask<TValue, TResult>(Func<TValue, ValueTask<TResult>> action, TValue value) => QueueValueTask(action, value, CancellationToken.None);

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueValueTask<TValue, TResult>(Func<TValue, ValueTask<TResult>> action, TValue value, CancellationToken cancellationToken)
		{
			var NewTask = new WrappedFuncValueTask<TValue, TResult>(this, action, value, cancellationToken);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Resets the stream, so future tasks will begin executing immediately (essentially starts a new stream)
		/// </summary>
		/// <remarks>This does not reset the pending actions counter</remarks>
		public void Reset() => Interlocked.Exchange(ref _NextTask, _CompletedTask);

		/// <summary>
		/// Completes when all queued tasks have been completed
		/// </summary>
		public Task Complete() => GetComplete();

		/// <summary>
		/// Completes when all queued tasks have been completed
		/// </summary>
		public Task Complete(CancellationToken token) => GetComplete().When(token);

		private Task GetComplete()
		{
			for (; ; )
			{
				var OldNextTask = _NextTask;

				if (OldNextTask == _CompletedTask)
					return Task.CompletedTask;

				if (OldNextTask is NullTask Null)
					return Null.Completed; // The next Task is a Completed Task, so just return that rather than allocate a new one

				// Insert a Null Task into the Stream
				var NewNextTask = new NullTask(this, CancellationToken.None);
				
				if (Interlocked.CompareExchange(ref _NextTask, NewNextTask, OldNextTask) == OldNextTask)
				{
					OldNextTask.Queue(NewNextTask);

					return NewNextTask.Completed;
				}

				// The next Task changed. Either a new Task was queued, or it just completed executing
			}
		}

		//****************************************

		/// <summary>
		/// Gets the number of actions that have yet to complete executing
		/// </summary>
		public int PendingActions => _PendingActions;

		//****************************************

		private interface IStreamTask
		{
			void Queue(IStreamTask child);
			void Execute();
		}

		private sealed class CompletedTask : IStreamTask
		{
			void IStreamTask.Queue(IStreamTask child) => child.Execute();

			void IStreamTask.Execute() => throw new InvalidOperationException("Cannot execute completed task");
		}

		//****************************************

		private abstract class BaseStreamTask : IStreamTask
		{ //****************************************
			private IStreamTask _NextTask;

			private Action<object> _Continuation;
			private object _State;

			private ExecutionContext _ExecutionContext;
			private object _Scheduler;

			private CancellationTokenRegistration _Registration;
			private volatile int _CanCancel;
			//****************************************

			protected BaseStreamTask(ValueTaskStream stream, CancellationToken token)
			{
				Stream = stream;

				if (token.CanBeCanceled)
				{
					Token = token;

					_CanCancel = 1;
					_Registration = token.Register(CancelTask, this);
				}
			}

			//****************************************

			public void Queue(IStreamTask child)
			{
				// Assign as the next task, only if there is none set
				var NextTask = Interlocked.CompareExchange(ref _NextTask, child, null);

				// If we're set to completed, just run it directly
				if (NextTask == _CompletedTask)
					child.Execute();
			}

			public abstract void Execute();

			public ValueTaskSourceStatus GetStatus(short token)
			{
				if (token != TaskToken)
					throw new InvalidOperationException("Value Task Token invalid");

				if (!IsCompleted)
					return ValueTaskSourceStatus.Pending;

				if (Exception == null)
					return ValueTaskSourceStatus.Succeeded;

				if (Exception.SourceException is OperationCanceledException)
					return ValueTaskSourceStatus.Canceled;

				return ValueTaskSourceStatus.Faulted;
			}

			public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
			{
				if (token != TaskToken)
					throw new InvalidOperationException("Value Task Token invalid");

				if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != ValueTaskSourceOnCompletedFlags.None)
					_ExecutionContext = ExecutionContext.Capture();

				if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != ValueTaskSourceOnCompletedFlags.None)
				{
					var CurrentContext = SynchronizationContext.Current;
					TaskScheduler CurrentScheduler;

					if (CurrentContext != null && CurrentContext.GetType() != typeof(SynchronizationContext))
						_Scheduler = CurrentContext;
					else if ((CurrentScheduler = TaskScheduler.Current) != TaskScheduler.Default)
						_Scheduler = CurrentScheduler;
				}

				_State = state;
				var Previous = Interlocked.CompareExchange(ref _Continuation, continuation, null);

				if (ReferenceEquals(Previous, _CompletedContinuation))
				{
					_ExecutionContext = null;
					_State = null;

					RaiseContinuation(continuation, state, true);
				}
				else if (Previous != null)
					throw new InvalidOperationException("Continuation already set");
			}

			//****************************************

			protected bool SwitchToPending()
			{
				if (_CanCancel == 0)
					return true; // Task cannot be cancelled, so its free to execute

				_Registration.Dispose();

				if (Interlocked.CompareExchange(ref _CanCancel, 0, 1) != 1)
				{
					// The task has already been cancelled. Move to the next Task in the stream
					Complete();

					return false; 
				}

				if (Token.IsCancellationRequested)
				{
					// The token was about to cancel, so cancel the Task
					SwitchToCancelled();

					return false;
				}

				// Task is allowed to begin execution
				return true;
			}

			protected void SwitchToSucceeded()
			{
				RaiseContinuation();
				Complete();
			}

			protected void SwitchToCancelled() => SwitchToFaulted(new OperationCanceledException());

			protected void SwitchToCancelled(CancellationToken token) => SwitchToFaulted(new OperationCanceledException(token));

			protected void SwitchToFaulted(Exception exception)
			{
				Exception = ExceptionDispatchInfo.Capture(exception);
				RaiseContinuation();
				Complete();
			}

			//****************************************

			private void Complete()
			{
				// Mark us as completed
				var NextTask = Interlocked.CompareExchange(ref _NextTask, _CompletedTask, null);

				// Reduce the number of pending actions
				Interlocked.Decrement(ref Stream._PendingActions);

				// If there's a task queued to run next, start it
				if (NextTask != null)
					NextTask.Execute();
			}

			private void RaiseContinuation()
			{
				var Continuation = _Continuation;

				if (Continuation == null && (Continuation = Interlocked.CompareExchange(ref _Continuation, _CompletedContinuation, null)) == null)
					return;

				if (Continuation == _CompletedContinuation)
					return;

				var ContinuationState = _State;

				_Continuation = _CompletedContinuation;
				_State = null;

				if (_ExecutionContext == null)
				{
					RaiseContinuation(Continuation, ContinuationState, false);
				}
				else
				{
					ExecutionContext.Run(_ExecutionContext, (state) =>
					{
						var (task, continuation, continuationState) = ((BaseStreamTask, Action<object>, object))state;

						task.RaiseContinuation(continuation, continuationState, false);
					}, (this, Continuation, ContinuationState));
				}
			}

			private void RaiseContinuation(Action<object> continuation, object state, bool forceAsync)
			{
				if (_Scheduler != null)
				{
					if (_Scheduler is SynchronizationContext Context)
						Context.Post(RaiseContinuation, (continuation, state));
					else if (_Scheduler is TaskScheduler Scheduler)
						Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler);
				}
				else if (forceAsync)
					ThreadPool.QueueUserWorkItem(RaiseContinuation, (continuation, state));
				else
					continuation(state);
			}

			private static void RaiseContinuation(object state)
			{
				var (innerContinuation, innerState) = ((Action<object>, object))state;

				innerContinuation(innerState);
			}

			private static void CancelTask(object state)
			{
				var Task = (BaseStreamTask)state;

				if (Task._CanCancel == 0 || Interlocked.CompareExchange(ref Task._CanCancel, 2, 1) != 1)
					return; // Task has already begun execution, can no longer cancel

				// Do not raise Complete here, since we can only do that once the Task has been reached in the Stream
				Task.Exception = ExceptionDispatchInfo.Capture(new OperationCanceledException(Task.Token));
				// We do run the continuation, since the Task has reached a final status
				Task.RaiseContinuation();
			}

			//****************************************

			public ValueTaskStream Stream { get; }

			public CancellationToken Token { get; }

			internal short TaskToken { get; }

			public bool IsCompleted => ReferenceEquals(_Continuation, _CompletedContinuation);

			public ExceptionDispatchInfo Exception { get; private set; }
		}

		private abstract class BaseValueTaskSource : BaseStreamTask, IValueTaskSource
		{
			protected BaseValueTaskSource(ValueTaskStream stream, CancellationToken token) : base(stream, token)
			{
			}

			//****************************************

			void IValueTaskSource.GetResult(short token)
			{
				if (token != TaskToken)
					throw new InvalidOperationException("Value Task Token invalid");

				Exception?.Throw();
			}

			//****************************************

			public ValueTask Task => new ValueTask(this, TaskToken);
		}

		private sealed class NullTask : BaseValueTaskSource
		{
			internal NullTask(ValueTaskStream stream, CancellationToken token) : base(stream, token)
			{
				Completed = Task.AsTask();
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem((state) => ((NullTask)state).SwitchToSucceeded(), this);
			}

			//****************************************

			public Task Completed { get; }
		}

		private sealed class ActionTask : BaseValueTaskSource
		{ //****************************************
			private readonly Action _Action;
			//****************************************

			internal ActionTask(ValueTaskStream stream, Action action, CancellationToken token) : base(stream, token)
			{
				_Action = action ?? throw new ArgumentNullException(nameof(action));
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteActionTask, this);
			}

			//****************************************

			private static void ExecuteActionTask(object state)
			{
				var Task = (ActionTask)state;

				try
				{
					Task._Action();

					Task.SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}

		private sealed class ActionTask<TValue> : BaseValueTaskSource
		{ //****************************************
			private readonly Action<TValue> _Action;
			private readonly TValue _Value;
			//****************************************

			internal ActionTask(ValueTaskStream stream, Action<TValue> action, TValue value, CancellationToken token) : base(stream, token)
			{
				_Action = action ?? throw new ArgumentNullException(nameof(action));
				_Value = value;
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteActionTask, this);
			}

			//****************************************

			private static void ExecuteActionTask(object state)
			{
				var Task = (ActionTask<TValue>)state;

				try
				{
					Task._Action(Task._Value);

					Task.SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}

		private sealed class WrappedTask : BaseValueTaskSource
		{ //****************************************
			private readonly Func<Task> _Action;
			//****************************************

			internal WrappedTask(ValueTaskStream stream, Func<Task> action, CancellationToken token) : base(stream, token)
			{
				_Action = action ?? throw new ArgumentNullException(nameof(action));
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedTask)state;

				try
				{
					var InnerTask = Task._Action();

					if (InnerTask.IsCompleted)
						ContinueWrappedTask(InnerTask, Task);
					else
						InnerTask.ContinueWith(ContinueWrappedTask, Task);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}

			private static void ContinueWrappedTask(Task innerTask, object state)
			{
				var Task = (WrappedTask)state;

				if (innerTask.IsFaulted)
					Task.SwitchToFaulted(innerTask.Exception.InnerException);
				else if (innerTask.IsCanceled)
					Task.SwitchToCancelled();
				else
					Task.SwitchToSucceeded();
			}
		}

		private sealed class WrappedTask<TValue> : BaseValueTaskSource
		{ //****************************************
			private readonly Func<TValue, Task> _Action;
			private readonly TValue _Value;
			//****************************************

			internal WrappedTask(ValueTaskStream stream, Func<TValue, Task> action, TValue value, CancellationToken token) : base(stream, token)
			{
				_Action = action ?? throw new ArgumentNullException(nameof(action));
				_Value = value;
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedTask<TValue>)state;

				try
				{
					var InnerTask = Task._Action(Task._Value);

					if (InnerTask.IsCompleted)
						ContinueWrappedTask(InnerTask, Task);
					else
						InnerTask.ContinueWith(ContinueWrappedTask, Task);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}

			private static void ContinueWrappedTask(Task innerTask, object state)
			{
				var Task = (WrappedTask<TValue>)state;

				if (innerTask.IsFaulted)
					Task.SwitchToFaulted(innerTask.Exception.InnerException);
				else if (innerTask.IsCanceled)
					Task.SwitchToCancelled();
				else
					Task.SwitchToSucceeded();
			}
		}

		private sealed class WrappedValueTask : BaseValueTaskSource
		{ //****************************************
			private readonly Func<ValueTask> _Action;
			private ValueTask _InnerTask;
			//****************************************

			internal WrappedValueTask(ValueTaskStream stream, Func<ValueTask> action, CancellationToken token) : base(stream, token)
			{
				_Action = action ?? throw new ArgumentNullException(nameof(action));
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private void ContinueWrappedTask()
			{
				var Awaiter = _InnerTask.GetAwaiter();

				try
				{
					Awaiter.GetResult();

					SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e);
				}
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedValueTask)state;

				try
				{
					var Awaiter = (Task._InnerTask = Task._Action()).GetAwaiter();

					if (Awaiter.IsCompleted)
						Task.ContinueWrappedTask();
					else
						Awaiter.OnCompleted(Task.ContinueWrappedTask);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}

		private sealed class WrappedValueTask<TValue> : BaseValueTaskSource
		{ //****************************************
			private readonly Func<TValue, ValueTask> _Action;
			private readonly TValue _Value;
			private ValueTask _InnerTask;
			//****************************************

			internal WrappedValueTask(ValueTaskStream stream, Func<TValue, ValueTask> action, TValue value, CancellationToken token) : base(stream, token)
			{
				_Action = action ?? throw new ArgumentNullException(nameof(action));
				_Value = value;
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private void ContinueWrappedTask()
			{
				var Awaiter = _InnerTask.GetAwaiter();

				try
				{
					Awaiter.GetResult();

					SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e);
				}
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedValueTask<TValue>)state;

				try
				{
					var Awaiter = (Task._InnerTask = Task._Action(Task._Value)).GetAwaiter();

					if (Awaiter.IsCompleted)
						Task.ContinueWrappedTask();
					else
						Awaiter.UnsafeOnCompleted(Task.ContinueWrappedTask);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}

		private abstract class BaseValueTaskSource<TResult> : BaseStreamTask, IValueTaskSource<TResult>
		{ //****************************************
			private TResult _Result;
			//****************************************

			protected BaseValueTaskSource(ValueTaskStream stream, CancellationToken token) : base(stream, token)
			{
			}

			//****************************************

			TResult IValueTaskSource<TResult>.GetResult(short token)
			{
				if (token != TaskToken)
					throw new InvalidOperationException("Value Task Token invalid");

				Exception?.Throw();

				return _Result;
			}

			//****************************************

			protected void SwitchToSucceeded(TResult result)
			{
				_Result = result;

				SwitchToSucceeded();
			}

			//****************************************

			public ValueTask<TResult> Task => new ValueTask<TResult>(this, TaskToken);
		}

		private sealed class FuncTask<TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly Func<TResult> _Func;
			//****************************************

			internal FuncTask(ValueTaskStream stream, Func<TResult> func, CancellationToken token) : base(stream, token)
			{
				_Func = func ?? throw new ArgumentNullException(nameof(func));
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteFuncTask, this);
			}

			//****************************************

			private static void ExecuteFuncTask(object state)
			{
				var Task = (FuncTask<TResult>)state;

				try
				{
					Task.SwitchToSucceeded(Task._Func());
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}

		private sealed class FuncTask<TValue, TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly Func<TValue, TResult> _Func;
			private readonly TValue _Value;
			//****************************************

			internal FuncTask(ValueTaskStream stream, Func<TValue, TResult> func, TValue value, CancellationToken token) : base(stream, token)
			{
				_Func = func ?? throw new ArgumentNullException(nameof(func));
				_Value = value;
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteFuncTask, this);
			}

			//****************************************

			private static void ExecuteFuncTask(object state)
			{
				var Task = (FuncTask<TValue, TResult>)state;

				try
				{
					Task.SwitchToSucceeded(Task._Func(Task._Value));
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}

		private sealed class WrappedFuncTask<TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly Func<Task<TResult>> _Func;
			//****************************************

			internal WrappedFuncTask(ValueTaskStream stream, Func<Task<TResult>> func, CancellationToken token) : base(stream, token)
			{
				_Func = func ?? throw new ArgumentNullException(nameof(func));
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedFuncTask<TResult>)state;

				try
				{
					var InnerTask = Task._Func();

					InnerTask.ContinueWith(ContinueWrappedTask, Task);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}

			private static void ContinueWrappedTask(Task<TResult> innerTask, object state)
			{
				var Task = (WrappedFuncTask<TResult>)state;

				if (innerTask.IsFaulted)
					Task.SwitchToFaulted(innerTask.Exception.InnerException);
				else if (innerTask.IsCanceled)
					Task.SwitchToCancelled();
				else
					Task.SwitchToSucceeded(innerTask.Result);
			}
		}

		private sealed class WrappedFuncTask<TValue, TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly Func<TValue, Task<TResult>> _Func;
			private readonly TValue _Value;
			//****************************************

			internal WrappedFuncTask(ValueTaskStream stream, Func<TValue, Task<TResult>> func, TValue value, CancellationToken token) : base(stream, token)
			{
				_Func = func ?? throw new ArgumentNullException(nameof(func));
				_Value = value;
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedFuncTask<TValue, TResult>)state;

				try
				{
					var InnerTask = Task._Func(Task._Value);

					InnerTask.ContinueWith(ContinueWrappedTask, Task);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}

			private static void ContinueWrappedTask(Task<TResult> innerTask, object state)
			{
				var Task = (WrappedFuncTask<TValue, TResult>)state;

				if (innerTask.IsFaulted)
					Task.SwitchToFaulted(innerTask.Exception.InnerException);
				else if (innerTask.IsCanceled)
					Task.SwitchToCancelled();
				else
					Task.SwitchToSucceeded(innerTask.Result);
			}
		}

		private sealed class WrappedFuncValueTask<TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly Func<ValueTask<TResult>> _Func;
			private ValueTask<TResult> _InnerTask;
			//****************************************

			internal WrappedFuncValueTask(ValueTaskStream stream, Func<ValueTask<TResult>> func, CancellationToken token) : base(stream, token)
			{
				_Func = func ?? throw new ArgumentNullException(nameof(func));
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private void ContinueWrappedTask()
			{
				var Awaiter = _InnerTask.GetAwaiter();

				try
				{
					SwitchToSucceeded(Awaiter.GetResult());
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e);
				}
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedFuncValueTask<TResult>)state;

				try
				{
					var Awaiter = (Task._InnerTask = Task._Func()).GetAwaiter();

					if (Awaiter.IsCompleted)
						Task.ContinueWrappedTask();
					else
						Awaiter.OnCompleted(Task.ContinueWrappedTask);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}

		private sealed class WrappedFuncValueTask<TValue, TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly Func<TValue, ValueTask<TResult>> _Func;
			private readonly TValue _Value;
			private ValueTask<TResult> _InnerTask;
			//****************************************

			internal WrappedFuncValueTask(ValueTaskStream stream, Func<TValue, ValueTask<TResult>> func, TValue value, CancellationToken token) : base(stream, token)
			{
				_Func = func ?? throw new ArgumentNullException(nameof(func));
				_Value = value;
			}

			//****************************************

			public override void Execute()
			{
				if (SwitchToPending())
					ThreadPool.QueueUserWorkItem(ExecuteWrappedTask, this);
			}

			//****************************************

			private void ContinueWrappedTask()
			{
				var Awaiter = _InnerTask.GetAwaiter();

				try
				{
					SwitchToSucceeded(Awaiter.GetResult());
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e);
				}
			}

			//****************************************

			private static void ExecuteWrappedTask(object state)
			{
				var Task = (WrappedFuncValueTask<TValue, TResult>)state;

				try
				{
					var Awaiter = (Task._InnerTask = Task._Func(Task._Value)).GetAwaiter();

					if (Awaiter.IsCompleted)
						Task.ContinueWrappedTask();
					else
						Awaiter.OnCompleted(Task.ContinueWrappedTask);
				}
				catch (OperationCanceledException e)
				{
					Task.SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					Task.SwitchToFaulted(e);
				}
			}
		}
	}
}
