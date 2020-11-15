using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Proximity.Threading;
//****************************************

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Provides the functionality for AsyncCounter.DecrementAny
	/// </summary>
	internal sealed class AsyncCollectionTakeAny<T> : BaseCancellable, IValueTaskSource<CollectionTakeResult<T>>
	{ //****************************************
		private static readonly ConcurrentBag<AsyncCollectionTakeAny<T>> Instances = new ConcurrentBag<AsyncCollectionTakeAny<T>>();
		private static readonly ConcurrentBag<PeekTakeWaiter> Waiters = new ConcurrentBag<PeekTakeWaiter>();
		//****************************************
		private int _InstanceState;
		private int _IsStarted;

		private ManualResetValueTaskSourceCore<CollectionTakeResult<T>> _TaskSource = new ManualResetValueTaskSourceCore<CollectionTakeResult<T>>();

		private AsyncCollection<T>? _SourceCollection;
		private int _OutstandingCounters;
		//****************************************

		private AsyncCollectionTakeAny() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		internal void Initialise(CancellationToken token, TimeSpan timeout)
		{
			_InstanceState = Status.Pending;

			RegisterCancellation(token, timeout);
		}

		internal void Attach(AsyncCollection<T> collection)
		{
			if (!collection.TryPeekTake(Token, out var Decrement))
				return; // Collection is completed, ignore it

			var Waiter = PeekTakeWaiter.GetOrCreate(this, collection);

			Interlocked.Increment(ref _OutstandingCounters);

			Waiter.Attach(Decrement);
		}

		internal void Activate()
		{
			Interlocked.Exchange(ref _IsStarted, 1);

			if (_InstanceState != Status.Pending)
				return;

			if (Volatile.Read(ref _OutstandingCounters) == 0)
			{
				// All the counters have activated, but we're still pending, which means they're all disposed
				TrySwitchToDisposed();

				return;
			}
		}

		//****************************************

		ValueTaskSourceStatus IValueTaskSource<CollectionTakeResult<T>>.GetStatus(short token) => _TaskSource.GetStatus(token);

		void IValueTaskSource<CollectionTakeResult<T>>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		CollectionTakeResult<T> IValueTaskSource<CollectionTakeResult<T>>.GetResult(short token)
		{
			try
			{
				return _TaskSource.GetResult(token);
			}
			finally
			{
				// We've received our result, mark as such
				if (Interlocked.CompareExchange(ref _InstanceState, Status.GotResult, Status.Waiting) != Status.Waiting)
					throw new InvalidOperationException("Collection Take Any is in an invalid state");

				// If there are no more counters we're attached to, try and release us
				if (Volatile.Read(ref _OutstandingCounters) == 0)
					TryRelease();
			}
		}

		//****************************************

		protected override void SwitchToCancelled()
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return; // Too late, we already have a result

			_TaskSource.SetException(CreateCancellationException());
		}

		protected override void UnregisteredCancellation()
		{
			if (_SourceCollection == null)
			{
				_TaskSource.SetException(new InvalidOperationException("All collections are completed"));
			}
			else
			{
				var Item = _SourceCollection.CompleteTake();

				_TaskSource.SetResult(new CollectionTakeResult<T>(_SourceCollection, Item));
			}
		}

		private bool TrySwitchToDisposed()
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return false; // Too late, we already have a result

			_SourceCollection = null;

			UnregisterCancellation();

			return true;
		}

		private void TryRelease()
		{
			if (_InstanceState == Status.GotResult || Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.GotResult) != Status.GotResult)
				return; // We're waiting on GetResult to execute

			ResetCancellation();

			_IsStarted = 0;
			_SourceCollection = null;
			_TaskSource.Reset();

			Instances.Add(this);
		}

		private void OnPeekCompleted(AsyncCollection<T> collection)
		{
			// Can be one of: Pending, Waiting, GotResult
			if (_InstanceState == Status.Pending)
			{
				if (collection.TryReserveTake())
				{
					// State can be one of: Pending, Waiting, GotResult
					// Try to switch from Pending to Waiting
					if (_InstanceState == Status.Pending && Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) == Status.Pending)
					{
						_SourceCollection = collection;

						UnregisteredCancellation();

						return; // Success, no need to try and release since we'll do that after GetResult
					}

					// We can fail to switch to completion if we're cancelled - in which case we should return the counter we just took
					collection.ReleaseTake();
				}
				else
				{
					// We failed to decrement this counter, but we're still pending, so try again
					if (collection.TryPeekTake(Token, out var Decrement))
					{
						var Waiter = PeekTakeWaiter.GetOrCreate(this, collection);

						Waiter.Attach(Decrement);

						// NOTE: If we implement early-clear on the queue, we could accidentally revive the object here

						return;
					}

					// Counter is disposed, fail
					OnPeekFailed();

					return;
				}
			}

			// If we're the last peek waiter, try and release
			if (Interlocked.Decrement(ref _OutstandingCounters) <= 0)
				TryRelease();
		}

		private void OnPeekFailed()
		{
			if (Interlocked.Decrement(ref _OutstandingCounters) > 0)
				return; // There are other counters still waiting

			if (Volatile.Read(ref _IsStarted) == 0)
				return; // Still adding counters

			if (TrySwitchToDisposed())
				return; // Dispose
		}

		//****************************************

		public short Version => _TaskSource.Version;

		//****************************************

		internal static AsyncCollectionTakeAny<T> GetOrCreate(CancellationToken token, TimeSpan timeout)
		{
			if (!Instances.TryTake(out var Instance))
				Instance = new AsyncCollectionTakeAny<T>();

			Instance.Initialise(token, timeout);

			return Instance;
		}

		//****************************************

		private sealed class PeekTakeWaiter
		{ //****************************************
			private readonly Action _ContinuePeekDecrement;

			private AsyncCollectionTakeAny<T>? _Operation;
			private AsyncCollection<T>? _Collection;

			private ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			public PeekTakeWaiter()
			{
				_ContinuePeekDecrement = OnContinuePeekDecrement;
			}

			//****************************************

			public void Attach(AsyncCounterDecrement decrement)
			{
				var Task = new ValueTask<bool>(decrement, decrement.Version);

				_Awaiter = Task.ConfigureAwait(false).GetAwaiter();

				if (_Awaiter.IsCompleted)
					OnContinuePeekDecrement();
				else
					_Awaiter.OnCompleted(_ContinuePeekDecrement);
			}

			//****************************************

			private void OnContinuePeekDecrement()
			{
				try
				{
					if (_Awaiter.GetResult())
						_Operation!.OnPeekCompleted(_Collection!);
					else
						_Operation!.OnPeekFailed();
				}
				catch
				{
					_Operation!.OnPeekFailed();
				}
				finally
				{
					Release();
				}
			}

			private void Release()
			{
				_Operation = null;
				_Collection = null;
				_Awaiter = default;

				Waiters.Add(this);
			}

			//****************************************

			internal static PeekTakeWaiter GetOrCreate(AsyncCollectionTakeAny<T> operation, AsyncCollection<T> collection)
			{
				if (!Waiters.TryTake(out var Waiter))
					Waiter = new PeekTakeWaiter();

				Waiter._Operation = operation;
				Waiter._Collection = collection;

				return Waiter;
			}
		}

		private static class Status
		{
			/// <summary>
			/// A decrement-any starts in the Unused state
			/// </summary>
			internal const int Unused = 0;
			/// <summary>
			/// The decrement-any is waiting for a counter to decrement
			/// </summary>
			internal const int Pending = 1;
			/// <summary>
			/// The decrement has been applied/cancelled/disposed, and waiting for GetResult
			/// </summary>
			internal const int Waiting = 2;
			/// <summary>
			/// The decrement has been applied/cancelled/disposed, but is still attached to one or more counters
			/// </summary>
			internal const int GotResult = 3;
		}
	}
}
