/****************************************\
 AsyncTaskFlag.cs
 Created: 2012-09-13
\****************************************/
using System;
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
		private Func<Task> _Callback;
		
		private TimeSpan _Delay = TimeSpan.Zero;
		private int _State; // 0 if not running, 1 if flagged to run, 2 if running

		private TaskCompletionSource<bool> _WaitTask, _PendingWaitTask;
		
		private WaitCallback _ProcessTaskFlag;
		private Action _CompleteProcessTask;
		//****************************************
		
		private AsyncTaskFlag()
		{
			_ProcessTaskFlag = ProcessTaskFlag;
			_CompleteProcessTask = CompleteProcessTask;
		}
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public AsyncTaskFlag(Func<Task> callback) : this()
		{
			_Callback = callback;
		}
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public AsyncTaskFlag(Func<Task> callback, TimeSpan delay) : this()
		{
			_Callback = callback;
			_Delay = delay;
		}
		
		//****************************************
		
		/// <summary>
		/// Sets the flag, causing the task to run/re-run depending on the status
		/// </summary>
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
		
		private void ProcessTaskFlag(object state)
		{
			if (_Delay != TimeSpan.Zero)
				// Wait a bit before acknowledging the flag
				Thread.Sleep(_Delay);
			
			// Set the processing state to 2, showing we've acknowledged this flag
			Interlocked.Exchange(ref _State, 2);
	
			// Capture any requests to wait for this callback
			_PendingWaitTask = Interlocked.Exchange(ref _WaitTask, null);
			
			// Raise the callback and get an awaiter
			// Using GetAwaiter results in less allocations (TaskContinuation) than using ContinueWith (Task and TaskContinuation)
			var MyResultAwaiter = _Callback().GetAwaiter();
			
			// If the operation finished, run the callback
			if (MyResultAwaiter.IsCompleted)
				CompleteProcessTask(true);
			else
				// Still pending, queue the completion task
				MyResultAwaiter.OnCompleted(_CompleteProcessTask);
		}
		
		private void CompleteProcessTask()
		{
			CompleteProcessTask(false);
		}
		
		private void CompleteProcessTask(bool isNested)
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
					ProcessTaskFlag(null);
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
