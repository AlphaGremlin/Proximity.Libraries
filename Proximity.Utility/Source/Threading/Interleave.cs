/****************************************\
 Interleave.cs
 Created: 2014-07-15
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Enumerates over an enumeration of tasks in the order they complete
	/// </summary>
	public class Interleave<TResult> : IEnumerable<Task<TResult>>, IEnumerator<Task<TResult>>
	{	//****************************************
		private readonly int _StartingThreadID;
		
		private readonly IEnumerable<Task<TResult>> _Source;
		
		private Task<TResult> _Current;
		
		private int _Count;
		private TaskCompletionSource<TResult> _Next;
		private Queue<Task<TResult>> _Queued;
		//****************************************
		
		/// <summary>
		/// Creates a new interleaving enumerator
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		public Interleave(IEnumerable<Task<TResult>> source)
		{
			_Source = source;
			_StartingThreadID = Environment.CurrentManagedThreadId;
		}
		
		//****************************************
		
		public IEnumerator<Task<TResult>> GetEnumerator()
		{
			if (Environment.CurrentManagedThreadId == _StartingThreadID)
				InternalGetEnumerator();
			
			return new Interleave<TResult>(_Source).InternalGetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the interleaving enuerator
		/// </summary>
		public void Dispose()
		{
		}
		
		public bool MoveNext()
		{
			lock (this)
			{
				// Is there a task already completed?
				if (_Queued != null && _Queued.Count > 0)
				{
					_Current = _Queued.Dequeue();
					
					return true;
				}
				
				// Are there any more tasks to complete?
				if (_Count <= 0)
				{
					_Current = null;
					_Next = null;
					
					return false;
				}
				
				// Still tasks to complete, return a task to wait on
				_Next = new TaskCompletionSource<TResult>();
				_Current = _Next.Task;
				
				return true;
			}
		}
		
		/// <summary>
		/// Resets the interleaving enumerator
		/// </summary>
		/// <remarks>Does nothing. You cannot enumerate over an interleaved task set more than once</remarks>
		public void Reset()
		{
		}
		
		//****************************************
		
		private IEnumerator<Task<TResult>> InternalGetEnumerator()
		{
			_Count = 0;
			
			lock (this)
			{
				// Attach the completion handler to each task
				foreach (var MyTask in _Source)
				{
					MyTask.ContinueWith(CompleteTask, TaskContinuationOptions.ExecuteSynchronously);
					
					_Count++;
				}
			}
			
			return this;
		}
		
		private void CompleteTask(Task<TResult> task)
		{
			lock (this)
			{
				_Count--;
				
				if (_Next == null)
				{
					// No task being waited on, add the existing task to the queue (rather than create another)
					if (_Queued == null)
						_Queued = new Queue<Task<TResult>>();
					
					_Queued.Enqueue(task);
				}
				else
				{
				// Task being waited on, pass on the results
					if (task.IsFaulted)
						_Next.SetException(task.Exception.InnerException);
					else if (task.IsCanceled)
						_Next.SetCanceled();
					else
						_Next.SetResult(task.Result);
					
					// Clear the next task since it's now complete
					_Next = null;
				}
			}
		}
		
		//****************************************
		
		public Task<TResult> Current
		{
			get { return _Current; }
		}
		
		object System.Collections.IEnumerator.Current
		{
			get { return _Current; }
		}
	}
}
