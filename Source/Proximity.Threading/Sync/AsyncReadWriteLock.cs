using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides a reader/writer synchronisation object for async/await, and a disposable model for releasing
	/// </summary>
	public sealed partial class AsyncReadWriteLock : IDisposable, IAsyncDisposable
	{ //****************************************
		private readonly WaiterQueue<ILockWaiter> _Writers = new WaiterQueue<ILockWaiter>();
		private readonly WaiterQueue<ILockWaiter> _Readers = new WaiterQueue<ILockWaiter>();

		private LockDisposer? _Disposer;

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
			if (_Disposer != null || Interlocked.CompareExchange(ref _Disposer, new LockDisposer(), null) != null)
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
		/// Take a read lock, running concurrently with other read locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<Instance> LockRead(CancellationToken token = default) => LockRead(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Take a read lock, running concurrently with other read locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the read lock</param>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<Instance> LockRead(TimeSpan timeout, CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of");

			// Try and take the lock
			var NewInstance = LockInstance.GetOrCreate(this, false, TryTakeReader());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				NewInstance.ApplyCancellation(token, timeout);

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
		/// Take a write lock, running exclusively, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<Instance> LockWrite(CancellationToken token = default) => LockWrite(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Take a write lock, running exclusively, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the write lock</param>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<Instance> LockWrite(TimeSpan timeout, CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncReadWriteLock), "Read Write Lock has been disposed of");

			// Try and take the lock
			var NewInstance = LockInstance.GetOrCreate(this, true, TryTakeWriter());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				NewInstance.ApplyCancellation(token, timeout);

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

		//****************************************

		private ValueTask<bool> Upgrade(LockInstance instance, bool requireFirst, CancellationToken token, TimeSpan timeout)
		{
			bool TryUpgrade()
			{
				int PreviousCounter;

				do
				{
					PreviousCounter = _Counter;

					Debug.Assert(PreviousCounter >= LockState.Reader, "Read Write Lock is in an invalid state (Upgrade)");

					if (PreviousCounter > LockState.Reader)
						return false; // There are other Readers active, so we can't instantly upgrade

					if (!_Writers.IsEmpty)
						return false; // There are writers waiting, so we can't upgrade in front of them
				}
				while (Interlocked.CompareExchange(ref _Counter, LockState.Writer, PreviousCounter) != PreviousCounter);

				return true;
			}

			// Try to instantly transition to a Writer
			if (TryUpgrade())
			{
				instance.CompleteUpgrade();

				return new ValueTask<bool>(true);
			}

			var Instance = LockUpgradeInstance.GetOrCreate(instance);

			_Writers.Enqueue(Instance);

			// We can't be dequeued yet, as we're still holding the read lock
			var IsFirst = _Writers.TryPeek(out var FirstWriter) && FirstWriter == Instance;

			// If first place is required and we weren't first, exit
			if (requireFirst && !IsFirst)
			{
				if (!_Writers.Erase(Instance))
					Debug.Fail("Read Write lock failed to remove instance");

				Instance.Release(); // Return the Instance to the pool

				return new ValueTask<bool>(false);
			}

			// Activate the upgrader, letting it know if we're first
			Instance.Prepare(IsFirst, token, timeout);

			// We're ready to upgrade, so release the read lock
			// This complicates cancellation, as we will need to re-take the read lock before cancelling.
			// This means a cancellation may not occur immediately (potentially never in unfair write mode), just less than if we waited for the writer
			Release(false);

			return new ValueTask<bool>(Instance, Instance.Version);
		}

		private bool TryTakeReader(bool ignoreOthers = false)
		{
			int PreviousCounter;

			do
			{
				PreviousCounter = _Counter;

				if (PreviousCounter < LockState.Unlocked)
					return false; // We're disposed, or there is a writer active, can't take no matter what

				if (!ignoreOthers && !_Writers.IsEmpty && !UnfairRead)
					return false; // There are writers waiting, and we're not in unfair mode, so we can't take a reader lock

				// Take a reader lock if there are no writers waiting, or if there are but we're in unfair mode
			}
			while (Interlocked.CompareExchange(ref _Counter, PreviousCounter + 1, PreviousCounter) != PreviousCounter);

			return true;
		}

		private bool TryTakeWriter()
		{
			// We must be in the unlocked state to switch to write mode
			return Interlocked.CompareExchange(ref _Counter, LockState.Writer, LockState.Unlocked) == LockState.Unlocked;
		}

		private void Downgrade()
		{
			int PreviousCounter, NewCounter;

			do
			{
				PreviousCounter = _Counter;

				Debug.Assert(PreviousCounter == LockState.Writer, "Read Write Lock is in an invalid state (Reader)");

				if (_Readers.IsEmpty)
					NewCounter = LockState.Reader; // No other readers, so just activate ourselves
				else
					NewCounter = LockState.Reader + 1; // Other readers, reserve at least one lock
			}
			while (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) != PreviousCounter);

			if (NewCounter > LockState.Reader)
			{
				// We saw there were more Readers, so try and activate them
				if (TryActivateReader(true))
					return;

				// Failed to activate the Reader, so release it
				// Safe to just do a decrement, since we're holding the read lock ourselves
				Interlocked.Decrement(ref _Counter);
			}
		}

		private void Downgrade(LockUpgradeInstance instance)
		{
			_Readers.Enqueue(instance);

			if (!TryTakeReader(true) || TryActivateReader())
				return;

			Release(false);
		}

		private void Release(bool isWriter)
		{
			int PreviousCounter, NewCounter;

			for (; ; )
			{
				do
				{
					PreviousCounter = _Counter;

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
				}
				while (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) != PreviousCounter);

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
						ConsumedLock = OtherReader.TrySwitchToCompleted(false);
					}

					// If the reader didn't take the lock, we need to release it again
					if (!ConsumedLock)
						Interlocked.Decrement(ref _Counter);
				}

				return Reader.TrySwitchToCompleted(false);
			}

			// Failed to dequeue, so trigger a release
			return false;
		}

		private bool TryActivateWriter()
		{
			while (_Writers.TryDequeue(out var Writer))
			{
				if (Writer.TrySwitchToCompleted(true))
					return true;
			}

			// Failed to dequeue, so if we hold the lock return false to trigger a release
			return false;
		}

		private bool CancelWaiter(ILockWaiter instance, bool isWriter)
		{
			if (isWriter)
			{
				if (!_Writers.Erase(instance))
					return false;

				// We cancelled writing, this may allow readers to start work when we're not in unfair mode
				if (!UnfairRead && TryTakeReader() && !TryActivateReader(true))
					Release(false);
			}
			else
			{
				if (!_Readers.Erase(instance))
					return false;

				// We cancelled reading, this may allow a writer to start work when we're not in unfair mode
				if (!UnfairWrite && TryTakeWriter() && !TryActivateWriter())
					Release(true);
			}

			return true;
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
			/// Upgrades a Read lock to a Write lock
			/// </summary>
			/// <param name="token">A cancellation token to abort the upgrade</param>
			/// <returns>A Task returning True if we managed an exclusive upgrade, False if another Writer was activated first</returns>
			/// <remarks>Can wait until other Readers finish. Can also wait until other waiting Writers have finished. Does nothing if you're already a Writer.</remarks>
			/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
			public ValueTask<bool> Upgrade(CancellationToken token = default) => _Instance.Upgrade(_Token, token, Timeout.InfiniteTimeSpan);

			/// <summary>
			/// Upgrades a Read lock to a Write lock
			/// </summary>
			/// <param name="timeout">The amount of time to wait for the write lock</param>
			/// <param name="token">A cancellation token to abort the upgrade</param>
			/// <returns>A Task returning True if we managed an exclusive upgrade, False if another Writer was activated first</returns>
			/// <remarks>Can wait until other Readers finish. Can also wait until other waiting Writers have finished. Does nothing if you're already a Writer.</remarks>
			/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
			/// <exception cref="TimeoutException">The timeout elapsed</exception>
			public ValueTask<bool> Upgrade(TimeSpan timeout, CancellationToken token = default) => _Instance.Upgrade(_Token, token, timeout);

			/// <summary>
			/// Tries to exclusively upgrade a Read lock to a Write lock
			/// </summary>
			/// <param name="token">A cancellation token to abort the upgrade</param>
			/// <returns>A Task returning True if we managed an upgrade to a Write lock, False if we remain a Read lock because there are waiting Writers.</returns>
			/// <remarks>Can wait until other Readers finish. Will not wait for other Writers. Does nothing if you're already a Writer.</remarks>
			/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
			public ValueTask<bool> TryUpgrade(CancellationToken token = default) => _Instance.TryUpgrade(_Token, token, Timeout.InfiniteTimeSpan);

			/// <summary>
			/// Tries to exclusively upgrade a Read lock to a Write lock
			/// </summary>
			/// <param name="timeout">The amount of time to wait for the write lock</param>
			/// <param name="token">A cancellation token to abort the upgrade</param>
			/// <returns>A Task returning True if we managed an upgrade to a Write lock, False if we remain a Read lock because there are waiting Writers.</returns>
			/// <remarks>Can wait until other Readers finish. Will not wait for other Writers. Does nothing if you're already a Writer.</remarks>
			public ValueTask<bool> TryUpgrade(TimeSpan timeout, CancellationToken token = default) => _Instance.TryUpgrade(_Token, token, timeout);

			/// <summary>
			/// Downgrades a Write lock to a Read lock
			/// </summary>
			/// <remarks>Does nothing if you're already a Reader</remarks>
			public void Downgrade() => _Instance.Downgrade(_Token);

			/// <summary>
			/// Releases the lock currently held
			/// </summary>
			public void Dispose() => _Instance.Release(_Token);

			//****************************************

			/// <summary>
			/// Gets whether the lock is a write (exclusive) lock
			/// </summary>
			public bool IsWriter => _Instance.IsWriter;
		}

		internal interface ILockWaiter
		{
			void SwitchToDisposed();

			bool TrySwitchToCompleted(bool isWriter);
		}

		private static class LockState
		{
			internal const int Reader = 1;
			internal const int Unlocked = 0;
			internal const int Writer = -1;
			internal const int Disposed = -2;
		}
	}
}
