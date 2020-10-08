using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;

namespace System.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for a Manual Reset Event supporting Valuetask async/await
	/// </summary>
	public sealed class AsyncManualResetEvent : IDisposable
	{ //****************************************
		private readonly WaiterQueue<ManualResetInstance> _Waiters = new WaiterQueue<ManualResetInstance>();

		private int _State;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Manual Reset Event with the initial state Unset
		/// </summary>
		public AsyncManualResetEvent() : this(false)
		{
		}

		/// <summary>
		/// Creates a new Asynchronous Manual Reset Event
		/// </summary>
		/// <param name="isSet">The initial state</param>
		public AsyncManualResetEvent(bool isSet)
		{
			_State = isSet ? State.Set : State.Unset;
		}

		//****************************************

		/// <summary>
		/// Disposes of the event
		/// </summary>
		/// <remarks>All waits on the event will throw <see cref="ObjectDisposedException"/></remarks>
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _State, State.Disposed) != State.Disposed)
			{
				// Success, now close any pending waiters
				while (_Waiters.TryDequeue(out var Instance))
					Instance.SwitchToDisposed();
			}
		}

		/// <summary>
		/// Releases any waiters, or sets the event if there are none
		/// </summary>
		/// <exception cref="ObjectDisposedException">The event has been disposed</exception>
		public void Set()
		{
			var OldState = Interlocked.CompareExchange(ref _State, State.Set, State.Unset);

			if (OldState == State.Disposed)
				throw new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of");

			// We set the event, release any waiters
			if (OldState == State.Unset)
				ReleaseAll();
		}

		/// <summary>
		/// Resets the event if set
		/// </summary>
		/// <exception cref="ObjectDisposedException">The event has been disposed</exception>
		public void Reset()
		{
			if (Interlocked.CompareExchange(ref _State, State.Unset, State.Set) == State.Disposed)
				throw new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of");
		}

		/// <summary>
		/// Waits for the event to become set, or if set return immediately
		/// </summary>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that completes when the event has been set</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask Wait(CancellationToken token = default) => Wait(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Waits for the event to become set, or if set return immediately
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the event to be set</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that completes when the event has been set</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask Wait(TimeSpan timeout, CancellationToken token = default)
		{
			if (GetIsSet())
				return default;

			if (_State == State.Disposed)
				throw new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of");

			var Instance = ManualResetInstance.GetOrCreate(this, GetIsSet());

			var ValueTask = new ValueTask(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				Instance.ApplyCancellation(token, timeout);

				// Not set, add ourselves to the queue waiting
				_Waiters.Enqueue(Instance);

				// Did the event become set while we were busy?
				if (GetIsSet())
					ReleaseAll();
			}

			return ValueTask;
		}

		/// <summary>
		/// Checks if the event is set without waiting
		/// </summary>
		/// <returns>True if the event is set, otherwise false</returns>
		public bool TryWait() => GetIsSet();

		/// <summary>
		/// Blocks waiting for the event
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the event</param>
		/// <returns>True if the event was set, otherwise False due to disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryWait(CancellationToken token) => TryWait(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks waiting for the event
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for the event to be set. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the event</param>
		/// <returns>True if the event was set, otherwise False due to timeout or disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryWait(TimeSpan timeout, CancellationToken token = default)
		{
			// First up, try and reset the event if possible
			if (GetIsSet())
				return true;

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero && !token.CanBeCanceled)
				return false;

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			var Instance = ManualResetInstance.GetOrCreate(this, false);

			_Waiters.Enqueue(Instance);

			// Was the event set while we were busy?
			if (GetIsSet())
				ReleaseAll();

			Instance.ApplyCancellation(token, timeout);

			//****************************************

			try
			{
				lock (Instance)
				{
					// Queue this inside the lock, so Pulse cannot execute until we've reached Wait
					Instance.OnCompleted((state) => { lock (state!) Monitor.Pulse(state); }, Instance, Instance.Version, ValueTaskSourceOnCompletedFlags.None);

					// Wait for a definitive result
					Monitor.Wait(Instance);
				}

				// Result retrieved, now check if we were cancelled
				Instance.GetResult(Instance.Version);

				return true;
			}
			catch
			{
				// We may get a cancel or dispose exception
				if (!token.IsCancellationRequested)
					return false; // If it's not the original token, we cancelled due to a Timeout. Return false

				throw; // Throw the cancellation
			}
		}

		//****************************************

		private bool GetIsSet() => _State == State.Set;

		private void ReleaseAll()
		{
			while (_Waiters.TryDequeue(out var Instance))
				Instance.SwitchToCompleted();
		}

		//****************************************

		/// <summary>
		/// Gets whether the event is currently set
		/// </summary>
		public bool IsSet => _State == State.Set;

		/// <summary>
		/// Gets the approximate number of waiters on the event
		/// </summary>
		public int WaitingCount => _Waiters.Count;

		//****************************************

		private sealed class ManualResetInstance : BaseCancellable, IValueTaskSource
		{ //****************************************
			private static readonly ConcurrentBag<ManualResetInstance> Instances = new ConcurrentBag<ManualResetInstance>();
			//****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<bool> _TaskSource = new ManualResetValueTaskSourceCore<bool>();
			//****************************************

			internal ManualResetInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			//****************************************

			internal void Initialise(AsyncManualResetEvent owner, bool isSet)
			{
				Owner = owner;

				if (isSet)
				{
					_InstanceState = Status.Set;
					_TaskSource.SetResult(true);
				}
				else
				{
					_InstanceState = Status.Pending;
				}
			}

			internal void ApplyCancellation(CancellationToken token, TimeSpan timeout)
			{
				if (_InstanceState != Status.Pending)
					throw new InvalidOperationException("Cannot register for cancellation when not pending");

				RegisterCancellation(token, timeout);
			}

			internal void SwitchToDisposed()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				UnregisterCancellation();
			}

			internal void SwitchToCompleted()
			{
				// Try and assign the counter to this Instance
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Set, Status.Pending) == Status.Pending)
				{
					UnregisterCancellation();

					return;
				}

				// Assignment failed, so it was cancelled
				// If the result has already been retrieved, return the Instance to the pool
				int InstanceState, NewInstanceState;

				do
				{
					InstanceState = _InstanceState;

					switch (InstanceState)
					{
					case Status.Cancelled:
						NewInstanceState = Status.CancelledNotWaiting;
						break;

					case Status.CancelledGotResult:
						NewInstanceState = Status.Unused;
						break;

					default:
						return;
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				if (InstanceState == Status.CancelledGotResult)
					Release(); // GetResult has been called, so we can return to the pool
			}

			//****************************************

			protected override void SwitchToCancelled()
			{
				if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				Owner!._Waiters.Erase(this);

				// The cancellation token was raised
				_TaskSource.SetException(CreateCancellationException());
			}

			protected override void UnregisteredCancellation()
			{
				switch (_InstanceState)
				{
				case Status.Disposed:
					_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of"));
					break;

				case Status.Set:
					_TaskSource.SetResult(default);
					break;
				}
			}

			ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public void GetResult(short token)
			{
				try
				{
					_TaskSource.GetResult(token);
				}
				finally
				{
					if (_InstanceState == Status.CancelledNotWaiting || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
						Release(); // We're cancelled and no longer on the Wait queue, so we can return to the pool
				}
			}

			//****************************************

			private void Release()
			{
				Owner = null;

				_TaskSource.Reset();
				_InstanceState = Status.Unused;
				ResetCancellation();

				Instances.Add(this);
			}

			//****************************************

			public AsyncManualResetEvent? Owner { get; private set; }

			public bool IsPending => _InstanceState == Status.Pending;

			public short Version => _TaskSource.Version;

			//****************************************

			internal static ManualResetInstance GetOrCreate(AsyncManualResetEvent owner, bool isSet)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new ManualResetInstance();

				Instance.Initialise(owner, isSet);

				return Instance;
			}

			private static class Status
			{
				/// <summary>
				/// The wait was cancelled and is currently on the list of waiters
				/// </summary>
				internal const int CancelledGotResult = -4;
				/// <summary>
				/// The wait is cancelled and waiting for GetResult
				/// </summary>
				internal const int CancelledNotWaiting = -3;
				/// <summary>
				/// The wait was cancelled and is currently on the list of waiters while waiting for GetResult
				/// </summary>
				internal const int Cancelled = -2;
				/// <summary>
				/// The wait was disposed of
				/// </summary>
				internal const int Disposed = -1;
				/// <summary>
				/// A wait starts in the Unused state
				/// </summary>
				internal const int Unused = 0;
				/// <summary>
				/// The wait is currently activated, and waiting for GetResult
				/// </summary>
				internal const int Set = 1;
				/// <summary>
				/// The wait is on the list of waiters
				/// </summary>
				internal const int Pending = 2;
			}
		}

		private static class State
		{
			public const int Disposed = -1;
			public const int Unset = 0;
			public const int Set = 1;
		}
	}
}
