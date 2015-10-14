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
			// Do not pass the cancellation token to the continuation, since if the counter is disposed when the token cancels,
			// an ObjectDisposedException could be thrown but never be observed and cause an Unobserved Task Exception
			counter.PeekDecrement(_TokenSource.Token).ContinueWith((Action<Task<AsyncCounter>, object>)OnPeekCompleted, counter, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current);
		}

		//****************************************

		private void OnPeekCompleted(Task task, object state)
		{	//****************************************
			var MyCounter = (AsyncCounter)state;
			//****************************************

			if (task.IsFaulted)
			{
				bool Result;

				// Register the exception
				if (task.Exception.InnerException is ObjectDisposedException)
					Result = TrySetException(new ObjectDisposedException("Counter was disposed", task.Exception.InnerException));
				else
					Result = TrySetException(new Exception("Failed to peek any counters", task.Exception.InnerException));

				// Exception registered. Cancel the other waiters so they don't hold a reference
				if (Result)
				{
					_TokenSource.Cancel();
					_TokenSource.Dispose();
				}

				_Registration.Dispose();

				return;
			}

			// Can we decrement the counter?
			if (MyCounter.TryDecrement())
			{
				// Can we set the result?
				if (TrySetResult(MyCounter))
				{
					// Success. Cancel the other waiters so they don't hold a reference
					_TokenSource.Cancel();
					_TokenSource.Dispose();

					return;
				}

				// Failed. Might have been cancelled, or been pre-empted by another counter
				// Restore the counter we incorrectly decremented
				MyCounter.ForceIncrement();
			}

			// Failed to increment the counter (pre-empted by someone else peeking), so re-attach
			Attach(MyCounter);
		}

		private void OnCancel()
		{
			// Cancel everything
			TrySetCanceled();
		}
	}
}
