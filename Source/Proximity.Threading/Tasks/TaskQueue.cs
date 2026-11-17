using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace System.Threading.Tasks;

/// <summary>
/// Provides a sequential queue of actions executed on the ThreadPool
/// </summary>
public sealed class TaskQueue
{ //****************************************
	private static readonly BaseTask CompleteStreamTask = new();
	//****************************************
	private BaseTask _NextTask = CompleteStreamTask;

	private int _PendingActions;
	//****************************************

	/// <summary>
	/// Creates a new Task Queue that executes all operations asynchronously
	/// </summary>
	public TaskQueue()
	{
	}

	//****************************************

	/// <summary>
	/// Queues an action
	/// </summary>
	/// <param name="action">The action to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the action</returns>
	public ValueTask Queue(Action action, CancellationToken token = default)
		=> EnqueueNew(ActionTask.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action that takes a value
	/// </summary>
	/// <param name="action">The action to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the action</returns>
	public ValueTask Queue<TValue>(Action<TValue> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(ActionTask<TValue>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Queues an action that returns a result
	/// </summary>
	/// <param name="action">The action to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the action</returns>
	/// <remarks>If the action returns a <see cref="Task"/> or <see cref="ValueTask"/>, you may want to use <see cref="O:QueueTask"/> instead</remarks>
	public ValueTask<TResult> Queue<TResult>(Func<TResult> action, CancellationToken token = default)
		=> EnqueueNew(FuncTask<TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action that takes a value and returns a result
	/// </summary>
	/// <param name="action">The action to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the action</returns>
	/// <remarks>If the action returns a <see cref="Task"/> or <see cref="ValueTask"/>, you may want to use <see cref="O:QueueTask"/> instead</remarks>
	public ValueTask<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(FuncTask<TValue, TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Queues an action returning a <see cref="Task"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task"/> has been completed</remarks>
	[Obsolete("Use QueueAsync overloads")]
	public ValueTask QueueTask(Func<Task> action, CancellationToken token = default)
		=> EnqueueNew(WrappedTask.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action returning a <see cref="Task{TResult}"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task and its result</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task{TResult}"/> has been completed</remarks>
	[Obsolete("Use QueueAsync overloads")]
	public ValueTask<TResult> QueueTask<TResult>(Func<Task<TResult>> action, CancellationToken token = default)
		=> EnqueueNew(WrappedFuncTask<TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action taking a value and returning a <see cref="Task"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task"/> has been completed</remarks>
	[Obsolete("Use QueueAsync overloads")]
	public ValueTask QueueTask<TValue>(Func<TValue, Task> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(WrappedTask<TValue>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Queues an action taking a value and returning a <see cref="Task{TResult}"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task and its result</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task{TResult}"/> has been completed</remarks>
	[Obsolete("Use QueueAsync overloads")]
	public ValueTask<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(WrappedFuncTask<TValue, TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Queues an action returning a <see cref="Task"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task"/> has been completed</remarks>
	public ValueTask QueueAsync(Func<Task> action, CancellationToken token = default)
		=> EnqueueNew(WrappedTask.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action returning a <see cref="Task{TResult}"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task and its result</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task{TResult}"/> has been completed</remarks>
	public ValueTask<TResult> QueueAsync<TResult>(Func<Task<TResult>> action, CancellationToken token = default)
		=> EnqueueNew(WrappedFuncTask<TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action taking a value and returning a <see cref="Task"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task"/> has been completed</remarks>
	public ValueTask QueueAsync<TValue>(Func<TValue, Task> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(WrappedTask<TValue>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Queues an action taking a value and returning a <see cref="Task{TResult}"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task and its result</returns>
	/// <remarks>The queue will not continue until the returned <see cref="Task{TResult}"/> has been completed</remarks>
	public ValueTask<TResult> QueueAsync<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(WrappedFuncTask<TValue, TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Queues an action returning a <see cref="ValueTask"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task</returns>
	/// <remarks>The queue will not continue until the returned <see cref="ValueTask"/> has been completed</remarks>
	public ValueTask QueueAsync(Func<ValueTask> action, CancellationToken token = default)
		=> EnqueueNew(WrappedValueTask.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action returning a <see cref="ValueTask{TResult}"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task and its result</returns>
	/// <remarks>The queue will not continue until the returned <see cref="ValueTask{TResult}"/> has been completed</remarks>
	public ValueTask<TResult> QueueAsync<TResult>(Func<ValueTask<TResult>> action, CancellationToken token = default)
		=> EnqueueNew(WrappedFuncValueTask<TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), false, token));

	/// <summary>
	/// Queues an action taking a value and returning a <see cref="ValueTask"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task</returns>
	/// <remarks>The queue will not continue until the returned <see cref="ValueTask"/> has been completed</remarks>
	public ValueTask QueueAsync<TValue>(Func<TValue, ValueTask> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(WrappedValueTask<TValue>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Queues an action taking a value and returning a <see cref="ValueTask{TResult}"/>
	/// </summary>
	/// <param name="action">The method to execute at the end of the queue</param>
	/// <param name="value">The value to pass to the action</param>
	/// <param name="token">A token to cancel this action</param>
	/// <returns>A task representing the completion of the returned task and its result</returns>
	/// <remarks>The queue will not continue until the returned <see cref="ValueTask{TResult}"/> has been completed</remarks>
	public ValueTask<TResult> QueueAsync<TValue, TResult>(Func<TValue, ValueTask<TResult>> action, TValue value, CancellationToken token = default)
		=> EnqueueNew(WrappedFuncValueTask<TValue, TResult>.Retrieve(this, action ?? throw new ArgumentNullException(nameof(action)), value, false, token));

	/// <summary>
	/// Resets the queue, so future queued actions will begin executing immediately (essentially starts a new stream)
	/// </summary>
	/// <remarks>This does not reset the pending actions counter or abort any queued actions</remarks>
	public void Reset() => Interlocked.Exchange(ref _NextTask, CompleteStreamTask);

	/// <summary>
	/// Completes when all queued actions have been executed, and all returned tasks (if any) completed
	/// </summary>
	/// <param name="token">A token to abort waiting</param>
	public ValueTask Complete(CancellationToken token = default)
		// ValueTask contract requires only one waiter, so we have to append a dummy task. We can skip this if the queue is empty
		=> Volatile.Read(ref _NextTask) == CompleteStreamTask ? default : EnqueueNew(NullTask.Retrieve(this, token));

	//****************************************

	/// <summary>
	/// Gets the number of actions that have yet to execute
	/// </summary>
	public int PendingActions => Volatile.Read(ref _PendingActions);

	//****************************************

	private ValueTask EnqueueNew(BaseTask<VoidStruct> task)
	{
		Interlocked.Increment(ref _PendingActions);
		Interlocked.Exchange(ref _NextTask, task).Queue(task);

		return new(task, task.TaskToken);
	}

	private ValueTask<T> EnqueueNew<T>(BaseValueTaskSource<T> task)
	{
		Interlocked.Increment(ref _PendingActions);
		Interlocked.Exchange(ref _NextTask, task).Queue(task);

		return new(task, task.TaskToken);
	}

	//****************************************

	private static void ExecuteNextTask(BaseTask nextTask)
	{
		if (ReferenceEquals(nextTask, CompleteStreamTask))
			return;

		// Loop executing synchronous tasks - either inside QueueXX or from ContinueWrappedTask
		while (nextTask.ExecuteSynchronously)
		{
			if (nextTask.TryActivate(out var ContinueWithTask))
				nextTask = nextTask.Execute();
			else
				nextTask = ContinueWithTask;

			if (ReferenceEquals(nextTask, CompleteStreamTask))
				return;
		}

		// Task is not synchronous, so push to the thread pool
		ThreadPool.UnsafeQueueUserWorkItem(ExecuteNextTask, nextTask);

		static void ExecuteNextTask(object? state)
		{
			var NextTask = (BaseTask)state!;

			// Since we're on the threadpool ourselves, synchronous or asynchronous is fine
			do
			{
				if (NextTask.TryActivate(out var ContinueWithTask))
					NextTask = NextTask.Execute();
				else
					NextTask = ContinueWithTask;
			}
			while (!ReferenceEquals(NextTask, CompleteStreamTask));
		}
	}

	//****************************************

	private class BaseTask
	{
		public virtual void Queue(BaseTask child) => ExecuteNextTask(child);

		public virtual bool TryActivate(
#if !NETSTANDARD2_0
			[MaybeNullWhen(true)]
#endif
		out BaseTask nextTask) => throw new InvalidOperationException("Cannot activate completed task");

		public virtual BaseTask Execute() => throw new InvalidOperationException("Cannot execute completed task");

		public virtual bool ExecuteSynchronously => true;
	}

	private abstract class BaseTask<TResult> : BaseTask, IValueTaskSource
	{ //****************************************
		private TaskQueue? _Stream;
		private bool _ExecuteSynchronously;
		private BaseTask? _NextTask;

		private ManualResetValueTaskSourceCore<TResult> _TaskSource = new();

		private CancellationToken _Token;
		private CancellationTokenRegistration _Registration;

		private int _CanCancel, _TaskState;
		//****************************************

		protected BaseTask() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		public override sealed void Queue(BaseTask child)
		{
			if (_Stream == null)
				throw new InvalidOperationException("Task is not in a valid state");

			// Assign as the next task only if there is none set. If we're set to completed, just run it directly
			var NextTask = Interlocked.CompareExchange(ref _NextTask, child, null);

			if (NextTask == CompleteStreamTask)
			{
				// Make sure to release this Task to the pool now it's not longer referenced
				TryRelease();

				ExecuteNextTask(child);
			}
			else
			{
				Debug.Assert(NextTask is null, "Task already has a descendant");
			}
		}

		public override sealed bool TryActivate(
#if !NETSTANDARD2_0
			[MaybeNullWhen(true)]
#endif
		out BaseTask nextTask)
		{
			nextTask = null!;

			if (Volatile.Read(ref _CanCancel) == 0)
				return true; // Task cannot be cancelled, so its free to execute

			// TODO: Refactor to remove the blocking cancellation disposal?
			_Registration.Dispose();

			// We've abandoned cancellation, but it may still be executing right now.
			// Mark the Task as no longer cancellable
			if (Interlocked.CompareExchange(ref _CanCancel, 0, 1) == 1)
				return true; // Success, we're now able to begin executing

			// The Task has already cancelled, so we can complete it now we're not in the queue
			nextTask = Complete();

			return false;
		}

		public override abstract BaseTask Execute();

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

		void IValueTaskSource.GetResult(short token) => GetResult(token);

		public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		//****************************************

		protected void Initialise(TaskQueue stream, bool executeSynchronously, CancellationToken token)
		{
			_Stream = stream;
			_ExecuteSynchronously = executeSynchronously;

			if (token.CanBeCanceled)
			{
				Interlocked.Exchange(ref _CanCancel, 1);
				_Token = token;
				_Registration = token.Register(static (state) => ((BaseTask<TResult>)state!).Cancel(), this, false);
			}
			else
			{
				Interlocked.Exchange(ref _CanCancel, 0);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected BaseTask SwitchToSucceeded(TResult result)
		{
			var NextTask = Complete();

			// Anyone who awaits this queued item will have their continuation triggered here
			// Since we always run continuations async, this won't block continuing the queue
			_TaskSource.SetResult(result);

			return NextTask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected BaseTask SwitchToCancelled(CancellationToken token) => SwitchToFaulted(new OperationCanceledException(token));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected BaseTask SwitchToFaulted(Exception exception)
		{
			var NextTask = Complete();

			_TaskSource.SetException(exception);

			return NextTask;
		}

		protected abstract void Return();

		//****************************************

		private BaseTask Complete()
		{
			// If we're still the head of the queue, mark the entire queue as completed.
			// This improves TaskQueue.Complete performance, and ensures there's no leftover reference to us.
			var IsComplete = ReferenceEquals(Interlocked.CompareExchange(ref _Stream!._NextTask, CompleteStreamTask, this), this);

			// Reduce the number of pending actions
			Interlocked.Decrement(ref _Stream!._PendingActions);

			if (IsComplete)
			{
				// No need to mark ourselves as complete, since it's implied by clearing the queue head.
				// No more reference to us on the queue, so we can safely call TryRelease
				TryRelease();

				return CompleteStreamTask;
			}

			// We're not the head of the queue, so TaskQueue.QueueX has been called

			// Set this task to Complete, and capture the next Task in the chain to continue execution
			// If the next Task is null, then TaskQueue has yet to call our Queue method to attach the next link
			// ie: our completion preempted TaskQueue.EnqueueNew mid-processing
			var NextTask = Interlocked.Exchange(ref _NextTask, CompleteStreamTask);

			if (NextTask is null)
			// We're in the preempted scenario, so Queue will take care of releasing us and executing the next link
				return CompleteStreamTask;

			TryRelease();

			return NextTask;
		}

		private void TryRelease()
		{
			// Both GetResult and Complete need to be called before we can release this Task to the pool
			// If the task never gets awaited, we don't really care - we'll just get garbage collected
			if (Interlocked.Increment(ref _TaskState) < 2)
				return;

			// Zero out our fields and return us to the object pool
			_Stream = null;
			_Token = default;
			_Registration = default;
			Interlocked.Exchange(ref _NextTask, null);
			_TaskSource.Reset();
			Interlocked.Exchange(ref _TaskState, 0);

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

		public override bool ExecuteSynchronously => _ExecuteSynchronously;
	}

	private abstract class BaseValueTaskSource : BaseTask<VoidStruct>
	{ //****************************************
		private Action? _ContinueWrappedTask;

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _InnerTask;
		//****************************************

		public override sealed BaseTask Execute()
		{
			if (!TryActivate(out var NextTask))
				return NextTask;

			try
			{
				var Awaiter = GetAwaitable().ConfigureAwait(false).GetAwaiter();

				if (Awaiter.IsCompleted)
				{
					Awaiter.GetResult();

					return SwitchToSucceeded(default);
				}

				_InnerTask = Awaiter;

				Awaiter.OnCompleted(_ContinueWrappedTask ??= ContinueWrappedTask);

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

		protected abstract ValueTask GetAwaitable();

		private void ContinueWrappedTask()
		{
			var Awaiter = _InnerTask;

			_InnerTask = default;

			// ExecuteNextTask may block executing the next queued task(s) if they're synchronous
			try
			{
				Awaiter.GetResult();

				ExecuteNextTask(SwitchToSucceeded(default));
			}
			catch (OperationCanceledException e)
			{
				ExecuteNextTask(SwitchToCancelled(e.CancellationToken));
			}
			catch (Exception e)
			{
				ExecuteNextTask(SwitchToFaulted(e));
			}
		}
	}

	private abstract class BaseValueTaskSource<TResult> : BaseTask<TResult>, IValueTaskSource<TResult>
	{ //****************************************
		private Action? _ContinueWrappedTask;

		private ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter _InnerTask;
		//****************************************

		public override sealed BaseTask Execute()
		{
			try
			{
				var Awaiter = GetAwaitable().ConfigureAwait(false).GetAwaiter();

				if (Awaiter.IsCompleted)
					return SwitchToSucceeded(Awaiter.GetResult());

				_InnerTask = Awaiter;

				Awaiter.OnCompleted(_ContinueWrappedTask ??= ContinueWrappedTask);

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

		protected abstract ValueTask<TResult> GetAwaitable();

		private void ContinueWrappedTask()
		{
			var Awaiter = _InnerTask;

			_InnerTask = default;

			// ExecuteNextTask may block executing the next queued task(s) if they're synchronous
			try
			{
				ExecuteNextTask(SwitchToSucceeded(Awaiter.GetResult()));
			}
			catch (OperationCanceledException e)
			{
				ExecuteNextTask(SwitchToCancelled(e.CancellationToken));
			}
			catch (Exception e)
			{
				ExecuteNextTask(SwitchToFaulted(e));
			}
		}
	}

	private sealed class NullTask : BaseTask<VoidStruct>, IValueTaskSource
	{ //****************************************
		private readonly static ConcurrentBag<NullTask> Cache = new();
		//****************************************

		public override BaseTask Execute() => SwitchToSucceeded(default);

		protected override void Return() => Cache.Add(this);

		void IValueTaskSource.GetResult(short token) => GetResult(token);

		//****************************************

		public static NullTask Retrieve(TaskQueue stream, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new NullTask();

			Task.Initialise(stream, true, token);

			return Task;
		}
	}

	private sealed class ActionTask : BaseValueTaskSource
	{ //****************************************
		private readonly static ConcurrentBag<ActionTask> Cache = new();
		//****************************************
		private Action? _Action;
		//****************************************

		protected override ValueTask GetAwaitable()
		{
			_Action!();

			return default;
		}

		protected override void Return()
		{
			_Action = null;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource Retrieve(TaskQueue stream, Action action, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new ActionTask();

			Task._Action = action;
			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class ActionTask<TValue> : BaseValueTaskSource
	{ //****************************************
		private readonly static ConcurrentBag<ActionTask<TValue>> Cache = new();
		//****************************************
		private Action<TValue>? _Action;
		private TValue _Value = default!;
		//****************************************

		protected override ValueTask GetAwaitable()
		{
			_Action!(_Value);

			return default;
		}

		protected override void Return()
		{
			_Action = null;
			_Value = default!;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource Retrieve(TaskQueue stream, Action<TValue> action, TValue value, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new ActionTask<TValue>();

			Task._Action = action;
			Task._Value = value;
			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class FuncTask<TResult> : BaseValueTaskSource<TResult>
	{ //****************************************
		private readonly static ConcurrentBag<FuncTask<TResult>> Cache = new();
		//****************************************
		private Func<TResult>? _Func;
		//****************************************

		protected override ValueTask<TResult> GetAwaitable() => new(_Func!());

		protected override void Return()
		{
			_Func = null;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource<TResult> Retrieve(TaskQueue stream, Func<TResult> func, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new FuncTask<TResult>();

			Task._Func = func;
			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class FuncTask<TValue, TResult> : BaseValueTaskSource<TResult>
	{ //****************************************
		private readonly static ConcurrentBag<FuncTask<TValue, TResult>> Cache = new();
		//****************************************
		private Func<TValue, TResult>? _Func;
		private TValue _Value = default!;
		//****************************************

		protected override ValueTask<TResult> GetAwaitable() => new(_Func!(_Value));

		protected override void Return()
		{
			_Func = null;
			_Value = default!;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource<TResult> Retrieve(TaskQueue stream, Func<TValue, TResult> func, TValue value, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new FuncTask<TValue, TResult>();

			Task._Func = func;
			Task._Value = value;

			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedTask : BaseValueTaskSource
	{ //****************************************
		private readonly static ConcurrentBag<WrappedTask> Cache = new();
		//****************************************
		private Func<Task>? _Action;
		//****************************************

		protected override ValueTask GetAwaitable() => new(_Action!());

		protected override void Return()
		{
			_Action = null;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource Retrieve(TaskQueue stream, Func<Task> action, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedTask();

			Task._Action = action;
			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedTask<TValue> : BaseValueTaskSource
	{ //****************************************
		private readonly static ConcurrentBag<WrappedTask<TValue>> Cache = new();
		//****************************************
		private Func<TValue, Task>? _Action;
		private TValue _Value = default!;
		//****************************************

		protected override ValueTask GetAwaitable() => new(_Action!(_Value));

		protected override void Return()
		{
			_Action = null;
			_Value = default!;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource Retrieve(TaskQueue stream, Func<TValue, Task> action, TValue value, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedTask<TValue>();

			Task._Action = action;
			Task._Value = value;
			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedValueTask : BaseValueTaskSource
	{ //****************************************
		private readonly static ConcurrentBag<WrappedValueTask> Cache = new();
		//****************************************
		private Func<ValueTask>? _Action;
		//****************************************

		protected override ValueTask GetAwaitable() => _Action!();

		protected override void Return()
		{
			_Action = null;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource Retrieve(TaskQueue stream, Func<ValueTask> action, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedValueTask();

			Task._Action = action;
			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedValueTask<TValue> : BaseValueTaskSource
	{ //****************************************
		private readonly static ConcurrentBag<WrappedValueTask<TValue>> Cache = new();
		//****************************************
		private Func<TValue, ValueTask>? _Action;
		private TValue _Value = default!;
		//****************************************

		protected override ValueTask GetAwaitable() => _Action!(_Value);

		protected override void Return()
		{
			_Action = null;
			_Value = default!;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource Retrieve(TaskQueue stream, Func<TValue, ValueTask> action, TValue value, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedValueTask<TValue>();

			Task._Action = action;
			Task._Value = value;

			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedFuncTask<TResult> : BaseValueTaskSource<TResult>
	{ //****************************************
		private readonly static ConcurrentBag<WrappedFuncTask<TResult>> Cache = new();
		//****************************************
		private Func<Task<TResult>>? _Func;
		//****************************************

		protected override ValueTask<TResult> GetAwaitable() => new(_Func!());

		protected override void Return()
		{
			_Func = null;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource<TResult> Retrieve(TaskQueue stream, Func<Task<TResult>> func, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedFuncTask<TResult>();

			Task._Func = func;

			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedFuncTask<TValue, TResult> : BaseValueTaskSource<TResult>
	{ //****************************************
		private readonly static ConcurrentBag<WrappedFuncTask<TValue, TResult>> Cache = new();
		//****************************************
		private Func<TValue, Task<TResult>>? _Func;
		private TValue _Value = default!;
		//****************************************

		protected override ValueTask<TResult> GetAwaitable() => new(_Func!(_Value));

		protected override void Return()
		{
			_Func = null;
			_Value = default!;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource<TResult> Retrieve(TaskQueue stream, Func<TValue, Task<TResult>> func, TValue value, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedFuncTask<TValue, TResult>();

			Task._Func = func;
			Task._Value = value;

			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedFuncValueTask<TResult> : BaseValueTaskSource<TResult>
	{ //****************************************
		private readonly static ConcurrentBag<WrappedFuncValueTask<TResult>> Cache = new();
		//****************************************
		private Func<ValueTask<TResult>>? _Func;
		//****************************************

		protected override ValueTask<TResult> GetAwaitable() => _Func!();

		protected override void Return()
		{
			_Func = null;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource<TResult> Retrieve(TaskQueue stream, Func<ValueTask<TResult>> func, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedFuncValueTask<TResult>();

			Task._Func = func;

			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}

	private sealed class WrappedFuncValueTask<TValue, TResult> : BaseValueTaskSource<TResult>
	{ //****************************************
		private readonly static ConcurrentBag<WrappedFuncValueTask<TValue, TResult>> Cache = new();
		//****************************************
		private Func<TValue, ValueTask<TResult>>? _Func;
		private TValue _Value = default!;
		//****************************************

		protected override ValueTask<TResult> GetAwaitable() => _Func!(_Value);

		protected override void Return()
		{
			_Func = null;

			Cache.Add(this);
		}

		//****************************************

		public static BaseValueTaskSource<TResult> Retrieve(TaskQueue stream, Func<TValue, ValueTask<TResult>> func, TValue value, bool executeSynchronously, CancellationToken token)
		{
			if (!Cache.TryTake(out var Task))
				Task = new WrappedFuncValueTask<TValue, TResult>();

			Task._Func = func;
			Task._Value = value;

			Task.Initialise(stream, executeSynchronously, token);

			return Task;
		}
	}
}
