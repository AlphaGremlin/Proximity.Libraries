using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides a reader/writer synchronisation object for async/await, and a disposable model for releasing
	/// </summary>
	public sealed class AsyncReadWriteLock : IDisposable, IAsyncDisposable
	{ //****************************************
		private static readonly ConcurrentBag<LockInstance> _Instances = new ConcurrentBag<LockInstance>();
		//****************************************
		private readonly WaiterQueue<LockInstance> _Writers = new WaiterQueue<LockInstance>();
		private readonly WaiterQueue<LockInstance> _Readers = new WaiterQueue<LockInstance>();

		private AsyncReadWriteDisposer? _Disposer;

		private int _Counter = 0;
		//****************************************

		/// <summary>
		/// Creates a new asynchronous reader/writer lock
		/// </summary>
		public AsyncReadWriteLock()
		{
		}

		/// <summary>
		/// Creates a new asynchronous reader/writer lock
		/// </summary>
		/// <param name="isUnfair">Whether to prefer fairness when switching</param>
		public AsyncReadWriteLock(bool isUnfair)
		{
			UnfairRead = UnfairWrite = isUnfair;
		}

		/// <summary>
		/// Creates a new asynchronous reader/writer lock
		/// </summary>
		/// <param name="unfairRead">If the read/write lock favours an unfair algorithm for readers</param>
		/// <param name="unfairWrite">if the read/write lock favours an unfair algorithm for writers</param>
		public AsyncReadWriteLock(bool unfairRead, bool unfairWrite)
		{
			UnfairRead = unfairRead;
			UnfairWrite = unfairWrite;
		}

		//****************************************

		/// <summary>
		/// Disposes of the semaphore
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public ValueTask DisposeAsync()
		{
			if (_Disposer != null || Interlocked.CompareExchange(ref _Disposer, new AsyncReadWriteDisposer(), null) != null)
				return default;

			// Success, now close any pending waiters
			while (_Writers.TryDequeue(out var Instance))
				Instance.SwitchToDisposed();

			while (_Readers.TryDequeue(out var Instance))
				Instance.SwitchToDisposed();

			// If there's no counters, we can complete the dispose task
			if (Interlocked.CompareExchange(ref _Counter, -2, 0) == 0)
				_Disposer.SwitchToComplete();

			return new ValueTask(_Disposer, _Disposer.Token);
		}

		void IDisposable.Dispose() => DisposeAsync();

		/// <summary>
		/// Take a read lock, running concurrently with other read locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the read lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<IDisposable> LockRead(TimeSpan timeout)
		{
			// Are we disposed?
			if (_Disposer != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of")).AsValueTask();

			// Try and take the lock
			var Instance = GetOrCreateInstance(false, TryTakeReader());

			var ValueTask = new ValueTask<IDisposable>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				var CancelSource = new CancellationTokenSource(timeout);

				Instance.ApplyCancellation(CancelSource.Token, CancelSource);

				// Unable to take the lock, add ourselves to the read waiters
				_Readers.Enqueue(Instance);

				// Try and release the reader, just in case we added it after we switched to unlocked or read
				TryActivateReader();
			}

			return ValueTask;
		}

		/// <summary>
		/// Take a read lock, running concurrently with other read locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<IDisposable> LockRead(CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of")).AsValueTask();

			// Try and take the lock
			var Instance = GetOrCreateInstance(false, TryTakeReader());

			var ValueTask = new ValueTask<IDisposable>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				Instance.ApplyCancellation(token, null);

				// Unable to take the lock, add ourselves to the read waiters
				_Readers.Enqueue(Instance);

				// Try and release the reader, just in case we added it after we switched to unlocked or read
				TryActivateReader();
			}

			return ValueTask;
		}

		/// <summary>
		/// Take a write lock, running exclusively, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the write lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<IDisposable> LockWrite(TimeSpan timeout)
		{
			// Are we disposed?
			if (_Disposer != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of")).AsValueTask();

			// Try and take the lock
			var Instance = GetOrCreateInstance(true, TryTakeWriter());

			var ValueTask = new ValueTask<IDisposable>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				var CancelSource = new CancellationTokenSource(timeout);

				Instance.ApplyCancellation(CancelSource.Token, CancelSource);

				// Unable to take the lock, queue ourselves to the write waiters
				_Writers.Enqueue(Instance);

				// Try and release the writer, just in case we added it after we switched to unlocked
				TryActivateWriter();
			}

			return ValueTask;
		}

		/// <summary>
		/// Take a write lock, running exclusively, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<IDisposable> LockWrite(CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				return Task.FromException<IDisposable>(new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of")).AsValueTask();

			// Try and take the lock
			var Instance = GetOrCreateInstance(true, TryTakeWriter());

			var ValueTask = new ValueTask<IDisposable>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				Instance.ApplyCancellation(token, null);

				// Unable to take the lock, queue ourselves to the write waiters
				_Writers.Enqueue(Instance);

				// Try and release the writer, just in case we added it after we switched to unlocked
				TryActivateWriter();
			}

			return ValueTask;
		}

		//****************************************

		private bool TryTakeReader()
		{
			int PreviousCounter, NewCounter;

			do
			{
				PreviousCounter = Volatile.Read(ref _Counter);

				if (PreviousCounter < 0)
					return false; // We're disposed, or there is a writer active, can't take no matter what

				if (!_Writers.IsEmpty && !UnfairRead)
					return false; // There are writers waiting, and we're not in unfair mode, so we can't take a reader lock

				// Take a reader lock if there are no writers waiting, or if there are but we're in unfair mode
				NewCounter = PreviousCounter + 1;
			}
			while (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) != PreviousCounter);

			return true;
		}

		private bool TryTakeWriter()
		{
			int PreviousCounter;

			do
			{
				PreviousCounter = Volatile.Read(ref _Counter);

				if (PreviousCounter != 0)
					return false; // We must be in the unlocked state to immediately switch to write mode

				if (!_Writers.IsEmpty)
					return false; // We're unlocked, but there are writers that got in ahead of us

				// No readers or writers, try and take the writer lock
			}
			while (Interlocked.CompareExchange(ref _Counter, -1, PreviousCounter) != PreviousCounter);

			return true;
		}

		private void Release(bool isWriter)
		{
			int PreviousCounter, NewCounter;

			for (; ; )
			{
				do
				{
					PreviousCounter = Volatile.Read(ref _Counter);

					if (PreviousCounter == 0)
						throw new InvalidOperationException("Read Write Lock is in an invalid state (Unheld)");

					if (isWriter)
					{
						if (PreviousCounter > 0)
						{
							Debugger.Launch();
							throw new InvalidOperationException("Read Write Lock is in an invalid state (Reader)");
						}

						if (_Disposer != null)
							NewCounter = -2; // We're disposing, so we can only switch to disposed mode
						else if (_Writers.IsEmpty)
						{
							if (_Readers.IsEmpty)
								NewCounter = 0; // No readers or writers, so we become unlocked
							else
								NewCounter = 1; // No writers, but readers are waiting, so we switch to read mode
						}
						else if (UnfairWrite)
							NewCounter = -1; // There are writers, and either unfair write is enabled, or there are no readers, so we stay in write mode
						else if (_Readers.IsEmpty)
							NewCounter = -1; // There are writers, and no readers, so stay in write mode
						else
							NewCounter = 1; // There are writers and also readers, and we're using fair writes, so switch to read mode
					}
					else
					{
						if (PreviousCounter < 0)
						{
							Debugger.Launch();
							throw new InvalidOperationException("Read Write Lock is in an invalid state (Writer)");
						}

						if (PreviousCounter > 1)
							NewCounter = PreviousCounter - 1; // There are still other readers, so just decrease the active counter and stay in read mode
						else if (_Disposer != null)
							NewCounter = -2; // We're the last reader and disposing, so we can only switch to disposed mode
						else if (_Writers.IsEmpty)
							NewCounter = 0; // We're the last reader and there are no writers, so switch to unlocked
						else
							NewCounter = -1; // We're the final reader, and there are writers, so we should switch to write mode
					}
				}
				while (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) != PreviousCounter);

				switch (NewCounter)
				{
				case 0:
					// Switched to unlocked mode
					// Readers or Writers may have gotten queued while we were busy, so double-check
					if (isWriter)
					{
						// We were a writer, so check readers first
						if (!_Readers.IsEmpty)
							TryActivateReader(true);
						else if (!_Writers.IsEmpty)
							TryActivateWriter();
					}
					else
					{
						// We were a reader, so check writers first
						if (!_Writers.IsEmpty)
							TryActivateWriter(true); // Force it to activate a writer even if there are readers and we're not in unfair mode
						else if (!_Readers.IsEmpty)
							TryActivateReader(true);
					}

					return;

				case -1:
					// Switching to Write mode
					while (_Writers.TryDequeue(out var Writer))
					{
						// Try to release the writer
						if (Writer.TrySwitchToCompleted())
							return;

						// Failed to activate the writer (maybe they were cancelled), so look for another one
					}

					// Unable to activate any writers, so we need to release the write lock
					isWriter = true;
					break;

				case -2:
					_Disposer!.SwitchToComplete();

					return;

				default:
					if (PreviousCounter > 0)
						return; // We were already in read mode, no need to check for further readers

					// Switching to Read mode, which means there was at least one reader in the queue
					if (_Readers.TryDequeue(out var Reader))
					{
						// There may be more than one reader in there
						while (_Readers.TryDequeue(out var OtherReader))
						{
							// Try to activate each waiting reader (they may be cancelled)
							if (OtherReader.TrySwitchToCompleted())
								// We can safely increment here, since we're holding a read lock (can't unexpectedly switch to write mode)
								Interlocked.Increment(ref _Counter);
						}

						// Now we try to release the final reader
						if (Reader.TrySwitchToCompleted())
							return;
					}

					// Failed to dequeue or activate the reader (maybe they were cancelled), so we need to release the read lock we're holding on its behalf
					isWriter = false;

					break;
				}
			}
		}

		private void TryActivateReader(bool multiple = false)
		{
			int PreviousCounter, NewCounter;

			do
			{
				PreviousCounter = Volatile.Read(ref _Counter);

				if (PreviousCounter == -2)
					break; // We're disposed, so we leave the counter alone and switch the instance to Disposed

				if (PreviousCounter == -1)
					return; // There is a writer active, so leave the reader on the queue

				if (PreviousCounter > 0 && !_Writers.IsEmpty && !UnfairRead)
					return; // There are writers waiting, and we're not in unfair mode, so we can't take a reader lock

				// Take a reader lock if there are no writers waiting, or if there are but we're in unfair mode
				NewCounter = PreviousCounter + 1;
			}
			while (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) != PreviousCounter);

			// We just queued a reader, so this should succeed unless we were preempted by a writer Release (or it cancelled)
			if (_Readers.TryDequeue(out var Reader))
			{
				if (PreviousCounter == -2)
				{
					Reader.SwitchToDisposed();

					return;
				}

				if (multiple)
				{
					// There may be more than one reader in there
					while (_Readers.TryDequeue(out var OtherReader))
					{
						// Try to activate each waiting reader (they may be cancelled)
						if (OtherReader.TrySwitchToCompleted())
							// We can safely increment here, since we're holding a read lock (can't unexpectedly switch to write mode)
							Interlocked.Increment(ref _Counter);
					}
				}

				if (Reader.TrySwitchToCompleted())
					return;
			}

			// Failed to dequeue and activate a reader
			if (PreviousCounter == -2)
				return; // We're disposed, so it doesn't matter

			// We're still active, which means we need to release the lock we just took
			Release(false);
		}

		private void TryActivateWriter(bool ignoreReaders = false)
		{
			int PreviousCounter;

			do
			{
				PreviousCounter = Volatile.Read(ref _Counter);

				if (PreviousCounter == -2)
					break; // We're disposed, so we leave the counter alone and switch the instance to Disposed

				if (PreviousCounter != 0)
					return; // There is a writer already active, or other readers, so leave the writer on the queue

				if (!ignoreReaders && !_Readers.IsEmpty && !UnfairWrite)
					return; // There are readers waiting, and we're not in unfair mode, so we can't take a write lock

				// Take a writer lock if there are no readers waiting, or if there are but we're in unfair mode
			}
			while (Interlocked.CompareExchange(ref _Counter, -1, PreviousCounter) != PreviousCounter);

			// We just queued a writer, so this should succeed unless were preempted by a reader Release (which triggered our writer, and then that writer Released and set us back to unlocked)
			if (_Writers.TryDequeue(out var Writer))
			{
				if (PreviousCounter == -2)
				{
					Writer.SwitchToDisposed();

					return;
				}

				if (Writer.TrySwitchToCompleted())
					return;
			}

			// Failed to dequeue and activate a writer
			if (PreviousCounter == -2)
				return; // We're disposed, so it doesn't matter

			// We're still active, which means we need to release the lock we just took
			Release(true);
		}

		private void CancelWaiter(LockInstance instance)
		{
			if (instance.IsWriter)
			{
				_Writers.Erase(instance);

				// We cancelled writing, this may allow readers to start work when we're not in unfair mode
				if (!UnfairRead)
					TryActivateReader(true);
			}
			else
			{
				_Readers.Erase(instance);

				// We cancelled reading, this may allow a writer to start work when we're not in unfair mode
				if (!UnfairWrite)
					TryActivateWriter();
			}
		}

		private LockInstance GetOrCreateInstance(bool isWriter, bool isTaken)
		{
			if (!_Instances.TryTake(out var Instance))
				Instance = new LockInstance();

			Instance.Initialise(this, isWriter, isTaken);

			return Instance;
		}

		//****************************************

		/// <summary>
		/// Gets whether any concurrent reading operations are in progress
		/// </summary>
		public bool IsReading => _Counter > 0;

		/// <summary>
		/// Gets whether an exclusive writing operation is in progress
		/// </summary>
		public bool IsWriting => _Counter == -1;

		/// <summary>
		/// Gets the number of readers queued
		/// </summary>
		public int WaitingReaders => _Readers.Count;

		/// <summary>
		/// Gets the number of writers queued
		/// </summary>
		public int WaitingWriters => _Writers.Count;

		/// <summary>
		/// Gets if the read/write lock favours an unfair algorithm for readers
		/// </summary>
		/// <remarks>If unfair reading, a read lock will succeed if it's already held elsewhere, even if there are writers waiting</remarks>
		public bool UnfairRead { get; } = false;

		/// <summary>
		/// Gets if the read/write lock favours an unfair algorithm for writers
		/// </summary>
		/// <remarks>If unfair writing, a write lock can transition directly to another write lock, even if there are readers waiting</remarks>
		public bool UnfairWrite { get; } = false;

		//****************************************

		private sealed class LockInstance : IDisposable, IValueTaskSource<IDisposable>
		{ //****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<IDisposable> _TaskSource = new ManualResetValueTaskSourceCore<IDisposable>();

			private CancellationTokenRegistration _Registration;
			private CancellationTokenSource? _TokenSource;
			//****************************************

			internal LockInstance() => _TaskSource.RunContinuationsAsynchronously = true;

			~LockInstance()
			{
				if (_InstanceState != Status.Unused)
				{
					// TODO: Lock instance was garbage collected without being released
				}
			}

			//****************************************

			internal void Initialise(AsyncReadWriteLock owner, bool isWriter, bool isHeld)
			{
				Owner = owner;
				IsWriter = isWriter;

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

					_Registration = token.Register((state) => ((LockInstance)state).SwitchToCancelled(), this);
				}
			}

			internal void SwitchToDisposed()
			{
				// Called when an Instance is removed from the Waiters queue due to a Dispose
				if (Interlocked.CompareExchange(ref _InstanceState, Status.Disposed, Status.Pending) != Status.Pending)
					return;

				_Registration.Dispose();
				_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of"));
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
				Owner!.Release(IsWriter);
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
					if (_InstanceState == Status.Disposed || _InstanceState == Status.CancelledNotWaiting || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
						Release(); // We're cancelled/disposed and no longer on the Wait queue, so we can return to the pool
				}
			}

			//****************************************

			private void SwitchToCancelled()
			{
				if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Cancelled, Status.Pending) != Status.Pending)
					return; // Instance is no longer in a cancellable state

				Owner!.CancelWaiter(this);

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
				IsWriter = false;

				GC.SuppressFinalize(this);
				_Instances.Add(this);
			}

			//****************************************

			public AsyncReadWriteLock? Owner { get; private set; }

			public bool IsWriter { get; private set; }

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

		private sealed class AsyncReadWriteDisposer : IValueTaskSource
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

			public bool IsDisposed => _IsDisposed == 1;
		}
	}
}
