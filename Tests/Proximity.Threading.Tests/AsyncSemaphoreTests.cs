using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//****************************************

namespace Proximity.Threading.Tests
{
	/// <summary>
	/// Tests the functionality of the Asynchronous Semaphore
	/// </summary>
	[TestFixture]
	public sealed class AsyncSemaphoreTests
	{
		[Test, MaxTime(1000)]
		public async Task Lock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			(await MyLock.Take()).Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, MaxTime(1000)]
		public void LockLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait1 = MyLock.Take();
			
			var MyWait2 = MyLock.Take();
			
			Assert.IsFalse(MyWait2.IsCompleted, "Nested lock taken");

			//****************************************
			
			MyWait1.Result.Dispose();
			
			MyWait2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, MaxTime(1000)]
		public async Task LockLockLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore(2);
			ValueTask<AsyncSemaphore.Instance> MyWaiter1, MyWaiter2;
			//****************************************
			
			using (await MyLock.Take())
			{
				MyWaiter1 = MyLock.Take();
				
				MyWaiter2 = MyLock.Take();
				
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
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task LockCancelLockLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			var Resource = false;
			ValueTask<AsyncSemaphore.Instance> MyWaiter1, MyWaiter2;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				using (await MyLock.Take())
				{
					MyWaiter1 = MyLock.Take(MySource.Token);
					
					MyWaiter2 = MyLock.Take();
					
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
		
		[Test, MaxTime(1000)]
		public async Task LockTimeoutLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			ValueTask<AsyncSemaphore.Instance> MyWait;
			//****************************************
			
			using (await MyLock.Take())
			{
				MyWait = MyLock.Take(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************

			try
			{
				await MyWait;

				Assert.Fail("Did not cancel");
			}
			catch (TimeoutException)
			{
			}

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public void TryTake()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle), "Lock not taken");

			MyHandle.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public void TryTakeTake()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle1), "Lock not taken");

			Assert.False(MyLock.TryTake(out var MyHandle2), "Nested lock taken");

			MyHandle1.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public void TryTakeTakeZero()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle1), "Lock not taken");

			Assert.False(MyLock.TryTake(out var MyHandle2, TimeSpan.Zero), "Nested lock taken");

			MyHandle1.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task TryTakeLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle), "Lock not taken");

			var MyWait = MyLock.Take();

			Assert.IsFalse(MyWait.IsCompleted, "Nested lock taken");

			MyHandle.Dispose();

			MyHandle = await MyWait;

			MyHandle.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public void TryTakeMaxTime()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle1), "Lock not taken");

			var StartTime = DateTime.Now;

			Assert.False(MyLock.TryTake(out var MyHandle2, TimeSpan.FromMilliseconds(50)), "Nested lock taken");

			var EndTime = DateTime.Now;

			MyHandle1.Dispose();

			//****************************************

			Assert.GreaterOrEqual((EndTime - StartTime).TotalMilliseconds, 50.0, "Finished too early");

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public void TryTakeNoMaxTime()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle, TimeSpan.FromMilliseconds(50)), "Lock not taken");

			MyHandle.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public void TryTakeCancel()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle1), "Lock not taken");

			using (var MyCancel = new CancellationTokenSource())
			{
				MyCancel.CancelAfter(50);

				var StartTime = DateTime.Now;

				try
				{
					MyLock.TryTake(out _, MyCancel.Token);

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

		[Test, MaxTime(1000)]
		public void NoTimeoutLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait = MyLock.Take(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyWait.IsCompleted, "Is not completed");
			
			MyWait.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, MaxTime(2000)]
		public async Task ConcurrentHoldSingle()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			var Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (var MyWait = await MyLock.Take())
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
		
		[Test, MaxTime(2000), Repeat(4)]
		public async Task ConcurrentHoldMulti([Values(5, 10, 25, 50)] int startCount)
		{	//****************************************
			var MyLock = new AsyncSemaphore(startCount);
			var Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (var MyWait = await MyLock.Take())
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
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task ConcurrentAll()
		{	//****************************************
			var MyLock = new AsyncSemaphore(100);
			var Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (var MyWait = await MyLock.Take())
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
			var Depth = 0;
			Task ResultTask;
			Task[] Results;
			//****************************************

			using (await MyLock.Take())
			{
				Results = Enumerable.Range(1, 40000).Select(
					async count => 
					{
						using (await MyLock.Take())
						{
							Depth++;
						}
					}).ToArray();
				
				ResultTask = Results[Results.Length -1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
			}
			
			await ResultTask;
		}
		
		[Test, MaxTime(1000)]
		public async Task LockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyWait = MyLock.Take().Result;
			
			var MyDispose = MyLock.DisposeAsync();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyWait.Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}
		
		[Test, MaxTime(1000)]
		public async Task LockLockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore(2);
			//****************************************
			
			var MyWait1 = MyLock.Take();
			
			var MyWait2 = MyLock.Take();
			
			var MyDispose = MyLock.DisposeAsync();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyWait2.Result.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyWait1.Result.Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task TryTakeTakeDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore(1);
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle1), "Lock not taken");

			var MyTask = Task.Run(async () =>
				{
					await Task.Delay(50);

					await MyLock.DisposeAsync();
				});

			Assert.False(MyLock.TryTake(out var MyHandle2, Timeout.InfiniteTimeSpan), "Nested lock taken");

			MyHandle1.Dispose();

			await MyTask;

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task TryTakeTakeTimeoutDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore(1);
			//****************************************

			Assert.True(MyLock.TryTake(out var MyHandle1), "Lock not taken");

			var MyTask = Task.Run(async () =>
			{
				await Task.Delay(50);

				await MyLock.DisposeAsync();
			});

			Assert.False(MyLock.TryTake(out var MyHandle2, TimeSpan.FromMilliseconds(100)), "Nested lock taken");

			MyHandle1.Dispose();

			await MyTask;

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task LockMaxLockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			var MyWait1 = MyLock.Take();

			var MyWait2 = MyLock.Take();

			var MyDispose = MyLock.DisposeAsync();

			try
			{
				(await MyWait2).Dispose();

				Assert.Fail("Did not dispose");
			}
			catch (ObjectDisposedException)
			{
			}

			MyWait1.Result.Dispose();

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task Dispose()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			await MyLock.DisposeAsync();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task DisposeLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyDispose = MyLock.DisposeAsync();
			
			try
			{
				MyLock.Take().GetAwaiter().GetResult().Dispose();
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************

			await MyDispose;
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task DisposeTryTake()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			var MyDispose = MyLock.DisposeAsync();

			Assert.False(MyLock.TryTake(out _), "Nested lock taken");

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public async Task DisposeTryTakeZero()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************

			var MyDispose = MyLock.DisposeAsync();

			Assert.False(MyLock.TryTake(out _, TimeSpan.Zero), "Nested lock taken");

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, MaxTime(1000)]
		public void LockDisposeContinueWithLock()
		{	//****************************************
			var MyLock = new AsyncSemaphore();
			//****************************************
			
			var MyCounter = MyLock.Take().Result;
			
			var MyTask = MyLock.Take().AsTask();
			
			var MyInnerTask = MyTask.ContinueWith((task) => MyLock.Take().AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
			
			MyLock.DisposeAsync();

			//****************************************

			Assert.ThrowsAsync<ObjectDisposedException>(() => MyTask);
			Assert.ThrowsAsync<ObjectDisposedException>(() => MyInnerTask);
		}
	}
}
