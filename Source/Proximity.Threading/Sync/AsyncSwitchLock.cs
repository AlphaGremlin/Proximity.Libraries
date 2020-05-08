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
	/// Provides a lock that switches between concurrent left and right for async/await, and a disposable model for releasing
	/// </summary>
	public sealed partial class AsyncSwitchLock : IAsyncDisposable, IDisposable
	{ //****************************************
		private readonly WaiterQueue<LockInstance> _LeftWaiters = new WaiterQueue<LockInstance>();
		private readonly WaiterQueue<LockInstance> _RightWaiters = new WaiterQueue<LockInstance>();

		private LockDisposer? _Disposer;
		
		private volatile int _Counter = 0; // Left is negative, Right is positive
		//****************************************

		/// <summary>
		/// Creates a new fair switch lock
		/// </summary>
		public AsyncSwitchLock()
		{
		}
		
		/// <summary>
		/// Creates a new switch lock
		/// </summary>
		/// <param name="isUnfair">Whether to prefer fairness when switching</param>
		public AsyncSwitchLock(bool isUnfair)
		{
			IsUnfair = isUnfair;
		}

		//****************************************

		/// <summary>
		/// Disposes of the switch lock
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public ValueTask DisposeAsync()
		{
			if (_Disposer != null || Interlocked.CompareExchange(ref _Disposer, new LockDisposer(), null) != null)
				return default;

			// Success, now close any pending waiters
			DisposeLeft();
			DisposeRight();

			// If there's no counters, we can complete the dispose task
			if (_Counter == 0)
				_Disposer.SwitchToComplete();

			return new ValueTask(_Disposer, _Disposer.Token);
		}

		void IDisposable.Dispose() => DisposeAsync();

		/// <summary>
		/// Take a left lock, running concurrently with other left locks
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<Instance> LockLeft(CancellationToken token = default) => LockLeft(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Take a left lock, running concurrently with other left locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the lock</param>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<Instance> LockLeft(TimeSpan timeout, CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncSwitchLock), "Switch Lock has been disposed of");

			// Try and take the lock
			var NewInstance = LockInstance.GetOrCreate(this, false, TryTakeLeft());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				NewInstance.ApplyCancellation(token, timeout);

				// Unable to take the lock, add ourselves to the read waiters
				_LeftWaiters.Enqueue(NewInstance);

				// Try and release the reader, just in case we added it after we switched to unlocked or read
				if (TryTakeLeft())
				{
					if (!TryActivateLeft())
						Release(false); // Failed, we need to release it
				}
				else if (_Disposer != null)
					DisposeLeft();
			}

			return ValueTask;
		}

		/// <summary>
		/// Take a right lock, running concurrently with other right locks
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<Instance> LockRight(CancellationToken token = default) => LockRight(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Take a right lock, running concurrently with other right locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the lock</param>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<Instance> LockRight(TimeSpan timeout, CancellationToken token = default)
		{
			// Are we disposed?
			if (_Disposer != null)
				throw new ObjectDisposedException(nameof(AsyncSwitchLock), "Switch Lock has been disposed of");

			// Try and take the lock
			var NewInstance = LockInstance.GetOrCreate(this, true, TryTakeRight());

			var ValueTask = new ValueTask<Instance>(NewInstance, NewInstance.Version);

			if (!ValueTask.IsCompleted)
			{
				NewInstance.ApplyCancellation(token, timeout);

				// Unable to take the lock, add ourselves to the read waiters
				_RightWaiters.Enqueue(NewInstance);

				// Try and release the reader, just in case we added it after we switched to unlocked or read
				if (TryTakeRight())
				{
					if (!TryActivateRight())
						Release(true); // Failed, we need to release it
				}
				else if (_Disposer != null)
					DisposeRight();
			}

			return ValueTask;
		}

		//****************************************

		private bool TryTakeRight(bool ignoreOthers = false)
		{
			int PreviousCounter;

			do
			{
				PreviousCounter = _Counter;

				if (PreviousCounter < LockState.Unlocked || PreviousCounter == LockState.Disposed)
					return false; // We're disposed, or there is a left active, can't take no matter what

				if (!ignoreOthers && !_LeftWaiters.IsEmpty && !IsUnfair)
					return false; // There are left locks waiting, and we're not in unfair mode, so we can't take a right lock

				// Take a right lock if there are no lefts waiting, or if there are but we're in unfair mode
			}
			while (Interlocked.CompareExchange(ref _Counter, PreviousCounter + 1, PreviousCounter) != PreviousCounter);

			return true;
		}

		private bool TryTakeLeft(bool ignoreOthers = false)
		{
			int PreviousCounter;

			do
			{
				PreviousCounter = _Counter;

				if (PreviousCounter > LockState.Unlocked || PreviousCounter == LockState.Disposed)
					return false; // We're disposed, or there is a right active, can't take no matter what

				if (!ignoreOthers && !_RightWaiters.IsEmpty && !IsUnfair)
					return false; // There are right locks waiting, and we're not in unfair mode, so we can't take a left lock

				// Take a left lock if there are no rights waiting, or if there are but we're in unfair mode
			}
			while (Interlocked.CompareExchange(ref _Counter, PreviousCounter - 1, PreviousCounter) != PreviousCounter);

			return true;
		}

		private void Release(bool isRight)
		{
			int PreviousCounter, NewCounter;

			for (; ; )
			{
				do
				{
					PreviousCounter = _Counter;

					Debug.Assert(PreviousCounter != LockState.Unlocked, "Switch Lock is in an invalid state (Unheld)");
					Debug.Assert(PreviousCounter != LockState.Disposed, "Switch Lock is in an invalid state (Disposed)");

					if (isRight)
					{
						Debug.Assert(PreviousCounter >= LockState.Right, "Switch Lock is in an invalid state (Left)");

						if (PreviousCounter > LockState.Right)
							NewCounter = PreviousCounter - 1; // There are still other rights, so just decrease the active counter and stay in right mode
						else if (_Disposer != null)
							NewCounter = LockState.Disposed; // We're the last right and disposing, so we can only switch to disposed mode
						else if (_LeftWaiters.IsEmpty)
							NewCounter = LockState.Unlocked; // We're the last right and there are no lefts, so switch to unlocked
						else
							NewCounter = LockState.Left; // We're the last right, and there are lefts, so we should switch to left mode
					}
					else
					{
						Debug.Assert(PreviousCounter <= LockState.Left, "Switch Lock is in an invalid state (Right)");

						if (PreviousCounter < LockState.Left)
							NewCounter = PreviousCounter + 1; // There are still other lefts, so just decrease the active counter and stay in left mode
						else if (_Disposer != null)
							NewCounter = LockState.Disposed; // We're the last left and disposing, so we can only switch to disposed mode
						else if (_RightWaiters.IsEmpty)
							NewCounter = LockState.Unlocked; // We're the last left and there are no rights, so switch to unlocked
						else
							NewCounter = LockState.Right; // We're the last left, and there are rights, so we should switch to right mode
					}

					if (NewCounter == PreviousCounter)
					{
						Debug.Assert(NewCounter == LockState.Right, $"Switch Lock did not change state ({NewCounter})");
						break;
					}
				}
				while (Interlocked.CompareExchange(ref _Counter, NewCounter, PreviousCounter) != PreviousCounter);

				switch (NewCounter)
				{
				case LockState.Unlocked:
					// Switched to unlocked mode
					if (isRight && !_LeftWaiters.IsEmpty)
					{
						// One or more lefts got queued, so try and activate them
						// We only check if this is a right, since if it's a left they should auto-succeeded in unfair mode, and in fair mode it's up to the right that originally blocked them to release, not us
						if (!TryTakeLeft(true) || TryActivateLeft(true))
							return;

						isRight = false;

						continue; // Locked but failed to activate, release
					}

					if (!isRight && !_RightWaiters.IsEmpty)
					{
						// One or more rights got queued, try and activate one
						// We only check if this is a ;eft, since if it's a right they should auto-succeeded in unfair mode, and in fair mode it's up to the left that originally blocked them to release, not us
						if (!TryTakeRight(true) || TryActivateRight())
							return;

						isRight = true;

						continue; // Locked but failed to activate, release
					}

					// No lefts or rights
					return;
				case LockState.Disposed:
					_Disposer!.SwitchToComplete();

					return;

				default:
					if (NewCounter <= LockState.Left)
					{
						if (PreviousCounter < NewCounter)
							return; // We started in left, so no need to check for queued lefts

						// Switching to left mode, which means there was at least one waiter in the queue
						if (TryActivateLeft(true))
							return;

						// Failed to dequeue or activate the waiter (maybe they were cancelled), so we need to release the left lock we're holding on its behalf
						isRight = false;
					}
					else
					{
						if (PreviousCounter > NewCounter)
							return; // We started in right, so no need to check for queued rights

						// Switching to read mode, which means there was at least one waiter in the queue
						if (TryActivateRight(true))
							return;

						// Failed to dequeue or activate the waiter (maybe they were cancelled), so we need to release the read lock we're holding on its behalf
						isRight = true;
					}

					break;
				}
			}
		}

		private bool TryActivateLeft(bool multiple = false)
		{
			// Either we hold a left lock, or we're disposing
			if (_LeftWaiters.TryDequeue(out var Left))
			{
				if (multiple)
				{
					// We're allowed to release more than one waiter
					// Start off with a consumed lock, so we need to take another
					var ConsumedLock = true;

					while (_LeftWaiters.TryDequeue(out var OtherLeft))
					{
						if (ConsumedLock)
							// Need to decrement (left locks decrease) before we switch to completed, because otherwise the left could be scheduled and release the lock we're holding
							Interlocked.Decrement(ref _Counter);

						// Try to activate each waiting left (they may be cancelled)
						ConsumedLock = OtherLeft.TrySwitchToCompleted();
					}

					// If the waiter didn't take the lock, we need to release it again
					if (!ConsumedLock)
						Interlocked.Increment(ref _Counter);
				}

				return Left.TrySwitchToCompleted();
			}

			// Failed to dequeue, so trigger a release
			return false;
		}

		private bool TryActivateRight(bool multiple = false)
		{
			// Either we hold a right lock, or we're disposing
			if (_RightWaiters.TryDequeue(out var Right))
			{
				if (multiple)
				{
					// We're allowed to release more than one right
					// Start off with a consumed lock, so we need to take another
					var ConsumedLock = true;

					while (_RightWaiters.TryDequeue(out var OtherRight))
					{
						if (ConsumedLock)
							// Need to increment (right locks increment) before we switch to completed, because otherwise the waiter could be scheduled and release the lock we're holding
							Interlocked.Increment(ref _Counter);

						// Try to activate each waiting right (they may be cancelled)
						ConsumedLock = OtherRight.TrySwitchToCompleted();
					}

					// If the waiter didn't take the lock, we need to release it again
					if (!ConsumedLock)
						Interlocked.Decrement(ref _Counter);
				}

				return Right.TrySwitchToCompleted();
			}

			// Failed to dequeue, so trigger a release
			return false;
		}

		private void DisposeLeft()
		{
			while (_LeftWaiters.TryDequeue(out var Instance))
				Instance.SwitchToDisposed();
		}

		private void DisposeRight()
		{
			while (_RightWaiters.TryDequeue(out var Instance))
				Instance.SwitchToDisposed();
		}

		//****************************************

		/// <summary>
		/// Gets the number of left waiters queued
		/// </summary>
		public int WaitingLeft => _LeftWaiters.Count;

		/// <summary>
		/// Gets the number of right waiters queued
		/// </summary>
		public int WaitingRight => _RightWaiters.Count;

		/// <summary>
		/// Gets whether any concurrent left operations are in progress
		/// </summary>
		public bool IsLeft
		{
			get
			{
				var Counter = _Counter;

				return Counter <= LockState.Left && Counter != LockState.Disposed;
			}
		}

		/// <summary>
		/// Gets whether any concurrent right operations are in progress
		/// </summary>
		public bool IsRight
		{
			get
			{
				var Counter = _Counter;

				return Counter >= LockState.Right && Counter != LockState.Disposed;
			}
		}

		/// <summary>
		/// Gets if the switch lock favours an unfair algorithm
		/// </summary>
		/// <remarks>In unfair mode, a lock on one side will succeed if it's already held elsewhere, even if there are waiters on the other side</remarks>
		public bool IsUnfair { get; } = false;

		//****************************************

		/// <summary>
		/// Represents the Switch Lock currently held
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

			/// <summary>
			/// Gets whether the Switch Lock is in Left mode
			/// </summary>
			public bool IsLeft => !_Instance.IsRight;

			/// <summary>
			/// Gets whether the Switch Lock is in Right mode
			/// </summary>
			public bool IsRight => _Instance.IsRight;
		}

		private static class LockState
		{
			internal const int Left = -1;
			internal const int Unlocked = 0;
			internal const int Right = 1;
			internal const int Disposed = int.MinValue;
		}
	}
}
