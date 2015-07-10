﻿/****************************************\
 AsyncSemaphoreTests.cs
 Created: 2014-02-26
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
	/// Tests the functionality of the Asynchronous Semaphore
	/// </summary>
	[TestFixture]
	public sealed class AsyncSemaphoreTests
	{
		[Test, Timeout(1000)]
		public void Lock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait = MyLock.Wait();
			
			MyWait.Result.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public void LockLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait1 = MyLock.Wait();
			
			var MyWait2 = MyLock.Wait();
			
			Assert.IsFalse(MyWait2.IsCompleted, "Nested lock taken");

			//****************************************
			
			MyWait1.Result.Dispose();
			
			MyWait2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public async Task LockLockLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore(2);
			Task<IDisposable> MyWaiter1, MyWaiter2;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWaiter1 = MyLock.Wait();
				
				MyWaiter2 = MyLock.Wait();
				
				Assert.IsTrue(MyWaiter1.IsCompleted, "Nested lock not taken");
				
				Assert.IsFalse(MyWaiter2.IsCompleted, "Nested lock taken");
			}
			
			//****************************************
			
			await MyWaiter2;
			
			MyWaiter1.Result.Dispose();
			MyWaiter2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task LockCancelLockLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			bool Resource = false;
			Task<IDisposable> MyWaiter1, MyWaiter2;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				using (await MyLock.Wait())
				{
					MyWaiter1 = MyLock.Wait(MySource.Token);
					
					MyWaiter2 = MyLock.Wait();
					
					MySource.Cancel();
					
					Assert.IsTrue(MyWaiter1.IsCanceled, "Wait not cancelled");
					
					Assert.IsFalse(MyWaiter2.IsCompleted, "Nested lock taken");
					
					Resource = true;
				}
			}
			
			//****************************************
			
			await MyWaiter2;
			
			Assert.IsTrue(Resource, "Block not entered");
			
			MyWaiter2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public async Task LockTimeoutLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			Task<IDisposable> MyWait;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWait = MyLock.Wait(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************
			
			Thread.Sleep(100); // Release happens on another thread and there's nothing to wait on
			
			Assert.IsTrue(MyWait.IsCanceled, "Did not cancel");
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public void NoTimeoutLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			Task<IDisposable> MyWait;
			//****************************************
			
			MyWait = MyLock.Wait(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyWait.IsCompleted, "Is not completed");
			
			MyWait.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(2000)]
		public async Task ConcurrentHoldSingle()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			int Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (var MyWait = await MyLock.Wait())
						{
							Interlocked.Increment(ref Resource);
							
							await Task.Delay(1);
						}
						
						return;
					})
			);
			
			//****************************************
			
			Assert.AreEqual(100, Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000), Repeat(4)]
		public async Task ConcurrentHoldMulti([Values(5, 10, 25, 50)] int startCount)
		{	//****************************************
			var MyLock = new AsyncSemaphore(startCount);
			int Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (var MyWait = await MyLock.Wait())
						{
							Interlocked.Increment(ref Resource);
							
							await Task.Delay(10);
						}
						
						return;
					})
			);
			
			//****************************************
			
			Assert.AreEqual(100, Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ConcurrentAll()
		{	//****************************************
			var MyLock = new AsyncSemaphore(100);
			int Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (var MyWait = await MyLock.Wait())
						{
							Interlocked.Increment(ref Resource);
							
							await Task.Delay(10);
						}
						
						return;
					})
			);
			
			//****************************************
			
			Assert.AreEqual(100, Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test]
		public async Task StackBlow()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			int Depth = 0;
			Task ResultTask;
			Task[] Results;
			//****************************************

			using (await MyLock.Wait())
			{
				Results = Enumerable.Range(1, 40000).Select(
					async count => 
					{
						using (await MyLock.Wait())
						{
							Depth++;
						}
					}).ToArray();
				
				ResultTask = Results[Results.Length -1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
			}
			
			await ResultTask;
		}
		
		[Test, Timeout(1000)]
		public void LockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait = MyLock.Wait().Result;
			
			MyLock.Dispose();
			
			MyWait.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public void LockLockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait1 = MyLock.Wait();
			
			var MyWait2 = MyLock.Wait();
			
			MyLock.Dispose();
			
			try
			{
				MyWait2.Result.Dispose();
			}
			catch (AggregateException e)
			{
				Assert.IsInstanceOf(typeof(ObjectDisposedException), e.InnerException, "Inner exception not as expected");
			}
			
			MyWait1.Result.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public void DisposeLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			MyLock.Dispose();
			
			try
			{
				MyLock.Wait().Result.Dispose();
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
				
		[Test, Timeout(1000)]
		public void LockDisposeContinueWithLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyCounter = MyLock.Wait().Result;
			
			var MyTask = MyLock.Wait();
			
			var MyInnerTask = MyTask.ContinueWith((task) => MyLock.Wait(), TaskContinuationOptions.ExecuteSynchronously);
			
			MyLock.Dispose();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsFaulted, "Lock did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyTask.Exception.InnerException, "Lock is not disposed");
			Assert.IsTrue(MyInnerTask.IsFaulted, "Lock did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyInnerTask.Exception.InnerException, "Lock is not disposed");
		}
	}
}
