using System;
using System.Security;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Manages execution of a callback on the ThreadPool based on a flag being set
	/// </summary>
	public sealed class ActionFlag : IDisposable, IAsyncDisposable
	{ //****************************************
		private readonly Func<ValueTask> _Callback;

		private readonly WaitCallback _ExecuteTask;
		private readonly Action _CompleteTask;

		private int _State;
		private readonly Timer? _DelayTimer;

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
		private TaskCompletionSource<VoidStruct>? _WaitTask, _PendingWaitTask;
		//****************************************

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public ActionFlag(Action callback) : this(WrapCallback(callback), TimeSpan.Zero)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public ActionFlag(Action callback, TimeSpan delay) : this(WrapCallback(callback), delay)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public ActionFlag(Func<Task> callback) : this(WrapCallback(callback), TimeSpan.Zero)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public ActionFlag(Func<Task> callback, TimeSpan delay) : this(WrapCallback(callback), delay)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public ActionFlag(Func<ValueTask> callback) : this(callback, TimeSpan.Zero)
		{
		}

		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public ActionFlag(Func<ValueTask> callback, TimeSpan delay)
		{
			_Callback = callback;

			if (delay == TimeSpan.Zero)
			{
				_ExecuteTask = ExecuteTask;
			}
			else
			{
				Delay = delay;
				_ExecuteTask = ExecuteDelay;
				_DelayTimer = new Timer(ExecuteTask);
			}

			_CompleteTask = CompleteTask;
		}

		//****************************************

		void IDisposable.Dispose() => DisposeAsync();

		/// <summary>
		/// Disposes of the asynchronous task flag
		/// </summary>
		public ValueTask DisposeAsync()
		{
			int PreviousState, NewState;

			do
			{
				PreviousState = _State;

				switch (PreviousState)
				{
				case Status.Disposed:
				case Status.DisposedFlagged:
				case Status.DisposedExecuting:
					return default; // Already Disposed, or waiting on a Dispose

				case Status.Executing:
					NewState = Status.DisposedExecuting;
					break;

				case Status.Flagged:
					NewState = Status.DisposedFlagged;
					break;

				default:
					NewState = Status.Disposed;
					break;
				}
			}
			while (Interlocked.CompareExchange(ref _State, NewState, PreviousState) != PreviousState);

#if NETSTANDARD2_0
			_DelayTimer?.Dispose();
#else
			if (_DelayTimer != null)
			{
				var DisposeTimer = _DelayTimer.DisposeAsync();

				if (!DisposeTimer.IsCompleted)
					return InternalDispose(DisposeTimer);

				async ValueTask InternalDispose(ValueTask disposeTask)
				{
					await disposeTask;

					await WaitForExecution();
				}
			}
#endif

			return WaitForExecution();
		}

		//****************************************

		/// <summary>
		/// Sets the flag, causing the task to run/re-run depending on the status
		/// </summary>
		public void Set()
		{
			int PreviousState;

			do
			{
				PreviousState = _State;

				switch (PreviousState)
				{
				case Status.Disposed:
				case Status.DisposedExecuting:
					throw new ObjectDisposedException(nameof(ActionFlag), "Action Flag has been disposed");

				case Status.Flagged:
				case Status.DisposedFlagged:
					return; // Already pending execution
				}

				// We're either Waiting or Executing
			}
			while (Interlocked.CompareExchange(ref _State, Status.Flagged, PreviousState) != PreviousState);

			if (PreviousState == Status.Waiting)
				// The ActionFlag was in a waiting state, so start the callback
				ThreadPool.UnsafeQueueUserWorkItem(_ExecuteTask, null);
		}

		/// <summary>
		/// Sets the flag and waits for the callback to run at least once.
		/// </summary>
		/// <returns>A task that will complete once the callback has run</returns>
		public Task SetAndWait()
		{
			var CurrentWaitTask = Volatile.Read(ref _WaitTask);

			// Has someone else already called SetAndWait but the callback hasn't run yet?
			if (CurrentWaitTask == null)
			{
				CurrentWaitTask = new TaskCompletionSource<VoidStruct>();

				// Try and assign a new task
				var NewWaitTask = Interlocked.CompareExchange(ref _WaitTask, CurrentWaitTask, null);

				// If we got pre-empted, piggyback of the other task
				if (NewWaitTask != null)
					return NewWaitTask.Task;

				// Set the flag so our callback runs
				// Our task may actually be completed at this point, if ProcessTaskFlag executes on another thread between now and the previous CompareExchange
				Set();
			}

			return CurrentWaitTask.Task;
		}

		//****************************************

		private ValueTask WaitForExecution()
		{
			TaskCompletionSource<VoidStruct>? CurrentWaitTask;

			for (; ; )
			{
				switch (Volatile.Read(ref _State))
				{
				case Status.Waiting:
				case Status.Disposed:
					return default; // Nothing to wait on

				case Status.DisposedFlagged:
				case Status.Flagged:
					CurrentWaitTask = Volatile.Read(ref _WaitTask);

					if (CurrentWaitTask != null)
						return new ValueTask(CurrentWaitTask.Task);

					Interlocked.CompareExchange(ref _WaitTask, new TaskCompletionSource<VoidStruct>(), null);
					// Now loop back to make sure we're still flagged, otherwise we risk not being picked up
					break;

				case Status.DisposedExecuting:
				case Status.Executing:
					CurrentWaitTask = Volatile.Read(ref _PendingWaitTask);

					if (CurrentWaitTask != null)
						return new ValueTask(CurrentWaitTask.Task);

					Interlocked.CompareExchange(ref _PendingWaitTask, new TaskCompletionSource<VoidStruct>(), null);
					// Now loop back to make sure we're still executing, otherwise we risk not being picked up
					break;
				}
			}
		}

		private void ExecuteDelay(object state)
		{
			try
			{
				// Wait a bit before beginning execution
				_DelayTimer!.Change(Delay, Timeout.InfiniteTimeSpan);
			}
			catch (ObjectDisposedException)
			{
			}
		}

		private void ExecuteTask(object state)
		{
			int PreviousState, NewState;

			// Update the state of the Action Flag to Executing/DisposedExecuting
			do
			{
				PreviousState = _State;

				NewState = PreviousState switch
				{
					Status.DisposedFlagged => Status.DisposedExecuting,
					Status.Flagged => Status.Executing,
					_ => throw new InvalidOperationException($"Action Flag is in an unexpected state: {PreviousState}"),
				};
			}
			while (Interlocked.CompareExchange(ref _State, NewState, PreviousState) != PreviousState);

			// Capture any requests to wait for this callback
			_PendingWaitTask = Interlocked.Exchange(ref _WaitTask, null);

			//****************************************

			// Raise the callback
			try
			{
				var Task = _Callback();

				_Awaiter = Task.ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					CompleteTask();
				else
					_Awaiter.OnCompleted(_CompleteTask);
			}
			catch (Exception)
			{
				if (!SwallowExceptions)
					throw;
			}
		}

		private void CompleteTask()
		{
			// Examine the result of the Task
			try
			{
				_Awaiter.GetResult();
			}
			catch (Exception)
			{
				if (!SwallowExceptions)
					throw;
			}

			//****************************************

			// If we captured a wait task, set it
			Interlocked.Exchange(ref _PendingWaitTask, null)?.SetResult(default);

			int PreviousState, NewState;

			// Update the state of the Action Flag to Waiting/Disposed, or Executing/DisposedExecuting if we've been flagged again
			do
			{
				PreviousState = _State;

				NewState = PreviousState switch
				{
					Status.Executing => Status.Waiting,
					Status.DisposedExecuting => Status.Disposed,
					Status.Flagged => Status.Executing,
					Status.DisposedFlagged => Status.DisposedExecuting,
					_ => throw new InvalidOperationException($"Action Flag is in an unexpected state: {PreviousState}"),
				};
			}
			while (Interlocked.CompareExchange(ref _State, NewState, PreviousState) != PreviousState);

			// If we're executing, queue the callback again
			switch (NewState)
			{
			case Status.DisposedExecuting:
			case Status.Executing:
				// It's not safe to rely on OnComplete to queue us on another thread, so this makes sure we break the call stack
				ThreadPool.UnsafeQueueUserWorkItem(_ExecuteTask, null);
				break;
			}
		}

		//****************************************

		/// <summary>
		/// Gets/Sets a delay to apply before executing the callback
		/// </summary>
		/// <remarks>Acts as a cheap batching mechanism, so rapid calls to Set do not execute the callback twice</remarks>
		public TimeSpan Delay { get; } = TimeSpan.Zero;

		/// <summary>
		/// Gets/Sets whether to swallow exceptions returned by the callback, or leave them to trigger an UnhandledException
		/// </summary>
		public bool SwallowExceptions { get; set; }

		//****************************************

		private static Func<ValueTask> WrapCallback(Func<Task> callback) => () => new ValueTask(callback());

		private static Func<ValueTask> WrapCallback(Action callback) => () => { callback(); return default; };

		//****************************************

		private static class Status
		{
			public const int DisposedExecuting = -3;

			public const int DisposedFlagged = -2;

			public const int Disposed = -1;

			public const int Waiting = 0;

			public const int Flagged = 1;

			public const int Executing = 2;
		}
	}
}