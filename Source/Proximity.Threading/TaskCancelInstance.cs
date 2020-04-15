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
		private readonly Action _CompleteTask;

		private volatile int _InstanceState;
		private bool _HasResult;

		private ManualResetValueTaskSourceCore<TResult> _TaskSource = new ManualResetValueTaskSourceCore<TResult>();

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
		private ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter _ResultAwaiter;
		//****************************************

		internal TaskCancelInstance()
		{
			_TaskSource.RunContinuationsAsynchronously = true;
			_CompleteTask = OnCompleteTask;
		}

		//****************************************

		internal void Initialise(ValueTask task)
		{
			_HasResult = false;
			_InstanceState = Status.Pending;
			_Awaiter = task.ConfigureAwait(false).GetAwaiter();

			if (_Awaiter.IsCompleted)
				OnCompleteTask();
			else
				_Awaiter.OnCompleted(_CompleteTask);
		}

		internal void Initialise(ValueTask<TResult> task)
		{
			_HasResult = true;
			_InstanceState = Status.Pending;
			_ResultAwaiter = task.ConfigureAwait(false).GetAwaiter();

			if (_ResultAwaiter.IsCompleted)
				OnCompleteTask();
			else
				_ResultAwaiter.OnCompleted(_CompleteTask);
		}

		internal void ApplyCancellation(CancellationToken token, TimeSpan timeout)
		{
			if (_InstanceState != Status.Pending)
				throw new InvalidOperationException("Cannot register for cancellation when not pending");

			RegisterCancellation(token, timeout);
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
				if (_InstanceState == Status.CancelledCompleted || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledCompleted)
					Release(); // We're cancelled and not waiting for completion, so we can return to the pool
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
				Release();
			}
		}

		protected override void SwitchToCancelled()
		{
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
					_TaskSource.SetResult(_ResultAwaiter.GetResult());
				else
					_TaskSource.SetResult(default!);
			}
			catch (Exception e)
			{
				_TaskSource.SetException(e);
			}
		}

		//****************************************

		private void OnCompleteTask()
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

		private void Release()
		{
			_TaskSource.Reset();
			_HasResult = false;
			_Awaiter = default;
			_ResultAwaiter = default;
			_InstanceState = Status.Unused;
			ResetCancellation();

			Instances.Add(this);
		}

		//****************************************

		public short Version => _TaskSource.Version;

		//****************************************

		internal static TaskCancelInstance<TResult> GetOrCreate(ValueTask task)
		{
			if (!Instances.TryTake(out var Instance))
				Instance = new TaskCancelInstance<TResult>();

			Instance.Initialise(task);

			return Instance;
		}

		internal static TaskCancelInstance<TResult> GetOrCreate(ValueTask<TResult> task)
		{
			if (!Instances.TryTake(out var Instance))
				Instance = new TaskCancelInstance<TResult>();

			Instance.Initialise(task);

			return Instance;
		}

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
