/****************************************\
 AsyncCollectionTakeFromAny.cs
 Created: 2014-02-20
\****************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Description of AsyncCollectionTakeFromAny.
	/// </summary>
	internal sealed class AsyncCollectionTakeFromAny<TItem> : TaskCompletionSource<AsyncCollection<TItem>.TakeResult>
	{	//****************************************
		private readonly CancellationTokenSource _TokenSource;
		private readonly CancellationTokenRegistration _Registration;
		//****************************************
		
		internal AsyncCollectionTakeFromAny(CancellationToken token)
		{
			if (token.CanBeCanceled)
			{
				_TokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
				
				_Registration = token.Register(OnCancel);
			}
			else
			{
				_TokenSource = new CancellationTokenSource();
			}
		}
		
		//****************************************
		
		internal void Attach(AsyncCounter counter, AsyncCollection<TItem> collection)
		{
			counter.PeekDecrement(_TokenSource.Token).ContinueWith(OnCounterPeek, collection, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
		}
		
		//****************************************
		
		private void OnCounterPeek(Task<AsyncCounter> task, object state)
		{	//****************************************
			var MyCollection = (AsyncCollection<TItem>)state;
			TItem MyResult;
			//****************************************
			
			if (task.IsFaulted)
			{
				lock (this)
				{
					if (Task.IsCompleted)
						return;
					
					if (task.Exception.InnerException is ObjectDisposedException)
						SetException(new ObjectDisposedException("Collection was disposed", task.Exception.InnerException));
					else
						SetException(new Exception("Failed to decrement any counters", task.Exception.InnerException));
				}
				
				return;
			}
			
			// Only take from one at a time
			lock (this)
			{
				// Have we already completed?
				if (Task.IsCompleted)
					return; // Yes, so this counter stays disabled
				
				// Not complete yet, try and take an item
				if (MyCollection.TryTake(out MyResult))
				{
					SetResult(new AsyncCollection<TItem>.TakeResult(MyCollection, MyResult));
					
					// Cancel the other waiters so they don't hold a reference
					_TokenSource.Cancel();
					_TokenSource.Dispose();
					
					return;
				}
			}
			
			// Failed to increment the counter (pre-empted by someone else peeking), so re-attach
			Attach(task.Result, MyCollection);
		}
		
		private void OnCancel()
		{
			// Make sure we're not taking
			lock (this)
			{
				if (Task.IsCompleted)
					return;
				
				SetCanceled();
			}
		}
	}
}
