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
	/// Provides a lock-free primitive for an Auto Reset Event supporting ValueTask async/await
	/// </summary>
	public sealed class AsyncAutoResetEvent : IDisposable
	{ //****************************************
		private readonly WaiterQueue<AutoResetInstance> _Waiters = new();

		private int _State;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Auto Reset Event with the initial state Unset
		/// </summary>
		public AsyncAutoResetEvent() : this(false)
		{
		}

		/// <summary>
		/// Creates a new Asynchronous Auto Reset Event
		/// </summary>
		/// <param name="isSet">The initial state</param>
		public AsyncAutoResetEvent(bool isSet)
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
			var FoundWaiter = false;

			// If there are waiters, release them and don't set the event
			while (_Waiters.TryDequeue(out var Instance))
			{
				Instance.SwitchToCompleted();

				FoundWaiter = true;
			}

			if (FoundWaiter)
				return;

			// No waiters were found, so set the event
			if (Interlocked.CompareExchange(ref _State, State.Set, State.Unset) == State.Disposed)
				throw new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of");
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
		/// Waits for the event to become set, or unsets it and returns immediately
		/// </summary>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that returns true when the event has been set</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="ObjectDisposedException">The event has been disposed</exception>
		public ValueTask<bool> Wait(CancellationToken token = default) => Wait(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Waits for the event to become set, or unsets it and returns immediately
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the event to be set</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that returns true when the event has been set, false if a timeout occurred</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="ObjectDisposedException">The event has been disposed</exception>
		public ValueTask<bool> Wait(TimeSpan timeout, CancellationToken token = default)
		{
			if (TryReset())
				return new ValueTask<bool>(true);

			if (_State == State.Disposed)
				throw new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of");

			var Instance = AutoResetInstance.GetOrCreate(this, token, timeout);

			var ValueTask = new ValueTask<bool>(Instance, Instance.Version);

			_Waiters.Enqueue(Instance);

			if (TryReset())
				ReleaseAll();

			return ValueTask;
		}

		/// <summary>
		/// Tries to reset the event without waiting
		/// </summary>
		/// <returns>True if the event was set and reset, otherwise false</returns>
		/// <exception cref="ObjectDisposedException">The event has been disposed</exception>
		public bool TryWait()
		{
			if (TryReset())
				return true;

			if (_State == State.Disposed)
				throw new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of");

			return false;
		}

		/// <summary>
		/// Blocks waiting for the event
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the event</param>
		/// <returns>True if the event was set</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="ObjectDisposedException">The event has been disposed</exception>
		public bool TryWait(CancellationToken token) => TryWait(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks waiting for the event
		/// </summary>
		/// <param name="timeout">Number of milliseconds to block for the event to be set. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the event</param>
		/// <returns>True if the event was set, otherwise False due to timeout</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="ObjectDisposedException">The event has been disposed</exception>
		public bool TryWait(TimeSpan timeout, CancellationToken token = default)
		{
			// First up, try and reset the event if possible
			if (TryReset())
				return true;

			if (_State == State.Disposed)
				throw new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of");

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero && !token.CanBeCanceled)
				return false;

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			var Instance = AutoResetInstance.GetOrCreate(this, token, timeout);

			_Waiters.Enqueue(Instance);

			// Was the event set while we were busy?
			if (TryReset())
				ReleaseAll();

			//****************************************

			lock (Instance)
			{
				// Queue this inside the lock, so Pulse cannot execute until we've reached Wait
				Instance.OnCompleted(PulseCompleted, Instance, Instance.Version, ValueTaskSourceOnCompletedFlags.None);

				// Wait for a definitive result
				Monitor.Wait(Instance);
			}

			// Result retrieved, now check if we were cancelled
			return Instance.GetResult(Instance.Version);
		}

		//****************************************

		private bool TryReset() => Interlocked.CompareExchange(ref _State, State.Unset, State.Set) == State.Set;

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

		private static void PulseCompleted(object? state)
		{
			lock (state!)
			{
				Monitor.Pulse(state);
			}
		}

		//****************************************

		private sealed class AutoResetInstance : BaseCancellable, IValueTaskSource<bool>
		{ //****************************************
			private static readonly ConcurrentBag<AutoResetInstance> Instances = new();
			//****************************************
			private int _InstanceState;

			private ManualResetValueTaskSourceCore<bool> _TaskSource = new();
			//****************************************

			internal AutoResetInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			//****************************************

			internal void Initialise(AsyncAutoResetEvent owner, CancellationToken token, TimeSpan timeout)
			{
				Owner = owner;

				Interlocked.Exchange(ref _InstanceState, Status.Pending);

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
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Held, Status.Pending) == Status.Pending)
				{
					UnregisterCancellation();

					return;
				}

				// Assignment failed, so it was cancelled
				// If the result has already been retrieved, return the Instance to the pool
				int InstanceState, NewInstanceState;

				do
				{
					InstanceState = Volatile.Read(ref _InstanceState);

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
				if (Volatile.Read(ref _InstanceState) != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				Owner!._Waiters.Erase(this);

				// The cancellation token was raised, or we timed out
				if (TryCreateCancellationException(out var Exception))
					_TaskSource.SetException(Exception);
				else
					_TaskSource.SetResult(false);
			}

			protected override void UnregisteredCancellation()
			{
				switch (Volatile.Read(ref _InstanceState))
				{
				case Status.Disposed:
					_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncAutoResetEvent), "Event has been disposed of"));
					break;

				case Status.Held:
					_TaskSource.SetResult(true);
					break;
				}
			}

			ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public bool GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					if (Volatile.Read(ref _InstanceState) == Status.CancelledNotWaiting || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
						Release(); // We're cancelled and no longer on the Wait queue, so we can return to the pool
				}
			}

			//****************************************

			private void Release()
			{
				Owner = null;

				_TaskSource.Reset();
				Interlocked.Exchange(ref _InstanceState, Status.Unused);
				ResetCancellation();

				if (Instances.Count < MaxInstanceCache)
					Instances.Add(this);
			}

			//****************************************

			public AsyncAutoResetEvent? Owner { get; private set; }

			public bool IsPending => Volatile.Read(ref _InstanceState) == Status.Pending;

			public short Version => _TaskSource.Version;

			//****************************************

			internal static AutoResetInstance GetOrCreate(AsyncAutoResetEvent owner, CancellationToken token, TimeSpan timeout)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new AutoResetInstance();

				Instance.Initialise(owner, token, timeout);

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
				internal const int Held = 1;
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
