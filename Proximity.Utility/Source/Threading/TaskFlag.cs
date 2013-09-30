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
	public class TaskFlag
	{	//****************************************
		private Action _Callback;
		
		private object _LockObject = new object();
		
		private bool _IsSet;
		private bool _IsExecuting;
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
				
				if (_IsExecuting)
					return;
				
				_IsExecuting = true;
			}
			
			ThreadPool.UnsafeQueueUserWorkItem(ProcessTaskFlag, null);
		}
		
		//****************************************
		
		private void ProcessTaskFlag(object state)
		{
			// Reset the task flag
			_IsSet = false;
			
			// Execute the task
			_Callback();
			
			while (true)
			{
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
