/****************************************\
 AsyncTaskFlag.cs
 Created: 2012-09-13
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Implements a callback that will continue to run as long as the flag is Set
	/// </summary>
	public sealed class AsyncTaskFlag
	{	//****************************************
		private readonly Func<Task> _Callback;
		
		private readonly WaitCallback _ProcessTaskFlag;
		private readonly Action _CompleteProcessTask;
		private readonly Action _CompleteSecondTask;
		private readonly TaskScheduler _Scheduler;
		
		private TimeSpan _Delay = TimeSpan.Zero;
		private int _State; // 0 if not running, 1 if flagged to run, 2 if running
		private Timer _DelayTimer;

		private TaskCompletionSource<bool> _WaitTask, _PendingWaitTask;
		
		private Task _CurrentTask;
		//****************************************
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public AsyncTaskFlag(Func<Task> callback) : this(callback, TimeSpan.Zero, TaskScheduler.Current)
		{
		}
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="scheduler">The task scheduler to schedule the callback on</param>
		public AsyncTaskFlag(Func<Task> callback, TaskScheduler scheduler) : this(callback, TimeSpan.Zero, scheduler)
		{
		}
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public AsyncTaskFlag(Func<Task> callback, TimeSpan delay) : this(callback, delay, TaskScheduler.Current)
		{
		}
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		/// <param name="scheduler">The task scheduler to schedule the callback on</param>
		public AsyncTaskFlag(Func<Task> callback, TimeSpan delay, TaskScheduler scheduler)
		{
			_Callback = callback;
			_Scheduler = scheduler;
			
			if (delay == TimeSpan.Zero)
			{
				_ProcessTaskFlag = ProcessTaskFlag;
			}
			else
			{
				_Delay = delay;
				_ProcessTaskFlag = ProcessDelayTaskFlag;
				_DelayTimer = new Timer(ProcessTaskFlag);
			}
			
			_CompleteProcessTask = CompleteProcessTask;
			_CompleteSecondTask = CompleteSecondTask;
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the asynchronous task flag
		/// </summary>
		public void Dispose()
		{
			var MyTimer = Interlocked.Exchange(ref _DelayTimer, null);
			
			if (MyTimer != null)
				MyTimer.Dispose();
		}
		
		//****************************************
		
		/// <summary>
		/// Sets the flag, causing the task to run/re-run depending on the status
		/// </summary>
		[SecuritySafeCritical]
		public void Set()
		{
			// Set the state to 1 (flagged)
			if (Interlocked.Exchange(ref _State, 1) == 0)
			{
				// If the previous state was 0 (not flagged), we need to start it executing
				ThreadPool.UnsafeQueueUserWorkItem(_ProcessTaskFlag, null);
			}
			
			// If the previous state was 1 (flagged) or 2 (executing), then ProcessTaskFlag will take care of it
		}
		
		/// <summary>
		/// Sets the flag and waits for the callback to run at least once.
		/// </summary>
		/// <returns>A task that will complete once the callback has run</returns>
		public Task SetAndWait()
		{
			var MyWaitTask = _WaitTask;
			
			// Has someone else already called SetAndWait but the callback hasn't captured it?
			if (MyWaitTask != null)
				return _WaitTask.Task; // Yes, piggyback off that task
			
			MyWaitTask = new TaskCompletionSource<bool>();
			
			// Try and assign a new task
			var NewWaitTask = Interlocked.CompareExchange(ref _WaitTask, MyWaitTask, null);
			
			// If we got pre-empted, piggyback of the other task
			if (NewWaitTask != null)
				return NewWaitTask.Task;
			
			// Set the flag so our callback runs
			// Our task may actually be completed at this point, if ProcessTaskFlag executes on another thread between now and the previous CompareExchange
			Set();
			
			return MyWaitTask.Task;
		}
		
		//****************************************
		
		private void ProcessDelayTaskFlag(object state)
		{
			var MyTimer = _DelayTimer;

			// Ensure we're not disposing
			if (MyTimer != null)
				// Wait a bit before acknowledging the flag
				_DelayTimer.Change(_Delay, new TimeSpan(0, 0, 0, 0, -1));
		}
		
		private void ProcessTaskFlag(object state)
		{
			// Set the processing state to 2, showing we've acknowledged this flag
			Interlocked.Exchange(ref _State, 2);
	
			// Capture any requests to wait for this callback
			_PendingWaitTask = Interlocked.Exchange(ref _WaitTask, null);
			
			// Raise the callback and get an awaiter
			var MyResult = Task.Factory.StartNew(_Callback, CancellationToken.None, TaskCreationOptions.None, _Scheduler);
			
			_CurrentTask = MyResult;
			
			// Using GetAwaiter results in less allocations (TaskContinuation) than using ContinueWith (Task and TaskContinuation)
			if (MyResult.IsCompleted)
				CompleteProcessTask(true);
			else
				MyResult.ConfigureAwait(false).GetAwaiter().OnCompleted(_CompleteProcessTask);
		}
		
		private void CompleteProcessTask()
		{
			CompleteProcessTask(false);
		}
		
		private void CompleteProcessTask(bool isNested)
		{	//****************************************
			var FirstTask = (Task<Task>)_CurrentTask;
			//****************************************
			
			if (FirstTask.IsFaulted)
			{
				CompleteSecondTask(isNested);
			}
			else if (FirstTask.IsCanceled)
			{
				CompleteSecondTask(isNested);
			}
			else
			{
				_CurrentTask = FirstTask.Result;
				
				// Is it complete?
				if (_CurrentTask.IsCompleted)
					CompleteSecondTask(true);
				else
					// No, wait for it then
					_CurrentTask.ConfigureAwait(false).GetAwaiter().OnCompleted(_CompleteSecondTask); // 2 (TaskCompletion and Action)
			}
		}
		
		private void CompleteSecondTask()
		{
			CompleteSecondTask(false);
		}

		[SecuritySafeCritical]
		private void CompleteSecondTask(bool isNested)
		{	//****************************************
			var MyWaitTask = Interlocked.Exchange(ref _PendingWaitTask, null);
			//****************************************
			
			// If we captured a wait task, set it
			if (MyWaitTask != null)
				MyWaitTask.SetResult(true);
			
			// Set the state to 0 (not flagged) if it's 2 (executing)
			if (Interlocked.CompareExchange(ref _State, 0, 2) == 1)
			{
				// Queue the callback again if the previous value is 1 (flagged)
				if (isNested)
				{
					// We're called directly from ProcessTaskFlag, so call it again on the ThreadPool rather than risking blowing the stack
					ThreadPool.UnsafeQueueUserWorkItem(_ProcessTaskFlag, null);
				}
				else
				{
					// Called from the Awaiter completion, so it's safe to call back to ProcessTaskFlag
					// Use the delegate, since we may be raising the timer
					_ProcessTaskFlag(null);
				}
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets a delay to apply before executing the callback
		/// </summary>
		/// <remarks>Acts as a cheap batching mechanism, so rapid calls to Set do not execute the callback twice</remarks>
		public TimeSpan Delay
		{
			get { return _Delay; }
		}
	}
}
#endif