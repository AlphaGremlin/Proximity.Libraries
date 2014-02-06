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
		private Action _Callback;
		
		private readonly object _LockObject = new object();
		
		private volatile bool _IsSet;
		private volatile bool _IsExecuting;
		private TaskCompletionSource<bool> _WaitTask;
		//****************************************
		
		/// <summary>
		/// Creates a Task Flag
		/// </summary>
		/// <param name="callback">The callback to execute</param>
		public TaskFlag(Action callback)
		{
			_Callback = callback;
		}
		
		//****************************************
		
		/// <summary>
		/// Sets the flag, causing the task to run/re-run depending on the status
		/// </summary>
		public void Set()
		{
			if (_IsSet)
				return;
			
			lock (_LockObject)
			{
				_IsSet = true;
				
				// If the callback is already executing, don't bother starting it again
				if (_IsExecuting)
					return;
				
				_IsExecuting = true;
			}
			
			ThreadPool.UnsafeQueueUserWorkItem(ProcessTaskFlag, null);
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
			Set();
			
			return MyWaitTask.Task;
		}
		
		//****************************************
		
		private void ProcessTaskFlag(object state)
		{
			// Reset the task flag
			_IsSet = false;
			
			// Capture any requests to wait for this callback
			// Must perform this -after- changing IsSet, since it acts as a Memory Barrier
			var MyWaitTask = Interlocked.Exchange(ref _WaitTask, null);
			
			// Execute the task
			_Callback();
			
			while (true)
			{
				// If we captured a wait task, set it
				if (MyWaitTask != null)
					MyWaitTask.SetResult(true);
					
				// Has the task been flagged again? If so, don't bother locking, just run the thing
				if (_IsSet)
				{
					ThreadPool.UnsafeQueueUserWorkItem(ProcessTaskFlag, null);
					
					return;
				}
				
				// Not flagged, try to stop executing
				lock (_LockObject)
				{
					if (!_IsSet)
					{
						_IsExecuting = false;
						
						return;
					}
				}
			}
		}
	}
}
