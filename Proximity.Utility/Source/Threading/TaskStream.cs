/****************************************\
 TaskStream.cs
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
	/// Manages the task of stringing together a sequence of tasks
	/// </summary>
	public sealed class TaskStream
	{	//****************************************
		private readonly object _LockObject = new object();
		
		private readonly TaskFactory _Factory;
		private Task _NextTask;
		
		private int _PendingActions;
		//****************************************
		
		/// <summary>
		/// Creates a new Task Stream with the default task factory
		/// </summary>
		public TaskStream()
		{
			_Factory = Task.Factory;
		}
		
		/// <summary>
		/// Creates a new Task Stream with a custom task factory
		/// </summary>
		/// <param name="factory">The target task factory to use</param>
		public TaskStream(TaskFactory factory)
		{
			_Factory = factory;
		}
		
		//****************************************
		
		/// <summary>
		/// Queues a task to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <returns>The task that was created</returns>
		public Task Queue(Action action)
		{
			return Queue(action, CancellationToken.None);
		}
		
		/// <summary>
		/// Queues a task to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task Queue(Action action, CancellationToken cancellationToken)
		{
			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);
				
				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = _Factory.StartNew(action, cancellationToken);
				}
				else
				{
					_NextTask = _NextTask.ContinueWith((task, state) => ((Action)state)(), action, cancellationToken);
				}
				
				AttachCompleteTask();
				
				return _NextTask;
			}
		}
		
		/// <summary>
		/// Queues a task that takes a value to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>The task that was created</returns>
		public Task Queue<TValue>(Action<TValue> action, TValue value)
		{
			return Queue(action, value, CancellationToken.None);
		}
		
		/// <summary>
		/// Queues a task that takes a value to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task Queue<TValue>(Action<TValue> action, TValue value, CancellationToken cancellationToken)
		{
			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);
				
				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = _Factory.StartNew(() => action(value), cancellationToken);
				}
				else
				{
					_NextTask = _NextTask.ContinueWith((task) => action(value), cancellationToken);
				}
				
				AttachCompleteTask();
				
				return _NextTask;
			}
		}
		
		/// <summary>
		/// Queues a task that returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TResult>(Func<TResult> action)
		{
			return Queue(action, CancellationToken.None);
		}
		
		/// <summary>
		/// Queues a task that returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TResult>(Func<TResult> action, CancellationToken cancellationToken)
		{	//****************************************
			Task<TResult> MyTask;
			//****************************************
			
			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);
				
				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = MyTask = _Factory.StartNew(action, cancellationToken);
				}
				else
				{
					_NextTask = MyTask = _NextTask.ContinueWith((task, state) => ((Func<TResult>)state)(), action, cancellationToken);
				}
				
				AttachCompleteTask();
				
				return MyTask;
			}
		}
		
		/// <summary>
		/// Queues a task that takes a value and returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value)
		{
			return Queue(action, value, CancellationToken.None);
		}
		
		/// <summary>
		/// Queues a task that takes a value and returns a result to the stream
		/// </summary>
		/// <param name="action">The action to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>The task that was created</returns>
		public Task<TResult> Queue<TValue, TResult>(Func<TValue, TResult> action, TValue value, CancellationToken cancellationToken)
		{	//****************************************
			Task<TResult> MyTask;
			//****************************************
			
			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);
				
				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = MyTask = _Factory.StartNew(() => action(value), cancellationToken);
				}
				else
				{
					_NextTask = MyTask = _NextTask.ContinueWith((task) => action(value), cancellationToken);
				}
				
				AttachCompleteTask();
				
				return MyTask;
			}
		}
		
		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask(Func<Task> action)
		{
			return QueueTask(action, CancellationToken.None);
		}
		
		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask(Func<Task> action, CancellationToken cancellationToken)
		{
			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);
				
				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = _Factory.StartNew(action, cancellationToken).Unwrap();
				}
				else
				{
					_NextTask = _NextTask.ContinueWith((task, state) => ((Func<Task>)state)(), action, cancellationToken).Unwrap();
				}
				
				AttachCompleteTask();
				
				return _NextTask;
			}
		}
		
		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TResult>(Func<Task<TResult>> action)
		{
			return QueueTask(action, CancellationToken.None);
		}

		/// <summary>
		/// Queues a method returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken)
		{	//****************************************
			Task<TResult> MyTask;
			//****************************************

			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);

				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = MyTask = _Factory.StartNew(action, cancellationToken).Unwrap();
				}
				else
				{
					_NextTask = MyTask = _NextTask.ContinueWith((task, state) => ((Func<Task<TResult>>)state)(), action, cancellationToken).Unwrap();
				}

				AttachCompleteTask();

				return MyTask;
			}
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask<TValue>(Func<TValue, Task> action, TValue value)
		{
			return QueueTask(action, value, CancellationToken.None);
		}
		
		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task QueueTask<TValue>(Func<TValue, Task> action, TValue value, CancellationToken cancellationToken)
		{
			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);
				
				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = _Factory.StartNew(() => action(value), cancellationToken).Unwrap();
				}
				else
				{
					_NextTask = _NextTask.ContinueWith((task) => action(value), cancellationToken).Unwrap();
				}
				
				AttachCompleteTask();
				
				return _NextTask;
			}
		}
		
		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value)
		{
			return QueueTask(action, value, CancellationToken.None);
		}

		/// <summary>
		/// Queues a method taking a value and returning a task at the end of the Stream
		/// </summary>
		/// <param name="action">The method to execute at the end of the stream</param>
		/// <param name="value">The value to pass to the task</param>
		/// <param name="cancellationToken">A token to cancel this action</param>
		/// <returns>A task representing the completion of the returned task</returns>
		/// <remarks>The stream will not continue until the returned task has been completed</remarks>
		public Task<TResult> QueueTask<TValue, TResult>(Func<TValue, Task<TResult>> action, TValue value, CancellationToken cancellationToken)
		{	//****************************************
			Task<TResult> MyTask;
			//****************************************

			lock (_LockObject)
			{
				Interlocked.Increment(ref _PendingActions);

				if (_NextTask == null || _NextTask.IsCompleted)
				{
					_NextTask = MyTask = _Factory.StartNew(() => action(value), cancellationToken).Unwrap();
				}
				else
				{
					_NextTask = MyTask = _NextTask.ContinueWith((task) => action(value), cancellationToken).Unwrap();
				}

				AttachCompleteTask();

				return MyTask;
			}
		}

		/// <summary>
		/// Resets the stream
		/// </summary>
		/// <remarks>This does not reset the pending actions counter</remarks>
		public void Reset()
		{
			lock (_LockObject)
			{
				_NextTask = null;
			}
		}
		
		/// <summary>
		/// Completes when there have been no new tasks queued to the stream and all existing tasks have been completed
		/// </summary>
		public async Task Complete()
		{
			var CurrentTask = _NextTask;
			
			if (CurrentTask == null) // Nothing has been queued
				return;
			
			do
			{
				CurrentTask = _NextTask;
				
				await CurrentTask;
				// Is this the current task that has finished?
			} while (Volatile.Read(ref _NextTask) != CurrentTask);
		}
		
		//****************************************
		
		private void AttachCompleteTask()
		{
			_NextTask.ContinueWith(delegate (Task innerTask) { Interlocked.Decrement(ref _PendingActions); }, TaskContinuationOptions.ExecuteSynchronously);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of actions that have yet to complete executing
		/// </summary>
		public int PendingActions
		{
			get { return _PendingActions; }
		}
	}
}
