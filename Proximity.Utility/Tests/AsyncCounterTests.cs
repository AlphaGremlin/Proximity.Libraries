/****************************************\
 AsyncCounterTests.cs
 Created: 2014-07-09
\****************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests the functionality of the asynchronous counter
	/// </summary>
	[TestFixture]
	public class AsyncCounterTests
	{
		[Test]
		public void Increment()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			//****************************************
			
			Assert.AreEqual(1, MyCounter.CurrentCount, "Counter not incremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void Decrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			var MyTask = MyCounter.Decrement();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void DecrementInitial()
		{	//****************************************
			var MyCounter = new AsyncCounter(1);
			//****************************************
			
			var MyTask = MyCounter.Decrement();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementIncrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement();
			
			Assert.IsFalse(MyTask.IsCompleted, "Decremented too early");
			
			MyCounter.Increment();
			
			//****************************************
			
			MyTask.Wait();
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			Task MyTask;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyCounter.Decrement(MySource.Token);
			
				Assert.IsFalse(MyTask.IsCompleted, "Decremented too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			}
			
			MyCounter.Increment();
			
			Thread.Sleep(100); // Increment happens on another thread and there's nothing to wait on
			
			//****************************************
			
			Assert.AreEqual(1, MyCounter.CurrentCount, "Counter still decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementMultiCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask1 = MyCounter.Decrement(MySource.Token);
				var MyTask2 = MyCounter.Decrement();
			
				Assert.IsFalse(MyTask1.IsCompleted, "Decremented too early");
				Assert.IsFalse(MyTask2.IsCompleted, "Decremented too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask1.IsCanceled, "Wait not cancelled");
				Assert.IsFalse(MyTask2.IsCanceled, "Wait cancelled");
				
				MyCounter.Increment();
				
				MyTask2.Wait();
				
				Assert.IsTrue(MyTask2.IsCompleted, "Still waiting to decrement");
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeout()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			Thread.Sleep(100);
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			
			MyCounter.Dispose();
			
			Assert.AreEqual(0, MyCounter.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyCounter.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeoutNone()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyCounter.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeoutDelayed()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			MyCounter.Increment();
			
			//****************************************
			
			MyTask.Wait();
			
			//****************************************
			
			Assert.AreEqual(0, MyCounter.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyCounter.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DisposeDecrement()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			MyLock.Dispose();
			
			try
			{
				var MyTask = MyLock.Decrement();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyLock.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DisposeIncrement()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			MyLock.Dispose();
			
			try
			{
				MyLock.Increment();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyLock.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DecrementDispose()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			var MyTask = MyLock.Decrement();
			
			MyLock.Dispose();
			
			try
			{
				MyTask.Wait();
				
				Assert.Fail("Should never reach this point");
			}
			catch (AggregateException e)
			{
				Assert.IsInstanceOf(typeof(ObjectDisposedException), e.InnerException, "Inner exception not as expected");
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyLock.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void IncrementDispose()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			MyLock.Increment();
			
			MyLock.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(1, MyLock.CurrentCount, "Count not one");
		}
		
		[Test]
		public async Task StackBlow()
		{	//****************************************
			var MyLock = new AsyncCounter();
			Task ResultTask;
			Task[] Results;
			//****************************************

			Results = Enumerable.Range(1, 40000).Select(
				async count => 
				{
					await MyLock.Decrement();
					
					MyLock.Increment();
				}).ToArray();
			
			ResultTask = Results[Results.Length -1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);

			MyLock.Increment();
			
			await ResultTask;
		}
	}
}
