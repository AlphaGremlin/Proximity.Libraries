/****************************************\
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
		[Test, Timeout(1000)]
		public void Lock()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
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
			var MyLock = new AsyncSemaphoreSlim();
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
			var MyLock = new AsyncSemaphoreSlim(2);
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

			await MyWaiter2;

			Assert.IsTrue(Resource, "Block not entered");

			MyWaiter2.Result.Dispose();

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task LockTimeoutLock()
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

			Thread.Sleep(100); // Release happens on another thread and there's nothing to wait on

			Assert.IsTrue(MyWait.IsCanceled, "Did not cancel");

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public void NoTimeoutLock()
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

		[Test]
		public async Task StackBlow()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
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

				ResultTask = Results[Results.Length - 1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
			}

			await ResultTask;
		}

		[Test, Timeout(1000)]
		public async Task LockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
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
			var MyLock = new AsyncSemaphoreSlim(2);
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
		public async Task LockMaxLockDispose()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
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
			var MyLock = new AsyncSemaphoreSlim();
			//****************************************

			await MyLock.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingCount, "Still waiting for Semaphore");
			Assert.AreEqual(MyLock.MaxCount, MyLock.CurrentCount, "Semaphore still held");
		}

		[Test, Timeout(1000)]
		public async Task DisposeLock()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
			//****************************************

			var MyDispose = MyLock.Dispose();

			try
			{
				MyLock.Wait().Result.Dispose();

				Assert.Fail("Wait did not throw disposed");
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
		public async Task LockDisposeContinueWithLock()
		{	//****************************************
			var MyLock = new AsyncSemaphoreSlim();
			//****************************************

			var MyCounter = MyLock.Wait().Result;

			var MyTask = MyLock.Wait();

			var MyInnerTask = MyTask.ContinueWith((task) => MyLock.Wait(), TaskContinuationOptions.ExecuteSynchronously);

			var MyDisposeTask = MyLock.Dispose();

			GC.KeepAlive(MyCounter);

			try
			{
				await MyTask;

				Assert.Fail("Wait did not throw disposed");
			}
			catch (ObjectDisposedException)
			{
			}

			try
			{
				await MyInnerTask;

				Assert.Fail("Wait did not throw disposed");
			}
			catch (ObjectDisposedException)
			{
			}

			//****************************************

			Assert.IsTrue(MyTask.IsFaulted, "Lock did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyTask.Exception.InnerException, "Lock is not disposed");
			Assert.IsTrue(MyInnerTask.IsFaulted, "Lock did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyInnerTask.Exception.InnerException, "Lock is not disposed");
		}
	}
}
