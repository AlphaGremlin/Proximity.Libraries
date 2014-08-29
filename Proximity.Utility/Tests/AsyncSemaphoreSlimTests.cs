﻿/****************************************\
 AsyncSemaphoreSlimTests.cs
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
	public sealed class AsyncSemaphoreSlimTests
	{
		[Test, Timeout(1000), Repeat(10)]
		public async Task SingleLock()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
			bool Resource = false;
			//****************************************
			
			using (await MyLock.Wait())
			{
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task LockContend()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
			bool Resource = false;
			Task<IDisposable> MyWaiter;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWaiter = MyLock.Wait();
				
				Assert.IsFalse(MyWaiter.IsCompleted, "Nested lock taken");
				
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(MyWaiter.IsCompleted, "Nested lock not taken");
			
			Assert.IsTrue(Resource, "Block not entered");
			
			MyWaiter.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task MultiContend()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim(2);
			bool Resource = false;
			Task<IDisposable> MyWaiter1, MyWaiter2;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWaiter1 = MyLock.Wait();
				
				MyWaiter2 = MyLock.Wait();
				
				Assert.IsTrue(MyWaiter1.IsCompleted, "Nested lock not taken");
				
				Assert.IsFalse(MyWaiter2.IsCompleted, "Nested lock taken");
				
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(MyWaiter2.IsCompleted, "Nested lock not taken");
			
			Assert.IsTrue(Resource, "Block not entered");
			
			MyWaiter1.Result.Dispose();
			MyWaiter2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task MultiContendCancel()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
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
			
			Assert.IsTrue(MyWaiter2.IsCompleted, "Nested lock not taken");
			
			Assert.IsTrue(Resource, "Block not entered");
			
			MyWaiter2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public async Task LockContendTimeout()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
			Task<IDisposable> MyWait;
			//****************************************
			
			using (await MyLock.Wait())
			{
				MyWait = MyLock.Wait(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************
			
			Assert.IsTrue(MyWait.IsCanceled, "Did not cancel");
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public void NoTimeout()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
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
			var MyLock = new AsyncSemaphoreSlim();
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
			var MyLock = new AsyncSemaphoreSlim(startCount);
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
			var MyLock = new AsyncSemaphoreSlim(100);
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
	}
}
