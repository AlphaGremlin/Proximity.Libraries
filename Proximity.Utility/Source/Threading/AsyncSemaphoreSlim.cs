/****************************************\
 AsyncSemaphoreSlim.cs
 Created: 2014-02-18
\****************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a primitive for semaphores supporting async/await and a disposable model for releasing
	/// </summary>
	public sealed class AsyncSemaphoreSlim : IDisposable
	{	//****************************************
		private readonly Task<IDisposable> _CompleteTask;
		private AsyncSemaphoreState _State;
		//****************************************
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with a single counter (acts as a lock)
		/// </summary>
		public AsyncSemaphoreSlim()
		{
			_CompleteTask = Task.FromResult<IDisposable>(new AsyncLockInstance(this));
			_State = new AsyncSemaphoreState(1);
		}
		
		/// <summary>
		/// Creates a new Asynchronous Semaphore with the given number of counters
		/// </summary>
		/// <param name="initialCount">The number of counters allowed</param>
		public AsyncSemaphoreSlim(int initialCount) : this()
		{
			if (initialCount < 1)
				throw new ArgumentException("Initial Count is invalid");
			
			_State = new AsyncSemaphoreState(initialCount);
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the semaphore, cancelling any waiters
		/// </summary>
		public void Dispose()
		{	//****************************************
			AsyncSemaphoreState OldState, NewState;
			//****************************************
			
			do
			{
				OldState = _State;
				NewState = new AsyncSemaphoreState(OldState, ImmutableQueue<TaskCompletionSource<IDisposable>>.Empty);
			}
			while (Interlocked.CompareExchange(ref _State, NewState, OldState) != OldState);
			
			//****************************************
			
			foreach (var MyWaiter in OldState.Waiters)
			{
				MyWaiter.TrySetCanceled();
			}
		}
		
		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Wait()
		{
			return Wait(CancellationToken.None);
		}
		
		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <param name="timeout">The amount of time to wait for a counters</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Wait(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = Wait(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource, TaskContinuationOptions.ExecuteSynchronously);
			
			return MyTask;
		}
		
		/// <summary>
		/// Attempts to take a counter
		/// </summary>
		/// <param name="token">A cancellation token that can be used to abort waiting on the lock</param>
		/// <returns>A task that completes when a counter is taken, giving an IDisposable to release the counter</returns>
		public Task<IDisposable> Wait(CancellationToken token)
		{	//****************************************
			AsyncSemaphoreState OldState, NewState;
			Task<IDisposable> Result;
			TaskCompletionSource<IDisposable> NewWaiter = null;
			//****************************************
			
			do
			{
				OldState = _State;
				
				// Is there a free counter?
				if (OldState.CurrentCount > 0)
				{
					// Yes, so we can just subtract one and return the completed task
					Result = _CompleteTask;
					
					NewState = new AsyncSemaphoreState(OldState, OldState.CurrentCount - 1);
					
					continue;
				}
				
				// No free counters, add ourselves to the queue waiting on a counter
				if (NewWaiter == null)
					NewWaiter = new TaskCompletionSource<IDisposable>();
				
				Result = NewWaiter.Task;
				
				NewState = new AsyncSemaphoreState(OldState, NewWaiter);
			}
			while (Interlocked.CompareExchange(ref _State, NewState, OldState) != OldState);
			
			//****************************************
			
			// Check if we can get cancelled
			if (!Result.IsCompleted && token.CanBeCanceled)
			{
				// Register for cancellation
				var MyRegistration = token.Register(Cancel, NewWaiter);
				
				// If we complete and haven't been cancelled, dispose of the registration
				Result.ContinueWith((task, state) => ((CancellationTokenRegistration)state).Dispose(), MyRegistration, TaskContinuationOptions.ExecuteSynchronously);
			}
			
			return Result;
		}
		
		//****************************************
		
		private void Release()
		{	//****************************************
			AsyncSemaphoreState OldState, NewState;
			TaskCompletionSource<IDisposable> NextWaiter;
			//****************************************
			
			do
			{
				do
				{
					OldState = _State;
				
					// Is there anybody waiting, or has MaxCount been dropped below the currently held number?
					if (OldState.Waiters.IsEmpty || OldState.CurrentCount < 0)
					{
						// No, free a counter for someone else
						NewState = new AsyncSemaphoreState(OldState, OldState.CurrentCount + 1);
						
						NextWaiter = null;
						
						continue;
					}
						
					// Yes, don't change the counter
					NewState = new AsyncSemaphoreState(OldState, OldState.Waiters.Dequeue(out NextWaiter));
				}
				while (Interlocked.CompareExchange(ref _State, NewState, OldState) != OldState);
				
				// Queue is empty, return
				if (NextWaiter == null)
					return;
				
				// Try to hand the counter over to the next waiter
				// It may cancel (or have already been cancelled), and if so we loop back and try again
			} while (!NextWaiter.TrySetResult(_CompleteTask.Result));
		}
		
		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already completed and been removed from the Waiters list.
			MyWaiter.TrySetCanceled();
		}
		
		private void TryReleaseWaiters(object state)
		{	//****************************************
			AsyncSemaphoreState OldState, NewState;
			TaskCompletionSource<IDisposable> NextWaiter = null;
			bool HasCounter = false;
			//****************************************
			
			while (true)
			{
				do
				{
					OldState = _State;
					
					// Check if nobody is waiting, or if there are no counters and we're not holding one
					if (OldState.Waiters.IsEmpty || (OldState.CurrentCount <= 0 && !HasCounter))
					{
						// Did we take a counter only for the task to be cancelled?
						if (HasCounter)
							Release();
						
						return;
					}
					
					// Somebody is waiting, and there are free counters. 
					if (HasCounter)
					{
						// We already have a counter, so just remove a waiter
						NewState = new AsyncSemaphoreState(OldState, OldState.Waiters.Dequeue(out NextWaiter));
					}
					else
					{
						// We don't have a counter. Take one and remove a waiter
						NewState = new AsyncSemaphoreState(OldState, OldState.Waiters.Dequeue(out NextWaiter), OldState.CurrentCount - 1);
					}
				}
				while (Interlocked.CompareExchange(ref _State, NewState, OldState) != OldState);
				
				HasCounter = true;

				// Try to hand the counter over to the next waiter
				// It may cancel (or have already been cancelled), and if so we loop back and check for another waiter
				if (NextWaiter.TrySetResult(_CompleteTask.Result))
				{
					// The waiting task will take responsibility for releasing it, so we clear the flag
					HasCounter = false;
				}
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of counters available for taking
		/// </summary>
		public int CurrentCount
		{
			get { return _State.CurrentCount; }
		}
		
		/// <summary>
		/// Gets the number of operations waiting for a counter
		/// </summary>
		public int WaitingCount
		{
			get { return _State.Waiters.Count(); }
		}
		
		/// <summary>
		/// Gets/Sets the maximum number of operations
		/// </summary>
		public int MaxCount
		{
			get { return _State.MaxCount; }
			set
			{	//****************************************
				AsyncSemaphoreState OldState, NewState;
				//****************************************
			
				if (value < 1)
					throw new ArgumentException("Maximum is invalid");
				
				if (_State.MaxCount == value)
					return;
				
				do
				{
					OldState = _State;
					// Adjust the number of slots based on the difference
					NewState = new AsyncSemaphoreState(OldState, OldState.CurrentCount + value - OldState.MaxCount, value);
				}
				while (Interlocked.CompareExchange(ref _State, NewState, OldState) != OldState);
				
				// Release any waiters that might now have free slots
				ThreadPool.QueueUserWorkItem(TryReleaseWaiters);
			}
		}
		
		//****************************************
		
		private struct AsyncLockInstance : IDisposable
		{	//****************************************
			private readonly AsyncSemaphoreSlim _Source;
			//****************************************
			
			internal AsyncLockInstance(AsyncSemaphoreSlim source)
			{
				_Source = source;
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null)
					_Source.Release();
			}
		}
		
		private class AsyncSemaphoreState
		{	//****************************************
			private readonly ImmutableQueue<TaskCompletionSource<IDisposable>> _Waiters;
			private readonly int _MaxCount, _CurrentCount;
			//****************************************
			
			internal AsyncSemaphoreState(int initialCount)
			{
				_CurrentCount = _MaxCount = initialCount;
				_Waiters = ImmutableQueue<TaskCompletionSource<IDisposable>>.Empty;
			}
			
			internal AsyncSemaphoreState(AsyncSemaphoreState ancestor, TaskCompletionSource<IDisposable> newWaiter)
			{
				_Waiters = ancestor._Waiters.Enqueue(newWaiter);
				_CurrentCount = ancestor._CurrentCount;
				_MaxCount = ancestor._MaxCount;
			}
			
			internal AsyncSemaphoreState(AsyncSemaphoreState ancestor, ImmutableQueue<TaskCompletionSource<IDisposable>> waiters)
			{
				_Waiters = waiters;
				_CurrentCount = ancestor._CurrentCount;
				_MaxCount = ancestor._MaxCount;
			}
			
			internal AsyncSemaphoreState(AsyncSemaphoreState ancestor, ImmutableQueue<TaskCompletionSource<IDisposable>> waiters, int currentCount)
			{
				_Waiters = waiters;
				_CurrentCount = currentCount;
				_MaxCount = ancestor._MaxCount;
			}
			
			internal AsyncSemaphoreState(AsyncSemaphoreState ancestor, int currentCount)
			{
				_Waiters = ancestor._Waiters;
				_CurrentCount = currentCount;
				_MaxCount = ancestor._MaxCount;
			}
			
			internal AsyncSemaphoreState(AsyncSemaphoreState ancestor, int currentCount, int maxCount)
			{
				_Waiters = ancestor._Waiters;
				_CurrentCount = currentCount;
				_MaxCount = maxCount;
			}
			
			//****************************************
			
			internal int CurrentCount
			{
				get { return _CurrentCount; }
			}
			
			internal int MaxCount
			{
				get { return _MaxCount; }
			}
			
			internal ImmutableQueue<TaskCompletionSource<IDisposable>> Waiters
			{
				get { return _Waiters; }
			}
		}
	}
}
