#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
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
		/// Creates a new Value Task Stream
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
			var NewTask = ActionTask.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), token);

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
			var NewTask = ActionTask<TValue>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, token);

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
			var NewTask = FuncTask<TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), token);

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
			var NewTask = FuncTask<TValue, TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, token);

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
			var NewTask = WrappedTask.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), token);

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
			var NewTask = WrappedFuncTask<TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), token);

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
			var NewTask = WrappedTask<TValue>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, token);

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
			var NewTask = WrappedFuncTask<TValue, TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, token);

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
			var NewTask = WrappedValueTask.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), token);

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
			var NewTask = WrappedFuncValueTask<TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), token);

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
			var NewTask = WrappedValueTask<TValue>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, token);

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
			var NewTask = WrappedFuncValueTask<TValue, TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, token);

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

		private static void ExecutePool(IStreamTask nextTask)
		{
#if NETSTANDARD2_0
			ThreadPool.QueueUserWorkItem((state) => ExecuteNextTask((IStreamTask)state), nextTask);
#else
				ThreadPool.QueueUserWorkItem(ExecuteNextTask, nextTask, false);
#endif
		}

		private static void ExecuteNextTask(IStreamTask nextTask)
		{
			while (!ReferenceEquals(nextTask, CompleteStreamTask))
			{
				if (nextTask.TryActivate(out var NextTask))
					nextTask = nextTask.Execute();
				else
					nextTask = NextTask;
			}
		}

		private interface IStreamTask
		{
			void Queue(IStreamTask child);

			IStreamTask Execute();

			bool TryActivate(
#if !NETSTANDARD2_0
				[MaybeNullWhen(true)]
#endif
			out IStreamTask nextTask);
		}

		private sealed class CompletedTask : IStreamTask
		{
			void IStreamTask.Queue(IStreamTask child) => ExecutePool(child);

			bool IStreamTask.TryActivate(
#if !NETSTANDARD2_0
				[MaybeNullWhen(true)]
#endif
			out IStreamTask nextTask) => throw new InvalidOperationException("Cannot activate completed task");

			IStreamTask IStreamTask.Execute() => throw new InvalidOperationException("Cannot execute completed task");
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

				// Assign as the next task, only if there is none set. If we're set to completed, just run it directly
				if (Interlocked.CompareExchange(ref _NextTask, child, null) == CompleteStreamTask)
					ExecutePool(child);
			}

			public bool TryActivate(
#if !NETSTANDARD2_0
				[MaybeNullWhen(true)]
#endif
			out IStreamTask nextTask)
			{
				nextTask = null!;

				if (_CanCancel == 0)
					return true; // Task cannot be cancelled, so its free to execute

				_Registration.Dispose();

				// We've abandoned cancellation, but it may still be executing right now.
				// Mark the Task as no longer cancellable
				if (Interlocked.CompareExchange(ref _CanCancel, 0, 1) == 1)
					return true; // Success, we're now able to begin executing

				// The Task has already cancelled, so we can complete it now were not in the queue
				nextTask = Complete();

				return false;
			}

			public abstract IStreamTask Execute();

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			public TResult GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					// Only safe to release after the TaskSource has returned (or thrown)
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
					_Registration = token.Register((state) => ((BaseStreamTask<TResult> )state).Cancel(), this, false);
				}
				else
				{
					_CanCancel = 0;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected IStreamTask SwitchToSucceeded(TResult result)
			{
				var NextTask = Complete();

				_TaskSource.SetResult(result);

				return NextTask;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected IStreamTask SwitchToCancelled() => SwitchToFaulted(new OperationCanceledException());

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected IStreamTask SwitchToCancelled(CancellationToken token) => SwitchToFaulted(new OperationCanceledException(token));

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected IStreamTask SwitchToFaulted(Exception exception)
			{
				var NextTask = Complete();

				_TaskSource.SetException(exception);

				return NextTask;
			}

			protected abstract void Return();

			//****************************************

			private IStreamTask Complete()
			{
				// Mark us as completed
				var NextTask = Interlocked.CompareExchange(ref _NextTask, CompleteStreamTask, null);

				// Reduce the number of pending actions
				Interlocked.Decrement(ref _Stream!._PendingActions);

				if (NextTask == null)
				{
					// There's no task queued to run next, so mark us as completed. Makes GetComplete faster
					Interlocked.CompareExchange(ref _Stream._NextTask, CompleteStreamTask, this);

					// Return Completed as well, so ExecuteNextTask can safely abort
					NextTask = CompleteStreamTask;
				}

				TryRelease();

				return NextTask;
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

			private void Cancel()
			{
				// Ensure we're still allowed to Cancel (the Task may have already been activated)
				if (_CanCancel != 0 && Interlocked.CompareExchange(ref _CanCancel, 2, 1) == 1)
					// Do not raise Complete here. It'll be called once the Task tries to activate
					_TaskSource.SetException(new OperationCanceledException(_Token));
			}

			//****************************************

			internal short TaskToken => _TaskSource.Version;
		}

		private abstract class BaseValueTaskSource : BaseStreamTask<VoidStruct>, IValueTaskSource
		{
			void IValueTaskSource.GetResult(short token) => GetResult(token);

			//****************************************

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected IStreamTask SwitchToSucceeded() => SwitchToSucceeded(default);

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

			public override IStreamTask Execute() => SwitchToSucceeded();

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

			public override IStreamTask Execute()
			{
				try
				{
					_Action!();

					return SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
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

			public override IStreamTask Execute()
			{
				try
				{
					_Action!(_Value!);

					return SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
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

			public override IStreamTask Execute()
			{
				try
				{
					_InnerTask = _Action!().ConfigureAwait(false).GetAwaiter();

					if (_InnerTask.IsCompleted)
						return CompleteWrappedTask();
					
					_InnerTask.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Action = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					_InnerTask.GetResult();

					return SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<Task> action, CancellationToken token)
			{
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

			public override IStreamTask Execute()
			{
				try
				{
					_InnerTask = _Action!(_Value!).ConfigureAwait(false).GetAwaiter();

					if (_InnerTask.IsCompleted)
						return CompleteWrappedTask();

					_InnerTask.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Action = null;
				_Value = default!;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					_InnerTask.GetResult();

					return SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<TValue, Task> action, TValue value, CancellationToken token)
			{
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

			public override IStreamTask Execute()
			{
				if (!TryActivate(out var NextTask))
					return NextTask;

				try
				{
					var Awaiter = _InnerTask = _Action!().ConfigureAwait(false).GetAwaiter();

					if (Awaiter.IsCompleted)
						return CompleteWrappedTask();
					
					Awaiter.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Action = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					_InnerTask.GetResult();

					return SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<ValueTask> action, CancellationToken token)
			{
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

			public override IStreamTask Execute()
			{
				try
				{
					var Awaiter = _InnerTask = _Action!(_Value!).ConfigureAwait(false).GetAwaiter();

					if (Awaiter.IsCompleted)
						return CompleteWrappedTask();

					Awaiter.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Action = null;
				_Value = default!;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					_InnerTask.GetResult();

					return SwitchToSucceeded();
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource Retrieve(ValueTaskStream stream, Func<TValue, ValueTask> action, TValue value, CancellationToken token)
			{
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

			public override IStreamTask Execute()
			{
				try
				{
					return SwitchToSucceeded(_Func!());
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
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

			public override IStreamTask Execute()
			{
				try
				{
					return SwitchToSucceeded(_Func!(_Value!));
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
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

			public override IStreamTask Execute()
			{
				try
				{
					_InnerTask = _Func!().ConfigureAwait(false).GetAwaiter();

					if (_InnerTask.IsCompleted)
						return CompleteWrappedTask();

					_InnerTask.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Func = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					return SwitchToSucceeded(_InnerTask.GetResult());
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<Task<TResult>> func, CancellationToken token)
			{
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

			public override IStreamTask Execute()
			{
				try
				{
					_InnerTask = _Func!(_Value!).ConfigureAwait(false).GetAwaiter();

					if (_InnerTask.IsCompleted)
						return CompleteWrappedTask();

					_InnerTask.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Func = null;
				_Value = default!;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					return SwitchToSucceeded(_InnerTask.GetResult());
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<TValue, Task<TResult>> func, TValue value, CancellationToken token)
			{
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

			public override IStreamTask Execute()
			{
				try
				{
					_InnerTask = _Func!().ConfigureAwait(false).GetAwaiter();

					if (_InnerTask.IsCompleted)
						return CompleteWrappedTask();

					_InnerTask.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Func = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					return SwitchToSucceeded(_InnerTask.GetResult());
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<ValueTask<TResult>> func, CancellationToken token)
			{
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

			public override IStreamTask Execute()
			{
				try
				{
					_InnerTask = _Func!(_Value!).ConfigureAwait(false).GetAwaiter();

					if (_InnerTask.IsCompleted)
						return CompleteWrappedTask();

					_InnerTask.OnCompleted(_ContinueWrappedTask);

					return CompleteStreamTask;
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			protected override void Return()
			{
				_Func = null;
				_InnerTask = default;

				Cache.Add(this);
			}

			private void ContinueWrappedTask() => ExecuteNextTask(CompleteWrappedTask());

			private IStreamTask CompleteWrappedTask()
			{
				try
				{
					return SwitchToSucceeded(_InnerTask.GetResult());
				}
				catch (OperationCanceledException e)
				{
					return SwitchToCancelled(e.CancellationToken);
				}
				catch (Exception e)
				{
					return SwitchToFaulted(e);
				}
			}

			//****************************************

			public static BaseValueTaskSource<TResult> Retrieve(ValueTaskStream stream, Func<TValue, ValueTask<TResult>> func, TValue value, CancellationToken token)
			{
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
