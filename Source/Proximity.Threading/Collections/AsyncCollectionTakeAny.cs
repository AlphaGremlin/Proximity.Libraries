using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
//****************************************

namespace System.Collections.Concurrent
{
	/// <summary>
	/// Provides the functionality for AsyncCounter.DecrementAny
	/// </summary>
	internal sealed class AsyncCollectionTakeAny<TItem> : IValueTaskSource<CollectionTakeResult<TItem>>
	{ //****************************************
		private static readonly ConcurrentBag<AsyncCollectionTakeAny<TItem>> _Instances = new ConcurrentBag<AsyncCollectionTakeAny<TItem>>();
		private static readonly ConcurrentBag<PeekTakeWaiter> _Waiters = new ConcurrentBag<PeekTakeWaiter>();
		//****************************************
		private int _InstanceState;
		private int _IsStarted;

		private ManualResetValueTaskSourceCore<CollectionTakeResult<TItem>> _TaskSource = new ManualResetValueTaskSourceCore<CollectionTakeResult<TItem>>();

		private CancellationTokenRegistration _Registration;
		private CancellationTokenSource? _TokenSource;

		private int _OutstandingCounters;
		//****************************************

		private AsyncCollectionTakeAny() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		internal void Initialise(CancellationToken token)
		{
			_InstanceState = Status.Pending;

			Token = token;
		}

		internal void Attach(AsyncCollection<TItem> collection)
		{
			if (!collection.TryPeekTake(Token, out var Decrement))
				return; // Collection is completed, ignore it

			var Waiter = PeekTakeWaiter.GetOrCreate(this, collection, Decrement);

			Interlocked.Increment(ref _OutstandingCounters);

			Waiter.Attach(Decrement);
		}

		internal void ApplyCancellation(CancellationTokenSource? tokenSource)
		{
			Interlocked.Exchange(ref _IsStarted, 1);

			if (_InstanceState != Status.Pending)
				return; // No need to do anything with cancellation, because we've already decremented/disposed

			if (Volatile.Read(ref _OutstandingCounters) == 0)
			{
				// All the counters have activated, but we're still pending, which means they're all disposed
				TrySwitchToDisposed();

				return;
			}

			if (Token.CanBeCanceled)
			{
				_TokenSource = tokenSource;
				_Registration = Token.Register((state) => ((AsyncCollectionTakeAny<TItem>)state).SwitchToCancelled(), this);
			}
		}

		//****************************************

		ValueTaskSourceStatus IValueTaskSource<CollectionTakeResult<TItem>>.GetStatus(short token) => _TaskSource.GetStatus(token);

		void IValueTaskSource<CollectionTakeResult<TItem>>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		CollectionTakeResult<TItem> IValueTaskSource<CollectionTakeResult<TItem>>.GetResult(short token)
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

		private void SwitchToCancelled()
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return; // Too late, we already have a result

			_Registration.Dispose();
			_TaskSource.SetException(new OperationCanceledException(Token));
		}

		private bool TrySwitchToDisposed()
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return false; // Too late, we already have a result

			_Registration.Dispose();
			//_TaskSource.SetResult(default);
			_TaskSource.SetException(new InvalidOperationException("All collections are completed"));

			return true;
		}

		private void TryRelease()
		{
			if (_InstanceState == Status.GotResult || Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.GotResult) != Status.GotResult)
				return; // We're waiting on GetResult to execute

			_IsStarted = 0;
			_Registration.Dispose();
			_TokenSource?.Dispose();
			_TokenSource = null;
			_TaskSource.Reset();
			Token = default;

			_Instances.Add(this);
		}

		private void OnPeekCompleted(AsyncCollection<TItem> collection)
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
						var Item = collection.CompleteTake();

						_Registration.Dispose();
						_TaskSource.SetResult(new CollectionTakeResult<TItem>(collection, Item));

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
						var Waiter = PeekTakeWaiter.GetOrCreate(this, collection, Decrement);

						Waiter.Attach(Decrement);

						// NOTE: If we implement early-clear on the queue, we could accidentally revive the object here

						return;
					}

					// Counter is disposed, fail
					OnPeekFailed(collection);

					return;
				}
			}

			// If we're the last peek waiter, try and release
			if (Interlocked.Decrement(ref _OutstandingCounters) <= 0)
				TryRelease();
		}

		private void OnPeekFailed(AsyncCollection<TItem> collection)
		{
			if (Interlocked.Decrement(ref _OutstandingCounters) > 0)
				return; // There are other counters still waiting

			if (Volatile.Read(ref _IsStarted) == 0)
				return; // Still adding counters

			if (TrySwitchToDisposed())
				return; // Dispose
		}

		//****************************************

		public CancellationToken Token { get; private set; }

		public short Version => _TaskSource.Version;

		//****************************************

		internal static AsyncCollectionTakeAny<TItem> GetOrCreate(CancellationToken token)
		{
			if (!_Instances.TryTake(out var Instance))
				Instance = new AsyncCollectionTakeAny<TItem>();

			Instance.Initialise(token);

			return Instance;
		}

		//****************************************

		private sealed class PeekTakeWaiter
		{ //****************************************
			private readonly Action _ContinuePeekDecrement;

			private AsyncCollectionTakeAny<TItem>? _Operation;
			private AsyncCollection<TItem>? _Collection;
			private AsyncCounterDecrement? _Decrement;

			private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			public PeekTakeWaiter()
			{
				_ContinuePeekDecrement = OnContinuePeekDecrement;
			}

			//****************************************

			public void Attach(AsyncCounterDecrement decrement)
			{
				_Decrement = decrement;

				var Task = new ValueTask(decrement, decrement.Version);

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
					_Awaiter.GetResult();

					_Operation!.OnPeekCompleted(_Collection!);
				}
				catch
				{
					_Operation!.OnPeekFailed(_Collection!);
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

				_Waiters.Add(this);
			}

			//****************************************

			internal static PeekTakeWaiter GetOrCreate(AsyncCollectionTakeAny<TItem> operation, AsyncCollection<TItem> collection, AsyncCounterDecrement decrement)
			{
				if (!_Waiters.TryTake(out var Waiter))
					Waiter = new PeekTakeWaiter();

				Waiter._Operation = operation;
				Waiter._Collection = collection;
				Waiter._Decrement = decrement;

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
