/****************************************\
 StreamTaskScheduler.cs
 Created: 2012-09-13
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Represents a scheduler placing everything on one thread
	/// </summary>
	public sealed class DedicatedThreadScheduler : TaskScheduler, IDisposable
	{	//****************************************
		private BlockingCollection<Task> _Tasks;
		private readonly Thread _Thread;
		//****************************************
		
		/// <summary>
		/// Creates a new dedicated thread scheduler
		/// </summary>
		
		public DedicatedThreadScheduler()
		{
			_Tasks = new BlockingCollection<Task>();
			
			_Thread = new Thread(ConsumeTasks);
			_Thread.IsBackground = true;
			_Thread.Start();
		}
		
		/// <summary>
		/// Creates a new dedicated thread scheduler with a specified name
		/// </summary>
		/// <param name="name">The name to assign to the new Thread</param>
		
		public DedicatedThreadScheduler(string name)
		{
			_Tasks = new BlockingCollection<Task>();
			
			_Thread = new Thread(ConsumeTasks);
			_Thread.IsBackground = true;
			_Thread.Name = name;
			_Thread.Start();
		}
		
		//****************************************
		
		/// <summary>
		/// Queues a Task on this Scheduler
		/// </summary>
		/// <param name="task">The task to execute</param>
		
		protected override void QueueTask(Task task)
		{
			_Tasks.Add(task);
		}
		
		/// <summary>
		/// Gets an enumerable of the currently scheduled tasks
		/// </summary>
		/// <returns>An enumerable of the currently scheduled tasks</returns>
		
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return _Tasks.ToArray();
		}
		
		/// <summary>
		/// Determines whether a Task may be inlined.
		/// </summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
		/// <returns>True if the task was successfully inlined, otherwise False.</returns>
		
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return Thread.CurrentThread == _Thread && TryExecuteTask(task);
		}
		
		/// <summary>
		/// Blocks until all tasks have completed and disables adding more tasks
		/// </summary>
		
		public void Dispose()
		{
			if (_Tasks != null)
			{
				_Tasks.CompleteAdding();
				
				_Thread.Join();
				
				_Tasks.Dispose();
				_Tasks = null;
			}
		}

		//****************************************
		
		
		private void ConsumeTasks(object state)
		{
			foreach (var MyTask in _Tasks.GetConsumingEnumerable())
				TryExecuteTask(MyTask);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the maximum concurrency level supported
		/// </summary>
		public override int MaximumConcurrencyLevel
		{
			
			get { return 1; }
		}
	}
}
#endif