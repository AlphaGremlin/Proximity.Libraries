/****************************************\
 StreamTaskScheduler.cs
 Created: 2012-09-13
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Implements a Task Scheduler that executes queued tasks on the ThreadPool in their order of queueing
	/// </summary>
	[SecurityCritical]
	public sealed class StreamTaskScheduler : TaskScheduler
	{	//****************************************
		[ThreadStatic()] private bool _IsThreadProcessing;
		
		private readonly LinkedList<Task> _Tasks = new LinkedList<Task>();
		private bool _IsProcessing;
		//****************************************
		
		/// <summary>
		/// Creates a new Stream Task Scheduler
		/// </summary>
		public StreamTaskScheduler()
		{
		}
		
		//****************************************
		
		/// <inheritdoc />
		[SecurityCritical]
		protected override void QueueTask(Task task)
		{
			lock (_Tasks)
			{
				_Tasks.AddLast(task);
				
				if (_IsProcessing)
					return;
				
				_IsProcessing = true;
			}
			
			ThreadPool.UnsafeQueueUserWorkItem(ProcessingOperation, null);
		}
		
		/// <inheritdoc />
		[SecurityCritical]
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			// Ensure we're within the Processing Loop
			if (!_IsThreadProcessing)
				return false;
			
			// Ensure we haven't got this task queued
			if (taskWasPreviouslyQueued)
				TryDequeue(task);
			
			// All good, execute it
			return base.TryExecuteTask(task);
		}
		
		/// <inheritdoc />
		[SecurityCritical]
		protected override bool TryDequeue(Task task)
		{
			lock (_Tasks)
			{
				return _Tasks.Remove(task);
			}
		}
		
		/// <inheritdoc />
		[SecurityCritical]
		protected override IEnumerable<Task> GetScheduledTasks()
		{	//****************************************
			bool LockTaken = false;
			//****************************************
			
			try
			{
				Monitor.TryEnter(_Tasks, ref LockTaken);
				
				if (LockTaken)
					return _Tasks.ToArray();
				
				throw new NotSupportedException();
			}
			finally
			{
				if (LockTaken)
					Monitor.Exit(_Tasks);
			}
		}
		
		//****************************************
		
		private void ProcessingOperation(object state)
		{	//****************************************
			Task CurrentTask;
			//****************************************
			
			_IsThreadProcessing = true;
			
			try
			{
				for(;;)
				{
					lock (_Tasks)
					{
						if (_Tasks.Count == 0)
						{
							_IsProcessing = false;
							
							return;
						}
						
						CurrentTask = _Tasks.First.Value;
						
						_Tasks.RemoveFirst();
					}
					
					base.TryExecuteTask(CurrentTask);
				}
			}
			finally
			{
				_IsThreadProcessing = false;
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the maximum concurrency level supported
		/// </summary>
		public override int MaximumConcurrencyLevel
		{
			[SecuritySafeCritical]
			get { return 1; }
		}
	}
}
#endif