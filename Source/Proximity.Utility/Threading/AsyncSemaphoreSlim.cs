using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a lock-free primitive for semaphores supporting ValueTask async/await and a disposable model for releasing
	/// </summary>
	public sealed class AsyncSemaphoreSlim : IDisposable
	{ //****************************************
		private static readonly ConcurrentBag<AsyncLockInstance> _Instances = new ConcurrentBag<AsyncLockInstance>();
		private static int _NextInstanceToken;

		private static readonly Action<object> _CompletedContinuation = (state) => throw new Exception("Should never be called");
		//****************************************
		private readonly ConcurrentQueue<AsyncLockInstance> _Waiters = new ConcurrentQueue<AsyncLockInstance>();
		private int _MaxCount, _CurrentCount;

		private TaskCompletionSource<AsyncSemaphoreSlim> _Dispose;
		//****************************************

		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphoreSlim() : this(1)
		{
		}

		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphoreSlim(int initialCount)
		{
			if (initialCount < 1)
				throw new ArgumentException("Initial Count is invalid");

			_CurrentCount = 0;
			_MaxCount = initialCount;
		}

		//****************************************

		/// <summary>
		/// Disposes of the semaphore
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public Task Dispose()
		{
			((IDisposable)this).Dispose();

			return _Dispose.Task;
		}

		void IDisposable.Dispose()
		{
			if (_Dispose != null || Interlocked.CompareExchange(ref _Dispose, new TaskCompletionSource<AsyncSemaphoreSlim>(), null) != null)
				return;

			// Success, now close any pending waiters
			while (_Waiters.TryDequeue(out var Instance))
				Instance.TryDispose();

			// If there's no counters, we can complete the dispose task
			if (_CurrentCount == 0)
				_Dispose.TrySetResult(this);
		}

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
			if (_Dispose != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException("AsyncSemaphoreSlim", "Semaphore has been disposed of")).AsValueTask();

			// Try and add a counter as long as nobody is waiting on it
			var Instance = GetOrCreateInstance(_Waiters.IsEmpty && TryIncrement());

			if (!Instance.IsCompleted)
			{
				var CancelSource = new CancellationTokenSource(timeout);

				Instance.ApplyCancellation(CancelSource.Token, CancelSource);

				// No free counters, add ourselves to the queue waiting on a counter
				_Waiters.Enqueue(Instance);

				// Did a counter become available while we were busy?
				if (TryIncrement())
					Decrement();
			}

			return new ValueTask<IDisposable>(Instance, Instance.TaskToken);
		}

		/// <summary>
			/// Attempts to take a counter
			/// </summary>
			/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
			/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public ValueTask<IDisposable> Take(CancellationToken token)
		{
			// Are we disposed?
			if (_Dispose != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException("AsyncSemaphoreSlim", "Semaphore has been disposed of")).AsValueTask();

			// Try and add a counter as long as nobody is waiting on it
			var Instance = GetOrCreateInstance(_Waiters.IsEmpty && TryIncrement());

			if (!Instance.IsCompleted)
			{
				Instance.ApplyCancellation(token, null);

				// No free counters, add ourselves to the queue waiting on a counter
				_Waiters.Enqueue(Instance);

				// Did a counter become available while we were busy?
				if (TryIncrement())
					Decrement();
			}

			return new ValueTask<IDisposable>(Instance, Instance.TaskToken);
		}

		/// <summary>
		/// Tries to take a counter without waiting
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <returns>True if the counter was taken without waiting, otherwise False</returns>
		public bool TryTake(out IDisposable handle)
		{
			// Are we disposed?
			if (_Dispose == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = GetOrCreateInstance(true);
				return true;
			}

			handle = null;

			return false;
		}

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on a counter</param>
		/// <returns>True if the counter was taken without waiting, otherwise False due to disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryTake(out IDisposable handle, CancellationToken token) => TryTake(out handle, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <returns>True if the counter was taken, otherwise False due to timeout or disposal</returns>
		public bool TryTake(out IDisposable handle, TimeSpan timeout) => TryTake(out handle, timeout, CancellationToken.None);

		/// <summary>
		/// Blocks attempting to take a counter
		/// </summary>
		/// <param name="handle">An IDisposable to release the counter if TryTake succeeds</param>
		/// <param name="timeout">Number of milliseconds to block for a counter to become available. Pass zero to not block, or Timeout.InfiniteTimeSpan to block indefinitely</param>
		/// <param name="token">A cancellation token that can be used to abort waiting on the counter</param>
		/// <returns>True if the counter was taken, otherwise False due to timeout or disposal</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public bool TryTake(out IDisposable handle, TimeSpan timeout, CancellationToken token)
		{
			// First up, try and take the counter if possible
			if (_Dispose == null && _Waiters.IsEmpty && TryIncrement())
			{
				handle = GetOrCreateInstance(true);
				return true;
			}

			handle = null;

			// If we're not okay with blocking, then we fail
			if (timeout == TimeSpan.Zero)
				return false;

			//****************************************

			if (timeout != Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException("timeout", "Timeout must be Timeout.InfiniteTimeSpan or a positive time");

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
				var TokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

				// We can't just use Decrement(token).Wait(timeout) because that won't cancel the decrement attempt
				// Instead, we have to cancel on the original token
				TokenSource.CancelAfter(timeout);

				// Prepare the cancellation token
				Instance.ApplyCancellation(TokenSource.Token, TokenSource);
			}
			else
			{
				// Prepare the cancellation token (if any)
				Instance.ApplyCancellation(token, null);
			}

			// We need to block, so use a manual reset event
			var Waiter = new ManualResetEventSlim();

			try
			{
				Instance.OnCompleted((state) => ((ManualResetEventSlim)state).Set(), Waiter, Instance.TaskToken, ValueTaskSourceOnCompletedFlags.None);

				// Wait for a definitive result
				Waiter.Wait();

				handle = Instance.GetResult(Instance.TaskToken);

				return true;
			}
			catch
			{
				// We may get a cancel or dispose exception
				if (!token.IsCancellationRequested)
					return false; // If it's not the original token, we cancelled due to a Timeout. Return false

				throw; // Throw the cancellation
			}
			finally
			{
				Waiter.Dispose();
			}
		}

		//****************************************

		private AsyncLockInstance GetOrCreateInstance(bool isTaken)
		{
			if (!_Instances.TryTake(out var Instance))
				Instance = new AsyncLockInstance();

			Instance.Prepare(this, isTaken);

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

		private void Decrement(bool skipAsync = false)
		{ //****************************************
			int NewCount;
			//****************************************

			do
			{
				// Try and retrieve a waiter while we're not disposed
				while (_Waiters.TryDequeue(out var NextInstance))
				{
					if (!skipAsync)
					{
						if (NextInstance.IsPending)
						{
							// The Instance is in a valid state, but we don't want to complete it on the calling thread,
							// otherwise it can cause a stack overflow if Increment gets called from a Decrement continuation
							ThreadPool.QueueUserWorkItem(ReleaseInstance, NextInstance);

							return;
						}
					}
					else
					{
						// Try and assign the counter to this Instance
						if (NextInstance.TryAssign())
							return;
					}

					// Waiter is no longer able to be released into a Held state, keep looking
					NextInstance.CompleteWaiting();
				}

				// Release a counter
				NewCount = Interlocked.Decrement(ref _CurrentCount);

				// If someone is waiting, try to put the counter back, and try again. If it fails, someone else can take care of the waiter
			} while (!_Waiters.IsEmpty && TryIncrement());

			// If we've disposed and there's no counters, we can complete the dispose task
			if (_Dispose != null && NewCount == 0)
				_Dispose.TrySetResult(this);
		}

		private static void ReleaseInstance(object state)
		{ //****************************************
			var Instance = (AsyncLockInstance)state;
			//****************************************

			// Try and assign the counter to this Instance
			if (Instance.TryAssign())
				return;

			// Assignment failed, so it was cancelled
			var Owner = Instance.Owner; 

			// If the result has already been retrieved, return the Instance to the pool
			Instance.CompleteWaiting();

			// Try and find another Instance to assign the counter to
			Owner.Decrement(true);
		}

		//****************************************

		/// <summary>
		/// Gets the number of counters available for taking
		/// </summary>
		public int CurrentCount => _MaxCount - _CurrentCount;

		/// <summary>
		/// Gets the number of operations waiting for a counter
		/// </summary>
		public int WaitingCount => _Waiters.Count(waiter => !waiter.IsCompleted);

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

				if (_Dispose != null)
					throw new ObjectDisposedException("AsyncSemaphoreSlim", "Object has been disposed");

				_MaxCount = value;

				// If there are waiters, try and assign a counter if possible
				if (!_Waiters.IsEmpty && TryIncrement())
					Decrement();
			}
		}

		//****************************************

		private sealed class AsyncLockInstance : IDisposable, IValueTaskSource<IDisposable>
		{ //****************************************
			private volatile int _InstanceState;

			private Action<object> _Continuation;
			private object _State;

			private ExecutionContext _ExecutionContext;
			private object _Scheduler;

			private CancellationTokenRegistration _Registration;
			//****************************************

			internal AsyncLockInstance()
			{
				TaskToken = (short)(Interlocked.Increment(ref _NextInstanceToken) & 0xFFFF);
			}

			//****************************************

			internal void Prepare(AsyncSemaphoreSlim owner, bool isTaken)
			{
				Owner = owner;

				if (isTaken)
				{
					_InstanceState = Status.Held;
					_Continuation = _CompletedContinuation;
				}
				else
				{
					_InstanceState = Status.Pending;
					_Continuation = null;
				}
			}

			internal void ApplyCancellation(CancellationToken token, CancellationTokenSource tokenSource)
			{
				if (token.CanBeCanceled)
				{
					Token = token;

					if (_InstanceState != Status.Pending)
						throw new InvalidOperationException("Cannot register for cancellation when not pending");

					_Registration = token.Register(CancelTask, this);
				}
			}

			internal bool TryAssign()
			{
				// Called when an Instance is removed from the Waiters queue due to a Decrement
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Held, Status.Pending) != Status.Pending)
					return false;

				_Registration.Dispose();

				RaiseContinuation();

				return true;
			}

			internal void TryDispose()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				_Registration.Dispose();

				RaiseContinuation();
			}

			internal void ResetAndReturn()
			{
				Owner = null;
				Token = default;
				TaskToken = (short)(Interlocked.Increment(ref _NextInstanceToken) & 0xFFFF);
				_Continuation = null;

				_Instances.Add(this);
			}

			internal void CompleteWaiting()
			{
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

						return;
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				_Registration.Dispose();

				if (InstanceState == Status.CancelledGotResult)
					ResetAndReturn(); // GetResult has been called, so we can release this Instance
			}

			//****************************************

			void IDisposable.Dispose()
			{
				if (Interlocked.Exchange(ref _InstanceState, 0) != 1)
					return;

				Owner.Decrement();
				ResetAndReturn();
			}

			ValueTaskSourceStatus IValueTaskSource<IDisposable>.GetStatus(short token)
			{
				if (token != TaskToken)
					throw new InvalidOperationException("Value Task Token invalid");

				if (!IsCompleted)
					return ValueTaskSourceStatus.Pending;

				switch (_InstanceState)
				{
				case Status.Held:
					return ValueTaskSourceStatus.Succeeded;

				case Status.Cancelled:
				case Status.CancelledNotWaiting:
					return ValueTaskSourceStatus.Canceled;

				case Status.Disposed:
					return ValueTaskSourceStatus.Faulted;

				default:
					throw new InvalidOperationException("Task is completed in an invalid state");
				}
			}

			public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
			{
				if (token != TaskToken)
					throw new InvalidOperationException("Value Task Token invalid");

				if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != ValueTaskSourceOnCompletedFlags.None)
					_ExecutionContext = ExecutionContext.Capture();

				if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != ValueTaskSourceOnCompletedFlags.None)
				{
					var CurrentContext = SynchronizationContext.Current;
					TaskScheduler CurrentScheduler;

					if (CurrentContext != null && CurrentContext.GetType() != typeof(SynchronizationContext))
						_Scheduler = CurrentContext;
					else if ((CurrentScheduler = TaskScheduler.Current) != TaskScheduler.Default)
						_Scheduler = CurrentScheduler;
				}

				_State = state;
				var Previous = Interlocked.CompareExchange(ref _Continuation, continuation, null);

				if (ReferenceEquals(Previous, _CompletedContinuation))
				{
					_ExecutionContext = null;
					_State = null;

					RaiseContinuation(continuation, state, true);
				}
				else if (Previous != null)
					throw new InvalidOperationException("Continuation already set");
			}

			public IDisposable GetResult(short token)
			{
				if (token != TaskToken)
					throw new InvalidOperationException("Value Task Token invalid");

				int InstanceState;
				var InnerToken = Token;

				// If we're Held or Cancelled, the Instance may exist elsewhere so we can't release it back into the pool
				// CancelledNotWaiting and Disposed are both valid states where we can clear and release the Instance
				// If we're Cancelled, we need to switch to CancelledGotResult, so when we're removed from the Waiters, we can release properly
				do
				{
					InstanceState = _InstanceState;
				}
				while (InstanceState == Status.Cancelled && Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) != Status.Cancelled);

				switch (InstanceState)
				{
				case Status.Held:
					return this;

				case Status.Cancelled:
					// Continuation has been run, but we're still in the wait list, so we can't release ourselves
					throw new OperationCanceledException(InnerToken);

				case Status.CancelledNotWaiting:
					// Continuation has been run and we're not on the wait list, so we can release ourselves
					ResetAndReturn();

					throw new OperationCanceledException(InnerToken);

				case Status.Disposed:
					throw new ObjectDisposedException("AsyncSemaphoreSlim", "Semaphore has been disposed of");

				default:
					throw new InvalidOperationException("Task is completed in an invalid state");
				}
			}

			//****************************************

			private void RaiseContinuation()
			{
				var Continuation = _Continuation;

				if (Continuation == null && (Continuation = Interlocked.CompareExchange(ref _Continuation, _CompletedContinuation, null)) == null)
					return;

				var ContinuationState = _State;

				_Continuation = _CompletedContinuation;
				_State = null;

				if (_ExecutionContext == null)
				{
					RaiseContinuation(Continuation, ContinuationState, false);
				}
				else
				{
					ExecutionContext.Run(_ExecutionContext, (state) =>
					{
						var (task, continuation, continuationState) = ((AsyncLockInstance, Action<object>, object))state;

						task.RaiseContinuation(continuation, continuationState, false);
					}, (this, Continuation, ContinuationState));
				}
			}

			private void RaiseContinuation(Action<object> continuation, object state, bool forceAsync)
			{
				if (_Scheduler != null)
				{
					if (_Scheduler is SynchronizationContext Context)
						Context.Post(RaiseContinuation, (continuation, state));
					else if (_Scheduler is TaskScheduler Scheduler)
						Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler);

					_Scheduler = null;
				}
				else if (forceAsync)
					ThreadPool.QueueUserWorkItem(RaiseContinuation, (continuation, state));
				else
					continuation(state);
			}

			private static void RaiseContinuation(object state)
			{
				var (innerContinuation, innerState) = ((Action<object>, object))state;

				innerContinuation(innerState);
			}

			private static void CancelTask(object state)
			{
				var Instance = (AsyncLockInstance)state;

				if (Instance._InstanceState != Status.Pending || Interlocked.CompareExchange(ref Instance._InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				Instance.RaiseContinuation();
			}

			//****************************************

			public AsyncSemaphoreSlim Owner { get; private set; }

			public short TaskToken { get; private set; }

			public bool IsCompleted => ReferenceEquals(_Continuation, _CompletedContinuation);

			public bool IsPending => _InstanceState == Status.Pending;

			public CancellationToken Token { get; private set; }
		}

		private static class Status
		{
			internal const int CancelledGotResult = -4;
			internal const int CancelledNotWaiting = -3;
			internal const int Cancelled = -2;
			internal const int Disposed = -1;
			internal const int Unused = 0;
			internal const int Held = 1;
			internal const int Pending = 2;
		}
	}
}
