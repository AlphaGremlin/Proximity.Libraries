#nullable enable
#pragma warning disable IDE0016 // Use 'throw' expression

using System;
using System.Collections.Concurrent;
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
		private static readonly IStreamTask CompleteStreamTask = new CompletedTask();
		//****************************************
		private IStreamTask _NextTask = CompleteStreamTask;

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
		/// <param name="token">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask Queue(Action action, CancellationToken token = default)
		{
			var NewTask = ActionTask.Retrieve(this, action, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a task that takes a value to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask Queue<TValue>(Action<TValue> action, TValue value, CancellationToken token = default)
		{
			var NewTask = ActionTask<TValue>.Retrieve(this, action, value, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a task that returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask<TResult> Queue<TResult>(Func<TResult> action, CancellationToken token = default)
		{
			var NewTask = FuncTask<TResult>.Retrieve(this, action, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a task that takes a value and returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public ValueTask<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value, CancellationToken token = default)
		{
			var NewTask = FuncTask<TValue, TResult>.Retrieve(this, action, value, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask(Func<Task> action, CancellationToken token = default)
		{
			var NewTask = WrappedTask.Retrieve(this, action, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TResult>(Func<Task<TResult>> action, CancellationToken token = default)
		{
			var NewTask = WrappedFuncTask<TResult>.Retrieve(this, action, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask<TValue>(Func<TValue, Task> action, TValue value, CancellationToken token = default)
		{
			var NewTask = WrappedTask<TValue>.Retrieve(this, action, value, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value, CancellationToken token = default)
		{
			var NewTask = WrappedFuncTask<TValue, TResult>.Retrieve(this, action, value, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask(Func<ValueTask> action, CancellationToken token = default)
		{
			var NewTask = WrappedValueTask.Retrieve(this, action, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TResult>(Func<ValueTask<TResult>> action, CancellationToken token = default)
		{
			var NewTask = WrappedFuncValueTask<TResult>.Retrieve(this, action, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask QueueTask<TValue>(Func<TValue, ValueTask> action, TValue value, CancellationToken token = default)
		{
			var NewTask = WrappedValueTask<TValue>.Retrieve(this, action, value, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="token">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public ValueTask<TResult> QueueTask<TValue, TResult>(Func<TValue, ValueTask<TResult>> action, TValue value, CancellationToken token = default)
		{
			var NewTask = WrappedFuncValueTask<TValue, TResult>.Retrieve(this, action, value, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
		}

		/// <summary>
		/// Resets the stream, so future tasks will begin executing immediately (essentially starts a new stream)
		/// </summary>
		/// <remarks>This does not reset the pending actions counter</remarks>
		public void Reset() => Interlocked.Exchange(ref _NextTask, CompleteStreamTask);

		/// <summary>
		/// Completes when all queued operations have been completed
		/// </summary>
		/// <param name="token">A token to abort waiting</param>
		public ValueTask Complete(CancellationToken token = default)
		{
			var OldNextTask = _NextTask;

			// Optimisation so we don't add Null Tasks when the stream is empty
			if (OldNextTask == CompleteStreamTask)
				return default;

			// Append a Null Task to the Stream.
			var NewTask = NullTask.Retrieve(this, token);

			Interlocked.Increment(ref _PendingActions);
			Interlocked.Exchange(ref _NextTask, NewTask).Queue(NewTask);

			return NewTask.Task;
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
			void ExecutePool();
			void ExecuteLocal(bool isNested);
		}

		private sealed class CompletedTask : IStreamTask
		{
			void IStreamTask.Queue(IStreamTask child) => child.ExecutePool();

			void IStreamTask.ExecutePool() => throw new InvalidOperationException("Cannot execute completed task");

			void IStreamTask.ExecuteLocal(bool isNested) => throw new InvalidOperationException("Cannot execute completed task");
		}

		//****************************************

		private abstract class BaseStreamTask<TResult> : IStreamTask
		{ //****************************************
			private ValueTaskStream? _Stream;
			private IStreamTask? _NextTask;

#if NETSTANDARD2_0
			private readonly ManualResetValueTaskSourceCore<TResult> _TaskSource = new ManualResetValueTaskSourceCore<TResult>();
#else
			private ManualResetValueTaskSourceCore<TResult> _TaskSource = new ManualResetValueTaskSourceCore<TResult>();
#endif

			private CancellationToken _Token;
			private CancellationTokenRegistration _Registration;

			private volatile int _CanCancel, _TaskState;
			//****************************************

			protected BaseStreamTask() => _TaskSource.RunContinuationsAsynchronously = true;

			//****************************************

			public void Queue(IStreamTask child)
			{
				if (_Stream == null)
					throw new InvalidOperationException("Task is not in a valid state");

				// Assign as the next task, only if there is none set
				var NextTask = Interlocked.CompareExchange(ref _NextTask, child, null);

				// If we're set to completed, just run it directly
				if (NextTask == CompleteStreamTask)
					child.ExecutePool();
			}

			public void ExecutePool()
			{
#if NETSTANDARD2_0
				ThreadPool.QueueUserWorkItem((state) => ((BaseStreamTask<TResult>)state).ExecuteLocal(false));
#else
				ThreadPool.QueueUserWorkItem((state) => state.ExecuteLocal(false), this, false);
#endif
			}

			public abstract void ExecuteLocal(bool isNested);

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			public TResult GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					TryRelease();
				}
			}

			public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			//****************************************

			protected void Initialise(ValueTaskStream stream, CancellationToken token)
			{
				_Stream = stream;

				if (token.CanBeCanceled)
				{
					_CanCancel = 1;
					_Token = token;
					_Registration = token.Register(CancelTask, this);
				}
				else
				{
					_CanCancel = 0;
				}
			}

			protected bool SwitchToPending(bool isNested)
			{
				if (_CanCancel == 0)
					return true; // Task cannot be cancelled, so its free to execute

				_Registration.Dispose();

				if (Interlocked.CompareExchange(ref _CanCancel, 0, 1) != 1)
				{
					// The task has already been cancelled. Move to the next Task in the stream
					Complete(isNested);

					return false;
				}

				if (_Token.IsCancellationRequested)
				{
					// The token was about to cancel, so cancel the Task
					SwitchToCancelled(isNested);

					return false;
				}

				// Task is allowed to begin execution
				return true;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SwitchToSucceeded(TResult result, bool isNested)
			{
				Complete(isNested);
				_TaskSource.SetResult(result);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SwitchToCancelled(bool isNested) => SwitchToFaulted(new OperationCanceledException(), isNested);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SwitchToCancelled(CancellationToken token, bool isNested) => SwitchToFaulted(new OperationCanceledException(token), isNested);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SwitchToFaulted(Exception exception, bool isNested)
			{
				Complete(isNested);
				_TaskSource.SetException(exception);
			}

			protected abstract void Return();

			//****************************************

			private void Complete(bool isNested)
			{
				// Mark us as completed
				var NextTask = Interlocked.CompareExchange(ref _NextTask, CompleteStreamTask, null);

				// Reduce the number of pending actions
				Interlocked.Decrement(ref _Stream!._PendingActions);

				// If there's no task queued to run next, mark us as completed. Makes GetComplete faster
				if (NextTask == null)
					Interlocked.CompareExchange(ref _Stream._NextTask, CompleteStreamTask, this);
				else if (isNested)
					NextTask.ExecutePool(); // We're in the Complete of the previous Task, so break the call stack
				else
					NextTask.ExecuteLocal(true); // It's safe to try and start the next Task here

				TryRelease();
			}

			private void TryRelease()
			{
				// Both GetResult and Complete need to be called before we can release this Task to the pool
				if (Interlocked.Increment(ref _TaskState) < 2)
					return;

				// Zero out our fields and return us to the object pool
				_Stream = null;
				_Token = default;
				_Registration = default;
				_NextTask = null;
				_TaskSource.Reset();
				_TaskState = 0;

				Return();
			}

			private static void CancelTask(object state)
			{
				var Task = (BaseStreamTask<TResult>)state;

				if (Task._CanCancel == 0 || Interlocked.CompareExchange(ref Task._CanCancel, 2, 1) != 1)
					return; // Task has already begun execution, can no longer cancel

				// Do not raise Complete here, since we can only do that once the Task has been reached in the Stream
				// We do run the continuation, since the Task has reached a final status
				Task._TaskSource.SetException(new OperationCanceledException(Task._Token));
			}

			//****************************************

			internal short TaskToken => _TaskSource.Version;
		}

		private abstract class BaseValueTaskSource : BaseStreamTask<VoidStruct>, IValueTaskSource
		{
			void IValueTaskSource.GetResult(short token) => GetResult(token);

			//****************************************

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SwitchToSucceeded(bool isNested) => SwitchToSucceeded(default, isNested);

			//****************************************

			public ValueTask Task => new ValueTask(this, TaskToken);
		}

		private abstract class BaseValueTaskSource<TResult> : BaseStreamTask<TResult>, IValueTaskSource<TResult>
		{
			public ValueTask<TResult> Task => new ValueTask<TResult>(this, TaskToken);
		}

		private sealed class NullTask : BaseValueTaskSource
		{ //****************************************
			private readonly static ConcurrentBag<NullTask> Cache = new ConcurrentBag<NullTask>();
			//****************************************

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
					SwitchToSucceeded(isNested);
			}

			protected override void Return() => Cache.Add(this);

			//****************************************

			public static NullTask Retrieve(ValueTaskStream stream, CancellationToken token)
			{
				if (!Cache.TryTake(out var Task))
					Task = new NullTask();

				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class ActionTask : BaseValueTaskSource
		{ //****************************************
			private readonly static ConcurrentBag<ActionTask> Cache = new ConcurrentBag<ActionTask>();
			//****************************************
			private Action? _Action;
			//****************************************

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_Action!();

						SwitchToSucceeded(isNested);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Action = null;

				Cache.Add(this);
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Action action, CancellationToken token)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				if (!Cache.TryTake(out var Task))
					Task = new ActionTask();

				Task._Action = action;
				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class ActionTask<TValue> : BaseValueTaskSource
		{ //****************************************
			private readonly static ConcurrentBag<ActionTask<TValue>> Cache = new ConcurrentBag<ActionTask<TValue>>();
			//****************************************
			private Action<TValue>? _Action;
			private TValue _Value = default!;
			//****************************************

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_Action!(_Value!);

						SwitchToSucceeded(isNested);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Action = null;
				_Value = default!;

				Cache.Add(this);
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Action<TValue> action, TValue value, CancellationToken token)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				if (!Cache.TryTake(out var Task))
					Task = new ActionTask<TValue>();

				Task._Action = action;
				Task._Value = value;
				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedTask : BaseValueTaskSource
		{ //****************************************
			private readonly static ConcurrentBag<WrappedTask> Cache = new ConcurrentBag<WrappedTask>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<Task>? _Action;
			private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _InnerTask;
			//****************************************

			public WrappedTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_InnerTask = _Action!().ConfigureAwait(false).GetAwaiter();

						if (_InnerTask.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							_InnerTask.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Action = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					_InnerTask.GetResult();

					SwitchToSucceeded(isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<Task> action, CancellationToken token)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedTask();

				Task._Action = action;
				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedTask<TValue> : BaseValueTaskSource
		{ //****************************************
			private readonly static ConcurrentBag<WrappedTask<TValue>> Cache = new ConcurrentBag<WrappedTask<TValue>>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<TValue, Task>? _Action;
			private TValue _Value = default!;
			private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _InnerTask;
			//****************************************

			public WrappedTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_InnerTask = _Action!(_Value!).ConfigureAwait(false).GetAwaiter();

						if (_InnerTask.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							_InnerTask.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Action = null;
				_Value = default!;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					_InnerTask.GetResult();

					SwitchToSucceeded(isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<TValue, Task> action, TValue value, CancellationToken token)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedTask<TValue>();

				Task._Action = action;
				Task._Value = value;
				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedValueTask : BaseValueTaskSource
		{ //****************************************
			private readonly static ConcurrentBag<WrappedValueTask> Cache = new ConcurrentBag<WrappedValueTask>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<ValueTask>? _Action;
			private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _InnerTask;
			//****************************************

			public WrappedValueTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						var Awaiter = _InnerTask = _Action!().ConfigureAwait(false).GetAwaiter();

						if (Awaiter.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							Awaiter.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Action = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					_InnerTask.GetResult();

					SwitchToSucceeded(isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<ValueTask> action, CancellationToken token)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedValueTask();

				Task._Action = action;
				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedValueTask<TValue> : BaseValueTaskSource
		{ //****************************************
			private readonly static ConcurrentBag<WrappedValueTask<TValue>> Cache = new ConcurrentBag<WrappedValueTask<TValue>>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<TValue, ValueTask>? _Action;
			private TValue _Value = default!;
			private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _InnerTask;
			//****************************************

			internal WrappedValueTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						var Awaiter = _InnerTask = _Action!(_Value!).ConfigureAwait(false).GetAwaiter();

						if (Awaiter.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							Awaiter.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Action = null;
				_Value = default!;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					_InnerTask.GetResult();

					SwitchToSucceeded(isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<TValue, ValueTask> action, TValue value, CancellationToken token)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedValueTask<TValue>();

				Task._Action = action;
				Task._Value = value;

				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class FuncTask<TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly static ConcurrentBag<FuncTask<TResult>> Cache = new ConcurrentBag<FuncTask<TResult>>();
			//****************************************
			private Func<TResult>? _Func;
			//****************************************

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						SwitchToSucceeded(_Func!(), isNested);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Func = null;

				Cache.Add(this);
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<TResult> func, CancellationToken token)
			{
				if (func == null)
					throw new ArgumentNullException(nameof(func));

				if (!Cache.TryTake(out var Task))
					Task = new FuncTask<TResult>();

				Task._Func = func;

				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class FuncTask<TValue, TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly static ConcurrentBag<FuncTask<TValue, TResult>> Cache = new ConcurrentBag<FuncTask<TValue, TResult>>();
			//****************************************
			private Func<TValue, TResult>? _Func;
			private TValue _Value = default!;
			//****************************************

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						SwitchToSucceeded(_Func!(_Value!), isNested);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Func = null;
				_Value = default!;

				Cache.Add(this);
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<TValue, TResult> func, TValue value, CancellationToken token)
			{
				if (func == null)
					throw new ArgumentNullException(nameof(func));

				if (!Cache.TryTake(out var Task))
					Task = new FuncTask<TValue, TResult>();

				Task._Func = func;
				Task._Value = value;

				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedFuncTask<TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly static ConcurrentBag<WrappedFuncTask<TResult>> Cache = new ConcurrentBag<WrappedFuncTask<TResult>>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<Task<TResult>>? _Func;
			private ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter _InnerTask;
			//****************************************

			public WrappedFuncTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_InnerTask = _Func!().ConfigureAwait(false).GetAwaiter();

						if (_InnerTask.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							_InnerTask.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Func = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					SwitchToSucceeded(_InnerTask.GetResult(), isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<Task<TResult>> func, CancellationToken token)
			{
				if (func == null)
					throw new ArgumentNullException(nameof(func));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedFuncTask<TResult>();

				Task._Func = func;

				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedFuncTask<TValue, TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly static ConcurrentBag<WrappedFuncTask<TValue, TResult>> Cache = new ConcurrentBag<WrappedFuncTask<TValue, TResult>>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<TValue, Task<TResult>>? _Func;
			private TValue _Value = default!;
			private ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter _InnerTask;
			//****************************************

			public WrappedFuncTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_InnerTask = _Func!(_Value!).ConfigureAwait(false).GetAwaiter();

						if (_InnerTask.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							_InnerTask.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Func = null;
				_Value = default!;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					SwitchToSucceeded(_InnerTask.GetResult(), isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<TValue, Task<TResult>> func, TValue value, CancellationToken token)
			{
				if (func == null)
					throw new ArgumentNullException(nameof(func));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedFuncTask<TValue, TResult>();

				Task._Func = func;
				Task._Value = value;

				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedFuncValueTask<TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly static ConcurrentBag<WrappedFuncValueTask<TResult>> Cache = new ConcurrentBag<WrappedFuncValueTask<TResult>>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<ValueTask<TResult>>? _Func;
			private ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter _InnerTask;
			//****************************************

			internal WrappedFuncValueTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_InnerTask = _Func!().ConfigureAwait(false).GetAwaiter();

						if (_InnerTask.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							_InnerTask.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Func = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					SwitchToSucceeded(_InnerTask.GetResult(), isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<ValueTask<TResult>> func, CancellationToken token)
			{
				if (func == null)
					throw new ArgumentNullException(nameof(func));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedFuncValueTask<TResult>();

				Task._Func = func;

				Task.Initialise(stream, token);

				return Task;
			}
		}

		private sealed class WrappedFuncValueTask<TValue, TResult> : BaseValueTaskSource<TResult>
		{ //****************************************
			private readonly static ConcurrentBag<WrappedFuncValueTask<TValue, TResult>> Cache = new ConcurrentBag<WrappedFuncValueTask<TValue, TResult>>();
			//****************************************
			private readonly Action _ContinueWrappedTask;

			private Func<TValue, ValueTask<TResult>>? _Func;
			private TValue _Value = default!;
			private ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter _InnerTask;
			//****************************************

			public WrappedFuncValueTask() => _ContinueWrappedTask = ContinueWrappedTask;

			public override void ExecuteLocal(bool isNested)
			{
				if (SwitchToPending(isNested))
				{
					try
					{
						_InnerTask = _Func!(_Value!).ConfigureAwait(false).GetAwaiter();

						if (_InnerTask.IsCompleted)
							ContinueWrappedTask(isNested);
						else
							_InnerTask.OnCompleted(_ContinueWrappedTask);
					}
					catch (OperationCanceledException e)
					{
						SwitchToCancelled(e.CancellationToken, isNested);
					}
					catch (Exception e)
					{
						SwitchToFaulted(e, isNested);
					}
				}
			}

			protected override void Return()
			{
				_Func = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void ContinueWrappedTask() => ContinueWrappedTask(false);

			private void ContinueWrappedTask(bool isNested)
			{
				try
				{
					SwitchToSucceeded(_InnerTask.GetResult(), isNested);
				}
				catch (OperationCanceledException e)
				{
					SwitchToCancelled(e.CancellationToken, isNested);
				}
				catch (Exception e)
				{
					SwitchToFaulted(e, isNested);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<TValue, ValueTask<TResult>> func, TValue value, CancellationToken token)
			{
				if (func == null)
					throw new ArgumentNullException(nameof(func));

				if (!Cache.TryTake(out var Task))
					Task = new WrappedFuncValueTask<TValue, TResult>();

				Task._Func = func;
				Task._Value = value;

				Task.Initialise(stream, token);

				return Task;
			}
		}
	}
}
