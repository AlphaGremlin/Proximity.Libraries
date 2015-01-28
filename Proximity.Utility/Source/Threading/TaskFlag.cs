/****************************************\
 TaskFlag.cs
 Created: 2012-09-13
\****************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Implements a callback that will continue to run as long as the flag is Set
	/// </summary>
	public sealed class TaskFlag
	{	//****************************************
		private readonly Action _Callback;
		private readonly WaitCallback _ProcessTaskFlag;
		
		private TimeSpan _Delay = TimeSpan.Zero;
		private int _State; // 0 if not running, 1 if flagged to run, 2 if running
		private Timer _DelayTimer;

		private TaskCompletionSource<bool> _WaitTask;
		//****************************************
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public TaskFlag(Action callback)
		{
			_Callback = callback;
			_ProcessTaskFlag = ProcessTaskFlag;
		}
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		/// <param name="delay">A fixed delay between callback executions</param>
		public TaskFlag(Action callback, TimeSpan delay)
		{
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
			
			_Callback = callback;
		}
		
		//****************************************
		
		/// <summary>
		/// Sets the flag, causing the callback to run/re-run depending on the status
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
		
		private void ProcessDelayTaskFlag(object state)
		{
			// Wait a bit before acknowledging the flag
			_DelayTimer.Change(_Delay, Timeout.InfiniteTimeSpan);
		}
		
		private void ProcessTaskFlag(object state)
		{
			// Set the processing state to 2, showing we've acknowledged this flag
			Interlocked.Exchange(ref _State, 2);
	
			// Capture any requests to wait for this callback
			var MyWaitTask = Interlocked.Exchange(ref _WaitTask, null);
			
			_Callback();
			
			// If we captured a wait task, set it
			if (MyWaitTask != null)
				MyWaitTask.SetResult(true);
			
			// Set the state to 0 (not flagged) if it's 2 (executing)
			if (Interlocked.CompareExchange(ref _State, 0, 2) == 1)
			{
				// Queue the callback again if the previous value is 1 (flagged)
				
				// We don't just use a while loop so we don't hold the ThreadPool thread
				ThreadPool.UnsafeQueueUserWorkItem(_ProcessTaskFlag, null);
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