using System;
using System.Security;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace System.Threading
{
	/// <summary>
	/// Manages execution of a callback on the ThreadPool based on a flag being set
	/// </summary>
	public sealed class ActionFlag : IDisposable, IAsyncDisposable
	{ //****************************************
		private readonly Func<ValueTask> _Callback;

		private readonly AsyncCounter _Counter;

		private readonly ValueTask _RunLoop;

		private TaskCompletionSource<VoidStruct>? _WaitTask;
		//****************************************

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public ActionFlag(Action callback) : this(WrapCallback(callback), TimeSpan.Zero)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public ActionFlag(Action callback, TimeSpan delay) : this(WrapCallback(callback), delay)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public ActionFlag(Func<Task> callback) : this(WrapCallback(callback), TimeSpan.Zero)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public ActionFlag(Func<Task> callback, TimeSpan delay) : this(WrapCallback(callback), delay)
		{
		}

		/// <summary>
		/// Creates an Action Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public ActionFlag(Func<ValueTask> callback) : this(callback, TimeSpan.Zero)
		{
		}

		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public ActionFlag(Func<ValueTask> callback, TimeSpan delay)
		{
			if (delay < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(delay));

			Delay = delay;

			_Counter = new AsyncCounter();
			_Callback = callback;
			_RunLoop = OnActionFlag();
		}

		//****************************************

		void IDisposable.Dispose() => _ = DisposeAsync();

		/// <summary>
		/// Disposes of the asynchronous task flag
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			await _Counter.DisposeAsync();

			await _RunLoop;
		}

		//****************************************

		/// <summary>
		/// Sets the flag, causing the task to run/re-run depending on the status
		/// </summary>
		public void Set()
		{
			_Counter.Increment();
		}

		/// <summary>
		/// Sets the flag and waits for the callback to run at least once.
		/// </summary>
		/// <returns>A task that will complete once the callback has run</returns>
		public Task SetAndWait()
		{
			var CurrentWaitTask = Volatile.Read(ref _WaitTask);

			// Has someone else already called SetAndWait but the callback hasn't run yet?
			if (CurrentWaitTask == null)
			{
				CurrentWaitTask = new TaskCompletionSource<VoidStruct>();

				// Try and assign a new task
				var NewWaitTask = Interlocked.CompareExchange(ref _WaitTask, CurrentWaitTask, null);

				// If we got pre-empted, piggyback of the other task
				if (NewWaitTask != null)
					return NewWaitTask.Task;

				// Set the flag so our callback runs
				// Our task may actually be completed at this point, if ProcessTaskFlag executes on another thread between now and the previous CompareExchange
				Set();
			}

			return CurrentWaitTask.Task;
		}

		//****************************************

		private async ValueTask OnActionFlag()
		{
			for (; ; )
			{
				try
				{
					await _Counter.Decrement();
				}
				catch (ObjectDisposedException)
				{
					break;
				}

				if (Delay > TimeSpan.Zero)
					await Task.Delay(Delay);

				_Counter.TryDecrementToZero(out _);

				// Capture any requests to wait for this callback
				var PendingWaitTask = Interlocked.Exchange(ref _WaitTask, null);

				try
				{
					await _Callback();
				}
				catch (Exception)
				{
					if (!SwallowExceptions)
						throw;
				}

				// If we captured a wait task, set it
				PendingWaitTask?.SetResult(default);
			}
		}

		//****************************************

		/// <summary>
		/// Gets/Sets a delay to apply before executing the callback
		/// </summary>
		/// <remarks>Acts as a cheap batching mechanism, so rapid calls to Set do not execute the callback twice</remarks>
		public TimeSpan Delay { get; set; } = TimeSpan.Zero;

		/// <summary>
		/// Gets/Sets whether to swallow exceptions returned by the callback, or leave them to trigger an UnhandledException
		/// </summary>
		public bool SwallowExceptions { get; set; }

		//****************************************

		private static Func<ValueTask> WrapCallback(Func<Task> callback) => () => new ValueTask(callback());

		private static Func<ValueTask> WrapCallback(Action callback) => () => { callback(); return default; };
	}
}
