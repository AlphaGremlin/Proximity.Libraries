using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Threading.Tasks
{
	internal sealed class ValueTaskCompletionInstance<TResult> : BaseCancellable, IValueTaskSource, IValueTaskSource<TResult>
	{ //****************************************
		private static readonly ConcurrentBag<ValueTaskCompletionInstance<TResult>> Instances = new();
		//****************************************
		private ManualResetValueTaskSourceCore<TResult> _TaskSource = new();

		private volatile int _InstanceState;

		private TResult _PendingResult = default!;
		private Exception? _PendingException = null;
		//****************************************

		internal void TryDispose(short token)
		{
			if (_TaskSource.Version != token)
				throw new InvalidOperationException("Instance has already been disposed");

			var PreviousState = Interlocked.Exchange(ref _InstanceState, Status.Disposed);

			switch (PreviousState)
			{
			case Status.Disposed:
				throw new InvalidOperationException("Instance has already been disposed");

			case Status.Cancelled:
			case Status.SetException:
			case Status.SetResult:
				return; // We're waiting on GetResult, which will release our Instance now that we're Disposed

			case Status.Pending:
				_PendingException = new ObjectDisposedException(nameof(ValueTaskCompletionSource), "Instance was disposed without setting a result");

				UnregisterCancellation();
				return; // GetResult will release our Instance once we're Disposed

			case Status.GotResult:
				Release(); // Await has completed, so we can Release safely
				return;

			default:
				throw new InvalidOperationException($"Instance was in an invalid state: {PreviousState}");
			}
		}

		internal bool TrySetResult(TResult result, short token)
		{
			if (_TaskSource.Version != token)
				return false; // Instance has already been disposed

			if (Interlocked.CompareExchange(ref _InstanceState, Status.SetResult, Status.Pending) != Status.Pending)
				return false; // Result has already been set

			_PendingResult = result;

			UnregisterCancellation();

			return true;
		}

		internal bool TrySetException(Exception exception, short token)
		{
			if (_TaskSource.Version != token)
				return false; // Instance has already been disposed

			if (exception is null)
				throw new ArgumentNullException(nameof(exception));

			if (Interlocked.CompareExchange(ref _InstanceState, Status.SetException, Status.Pending) != Status.Pending)
				return false; // Result has already been set

			_PendingException = exception;

			UnregisterCancellation();

			return true;
		}

		internal bool IsCompleted(short token)
		{
			if (_TaskSource.Version != token)
				return true;

			return _InstanceState == Status.Pending;
		}

		public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

		public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		public TResult GetResult(short token)
		{
			try
			{
				return _TaskSource.GetResult(token);
			}
			finally
			{
				int InstanceState;

				do
				{
					InstanceState = _InstanceState;

					if (InstanceState == Status.Disposed)
						break;
				}
				while (Interlocked.CompareExchange(ref _InstanceState, Status.GotResult, InstanceState) != InstanceState);

				if (InstanceState == Status.Disposed)
					Release(); // We're disposed, and we've awaited, so we can release the Instance
			}
		}

		void IValueTaskSource.GetResult(short token) => GetResult(token);

		//****************************************

		protected override void UnregisteredCancellation()
		{
			switch (_InstanceState)
			{
			case Status.SetResult:
				_TaskSource.SetResult(_PendingResult);
				break;

			case Status.SetException:
				_TaskSource.SetException(_PendingException!);
				break;

			case Status.Disposed:
				_TaskSource.SetException(_PendingException!);
				break;
			}
		}

		protected override void SwitchToCancelled()
		{
			if (Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
				return; // Result has already been set

			_TaskSource.SetException(CreateCancellationException());
		}

		//****************************************

		private void Initialise(bool runContinuationsAsynchronously, CancellationToken token)
		{
			_TaskSource.RunContinuationsAsynchronously = runContinuationsAsynchronously;

			_InstanceState = Status.Pending;

			RegisterCancellation(token, Timeout.InfiniteTimeSpan);
		}

		private void Release()
		{
			_TaskSource.Reset();
			_InstanceState = Status.Unused;
			ResetCancellation();

			Instances.Add(this);
		}

		//****************************************

		public short Version => _TaskSource.Version;

		//****************************************

		internal static ValueTaskCompletionInstance<TResult> GetOrCreate(bool runContinuationsAsynchronously, CancellationToken token)
		{
			if (!Instances.TryTake(out var NewInstance))
				NewInstance = new ValueTaskCompletionInstance<TResult>();

			NewInstance.Initialise(runContinuationsAsynchronously, token);

			return NewInstance;
		}

		//****************************************

		private static class Status
		{
			/// <summary>
			/// The instance was disposed while waiting for a result
			/// </summary>
			internal const int Disposed = -2;
			/// <summary>
			/// The instance was cancelled
			/// </summary>
			internal const int Cancelled = -1;
			/// <summary>
			/// The instance was disposed of
			/// </summary>
			internal const int Unused = 0;
			/// <summary>
			/// An instance is waiting for a result
			/// </summary>
			internal const int Pending = 1;
			/// <summary>
			/// The instance value has been set
			/// </summary>
			internal const int SetResult = 2;
			/// <summary>
			/// The instance exception has been set
			/// </summary>
			internal const int SetException = 3;
			/// <summary>
			/// The instance result has been retrieved
			/// </summary>
			internal const int GotResult = 4;
		}
	}
}
