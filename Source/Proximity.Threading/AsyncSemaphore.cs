using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for semaphores supporting ValueTask async/await and a disposable model for releasing
	/// </summary>
	public sealed class AsyncSemaphore : IDisposable, IAsyncDisposable
	{ //****************************************
		private static readonly ConcurrentBag<SemaphoreInstance> _Instances = new ConcurrentBag<SemaphoreInstance>();
		//****************************************
		private readonly WaiterQueue<SemaphoreInstance> _Waiters = new WaiterQueue<SemaphoreInstance>();
		private int _MaxCount, _CurrentCount;

		private AsyncSemaphoreDisposer? _Disposer;
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
			if (_Disposer != null || Interlocked.CompareExchange(ref _Disposer, new AsyncSemaphoreDisposer(), null) != null)
				return default;

			// Success, now close any pending waiters
			while (_Waiters.TryDequeue(out var Instance))
				Instance.SwitchToDisposed();

			// If there's no counters, we can complete the dispose task
			if (_CurrentCount == 0)
				_Disposer.SwitchToComplete();

			return new ValueTask(_Disposer, _Disposer.Token);
		}

		void IDisposable.Dispose() => DisposeAsync();

		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public ValueTask<IDisposable> Take() => Take(CancellationToken.None);

		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait for a counters</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public ValueTask<IDisposable> Take(TimeSpan timeout)
		{
			// Are we disposed?
			if (_Disposer != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException(nameof(AsyncSemaphore), "Semaphore has been disposed of")).AsValueTask();

			// Try and add a counter as long as nobody is waiting on it
			var Instance = GetOrCreateInstance(_Waiters.IsEmpty && TryIncrement());

			var ValueTask = new ValueTask<IDisposable>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				var CancelSource = new CancellationTokenSource(timeout);

				Instance.ApplyCancellation(CancelSource.Token, CancelSource);

				// No free counters, add ourselves to the queue waiting on a counter
				_Waiters.Enqueue(Instance);

				// Did a counter become available while we were busy?
				if (TryIncrement())
					Decrement();
			}

			return ValueTask;
		}

		/// <summary>
			/// Attempts to take a counter
			/// </summary>
			/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
			/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public ValueTask<IDisposable> Take(CancellationToken token)
		{
			// Are we disposed?
			if (_Disposer != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException(nameof(AsyncSemaphore), "Semaphore has been disposed of")).AsValueTask();

			// Try and add a counter as long as nobody is waiting on it
			var Instance = GetOrCreateInstance(_Waiters.IsEmpty && TryIncrement());

			var ValueTask = new ValueTask<IDisposable>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				Instance.ApplyCancellation(token, null);

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
			out IDisposable handle)
		{
			// Are we disposed?
			if (_Disposer == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = GetOrCreateInstance(true);
				return true;
			}

			handle = null!;

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
			out IDisposable handle, CancellationToken token) => TryTake(out handle!, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <returns>True if the counter was taken, otherwise False due to timeout or disposal</returns>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out IDisposable handle, TimeSpan timeout) => TryTake(out handle!, timeout, CancellationToken.None);

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was taken, otherwise False due to timeout or disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryTake(
#if !NETSTANDARD2_0
			[MaybeNullWhen(false)]
#endif
			out IDisposable handle, TimeSpan timeout, CancellationToken token)
		{
			// First up, try and take the counter if possible
			if (_Disposer == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = GetOrCreateInstance(true);
				return true;
			}

			handle = null!;

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero)
				return false;

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

			// We're okay with blocking, so create our waiter and add it to the queue
			var Instance = GetOrCreateInstance(false);

			_Waiters.Enqueue(Instance);

			// Was a counter added while we were busy?
			if (TryIncrement())
				Decrement(); // Try and activate a waiter, or at least increment

			//****************************************

			// Are we okay with blocking indefinitely (or until the token is raised)?
			if (timeout != Timeout.InfiniteTimeSpan)
			{
				var TokenSource = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token) : new CancellationTokenSource();

				TokenSource.CancelAfter(timeout);

				// Prepare the cancellation token
				Instance.ApplyCancellation(TokenSource.Token, TokenSource);
			}
			else
			{
				// Prepare the cancellation token (if any)
				Instance.ApplyCancellation(token, null);
			}

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

		//****************************************

		private SemaphoreInstance GetOrCreateInstance(bool isTaken)
		{
			if (!_Instances.TryTake(out var Instance))
				Instance = new SemaphoreInstance();

			Instance.Initialise(this, isTaken);

			return Instance;
		}

		private bool TryIncrement()
		{ //****************************************
			var MyWait = new SpinWait();
			//****************************************

			for (; ; )
			{
				int OldCount = _CurrentCount;

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

		private sealed class SemaphoreInstance : IDisposable, IValueTaskSource<IDisposable>
		{ //****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<IDisposable> _TaskSource = new ManualResetValueTaskSourceCore<IDisposable>();

			private CancellationTokenRegistration _Registration;
			private CancellationTokenSource? _TokenSource;
			//****************************************

			internal SemaphoreInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			~SemaphoreInstance()
			{
				if (_InstanceState != Status.Unused)
				{
					// TODO: Lock instance was garbage collected without being released
				}
			}

			//****************************************

			internal void Initialise(AsyncSemaphore owner, bool isHeld)
			{
				Owner = owner;

				GC.ReRegisterForFinalize(this);

				if (isHeld)
				{
					_InstanceState = Status.Held;
					_TaskSource.SetResult(this);
				}
				else
				{
					_InstanceState = Status.Pending;
				}
			}

			internal void ApplyCancellation(CancellationToken token, CancellationTokenSource? tokenSource)
			{
				if (token.CanBeCanceled)
				{
					Token = token;

					if (_InstanceState != Status.Pending)
						throw new InvalidOperationException("Cannot register for cancellation when not pending");

					_TokenSource = tokenSource;

					_Registration = token.Register((state) => ((SemaphoreInstance)state).SwitchToCancelled(), this);
				}
			}

			internal void SwitchToDisposed()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				_Registration.Dispose();
				_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncSemaphore), "Semaphore has been disposed of"));
			}

			internal bool TrySwitchToCompleted()
			{
				// Try and assign the counter to this Instance
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Held, Status.Pending) == Status.Pending)
				{
					_Registration.Dispose();
					_TaskSource.SetResult(this);

					return true;
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
						_Registration.Dispose();

						return false;
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				_Registration.Dispose();

				if (InstanceState == Status.CancelledGotResult)
					Release(); // GetResult has been called, so we can return to the pool

				return false;
			}

			//****************************************

			void IDisposable.Dispose()
			{
				if (Interlocked.Exchange(ref _InstanceState, Status.Unused) != Status.Held)
					return;

				// Release the counter and then return to the pool
				Owner!.Decrement();
				Release();
			}

			ValueTaskSourceStatus IValueTaskSource<IDisposable>.GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public IDisposable GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					if (_InstanceState == Status.CancelledNotWaiting || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
						Release(); // We're cancelled and no longer on the Wait queue, so we can return to the pool
				}
			}

			//****************************************

			private void SwitchToCancelled()
			{
				if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				_TaskSource.SetException(new OperationCanceledException(Token));
			}

			private void Release()
			{
				Owner = null;
				Token = default;
				_TaskSource.Reset();
				_TokenSource?.Dispose();
				_TokenSource = null;
				_InstanceState = Status.Unused;

				GC.SuppressFinalize(this);
				_Instances.Add(this);
			}

			//****************************************

			public AsyncSemaphore? Owner { get; private set; }

			public bool IsPending => _InstanceState == Status.Pending;

			public CancellationToken Token { get; private set; }

			public short Version => _TaskSource.Version;

			//****************************************

			private static class Status
			{
				/// <summary>
				/// The lock was cancelled and is currently on the list of waiters
				/// </summary>
				internal const int CancelledGotResult = -4;
				/// <summary>
				/// The lock is cancelled and waiting for GetResult
				/// </summary>
				internal const int CancelledNotWaiting = -3;
				/// <summary>
				/// The lock was cancelled and is currently on the list of waiters while waiting for GetResult
				/// </summary>
				internal const int Cancelled = -2;
				/// <summary>
				/// The lock was disposed of
				/// </summary>
				internal const int Disposed = -1;
				/// <summary>
				/// A lock starts in the Unused state
				/// </summary>
				internal const int Unused = 0;
				/// <summary>
				/// The lock is currently held, and waiting for GetResult and then Dispose
				/// </summary>
				internal const int Held = 1;
				/// <summary>
				/// The lock is on the list of waiters
				/// </summary>
				internal const int Pending = 2;
			}
		}

		private sealed class AsyncSemaphoreDisposer : IValueTaskSource
		{ //****************************************
			private ManualResetValueTaskSourceCore<VoidStruct> _TaskSource = new ManualResetValueTaskSourceCore<VoidStruct>();

			private int _IsDisposed;
			//****************************************

			public void SwitchToComplete()
			{
				if (Interlocked.Exchange(ref _IsDisposed, 1) == 0)
					_TaskSource.SetResult(default);
			}

			//****************************************

			void IValueTaskSource.GetResult(short token) => _TaskSource.GetResult(token);

			ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _TaskSource.GetStatus(token);

			void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			//****************************************

			public short Token => _TaskSource.Version;
		}
	}
}
