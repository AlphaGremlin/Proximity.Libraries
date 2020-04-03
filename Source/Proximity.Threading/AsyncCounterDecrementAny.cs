using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Provides the functionality for AsyncCounter.DecrementAny
	/// </summary>
	internal sealed class AsyncCounterDecrementAny : IValueTaskSource<AsyncCounter>
	{ //****************************************
		private static readonly ConcurrentBag<AsyncCounterDecrementAny> _Instances = new ConcurrentBag<AsyncCounterDecrementAny>();
		private static readonly ConcurrentBag<PeekDecrementWaiter> _Waiters = new ConcurrentBag<PeekDecrementWaiter>();
		//****************************************
		private int _InstanceState;
		private int _IsStarted;

		private ManualResetValueTaskSourceCore<AsyncCounter> _TaskSource = new ManualResetValueTaskSourceCore<AsyncCounter>();

		private CancellationTokenRegistration _Registration;
		private CancellationTokenSource? _TokenSource;

		private int _OutstandingCounters;
		//****************************************

		internal AsyncCounterDecrementAny() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		internal void Initialise()
		{
			_InstanceState = Status.Pending;
		}

		internal void Attach(AsyncCounter counter)
		{
			if (!counter.TryPeekDecrement(Token, out var Decrement))
				return; // Counter is disposed, ignore it

			var Waiter = PeekDecrementWaiter.GetOrCreate(this, Decrement);

			Interlocked.Increment(ref _OutstandingCounters);

			Waiter.Attach(Decrement);
		}

		internal void ApplyCancellation(CancellationToken token, CancellationTokenSource? tokenSource)
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

			if (token.CanBeCanceled)
			{
				Token = token;

				_TokenSource = tokenSource;
				_Registration = token.Register((state) => ((AsyncCounterDecrementAny)state).SwitchToCancelled(), this);
			}
		}

		//****************************************

		ValueTaskSourceStatus IValueTaskSource<AsyncCounter>.GetStatus(short token) => _TaskSource.GetStatus(token);

		void IValueTaskSource<AsyncCounter>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		AsyncCounter IValueTaskSource<AsyncCounter>.GetResult(short token)
		{
			try
			{
				return _TaskSource.GetResult(token);
			}
			finally
			{
				// We've received our result, mark as such
				if (Interlocked.CompareExchange(ref _InstanceState, Status.GotResult, Status.Waiting) != Status.Waiting)
					throw new InvalidOperationException("Counter Decrement Any is in an invalid state");

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

		private bool TrySwitchToCompleted(AsyncCounter counter)
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return false; // Too late, we already have a result

			_Registration.Dispose();
			_TaskSource.SetResult(counter);

			return false;
		}

		private bool TrySwitchToDisposed()
		{
			// State can be one of: Pending, Waiting, GotResult
			// Try to switch from Pending to Waiting
			if (_InstanceState != Status.Pending || Interlocked.CompareExchange(ref _InstanceState, Status.Waiting, Status.Pending) != Status.Pending)
				return false; // Too late, we already have a result

			_Registration.Dispose();
			_TaskSource.SetException(new ObjectDisposedException(nameof(AsyncCounterDecrementAny), "All counters have been disposed"));

			return false;
		}

		private void TryRelease()
		{
			if (_InstanceState == Status.GotResult || Interlocked.CompareExchange(ref _InstanceState, Status.Unused, Status.GotResult) != Status.GotResult)
				return; // We're waiting on GetResult to execute


		}

		private void OnPeekCompleted(AsyncCounter counter)
		{
			// Can be one of: Pending, Waiting, GotResult
			if (_InstanceState == Status.Pending)
			{
				if (counter.TryDecrement())
				{
					if (TrySwitchToCompleted(counter))
						return; // Success, no need to try and release since we'll do that after GetResult

					// We can fail to switch to completion if we're cancelled - in which case we should return the counter we just took
					counter.ForceIncrement();
				}
				else
				{
					// We failed to decrement this counter, but we're still pending, so try again
					if (counter.TryPeekDecrement(Token, out var Decrement))
					{
						var Waiter = PeekDecrementWaiter.GetOrCreate(this, Decrement);

						Waiter.Attach(Decrement);

						// NOTE: If we implement early-clear on the queue, we could accidentally revive the object here

						return;
					}

					// Counter is disposed, fail
					OnPeekFailed(counter);

					return;
				}
			}

			// If we're the last peek waiter, try and release
			if (Interlocked.Decrement(ref _OutstandingCounters) <= 0)
				TryRelease();
		}

		private void OnPeekFailed(AsyncCounter counter)
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

		internal static AsyncCounterDecrementAny GetOrCreate()
		{
			if (!_Instances.TryTake(out var Instance))
				Instance = new AsyncCounterDecrementAny();

			Instance.Initialise();

			return Instance;
		}

		//****************************************

		private sealed class PeekDecrementWaiter
		{ //****************************************
			private readonly Action _ContinuePeekDecrement;

			private AsyncCounterDecrementAny? _Operation;
			private AsyncCounterDecrement? _Decrement;

			private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _Awaiter;
			//****************************************

			public PeekDecrementWaiter()
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

					_Operation!.OnPeekCompleted(_Decrement!.Owner!);
				}
				catch
				{
					_Operation!.OnPeekFailed(_Decrement!.Owner!);
				}
				finally
				{
					Release();
				}
			}

			private void Release()
			{
				_Operation = null;
				_Awaiter = default;

				_Waiters.Add(this);
			}

			//****************************************

			internal static PeekDecrementWaiter GetOrCreate(AsyncCounterDecrementAny operation, AsyncCounterDecrement decrement)
			{
				if (!_Waiters.TryTake(out var Waiter))
					Waiter = new PeekDecrementWaiter();

				Waiter._Operation = operation;
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
