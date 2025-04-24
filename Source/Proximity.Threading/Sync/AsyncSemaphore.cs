using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for semaphores supporting ValueTask async/await and a disposable model for releasing
	/// </summary>
	public sealed partial class AsyncSemaphore : IDisposable, IAsyncDisposable
	{ //****************************************
		private readonly WaiterQueue<SemaphoreInstance> _Waiters = new();
		private readonly WaiterQueue<SemaphoreWaitInstance> _PulseWaiters = new();
		private int _MaxCount, _CurrentCount;

		private LockDisposer? _Disposer;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphore() : this(1)
		{
		}

		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphore(int initialCount)
		{
			if (initialCount < 1)
				throw new ArgumentException("Initial Count is invalid", nameof(initialCount));

			_CurrentCount = 0;
			_MaxCount = initialCount;
		}

		//****************************************

		/// <summary>
		/// Disposes of the semaphore
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public ValueTask DisposeAsync()
		{
			if (_Disposer == null && Interlocked.CompareExchange(ref _Disposer, new LockDisposer(), null) == null)
			{
				// Success, now close any pending waiters
				while (_Waiters.TryDequeue(out var Instance))
					Instance.SwitchToDisposed();

				// If there's no counters, we can complete the dispose task
				if (_CurrentCount == 0)
					_Disposer.SwitchToComplete();
			}

			return new ValueTask(_Disposer.Task);
		}

		void IDisposable.Dispose() => DisposeAsync();

		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<Instance> Take(CancellationToken token = default) => Take(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait for a counter</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<Instance> Take(TimeSpan timeout, CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncSemaphore), "Semaphore has been disposed of");

			// Try and add a counter as long as nobody is waiting on it
			var Instance = SemaphoreInstance.GetOrCreate(this, _Waiters.IsEmpty && TryIncrement());

			var ValueTask = new ValueTask<Instance>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				Instance.ApplyCancellation(token, timeout);

				// No free counters, add ourselves to the queue waiting on a counter
				_Waiters.Enqueue(Instance);

				// Did a counter become available while we were busy?
				if (TryIncrement())
					Decrement();
			}

			return ValueTask;
		}

		/// <summary>
		/// Tries to take a counter without waiting
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <returns>True if the counter was taken without waiting, otherwise False</returns>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out Instance handle)
		{
			// Are we disposed?
			if (_Disposer == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = new Instance(SemaphoreInstance.GetOrCreate(this, true));
				return true;
			}

			handle = default!;

			return false;
		}

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on a counter</param>
		/// <returns>True if the counter was taken without waiting, otherwise False due to disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out Instance handle, CancellationToken token) => TryTake(out handle!, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was taken, otherwise False due to timeout or disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out Instance handle, TimeSpan timeout, CancellationToken token = default)
		{
			// First up, try and take the counter if possible
			if (_Disposer == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = new Instance(SemaphoreInstance.GetOrCreate(this, true));
				return true;
			}

			handle = default!;

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero && !token.CanBeCanceled)
				return false;

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			var Instance = SemaphoreInstance.GetOrCreate(this, false);

			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy?
			if (TryIncrement())
				Decrement(); // Try and activate a waiter, or at least increment

			//****************************************

			Instance.ApplyCancellation(token, timeout);

			try
			{
				lock (Instance)
				{
					// Queue this inside the lock, so Pulse cannot execute until we've reached Wait
					Instance.OnCompleted(static (state) => { lock (state!) Monitor.Pulse(state); }, Instance, Instance.Version, ValueTaskSourceOnCompletedFlags.None);

					// Wait for a definitive result
					Monitor.Wait(Instance);
				}

				// Result retrieved, now check if we were cancelled
				handle = Instance.GetResult(Instance.Version);

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

		/// <summary>
		/// Pulses the semaphore, triggering the first waiter (if any)
		/// </summary>
		public void Pulse()
		{
			while (_PulseWaiters.TryDequeue(out var Waiter))
			{
				if (Waiter.TrySwitchToCompleted())
					return;
			}
		}

		/// <summary>
		/// Pulses the semaphore, triggering all waiters (if any)
		/// </summary>
		public void PulseAll()
		{
			while (_PulseWaiters.TryDequeue(out var Waiter))
				Waiter.TrySwitchToCompleted();
		}

		//****************************************

		private ValueTask<bool> Wait(SemaphoreInstance instance, CancellationToken token, TimeSpan timeout)
		{
			// TODO: Pulse/Wait

			return new(true);
		}

		private bool TryIncrement()
		{ //****************************************
			var MyWait = new SpinWait();
			//****************************************

			for (; ; )
			{
				var OldCount = Volatile.Read(ref _CurrentCount);

				// Are there any free counters?
				if (OldCount >= _MaxCount)
					return false;

				// Free counter, attempt to take it
				if (Interlocked.CompareExchange(ref _CurrentCount, OldCount + 1, OldCount) == OldCount)
					return true;

				// Failed. Spin and try again
				MyWait.SpinOnce();
			}
		}

		private void Decrement()
		{ //****************************************
			int NewCount;
			//****************************************

			do
			{
				// Try and retrieve a waiter while we're not disposed
				while (_Waiters.TryDequeue(out var NextInstance))
				{
					// Try and assign the counter to this Instance
					if (NextInstance.TrySwitchToCompleted())
						return;
				}

				// Release a counter
				NewCount = Interlocked.Decrement(ref _CurrentCount);

				// If someone is waiting, try to put the counter back, and try again. If it fails, someone else can take care of the waiter
			} while (!_Waiters.IsEmpty && TryIncrement());

			// If we've disposed and there's no counters, we can complete the dispose task
			if (_Disposer != null && NewCount == 0)
				_Disposer.SwitchToComplete();
		}

		//****************************************

		/// <summary>
		/// Gets the number of counters available for taking
		/// </summary>
		public int CurrentCount => _MaxCount - _CurrentCount;

		/// <summary>
		/// Gets the approximate number of operations waiting for a counter
		/// </summary>
		public int WaitingCount => _Waiters.Count;

		/// <summary>
		/// Gets/Sets the maximum number of operations
		/// </summary>
		public int MaxCount
		{
			get => _MaxCount;
			set
			{
				if (_MaxCount == value)
					return;

				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (_Disposer != null)
					throw new ObjectDisposedException(nameof(AsyncSemaphore), "Object has been disposed");

				_MaxCount = value;

				// If there are waiters, try and assign a counter if possible
				if (!_Waiters.IsEmpty && TryIncrement())
					Decrement();
			}
		}

		//****************************************

		/// <summary>
		/// Represents the semaphore counter currently held
		/// </summary>
		public readonly struct Instance : IDisposable
		{ //****************************************
			private readonly SemaphoreInstance _Instance;
			private readonly short _Token;
			//****************************************

			internal Instance(SemaphoreInstance instance)
			{
				_Instance = instance;
				_Token = instance.Version;
			}

			//****************************************
			/*
			/// <summary>
			/// Releases the counter, and waits for a Pulse before reacquiring
			/// </summary>
			/// <param name="token">A cancellation token to abort waiting</param>
			/// <returns>A Task that returns true when the Semaphore has been pulsed</returns>
			/// <remarks>Always reacquires the counter before completing, even in the event of cancellation</remarks>
			/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
			public ValueTask<bool> Wait(CancellationToken token = default) => _Instance.Wait(_Token, token, Timeout.InfiniteTimeSpan);

			/// <summary>
			/// Releases the counter, and waits for a Pulse before reacquiring
			/// </summary>
			/// <param name="timeout">The amount of time to wait for the pulse</param>
			/// <param name="token">A cancellation token to abort waiting</param>
			/// <returns>A Task that returns true when the Semaphore has been pulsed, False when the timeout has elapsed</returns>
			/// <remarks>Always reacquires the counter before completing, even in the event of cancellation</remarks>
			/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
			public ValueTask<bool> Wait(TimeSpan timeout, CancellationToken token = default) => _Instance.Wait(_Token, token, timeout);
			*/
			/// <summary>
			/// Releases the lock currently held
			/// </summary>
			public void Dispose() => _Instance.Release(_Token);
		}
	}
}
