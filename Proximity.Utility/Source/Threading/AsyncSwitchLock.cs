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
	public sealed class AsyncSwitchLock
	{	//****************************************
		private object _LockObject = new object();
		private readonly List<TaskCompletionSource<IDisposable>> _LeftWaiting = new List<TaskCompletionSource<IDisposable>>();
		private readonly List<TaskCompletionSource<IDisposable>> _RightWaiting = new List<TaskCompletionSource<IDisposable>>();
		
		private int _Counter = 0;
		private bool _IsUnfair = false;
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
		{
			return LockLeft(new CancellationTokenSource(timeout));
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
				// If there are zero or more lefts and no waiting rights (or we're in unfair mode)...
				if (_Counter >= 0 && (_IsUnfair || _RightWaiting.Count == 0))
				{
					// Add another left and return a completed task the caller can use to release the lock
					Interlocked.Increment(ref _Counter);
					
					return Task.FromResult<IDisposable>(new AsyncSwitchLeftInstance(this));
				}
				
				// There's a right task running or waiting, add ourselves to the left queue
				var MyWaiter = new TaskCompletionSource<IDisposable>();
				
				_LeftWaiting.Add(MyWaiter);
				
				// Check if we can get cancelled
				if (token.CanBeCanceled)
				{
					// Register for cancellation
					var MyRegistration = token.Register(Cancel, MyWaiter);
					
					// When we complete, dispose of the registration
					MyWaiter.Task.ContinueWith((task) => MyRegistration.Dispose());
				}
				
				return MyWaiter.Task;
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
		{
			return LockRight(new CancellationTokenSource(timeout));
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
				// If there are zero or more rights and no waiting lefts (or we're in unfair mode)...
				if (_Counter <= 0 && (_IsUnfair || _LeftWaiting.Count == 0))
				{
					// Add another right and return a completed task the caller can use to release the lock
					Interlocked.Decrement(ref _Counter);
					
					return Task.FromResult<IDisposable>(new AsyncSwitchRightInstance(this));
				}
				
				// There's a left task running or waiting, add ourselves to the right queue
				var MyWaiter = new TaskCompletionSource<IDisposable>();
				
				_RightWaiting.Add(MyWaiter);
				
				// Check if we can get cancelled
				if (token.CanBeCanceled)
				{
					// Register for cancellation
					var MyRegistration = token.Register(Cancel, MyWaiter);
					
					// When we complete, dispose of the registration
					MyWaiter.Task.ContinueWith((task) => MyRegistration.Dispose());
				}
				
				return MyWaiter.Task;
			}
		}
		
		//****************************************
		
		private Task<IDisposable> LockLeft(CancellationTokenSource tokenSource)
		{	//****************************************
			var MyTask = LockLeft(tokenSource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				tokenSource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith(task => tokenSource.Dispose());
			
			return MyTask;
		}
		
		private Task<IDisposable> LockRight(CancellationTokenSource tokenSource)
		{	//****************************************
			var MyTask = LockRight(tokenSource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				tokenSource.Dispose();
				
				return MyTask;
			}
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith(task => tokenSource.Dispose());
			
			return MyTask;
		}
		
		private void ReleaseLeft()
		{	//****************************************
			TaskCompletionSource<IDisposable>[] NextRight;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Counter <= 0)
					throw new InvalidOperationException("No left lock is currently held");
				
				Interlocked.Decrement(ref _Counter);
				
				// If there are no more lefts and one or more rights waiting...
				if (_Counter == 0 && _RightWaiting.Count > 0)
				{
					// Yes, let's take the existing Right tasks (so we can activate them)
					NextRight = _RightWaiting.ToArray();
					
					// Swap the waiting rights with zero and update the counter (Right is negative, so negate the result)
					_Counter = -_RightWaiting.Count;
					
					_RightWaiting.Clear();
				}
				else
				{
					// If we don't have a Right waiting, abort early
					return;
				}
			}
			
			// Raise all the Right waiters
			// A waiter may cancel, so we need to check for that and cleanup if so
			foreach(var MyRight in NextRight)
			{
				if (!MyRight.TrySetResult(new AsyncSwitchRightInstance(this)))
				{
					ReleaseRight();
				}
				/*
				ThreadPool.QueueUserWorkItem(
					(state) =>
					{
						if (!((TaskCompletionSource<IDisposable>)state).TrySetResult(new AsyncSwitchRightInstance(this)))
						{
							ReleaseRight();
						}
					},
					MyRight
				);
				*/
			}
		}
		
		private void ReleaseRight()
		{	//****************************************
			TaskCompletionSource<IDisposable>[] NextLeft;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Counter >= 0)
					throw new InvalidOperationException("No right lock is currently held");
				
				Interlocked.Increment(ref _Counter);
				
				// If there are no more rights and one or more left waiting...
				if (_Counter == 0 && _LeftWaiting.Count > 0)
				{
					// Yes, let's take the existing Right tasks (so we can activate them)
					NextLeft = _LeftWaiting.ToArray();
					
					// Swap the waiting lefts with zero and update the counter (Left is positive, so we don't negate the result)
					_Counter = _LeftWaiting.Count;
					
					_LeftWaiting.Clear();
				}
				else
				{
					// If we don't have a Left waiting, abort early
					return;
				}
			}
			
			// Raise all the Left waiters
			// A waiter may cancel, so we need to check for that and cleanup if so
			foreach(var MyLeft in NextLeft)
			{
				if (!MyLeft.TrySetResult(new AsyncSwitchLeftInstance(this)))
				{
					ReleaseLeft();
				}
				
				/*
				ThreadPool.QueueUserWorkItem(
					(state) =>
					{
						if (!((TaskCompletionSource<IDisposable>)state).TrySetResult(new AsyncSwitchLeftInstance(this)))
						{
							ReleaseLeft();
						}
					},
					MyLeft
				);
				*/
			}
		}
		
		private void Cancel(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<IDisposable>)state;
			//****************************************
			
			// Try and cancel our task. If it fails, we've already activated and been removed from the waiters list
			MyWaiter.TrySetCanceled();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the number of left waiters queued
		/// </summary>
		public int WaitingLeft
		{
			get { return _LeftWaiting.Count; }
		}
		
		/// <summary>
		/// Gets the number of right waiters queued
		/// </summary>
		public int WaitingRight
		{
			get { return _RightWaiting.Count; }
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
