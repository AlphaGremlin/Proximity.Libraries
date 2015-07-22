﻿/****************************************\
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
		private readonly object _LockObject = new object();
		private TaskCompletionSource<VoidStruct> _Left= new TaskCompletionSource<VoidStruct>();
		private TaskCompletionSource<VoidStruct> _Right= new TaskCompletionSource<VoidStruct>();
		private TaskCompletionSource<VoidStruct> _Dispose = null;
		
		private int _Counter = 0;
		private readonly HashSet<AsyncSwitchLeftInstance> _LeftWaiting = new HashSet<AsyncSwitchLeftInstance>();
		private readonly HashSet<AsyncSwitchRightInstance> _RightWaiting = new HashSet<AsyncSwitchRightInstance>();
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
		/// Disposes of the switch lock
		/// </summary>
		/// <returns>A task that completes when all holders of the lock have exited</returns>
		/// <remarks>All tasks waiting on the lock will throw ObjectDisposedException</remarks>
		public Task Dispose()
		{
			((IDisposable)this).Dispose();

			return _Dispose.Task;
		}

		void IDisposable.Dispose()
		{	//****************************************
			TaskCompletionSource<VoidStruct> Left, Right;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Dispose != null)
					return;

				_Dispose = new TaskCompletionSource<VoidStruct>();

				if (_Counter == 0)
					_Dispose.SetResult(VoidStruct.Empty);

				_LeftWaiting.Clear();
				_RightWaiting.Clear();
				
				// We must set the task results outside the lock, because otherwise the continuation could run on this thread and modify state within the lock
				Left = _Left;
				Right = _Right;
			}
			
			Left.SetCanceled();
			Right.SetCanceled();
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
			var MySource = new CancellationTokenSource();
			var MyTask = LockLeft(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}

			MySource.CancelAfter(timeout);
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((Action<Task<IDisposable>, object>)CleanupCancelSource, MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Take a left lock, running concurrently with other left locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockLeft(CancellationToken token)
		{	//****************************************
			AsyncSwitchLeftInstance NewInstance;
			Task<IDisposable> NewTask;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Dispose != null)
					throw new ObjectDisposedException("AsyncSwitchLock", "Lock has been disposed of");
				
				// If there are zero or more lefts and no waiting rights (or we're in unfair mode)...
				if (_Counter >= 0 && (_IsUnfair || _RightWaiting.Count == 0))
				{
					// Add another left and return a completed task the caller can use to release the lock
					Interlocked.Increment(ref _Counter);
					
#if NET40
					return TaskEx.FromResult<IDisposable>(new AsyncSwitchLeftInstance(this));
#else
					return Task.FromResult<IDisposable>(new AsyncSwitchLeftInstance(this));
#endif
				}
				
				// There's a right task running or waiting, add ourselves to the left queue
				NewInstance = new AsyncSwitchLeftInstance(this);
				
				_LeftWaiting.Add(NewInstance);
				
				NewTask = _Left.Task.ContinueWith((Func<Task<VoidStruct>, IDisposable>)NewInstance.LockLeft, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			// If we can be cancelled, queue a task that runs if we get cancelled to release the waiter
			if (token.CanBeCanceled)
				NewTask.ContinueWith(NewInstance.CancelLeft, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
			
			return NewTask;
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
			var MySource = new CancellationTokenSource();
			var MyTask = LockRight(MySource.Token);
			//****************************************
		
			// If the task is already completed, no need for the token source
			if (MyTask.IsCompleted)
			{
				MySource.Dispose();
				
				return MyTask;
			}

			MySource.CancelAfter(timeout);
			
			// Ensure we cleanup the cancellation source once we're done
			MyTask.ContinueWith((Action<Task<IDisposable>, object>)CleanupCancelSource, MySource);
			
			return MyTask;
		}
		
		/// <summary>
		/// Take a right lock, running concurrently with other right locks, with cancellation
		/// </summary>
		/// <param name="token">The cancellation token to abort waiting for the lock</param>
		/// <returns>A task that completes when the lock is taken, resulting in a disposable to release the lock</returns>
		public Task<IDisposable> LockRight(CancellationToken token)
		{	//****************************************
			AsyncSwitchRightInstance NewInstance;
			Task<IDisposable> NewTask;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Dispose != null)
					throw new ObjectDisposedException("AsyncSwitchLock", "Lock has been disposed of");
				
				// If there are zero or more rights and no waiting lefts (or we're in unfair mode)...
				if (_Counter <= 0 && (_IsUnfair || _LeftWaiting.Count == 0))
				{
					// Add another right and return a completed task the caller can use to release the lock
					Interlocked.Decrement(ref _Counter);

#if NET40
					return TaskEx.FromResult<IDisposable>(new AsyncSwitchRightInstance(this));
#else
					return Task.FromResult<IDisposable>(new AsyncSwitchRightInstance(this));
#endif
				}
				
				// There's a left task running or waiting, add ourselves to the right queue
				NewInstance = new AsyncSwitchRightInstance(this);
				
				_RightWaiting.Add(NewInstance);
				
				NewTask = _Right.Task.ContinueWith((Func<Task<VoidStruct>, IDisposable>)NewInstance.LockRight, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			}
			
			// If we can be cancelled, queue a task that runs if we get cancelled to release the waiter
			if (token.CanBeCanceled)
				NewTask.ContinueWith(NewInstance.CancelRight, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
			
			return NewTask;
		}
		
		//****************************************
		
		private void ReleaseLeft()
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextTask;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Counter <= 0)
					throw new InvalidOperationException("No left lock is currently held");
				
				Interlocked.Decrement(ref _Counter);

				// If we're disposed, if the counter is zero flag as completed
				if (_Dispose != null)
				{
					if (_Counter != 0)
						return;

					NextTask = _Dispose;
				}
				// If there are no more lefts and one or more rights waiting...
				else if (_Counter == 0 && _RightWaiting.Count > 0)
				{
					// Yes, let's take the existing Right tasks (so we can activate them)
					NextTask = Interlocked.Exchange(ref _Right, new TaskCompletionSource<VoidStruct>());
					
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
			
			// Activate the next waiter
#if PORTABLE
			TryRelease(NextTask);
#else
			ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextTask);
#endif
		}
		
		private static void TryRelease(object state)
		{	//****************************************
			var MyWaiter = (TaskCompletionSource<VoidStruct>)state;
			//****************************************
			
			MyWaiter.SetResult(VoidStruct.Empty);
		}
		
		private void ReleaseRight()
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextTask;
			//****************************************
			
			lock (_LockObject)
			{
				if (_Counter >= 0)
					throw new InvalidOperationException("No right lock is currently held");
				
				Interlocked.Increment(ref _Counter);

				// If we're disposed, if the counter is zero flag as completed
				if (_Dispose != null)
				{
					if (_Counter != 0)
						return;

					NextTask = _Dispose;
				}
				// If there are no more rights and one or more left waiting...
				else if (_Counter == 0 && _LeftWaiting.Count > 0)
				{
					// Yes, let's take the existing Right tasks (so we can activate them)
					NextTask = Interlocked.Exchange(ref _Left, new TaskCompletionSource<VoidStruct>());
					
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
			
			// Activate the next waiter
#if PORTABLE
			TryRelease(NextTask);
#else
			ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextTask);
#endif
		}
		
		private void CancelLeft(AsyncSwitchLeftInstance instance)
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextRight= null;
			//****************************************
			
			lock (_LockObject)
			{
				// If we're disposed, no need to do anything
				if (_Dispose != null)
					return;
				
				// A waiting Left was cancelled. Remove it if it's still waiting
				if (_LeftWaiting.Remove(instance))
				{
					// If this is the last waiting Left, we may have been holding up Rights in fair mode
					if (_Counter >= 0 || _LeftWaiting.Count > 0 || _RightWaiting.Count == 0)
						return;
					
					// There are rights waiting and no more lefts, so we can release them
					NextRight = Interlocked.Exchange(ref _Right, new TaskCompletionSource<VoidStruct>());
					
					_Counter -= _RightWaiting.Count;
					_RightWaiting.Clear();
				}
			}
			
			if (NextRight != null)
			{
#if PORTABLE
				TryRelease(NextRight);
#else
				ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextRight);
#endif
				
				return;
			}
			
			// Right finished between the task cancelling and this continuation running
			// Need to release the Left
			ReleaseLeft();
		}
		
		private void CancelRight(AsyncSwitchRightInstance instance)
		{	//****************************************
			TaskCompletionSource<VoidStruct> NextLeft = null;
			//****************************************
			
			lock (_LockObject)
			{
				// If we're disposed, no need to do anything
				if (_Dispose != null)
					return;

				// A waiting Right was cancelled. Remove it if it's still waiting
				if (_RightWaiting.Remove(instance))
				{
					// If this is the last waiting Right, we may have been holding up Lefts in fair mode
					if (_Counter <= 0 || _RightWaiting.Count > 0 || _LeftWaiting.Count == 0)
						return;
					
					// There are lefts waiting and no more rights, so we can release them
					NextLeft = Interlocked.Exchange(ref _Left, new TaskCompletionSource<VoidStruct>());
					
					_Counter += _LeftWaiting.Count;
					_LeftWaiting.Clear();
				}
			}
			
			if (NextLeft != null)
			{
#if PORTABLE
				TryRelease(NextLeft);
#else
				ThreadPool.UnsafeQueueUserWorkItem(TryRelease, NextLeft);
#endif
				
				return;
			}
			
			// Left finished between the task cancelling and this continuation running
			// Need to release the Right
			ReleaseRight();
		}
		
		private static void CleanupCancelSource(Task task, object state)
		{
			((CancellationTokenSource)state).Dispose();
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
			
			public IDisposable LockLeft(Task<VoidStruct> task)
			{
				if (task.IsCanceled)
					throw new ObjectDisposedException("AsyncSwitchLock", "Lock has been disposed of");
				
				return this;
			}
			
			public void CancelLeft(Task<IDisposable> task)
			{
				_Source.CancelLeft(this);
			}
			
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
			
			public IDisposable LockRight(Task<VoidStruct> task)
			{
				if (task.IsCanceled)
					throw new ObjectDisposedException("AsyncSwitchLock", "Lock has been disposed of");
				
				return this;
			}
			
			public void CancelRight(Task<IDisposable> task)
			{
				_Source.CancelRight(this);
			}
			
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
