using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Proximity.Threading
{
	internal sealed class TaskCancelInstance<TResult> : BaseCancellable, IValueTaskSource, IValueTaskSource<TResult>
	{ //****************************************
		private static readonly ConcurrentBag<TaskCancelInstance<TResult>> Instances = new ConcurrentBag<TaskCancelInstance<TResult>>();
		//****************************************
		private Action? _CompleteValueTask;
		private Action<Task>? _CompleteTask;

		private volatile int _InstanceState;
		private bool _HasResult;
		private bool _WaitForCompletion;

		private ManualResetValueTaskSourceCore<TResult> _TaskSource = new ManualResetValueTaskSourceCore<TResult>();

		private Task? _Task;
		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
		private ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter _ResultAwaiter;
		//****************************************

		internal TaskCancelInstance()
		{
			_TaskSource.RunContinuationsAsynchronously = true;
		}

		//****************************************

		internal void Initialise(CancellationToken token, TimeSpan timeout)
		{
			_InstanceState = Status.Pending;

			RegisterCancellation(token, timeout);
		}

		internal void Attach(Task task)
		{
			_HasResult = false;
			_Task = task;

			// We use ContinueWith for Task.
			// This results in a some allocations, but it also lets us associate the Token with the continuation
			// Thus, if we cancel, then the continuation will be REMOVED from the attached Task

			// A common pattern for Task.When is for implementing a singleton-style operation, letting callers abort waiting as needed.
			// If we can't remove continuations, each call would then leak a TaskCancelInstance, potentially indefinitely.

			// Make sure we run the continuation on the ThreadPool (equivalent to ConfigureAwait(false))
			task.ContinueWith(_CompleteTask ?? (_CompleteTask = OnCompleteTask), Token, TaskContinuationOptions.None, TaskScheduler.Default);
		}

		internal void Attach(ValueTask task)
		{
			_HasResult = false;
			_WaitForCompletion = true; // ValueTask needs to wait before it can Release, since we can't unschedule the completion
			_Task = Task.CompletedTask; // Allows cancellation to proceed

			// We use GetAwaiter for ValueTask.
			// Unlike with Task, which can have When called multiple times (and would leak on each call to GetAwaiter), we assume a ValueTask is consumed by calling When on it.

			// The common pattern for ValueTask.When is to allow a caller to give up waiting on an operation without cancelling the operation (by passing a CancellationToken to the operation itself).
			// In this scenario, since ValueTask cannot be reused, we will leak at worst a single TaskCancelInstance until the underlying operation completes - presumably a minimal cost compared to the operation itself.

			// The only way this can result in an unbounded memory leak is if ValueTask.When is called on a ValueTask that wraps an underlying Task.
			// The only way to detect this with the current ValueTask API would be with `new ValueTask(task.AsTask()) == task`, which would return true in the case of an underlying Task,
			// but would unfortunately allocate (and consume the ValueTask) in the case where it's an underlying IValueTaskSource.

			// We want to optimise for ValueTask.When causing as few allocations as possible, so we assume it's IValueTaskSource and warn the caller of the risk if given a Task.
			_Awaiter = task.ConfigureAwait(false).GetAwaiter();

			if (_Awaiter.IsCompleted)
				OnCompleteValueTask();
			else
				_Awaiter.OnCompleted(_CompleteValueTask ?? (_CompleteValueTask = OnCompleteValueTask));
		}

		internal void Attach(Task<TResult> task)
		{
			_HasResult = true;
			_Task = task;

			// Make sure we run the continuation on the ThreadPool (equivalent to ConfigureAwait(false))
			task.ContinueWith(_CompleteTask ?? (_CompleteTask = OnCompleteTask), Token, TaskContinuationOptions.None, TaskScheduler.Default);
		}

		internal void Attach(ValueTask<TResult> task)
		{
			_HasResult = true;
			_WaitForCompletion = true; // ValueTask needs to wait before it can Release, since we can't unschedule the completion
			_Task = Task.CompletedTask; // Allows cancellation to proceed

			_ResultAwaiter = task.ConfigureAwait(false).GetAwaiter();

			if (_ResultAwaiter.IsCompleted)
				OnCompleteValueTask();
			else
				_ResultAwaiter.OnCompleted(_CompleteValueTask ?? (_CompleteValueTask = OnCompleteValueTask));
		}

		//****************************************

		public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

		public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		void IValueTaskSource.GetResult(short token)
		{
			try
			{
				_TaskSource.GetResult(token);
			}
			finally
			{
				if (_InstanceState == Status.Completed || !_WaitForCompletion || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledCompleted)
					Release(); // Completed and not waiting for the callback to execute (or we don't care because it should be unregistered)
			}
		}

		TResult IValueTaskSource<TResult>.GetResult(short token)
		{
			try
			{
				return _TaskSource.GetResult(token);
			}
			finally
			{
				if (_InstanceState == Status.Completed || !_WaitForCompletion || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledCompleted)
					Release(); // Completed and not waiting for the callback to execute
			}
		}

		protected override void SwitchToCancelled()
		{
			// Ensure that any OnCompleteTask operation doesn't manipulate this Instance if it got scheduled before we cancelled
			if (Interlocked.Exchange(ref _Task, null) == null)
				return; // OnCompleteTask has already executed

			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
				return; // Instance is no longer in a cancellable state

			// The cancellation token was raised
			_TaskSource.SetException(CreateCancellationException());
		}

		protected override void UnregisteredCancellation()
		{
			try
			{
				if (_HasResult)
				{
					var Result = _ResultAwaiter.GetResult();

					_TaskSource.SetResult(Result);
				}
				else
				{
					_Awaiter.GetResult();

					_TaskSource.SetResult(default!);
				}
			}
			catch (Exception e)
			{
				_TaskSource.SetException(e);
			}
		}

		//****************************************

		private void OnCompleteValueTask()
		{
			if (Interlocked.CompareExchange(ref _InstanceState, Status.Completed, Status.Pending) == Status.Pending)
			{
				// Task completed without the cancellation token triggering
				UnregisterCancellation();

				return;
			}

			// Cancellation triggered was probably triggered, are we waiting for GetResult?
			if (_InstanceState == Status.CancelledGotResult || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledCompleted, Status.Cancelled) == Status.CancelledGotResult)
				Release(); // We're cancelled and not waiting for GetResult, so we can return to the pool
		}

		private void OnCompleteTask(Task task)
		{
			// There is a scenario where this continuation can get scheduled, then SwitchToCancelled is called, GetResult is called, we're released,
			// and then another call to When reuses us -- all before OnCompleteTask occurs. InstanceState would thus be Pending as we expect, but for a different Task.

			// So, we want to compare that the Task we're working with is the one this Instance is currently associated with.
			// When cancellation runs, it will set this to null, and then any future reuse should have a different Task.

			// This leaves the final situation where we're reused against the SAME Task as last time.
			// In that case, OnCompleteTask will run twice, but only one will proceed past this point. Since each callback has the same Task, it doesn't actually matter which wins
			if (!ReferenceEquals(Interlocked.CompareExchange(ref _Task, null, task), task))
				return;

			// Creating a ValueTask awaiter around the Task is easier than adding special handling for the Task path in UnregisteredCancellation
			if (_HasResult && task is Task<TResult> ResultTask)
				_ResultAwaiter = new ValueTask<TResult>(ResultTask).ConfigureAwait(false).GetAwaiter();
			else
				_Awaiter = new ValueTask(task).ConfigureAwait(false).GetAwaiter();

			OnCompleteValueTask();
		}

		private void Release()
		{
			_TaskSource.Reset();
			_HasResult = false;
			_WaitForCompletion = false;
			_Task = null;
			_Awaiter = default;
			_ResultAwaiter = default;
			_InstanceState = Status.Unused;
			ResetCancellation();

			Instances.Add(this);
		}

		//****************************************

		public short Version => _TaskSource.Version;

		//****************************************

		internal static TaskCancelInstance<TResult> GetOrCreate(CancellationToken token, TimeSpan timeout)
		{
			if (!Instances.TryTake(out var Instance))
				Instance = new TaskCancelInstance<TResult>();

			Instance.Initialise(token, timeout);

			return Instance;
		}

		//****************************************

		private static class Status
		{
			/// <summary>
			/// The lock was cancelled and is waiting for GetResult
			/// </summary>
			internal const int CancelledCompleted = -2;
			/// <summary>
			/// The lock was cancelled and is waiting for Completion
			/// </summary>
			internal const int CancelledGotResult = -2;
			/// <summary>
			/// The lock was cancelled and is waiting for GetResult and Completion
			/// </summary>
			internal const int Cancelled = -1;
			/// <summary>
			/// An instance starts in the Unused state
			/// </summary>
			internal const int Unused = 0;
			/// <summary>
			/// The instance is pending completion
			/// </summary>
			internal const int Pending = 1;
			/// <summary>
			/// The instance is Completed and waiting for GetResult
			/// </summary>
			internal const int Completed = 2;
		}
	}
}
