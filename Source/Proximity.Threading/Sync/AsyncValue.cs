using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proximity.Threading.Sync
{
	public sealed class AsyncValue<T> : IDisposable
	{ //****************************************
		private readonly WaiterQueue<AsyncValueInstance> _Waiters = new WaiterQueue<AsyncValueInstance>();
		private StrongBox<T>? _State;
		private readonly IEqualityComparer<T> _Comparer;
		//****************************************

		public AsyncValue(T initialValue)
		{
			_State = new StrongBox<T>(initialValue);
			_Comparer = EqualityComparer<T>.Default;
		}

		public AsyncValue(T initialValue, IEqualityComparer<T> comparer)
		{
			_Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			_State = new StrongBox<T>(initialValue);
		}

		//****************************************

		/// <summary>
		/// Disposes of the event
		/// </summary>
		/// <remarks>All waits on the event will throw <see cref="ObjectDisposedException"/></remarks>
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _State, null) != null)
			{
				// Success, now close any pending waiters
				while (_Waiters.TryDequeue(out var Instance))
					Instance.SwitchToDisposed();
			}
		}

		/// <summary>
		/// Changes the value and releases any waiters
		/// </summary>
		/// <exception cref="ObjectDisposedException">The value has been disposed</exception>
		public void Set(T value)
		{
			StrongBox<T>? OldState, NewState = null;

			do
			{
				OldState = _State;

				if (OldState == null)
					throw new ObjectDisposedException(nameof(AsyncValue<T>), "Value has been disposed of");

				if (_Comparer.Equals(OldState.Value, value))
					return; // No change

				if (NewState == null)
					NewState = new StrongBox<T>(value);
			}
			while (Interlocked.CompareExchange(ref _State, NewState, OldState) != OldState);

			// We set the value, release any waiters
			ReleaseAll();
		}

		/// <summary>
		/// Waits for the value to change
		/// </summary>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that returns True when the value has changed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<bool> Wait(CancellationToken token = default) => Wait(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Waits for the value to change
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the event to be set</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that returns True when the value has changed, or False if the timeout elapsed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		/// <exception cref="TimeoutException">The timeout elapsed</exception>
		public ValueTask<bool> Wait(TimeSpan timeout, CancellationToken token = default)
		{
			if (_State == null)
				throw new ObjectDisposedException(nameof(AsyncValue<T>), "Value has been disposed of");

			var Instance = AsyncValueInstance.GetOrCreate(this);

			var ValueTask = new ValueTask<bool>(Instance, Instance.Version);

			Instance.ApplyCancellation(token, timeout);

			// Not set, add ourselves to the queue waiting
			_Waiters.Enqueue(Instance);

			return ValueTask;
		}

		/// <summary>
		/// Checks if the value is equal to the desired value and returns immediately, otherwise waits
		/// </summary>
		/// <param name="value">The value to check for</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that returns True when the value has changed</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<bool> WaitUnless(T value, CancellationToken token = default) => WaitUnless(value, Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Waits for the event to become set, or if set return immediately
		/// </summary>
		/// <param name="value">The value to check for</param>
		/// <param name="timeout">The amount of time to wait for the event to be set</param>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A task that completes when the event has been set</returns>
		/// <exception cref="OperationCanceledException">The given cancellation token was cancelled</exception>
		public ValueTask<bool> WaitUnless(T value, TimeSpan timeout, CancellationToken token = default)
		{
			if (_Comparer.Equals(value, Value))
				return new ValueTask<bool>(true);

			var Instance = AsyncValueInstance.GetOrCreate(this);

			var ValueTask = new ValueTask<bool>(Instance, Instance.Version);

			if (!ValueTask.IsCompleted)
			{
				Instance.ApplyCancellation(token, timeout);

				// Not set, add ourselves to the queue waiting
				_Waiters.Enqueue(Instance);

				// Did the event become set while we were busy?
				var State = _State;

				if (State == null)
				{
					_Waiters.Erase(Instance);

					Instance.SetDisposed();
				}
				else if (_Comparer.Equals(value, State.Value))
				{
					_Waiters.Erase(Instance);

					Instance.SetCompleted();
				}
			}

			return ValueTask;
		}

		//****************************************

		/// <summary>
		/// Gets the current value
		/// </summary>
		public T Value
		{
			get
			{
				var State = _State;

				if (State == null)
					throw new ObjectDisposedException(nameof(AsyncValue<T>), "Value has been disposed of");

				return State.Value;
			}
		}

		/// <summary>
		/// Gets the approximate number of waiters on the value
		/// </summary>
		public int WaitingCount => _Waiters.Count;

		//****************************************

	}
}
