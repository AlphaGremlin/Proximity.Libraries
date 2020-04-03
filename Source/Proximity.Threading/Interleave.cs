using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace System.Threading.Tasks
{
	/// <summary>
	/// Enumerates over an enumeration of tasks in the order they complete
	/// </summary>
	internal sealed class Interleave : IAsyncEnumerable<Task>
	{	//****************************************
		private readonly int _StartingThreadID;

		private readonly IEnumerable<Task> _Source;
		private readonly CancellationToken _Token;

		private Task _Current;

		private int _Count;
		private TaskCompletionSource<VoidStruct> _Next;
		private Queue<Task> _Queued;
		//****************************************

		/// <summary>
		/// Creates a new interleaving enumerator
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		public Interleave(IEnumerable<Task> source) : this(source, CancellationToken.None)
		{
		}

		/// <summary>
		/// Creates a new interleaving enumerator
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to abort the interleaved tasks</param>
		public Interleave(IEnumerable<Task> source, CancellationToken token)
		{
			_Source = source;
			_Token = token;
#if NET40
			_StartingThreadID = Thread.CurrentThread.ManagedThreadId;
#else
			_StartingThreadID = Environment.CurrentManagedThreadId;
#endif
		}

		//****************************************

		/// <summary>
		/// Gets an enumerator providing interleaved tasks
		/// </summary>
		/// <returns>The current list of tasks</returns>
		public IEnumerator<Task> GetEnumerator()
		{
#if NET40
			if (Thread.CurrentThread.ManagedThreadId == _StartingThreadID)
#else
			if (Environment.CurrentManagedThreadId == _StartingThreadID)
#endif
				InternalGetEnumerator();

			return new Interleave(_Source, _Token).InternalGetEnumerator();
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

		/// <summary>
		/// Tries to move to the next waiting task
		/// </summary>
		/// <returns>True if there's another task to wait on, otherwise False</returns>
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
				_Next = new TaskCompletionSource<VoidStruct>();
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

		private IEnumerator<Task> InternalGetEnumerator()
		{	//****************************************
			var MySourceTasks = _Source.ToArray();
			//****************************************

			_Count = MySourceTasks.Length;

			// Attach the completion handler to each task
			foreach (var MyTask in MySourceTasks)
			{
				MyTask.ContinueWith(CompleteTask, _Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			return this;
		}

		private void CompleteTask(Task task)
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextTask = null;
			//****************************************

			lock (this)
			{
				_Count--;

				if (_Next == null)
				{
					// No task being waited on, add the existing task to the queue (rather than create another)
					if (_Queued == null)
						_Queued = new Queue<Task>();

					_Queued.Enqueue(task);
				}
				else
				{
					// Retrieve and clear the next task since it's now complete
					NextTask = _Next;
					_Next = null;
				}
			}

			if (NextTask != null)
			{
				// Task being waited on, pass on the results
				if (task.IsFaulted)
					NextTask.SetException(task.Exception.InnerException);
				else if (task.IsCanceled)
					NextTask.SetCanceled();
				else
					NextTask.SetResult(default);
			}
		}

		//****************************************

		/// <summary>
		/// Gets the currently waiting task
		/// </summary>
		public Task Current
		{
			get { return _Current; }
		}

		object System.Collections.IEnumerator.Current
		{
			get { return _Current; }
		}
	}

	/// <summary>
	/// Enumerates over an enumeration of tasks in the order they complete
	/// </summary>
	public class Interleave<TResult> : IEnumerable<Task<TResult>>, IEnumerator<Task<TResult>>
	{	//****************************************
		private readonly int _StartingThreadID;
		
		private readonly IEnumerable<Task<TResult>> _Source;
		private readonly CancellationToken _Token;
		
		private Task<TResult> _Current;
		
		private int _Count;
		private TaskCompletionSource<TResult> _Next;
		private Queue<Task<TResult>> _Queued;
		//****************************************
		
		/// <summary>
		/// Creates a new interleaving enumerator
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		public Interleave(IEnumerable<Task<TResult>> source) : this(source, CancellationToken.None)
		{
		}
		
		/// <summary>
		/// Creates a new interleaving enumerator
		/// </summary>
		/// <param name="source">The enumeration of tasks to interleave</param>
		/// <param name="token">A cancellation token to abort the interleaved tasks</param>
		public Interleave(IEnumerable<Task<TResult>> source, CancellationToken token)
		{
			_Source = source;
			_Token = token;
#if NET40
			_StartingThreadID = Thread.CurrentThread.ManagedThreadId;
#else
			_StartingThreadID = Environment.CurrentManagedThreadId;
#endif
		}
		
		//****************************************
		
		/// <summary>
		/// Gets an enumerator providing interleaved tasks
		/// </summary>
		/// <returns>The current list of tasks</returns>
		public IEnumerator<Task<TResult>> GetEnumerator()
		{
#if NET40
			if (Thread.CurrentThread.ManagedThreadId == _StartingThreadID)
#else
			if (Environment.CurrentManagedThreadId == _StartingThreadID) 
#endif
				InternalGetEnumerator();
			
			return new Interleave<TResult>(_Source, _Token).InternalGetEnumerator();
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
		
		/// <summary>
		/// Tries to move to the next waiting task
		/// </summary>
		/// <returns>True if there's another task to wait on, otherwise False</returns>
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
		{	//****************************************
			var MySourceTasks = _Source.ToArray();
			//****************************************

			_Count = MySourceTasks.Length;

			// Attach the completion handler to each task
			foreach (var MyTask in MySourceTasks)
			{
				MyTask.ContinueWith(CompleteTask, _Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			return this;
		}
		
		private void CompleteTask(Task<TResult> task)
		{	//****************************************
			TaskCompletionSource<TResult> NextTask = null;
			//****************************************

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
					// Retrieve and clear the next task since it's now complete
					NextTask = _Next;
					_Next = null;
				}
			}

			if (NextTask != null)
			{
				// Task being waited on, pass on the results
				if (task.IsFaulted)
					NextTask.SetException(task.Exception.InnerException);
				else if (task.IsCanceled)
					NextTask.SetCanceled();
				else
					NextTask.SetResult(task.Result);
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the currently waiting task
		/// </summary>
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
