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
		private static readonly ConcurrentBag<LockInstance> Instances = new ConcurrentBag<LockInstance>();
		//****************************************
		private readonly WaiterQueue<LockInstance> _Writers = new WaiterQueue<LockInstance>();
		private readonly WaiterQueue<LockInstance> _Readers = new WaiterQueue<LockInstance>();

		private AsyncReadWriteDisposer? _Disposer;

		private volatile int _Counter = LockState.Unlocked;
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
			DisposeWriters();
			DisposeReaders();

			// If there's no counters, we can complete the dispose task
			if (Interlocked.CompareExchange(ref _Counter, LockState.Disposed, LockState.Unlocked) == LockState.Unlocked)
				_Disposer.SwitchToComplete();

			return new ValueTask(_Disposer, _Disposer.Token);
		}

		void IDisposable.Dispose() => DisposeAsync();

		/// <summary>
		/// Take a read lock, running concurrently with other read locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the read lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<Instance> LockRead(TimeSpan timeout)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of");

			// Try and take the lock
			var NewInstance = GetOrCreateInstance(false, TryTakeReader());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				var CancelSource = new CancellationTokenSource(timeout);

				NewInstance.ApplyCancellation(CancelSource.Token, CancelSource);

				// Unable to take the lock, add ourselves to the read waiters
				_Readers.Enqueue(NewInstance);

				// Try and release the reader, just in case we added it after we switched to unlocked or read
				if (TryTakeReader())
				{
					if (!TryActivateReader())
						Release(false); // Failed, we need to release it
				}
				else if (_Disposer != null)
					DisposeReaders();
			}

			return ValueTask;
		}

		/// <summary>
		/// Take a read lock, running concurrently with other read locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<Instance> LockRead(CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of");

			// Try and take the lock
			var NewInstance = GetOrCreateInstance(false, TryTakeReader());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				NewInstance.ApplyCancellation(token, null);

				// Unable to take the lock, add ourselves to the read waiters
				_Readers.Enqueue(NewInstance);

				// Try and release the reader, just in case we added it after we switched to unlocked or read
				if (TryTakeReader())
				{
					if (!TryActivateReader())
						Release(false); // Failed, we need to release it
				}
				else if (_Disposer != null)
					DisposeReaders();
			}

			return ValueTask;
		}

		/// <summary>
		/// Take a write lock, running exclusively, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the write lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<Instance> LockWrite(TimeSpan timeout)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of");

			// Try and take the lock
			var NewInstance = GetOrCreateInstance(true, TryTakeWriter());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				var CancelSource = new CancellationTokenSource(timeout);

				NewInstance.ApplyCancellation(CancelSource.Token, CancelSource);

				// Unable to take the lock, queue ourselves to the write waiters
				_Writers.Enqueue(NewInstance);

				// Try and release the writer, just in case we added it after we switched to unlocked
				if (TryTakeWriter())
				{
					if (!TryActivateWriter())
						Release(true); // Failed, we need to release it
				}
				else if (_Disposer != null)
					DisposeWriters();
			}

			return ValueTask;
		}

		/// <summary>
		/// Take a write lock, running exclusively, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public ValueTask<Instance> LockWrite(CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of");

			// Try and take the lock
			var NewInstance = GetOrCreateInstance(true, TryTakeWriter());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				NewInstance.ApplyCancellation(token, null);

				// Unable to take the lock, queue ourselves to the write waiters
				_Writers.Enqueue(NewInstance);

				// Try and activate the writer, just in case we added it after we switched to unlocked
				if (TryTakeWriter())
				{
					if (!TryActivateWriter())
						Release(true); // Failed, we need to release it
				}
				else if (_Disposer != null)
					DisposeWriters();
			}

			return ValueTask;
		}

		//****************************************

		private bool TryTakeReader(bool ignoreOthers = false)
		{
			int PreviousCounter, NewCounter;

			do
			{
				PreviousCounter = _Counter;

				if (PreviousCounter < LockState.Unlocked)
					return false; // We're disposed, or there is a writer active, can't take no matter what

				if (!ignoreOthers && !_Writers.IsEmpty && !UnfairRead)
					return false; // There are writers waiting, and we're not in unfair mode, so we can't take a reader lock

				// Take a reader lock if there are no writers waiting, or if there are but we're in unfair mode
				NewCounter = PreviousCounter + 1;
			}
			while (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) != PreviousCounter);

			return true;
		}

		private bool TryTakeWriter()
		{
			// We must be in the unlocked state to switch to write mode
			return Interlocked.CompareExchange(ref _Counter, LockState.Writer, LockState.Unlocked) == LockState.Unlocked;
		}

		private void Release(bool isWriter)
		{
			int PreviousCounter, NewCounter;
			var OuterAttempts = 0;

			for (; ; )
			{
				OuterAttempts++;
				var Attempts = 0;

				for (; ;)
				{
					Attempts++;
					PreviousCounter = Interlocked.CompareExchange(ref _Counter, 0, 0);

					Debug.Assert(PreviousCounter != LockState.Unlocked, "Read Write Lock is in an invalid state (Unheld)");
					Debug.Assert(PreviousCounter != LockState.Disposed, "Read Write Lock is in an invalid state (Disposed)");

					if (isWriter)
					{
						Debug.Assert(PreviousCounter == LockState.Writer, "Read Write Lock is in an invalid state (Reader)");

						if (_Disposer != null)
							NewCounter = LockState.Disposed; // We're disposing, so we can only switch to disposed mode
						else if (_Writers.IsEmpty)
						{
							if (_Readers.IsEmpty)
								NewCounter = LockState.Unlocked; // No readers or writers, so we become unlocked
							else
								NewCounter = LockState.Reader; // No writers, but readers are waiting, so we switch to read mode
						}
						else if (UnfairWrite)
							NewCounter = LockState.Writer; // There are writers, and either unfair write is enabled, or there are no readers, so we stay in write mode
						else if (_Readers.IsEmpty)
							NewCounter = LockState.Writer; // There are writers, and no readers, so stay in write mode
						else
							NewCounter = LockState.Reader; // There are writers and also readers, and we're using fair writes, so switch to read mode
					}
					else
					{
						Debug.Assert(PreviousCounter >= LockState.Reader, "Read Write Lock is in an invalid state (Writer)");

						if (PreviousCounter > LockState.Reader)
							NewCounter = PreviousCounter - 1; // There are still other readers, so just decrease the active counter and stay in read mode
						else if (_Disposer != null)
							NewCounter = LockState.Disposed; // We're the last reader and disposing, so we can only switch to disposed mode
						else if (_Writers.IsEmpty)
							NewCounter = LockState.Unlocked; // We're the last reader and there are no writers, so switch to unlocked
						else
							NewCounter = LockState.Writer; // We're the final reader, and there are writers, so we should switch to write mode
					}

					if (NewCounter == PreviousCounter)
					{
						Debug.Assert(NewCounter == LockState.Writer, $"Read Write Lock did not change state ({NewCounter})");
						break;
					}

					if (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) == PreviousCounter)
						break;
				}

				switch (NewCounter)
				{
				case LockState.Unlocked:
					// Switched to unlocked mode
					if (isWriter && !_Readers.IsEmpty)
					{
						// One or more readers got queued, so try and activate them
						// We only check if this is a writer, since if it's a reader they should auto-succeeded in unfair mode, and in fair mode it's up to the writer that originally blocked them to release, not us
						if (!TryTakeReader(true) || TryActivateReader(true))
							return;

						isWriter = false;

						continue; // Locked but failed to activate, release
					}

					if (!_Writers.IsEmpty)
					{
						// One or more writers got queued, try and activate one
						if (!TryTakeWriter() || TryActivateWriter())
							return;

						isWriter = true;

						continue; // Locked but failed to activate, release
					}

					// No readers or writers
					return;

				case LockState.Writer:
					// Switching to Write mode, try and activate a Writer
					if (TryActivateWriter())
						return;

					// Unable to activate any writers, so we need to release the write lock
					isWriter = true;
					break;

				case LockState.Disposed:
					_Disposer!.SwitchToComplete();

					return;

				default:
					Debug.Assert(NewCounter >= LockState.Reader, "Read Write lock is in an invalid state (out of bounds)");

					if (PreviousCounter > NewCounter)
						return; // We released a Reader, so no need to check for queued Readers

					// Switching to read mode, which means there was at least one reader in the queue
					if (TryActivateReader(true))
						return;

					// Failed to dequeue or activate the reader (maybe they were cancelled), so we need to release the read lock we're holding on its behalf
					isWriter = false;

					break;
				}
			}
		}

		private bool TryActivateReader(bool multiple = false)
		{
			// Either we hold a reader lock, or we're disposing
			if (_Readers.TryDequeue(out var Reader))
			{
				if (multiple)
				{
					// We're allowed to release more than one reader
					// Start off with a consumed lock, so we need to take another
					var ConsumedLock = true;

					while (_Readers.TryDequeue(out var OtherReader))
					{
						if (ConsumedLock)
							// Need to increment before we switch to completed, because otherwise the reader could be scheduled and release the lock we're holding
							Interlocked.Increment(ref _Counter);

						// Try to activate each waiting reader (they may be cancelled)
						ConsumedLock = OtherReader.TrySwitchToCompleted();
					}

					// If the reader didn't take the lock, we need to release it again
					if (!ConsumedLock)
						Interlocked.Decrement(ref _Counter);
				}

				return Reader.TrySwitchToCompleted();
			}

			// Failed to dequeue, so trigger a release
			return false;
		}

		private bool TryActivateWriter()
		{
			while (_Writers.TryDequeue(out var Writer))
			{
				if (Writer.TrySwitchToCompleted())
					return true;
			}

			// Failed to dequeue, so if we hold the lock return false to trigger a release
			return false;
		}

		private void CancelWaiter(LockInstance instance)
		{
			if (instance.IsWriter)
			{
				if (!_Writers.Erase(instance))
					return;

				// We cancelled writing, this may allow readers to start work when we're not in unfair mode
				if (!UnfairRead && TryTakeReader() && !TryActivateReader(true))
					Release(false);
			}
			else
			{
				if (!_Readers.Erase(instance))
					return;

				// We cancelled reading, this may allow a writer to start work when we're not in unfair mode
				if (!UnfairWrite && TryTakeWriter() && !TryActivateWriter())
					Release(true);
			}
		}

		private void DisposeReaders()
		{
			while (_Readers.TryDequeue(out var Instance))
				Instance.SwitchToDisposed();
		}

		private void DisposeWriters()
		{
			while (_Writers.TryDequeue(out var Instance))
				Instance.SwitchToDisposed();
		}

		private LockInstance GetOrCreateInstance(bool isWriter, bool isTaken)
		{
			if (!Instances.TryTake(out var Instance))
				Instance = new LockInstance();

			Instance.Initialise(this, isWriter, isTaken);

			return Instance;
		}

		//****************************************

		/// <summary>
		/// Gets whether any concurrent reading operations are in progress
		/// </summary>
		public bool IsReading => _Counter >= LockState.Reader;

		/// <summary>
		/// Gets whether an exclusive writing operation is in progress
		/// </summary>
		public bool IsWriting => _Counter == LockState.Writer;

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

		/// <summary>
		/// Represents the lock currently held
		/// </summary>
		public readonly struct Instance : IDisposable
		{ //****************************************
			private readonly LockInstance _Instance;
			private readonly short _Token;
			//****************************************

			internal Instance(LockInstance instance)
			{
				_Instance = instance;
				_Token = instance.Version;
			}

			//****************************************

			/// <summary>
			/// Releases the lock currently held
			/// </summary>
			public void Dispose() => _Instance.Release(_Token);

			//****************************************

			/// <summary>
			/// Gets whether the lock is an exclusive (write) lock
			/// </summary>
			public bool IsExclusive => _Instance.IsWriter;
		}

		internal sealed class LockInstance : IValueTaskSource<Instance>
		{ //****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<Instance> _TaskSource = new ManualResetValueTaskSourceCore<Instance>();

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
					_TaskSource.SetResult(new Instance(this));
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
					if (IsWriter)
						Debug.Assert(Owner!._Counter == LockState.Writer, "Read Write lock is in an invalid state (Unheld writer)");
					else
						Debug.Assert(Owner!._Counter >= LockState.Reader, "Read Write lock is in an invalid state (Unheld reader)");

					_Registration.Dispose();
					_TaskSource.SetResult(new Instance(this));

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

					case Status.Unused:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Unused)");

					case Status.Pending:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Pending)");

					case Status.Held:
						throw new InvalidOperationException("Read Write Lock instance is in an invalid state (Held)");

					case Status.CancelledNotWaiting:
						_Registration.Dispose();

						return false;

					default:
						throw new InvalidOperationException($"Read Write Lock instance is in an invalid state ({InstanceState})");
					}
				}
				while (Interlocked.CompareExchange(ref _InstanceState, NewInstanceState, InstanceState) != InstanceState);

				_Registration.Dispose();

				if (InstanceState == Status.CancelledGotResult)
					Release(); // GetResult has been called, so we can return to the pool

				return false;
			}

			internal void Release(short token)
			{
				if (_TaskSource.Version != token || Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.Held) != Status.Held)
					throw new InvalidOperationException("Lock cannot be released multiple times");

				// Release the counter and then return to the pool
				Owner!.Release(IsWriter);
				Release();
			}

			//****************************************

			ValueTaskSourceStatus IValueTaskSource<Instance>.GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public Instance GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					switch (_InstanceState)
					{
					case Status.Disposed:
					case Status.CancelledNotWaiting:
						Release();
						break;

					case Status.Cancelled:
						if (Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledNotWaiting)
							Release(); // We're cancelled/disposed and no longer on the Wait queue, so we can return to the pool
						break;
					}
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
				Instances.Add(this);
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

		private static class LockState
		{
			internal const int Reader = 1;
			internal const int Unlocked = 0;
			internal const int Writer = -1;
			internal const int Disposed = -2;
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
