using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading
{
	/// <summary>
	/// Provides extension methods for <see cref="WaitHandle" />
	/// </summary>
	public static class WaitHandleExtensions
	{ //****************************************
		private static readonly ConditionalWeakTable<WaitHandle, AsyncWaitHandle> _AsyncWaiters = new ConditionalWeakTable<WaitHandle, AsyncWaitHandle>();
		//****************************************

		/// <summary>
		/// Performs an asynchronous wait for a <see cref="WaitHandle" />
		/// </summary>
		/// <param name="waitObject">The <see cref="WaitHandle" /> to wait on</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A <see cref="ValueTask" /> that completes when the <see cref="WaitHandle" /> is signalled</returns>
		public static ValueTask WaitAsync(this WaitHandle waitObject, CancellationToken token = default) => CreateWaiter(waitObject, Timeout.InfiniteTimeSpan, token).ToTask();

		/// <summary>
		/// Gets a <see cref="Task" /> used to await this <see cref="WaitHandle" />, with a timeout
		/// </summary>
		/// <param name="waitObject">The <see cref="WaitHandle" /> to wait on</param>
		/// <param name="timeout">The time to wait. Use <see cref="Timeout.Infinite" /> for no timeout (wait forever)</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A <see cref="Task" /> returning true if the <see cref="WaitHandle" /> received a signal, or false if it timed out</returns>
		/// <remarks>This method ensures multiple waits on a single WaitHandle are valid</remarks>
		public static ValueTask<bool> WaitAsync(this WaitHandle waitObject, int timeout, CancellationToken token = default) => CreateWaiter(waitObject, new TimeSpan(timeout * TimeSpan.TicksPerMillisecond), token).ToTimeoutTask();

		/// <summary>
		/// Gets a <see cref="Task" /> used to await this <see cref="WaitHandle" />, with a timeout
		/// </summary>
		/// <param name="waitObject">The <see cref="WaitHandle" /> to wait on</param>
		/// <param name="timeout">The time to wait. Use <see cref="Timeout.Infinite" /> for no timeout (wait forever)</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A <see cref="Task" /> returning true if the <see cref="WaitHandle" /> received a signal, or false if it timed out</returns>
		/// <remarks>This method ensures multiple waits on a single WaitHandle are valid</remarks>
		public static ValueTask<bool> WaitAsync(this WaitHandle waitObject, TimeSpan timeout, CancellationToken token = default) => CreateWaiter(waitObject, timeout, token).ToTimeoutTask();

		//****************************************

		private static AsyncHandleWaiter CreateWaiter(WaitHandle waitObject, TimeSpan timeout, CancellationToken token = default)
		{
			if (waitObject == null)
				throw new ArgumentNullException(nameof(waitObject));

			if (timeout == TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be infinite, or positive");

			// We want to guarantee that only one waiter is associated per WaitHandle.
			var Handle = _AsyncWaiters.GetValue(waitObject, AsyncWaitHandle.Create);

			// GetValue may call GetOrCreate multiple times, so we only register the waiter once we have the final result
			Handle.RegisterWait();

			// To allow each async wait to be cancellable, we need to track them independently
			var Waiter = AsyncHandleWaiter.GetOrCreate(Handle);

			Waiter.ApplyCancellation(timeout, token);

			return Waiter;
		}

		//****************************************

		private sealed class AsyncWaitHandle
		{ //****************************************
			private readonly WaiterQueue<AsyncHandleWaiter> _Waiters = new WaiterQueue<AsyncHandleWaiter>();

			private RegisteredWaitHandle? _Registration;

			private volatile int _HandleState;
			//****************************************

			private AsyncWaitHandle(WaitHandle waitObject)
			{
				WaitObject = waitObject;
			}

			//****************************************

			internal void RegisterWait()
			{
				// Ensure the callback is only registered once
				if (_HandleState != Status.Unregistered || Interlocked.CompareExchange(ref _HandleState, Status.Registered, Status.Unregistered) != Status.Unregistered)
					return;

				_Registration = ThreadPool.RegisterWaitForSingleObject(WaitObject, OnWaitForSingleObject, this, Timeout.Infinite, false);
			}

			internal void Attach(AsyncHandleWaiter waiter) => _Waiters.Enqueue(waiter);

			internal bool Detach(AsyncHandleWaiter waiter) => _Waiters.Erase(waiter);

			//****************************************

			private void CompleteWaiters()
			{
				while (_Waiters.TryDequeue(out var Waiter))
					Waiter.SwitchToCompleted();
			}

			//****************************************

			public WaitHandle WaitObject { get; }

			//****************************************

			internal static AsyncWaitHandle Create(WaitHandle waitObject) => new AsyncWaitHandle(waitObject);

			//****************************************

			private static void OnWaitForSingleObject(object state, bool timedOut) => ((AsyncWaitHandle)state).CompleteWaiters();

			//****************************************

			private static class Status
			{
				internal const int Unregistered = 0;
				internal const int Registered = 1;
			}
		}

		private sealed class AsyncHandleWaiter : BaseCancellable, IValueTaskSource, IValueTaskSource<bool>
		{ //****************************************
			private static readonly ConcurrentBag<AsyncHandleWaiter> Instances = new ConcurrentBag<AsyncHandleWaiter>();
			//****************************************
			private volatile int _InstanceState;

			private ManualResetValueTaskSourceCore<bool> _TaskSource = new ManualResetValueTaskSourceCore<bool>();

			//****************************************

			internal void Initialise(AsyncWaitHandle handle)
			{
				Handle = handle;
			}

			internal void ApplyCancellation(TimeSpan timeout, CancellationToken token)
			{
				RegisterCancellation(token, timeout);
			}

			internal void SwitchToCompleted()
			{

			}


			internal ValueTask ToTask() => new ValueTask(this, _TaskSource.Version);

			internal ValueTask<bool> ToTimeoutTask() => new ValueTask<bool>(this, _TaskSource.Version);

			//****************************************

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			void IValueTaskSource.GetResult(short token)
			{
				try
				{
					_TaskSource.GetResult(token);
				}
				finally
				{
					if (_InstanceState == Status.Completed || !_WaitForCompletion || Interlocked.CompareExchange(ref _InstanceState, Status.CancelledGotResult, Status.Cancelled) == Status.CancelledCompleted)
						Release(); // Completed and not waiting for the callback to execute (or we don't care because it should be unregistered)
				}
			}

			protected override void SwitchToCancelled()
			{
				// TODO: Instance State

				if (!Handle!.Detach(this))
					return; // Couldn't dequeue, so the handle has already activated
			}

			protected override void UnregisteredCancellation()
			{
				throw new NotImplementedException();
			}

			//****************************************

			private void Release()
			{
				Handle = null;

				_TaskSource.Reset();

				Instances.Add(this);
			}

			//****************************************

			public AsyncWaitHandle? Handle { get; private set; }

			//****************************************

			internal static AsyncHandleWaiter GetOrCreate(AsyncWaitHandle handle)
			{
				if (!Instances.TryTake(out var Instance))
					Instance = new AsyncHandleWaiter();

				Instance.Initialise(handle);

				return Instance;
			}
		}
	}
}
