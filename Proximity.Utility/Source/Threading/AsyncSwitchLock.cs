/****************************************\
 AsyncSwitchLock.cs
 Created: 2014-03-07
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a lock that switches between concurrent left and right for async/await, and a disposable model for releasing
	/// </summary>
	public sealed class AsyncSwitchLock : IDisposable
	{	//****************************************
		private object _LockObject = new object();
		private TaskCompletionSource<VoidStruct> _Left= new TaskCompletionSource<VoidStruct>();
		private TaskCompletionSource<VoidStruct> _Right= new TaskCompletionSource<VoidStruct>();
		
		private int _Counter = 0;
		private int _LeftWaiting = 0, _RightWaiting = 0;
		private bool _IsUnfair = false, _IsDisposed;
		//****************************************
		
		/// <summary>
		/// Creates a new fair switch lock
		/// </summary>
		public AsyncSwitchLock()
		{
		}
		
		/// <summary>
		/// Creates a new switch lock
		/// </summary>
		/// <param name="isUnfair">Whether to prefer fairness when switching</param>
		public AsyncSwitchLock(bool isUnfair)
		{
			_IsUnfair = isUnfair;
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the switch lock, cancelling any waiters
		/// </summary>
		public void Dispose()
		{
			lock (_LockObject)
			{
				_IsDisposed = true;
				_Counter = 0;
				_LeftWaiting = 0;
				_RightWaiting = 0;
				
				_Left.SetCanceled();
				_Right.SetCanceled();
			}
		}
		
		/// <summary>
		/// Take a left lock, running concurrently with other left locks
		/// </summary>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockLeft()
		{
			return LockLeft(CancellationToken.None);
		}
		
		/// <summary>
		/// Take a left lock, running concurrently with other left locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the left lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockLeft(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = LockLeft(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Take a left lock, running concurrently with other left locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockLeft(CancellationToken token)
		{
			lock (_LockObject)
			{
				if (_IsDisposed)
					throw new ObjectDisposedException("Lock has been disposed of");
				
				// If there are zero or more lefts and no waiting rights (or we're in unfair mode)...
				if (_Counter >= 0 && (_IsUnfair || _RightWaiting == 0))
				{
					// Add another left and return a completed task the caller can use to release the lock
					Interlocked.Increment(ref _Counter);
					
					return Task.FromResult<IDisposable>(new AsyncSwitchLeftInstance(this));
				}
				
				// There's a right task running or waiting, add ourselves to the left queue
				Interlocked.Increment(ref _LeftWaiting);
				
				var MyTask = _Left.Task.ContinueWith(t => (IDisposable)new AsyncSwitchLeftInstance(this), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
				
				// If we can be cancelled, queue a task that runs if we get cancelled to release the waiter
				if (token.CanBeCanceled)
					MyTask.ContinueWith(CancelLeft, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
				
				return MyTask;
			}
		}
		
		/// <summary>
		/// Take a right lock, running concurrently with other right locks
		/// </summary>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRight()
		{
			return LockRight(CancellationToken.None);
		}
		
		/// <summary>
		/// Take a right lock, running concurrently with other right locks, cancelling after a given timeout
		/// </summary>
		/// <param name="timeout">The amount of time to wait for the left lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRight(TimeSpan timeout)
		{	//****************************************
			var MySource = new CancellationTokenSource(timeout);
			var MyTask = LockRight(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((task, innerSource) => ((CancellationTokenSource)innerSource).Dispose(), MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Take a right lock, running concurrently with other right locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRight(CancellationToken token)
		{
			lock (_LockObject)
			{
				if (_IsDisposed)
					throw new ObjectDisposedException("Lock has been disposed of");
				
				// If there are zero or more rights and no waiting lefts (or we're in unfair mode)...
				if (_Counter <= 0 && (_IsUnfair || _LeftWaiting == 0))
				{
					// Add another right and return a completed task the caller can use to release the lock
					Interlocked.Decrement(ref _Counter);
					
					return Task.FromResult<IDisposable>(new AsyncSwitchRightInstance(this));
				}
				
				// There's a left task running or waiting, add ourselves to the right queue
				Interlocked.Increment(ref _RightWaiting);
				
				var MyTask = _Right.Task.ContinueWith(t => (IDisposable)new AsyncSwitchRightInstance(this), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
				
				// If we can be cancelled, queue a task that runs if we get cancelled to release the waiter
				if (token.CanBeCanceled)
					MyTask.ContinueWith(CancelRight, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
				
				return MyTask;
			}
		}
		
		//****************************************
		
		private void ReleaseLeft()
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextRight;
			//****************************************
			
			lock (_LockObject)
			{
				if (_IsDisposed)
					return;
				
				if (_Counter <= 0)
					throw new InvalidOperationException("No left lock is currently held");
				
				Interlocked.Decrement(ref _Counter);
				
				// If there are no more lefts and one or more rights waiting...
				if (_Counter == 0 && _RightWaiting > 0)
				{
					// Yes, let's take the existing Right tasks (so we can activate them)
					NextRight = Interlocked.Exchange(ref _Right, new TaskCompletionSource<VoidStruct>());
					
					// Swap the waiting rights with zero and update the counter (Right is negative, so negate the result)
					_Counter = -Interlocked.Exchange(ref _RightWaiting, 0);
				}
				else
				{
					// If we don't have a Right waiting, abort early
					return;
				}
			}
			
			// Activate all the Right waiters
			ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextRight);
		}
		
		private void TryRelease(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<VoidStruct>)state;
			//****************************************
			
			MyWaiter.SetResult(VoidStruct.Empty);
		}
		
		private void ReleaseRight()
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextLeft;
			//****************************************
			
			lock (_LockObject)
			{
				if (_IsDisposed)
					return;
				
				if (_Counter >= 0)
					throw new InvalidOperationException("No right lock is currently held");
				
				Interlocked.Increment(ref _Counter);
				
				// If there are no more rights and one or more left waiting...
				if (_Counter == 0 && _LeftWaiting > 0)
				{
					// Yes, let's take the existing Right tasks (so we can activate them)
					NextLeft = Interlocked.Exchange(ref _Left, new TaskCompletionSource<VoidStruct>());
					
					// Swap the waiting lefts with zero and update the counter (Left is positive, so we don't negate the result)
					_Counter = Interlocked.Exchange(ref _LeftWaiting, 0);
				}
				else
				{
					// If we don't have a Left waiting, abort early
					return;
				}
			}
			
			// Activate all the Left waiters
			ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextLeft);
		}
		
		private void CancelLeft(Task<IDisposable> task)
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextRight= null;
			//****************************************
			
			lock (_LockObject)
			{
				if (_IsDisposed)
					return;
				
				// A waiting Left was cancelled. Is it still waiting?
				if (_Counter < 0)
				{
					// A Right is still working, so we can just decrement the waiting lefts count
					if (Interlocked.Decrement(ref _LeftWaiting) != 0 || _RightWaiting == 0)
						return;
					
					// There are rights waiting and no more lefts, so we can release them
					NextRight = Interlocked.Exchange(ref _Right, new TaskCompletionSource<VoidStruct>());
					
					Interlocked.Add(ref _Counter, -Interlocked.Exchange(ref _RightWaiting, 0));
				}
			}
			
			if (NextRight != null)
			{
				ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextRight);
				
				return;
			}
			
			// Right finished between the task cancelling and this continuation running
			// Need to release the Left
			ReleaseLeft();
		}
		
		private void CancelRight(Task<IDisposable> task)
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextLeft = null;
			//****************************************
			
			lock (_LockObject)
			{
				if (_IsDisposed)
					return;
				
				// A waiting Right was cancelled. Is it still waiting?
				if (_Counter > 0)
				{
					// A Left is still working, so we can decrement the waiting rights count
					if (Interlocked.Decrement(ref _RightWaiting) != 0 || _LeftWaiting == 0)
						return;
					
					// There are lefts waiting and no more rights, so we can release them
					NextLeft = Interlocked.Exchange(ref _Left, new TaskCompletionSource<VoidStruct>());
					
					Interlocked.Add(ref _Counter, Interlocked.Exchange(ref _LeftWaiting, 0));
				}
			}
			
			if (NextLeft != null)
			{
				ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextLeft);
				
				return;
			}
			
			// Left finished between the task cancelling and this continuation running
			// Need to release the Right
			ReleaseRight();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of left waiters queued
		/// </summary>
		public int WaitingLeft
		{
			get { return _LeftWaiting; }
		}
		
		/// <summary>
		/// Gets the number of right waiters queued
		/// </summary>
		public int WaitingRight
		{
			get { return _RightWaiting; }
		}
		
		/// <summary>
		/// Gets whether any concurrent left operations are in progress
		/// </summary>
		public bool IsLeft
		{
			get { return _Counter > 0; }
		}
		
		/// <summary>
		/// Gets whether any concurrent right operations are in progress
		/// </summary>
		public bool IsRight
		{
			get { return _Counter < 0; }
		}
		
		/// <summary>
		/// Gets if the switch lock favours an unfair algorithm
		/// </summary>
		/// <remarks>In unfair mode, a lock on one side will succeed if it's already held elsewhere, even if there are waiters on the other side</remarks>
		public bool IsUnfair
		{
			get { return _IsUnfair; }
		}
		
		//****************************************
		
		private class AsyncSwitchLeftInstance : IDisposable
		{	//****************************************
			private readonly AsyncSwitchLock _Source;
			private int _Released;
			//****************************************
			
			internal AsyncSwitchLeftInstance(AsyncSwitchLock source)
			{
				_Source = source;
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null && Interlocked.Exchange(ref _Released, 1) == 0)
				{
					_Source.ReleaseLeft();
				}
			}
		}
		
		
		private class AsyncSwitchRightInstance : IDisposable
		{	//****************************************
			private readonly AsyncSwitchLock _Source;
			private int _Released;
			//****************************************
			
			internal AsyncSwitchRightInstance(AsyncSwitchLock source)
			{
				_Source = source;
			}
			
			//****************************************
			
			public void Dispose()
			{
				if (_Source != null && Interlocked.Exchange(ref _Released, 1) == 0)
				{
					_Source.ReleaseRight();
				}
			}
		}
	}
}
