/****************************************\
 AsyncCounterDecrementAny.cs
 Created: 2015-07-01
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides the functionality for AsyncCounter.DecrementAny
	/// </summary>
	internal sealed class AsyncCounterDecrementAny : TaskCompletionSource<AsyncCounter>
	{	//****************************************
		private readonly CancellationTokenSource _TokenSource;
		private readonly CancellationTokenRegistration _Registration;
		//****************************************
		
		internal AsyncCounterDecrementAny(CancellationToken token)
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
		
		internal void Attach(AsyncCounter counter)
		{
			counter.PeekDecrement().ContinueWith((Action<Task<AsyncCounter>, object>)OnPeekCompleted, counter, _TokenSource.Token);
		}
		
		//****************************************
		
		private void OnPeekCompleted(Task task, object state)
		{	//****************************************
			var MyCounter = (AsyncCounter)state;
			//****************************************
			
			if (task.IsFaulted)
			{
				lock (this)
				{
					if (Task.IsCompleted)
						return;
					
					if (task.Exception.InnerException is ObjectDisposedException)
						SetException(new ObjectDisposedException("Counter was disposed", task.Exception.InnerException));
					else
						SetException(new Exception("Failed to peek any counters", task.Exception.InnerException));
				
					_Registration.Dispose();
				}
				
				return;
			}
			
			// Only decrement one at a time
			lock (this)
			{
				// Have we already completed?
				if (Task.IsCompleted)
					return; // Yes, so this counter stays disabled
				
				// Not complete yet, try and decrement the counter
				if (MyCounter.TryDecrement())
				{
					SetResult(MyCounter);
					
					// Cancel the other waiters so they don't hold a reference
					_TokenSource.Cancel();
					_TokenSource.Dispose();
					
					return;
				}
			}
			
			// Failed to increment the counter (pre-empted by someone else peeking), so re-attach
			Attach(MyCounter);
		}
		
		private void OnCancel()
		{
			// Make sure we're not decrementing
			lock (this)
			{
				if (Task.IsCompleted)
					return;
				
				SetCanceled();
			}
		}
	}
}
