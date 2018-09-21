/****************************************\
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

			try
			{
				await MyWait;

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public void TryTake()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle), "Lock not taken");

			MyHandle.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public void TryTakeTake()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle1, MyHandle2;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle1), "Lock not taken");

			Assert.False(MyLock.TryTake(out MyHandle2), "Nested lock taken");

			MyHandle1.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public void TryTakeTakeZero()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle1, MyHandle2;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle1), "Lock not taken");

			Assert.False(MyLock.TryTake(out MyHandle2, TimeSpan.Zero), "Nested lock taken");

			MyHandle1.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task TryTakeLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle), "Lock not taken");

			var MyWait = MyLock.Wait();

			Assert.IsFalse(MyWait.IsCompleted, "Nested lock taken");

			MyHandle.Dispose();

			MyHandle = await MyWait;

			MyHandle.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public void TryTakeTimeout()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle1, MyHandle2;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle1), "Lock not taken");

			var StartTime = DateTime.Now;

			Assert.False(MyLock.TryTake(out MyHandle2, TimeSpan.FromMilliseconds(50)), "Nested lock taken");

			var EndTime = DateTime.Now;

			MyHandle1.Dispose();

			//****************************************

			Assert.GreaterOrEqual((EndTime - StartTime).TotalMilliseconds, 50.0, "Finished too early");

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public void TryTakeNoTimeout()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle, TimeSpan.FromMilliseconds(50)), "Lock not taken");

			MyHandle.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public void TryTakeCancel()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle1, MyHandle2;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle1), "Lock not taken");

			using (var MyCancel = new CancellationTokenSource())
			{
				MyCancel.CancelAfter(50);

				var StartTime = DateTime.Now;

				try
				{
					MyLock.TryTake(out MyHandle2, MyCancel.Token);

					Assert.Fail("Nested lock taken");
				}
				catch (OperationCanceledException)
				{
				}

				var EndTime = DateTime.Now;

				Assert.GreaterOrEqual((EndTime - StartTime).TotalMilliseconds, 50.0, "Finished too early");
			}
			
			MyHandle1.Dispose();

			//****************************************

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
		
		[Test, Timeout(2000), Repeat(4)]
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
		public async Task LockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait = MyLock.Wait().Result;
			
			var MyDispose = MyLock.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyWait.Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, Timeout(1000)]
		public async Task LockLockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore(2);
			//****************************************
			
			var MyWait1 = MyLock.Wait();
			
			var MyWait2 = MyLock.Wait();
			
			var MyDispose = MyLock.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyWait2.Result.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyWait1.Result.Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task TryTakeTakeDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore(1);
			IDisposable MyHandle1, MyHandle2;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle1), "Lock not taken");

			var MyTask = Task.Run(async () =>
				{
					await Task.Delay(50);

					await MyLock.Dispose();
				});

			Assert.False(MyLock.TryTake(out MyHandle2, Timeout.InfiniteTimeSpan), "Nested lock taken");

			MyHandle1.Dispose();

			await MyTask;

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task TryTakeTakeTimeoutDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore(1);
			IDisposable MyHandle1, MyHandle2;
			//****************************************

			Assert.True(MyLock.TryTake(out MyHandle1), "Lock not taken");

			var MyTask = Task.Run(async () =>
			{
				await Task.Delay(50);

				await MyLock.Dispose();
			});

			Assert.False(MyLock.TryTake(out MyHandle2, TimeSpan.FromMilliseconds(100)), "Nested lock taken");

			MyHandle1.Dispose();

			await MyTask;

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task LockMaxLockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			var MyWait1 = MyLock.Wait();

			var MyWait2 = MyLock.Wait();

			var MyDispose = MyLock.Dispose();

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

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task Dispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			await MyLock.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task DisposeLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyDispose = MyLock.Dispose();
			
			try
			{
				MyLock.Wait().GetAwaiter().GetResult().Dispose();
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************

			await MyDispose;
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task DisposeTryTake()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle;
			//****************************************

			var MyDispose = MyLock.Dispose();

			Assert.False(MyLock.TryTake(out MyHandle), "Nested lock taken");

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task DisposeTryTakeZero()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			IDisposable MyHandle;
			//****************************************

			var MyDispose = MyLock.Dispose();

			Assert.False(MyLock.TryTake(out MyHandle, TimeSpan.Zero), "Nested lock taken");

			//****************************************

			await MyDispose;

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
			
			var MyInnerTask = MyTask.ContinueWith((task) => MyLock.Wait(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
			
			MyLock.Dispose();

			//****************************************

			Assert.ThrowsAsync<ObjectDisposedException>(() => MyTask);
			Assert.ThrowsAsync<ObjectDisposedException>(() => MyInnerTask);
		}
	}
}
