/****************************************\
 AsyncKeyedLockTests.cs
 Created: 2016-04-11
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
	[TestFixture]
	public class AsyncKeyedLockTests
	{
		[Test, Timeout(1000)]
		public async Task LockNull()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<object>();
			//****************************************

			try
			{
				await MyLock.Lock(null);

				Assert.Fail("Should not reach this point");
			}
			catch (ArgumentNullException)
			{
			}
		}

		[Test, Timeout(1000)]
		public void LockStruct([Values(-1, 0, 1, int.MaxValue)] int key)
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			//****************************************

			var MyTask = MyLock.Lock(key);

			//****************************************

			Assert.IsTrue(MyTask.IsCompleted, "Lock did not complete");
			CollectionAssert.AreEqual(new int[] { key }, MyLock.KeysHeld);
		}

		[Test, Timeout(1000)]
		public void LockClass([Values(typeof(int), typeof(long), typeof(string), typeof(Version))] Type key)
		{	//****************************************
			var MyLock = new AsyncKeyedLock<Type>();
			//****************************************

			var MyTask = MyLock.Lock(key);

			//****************************************

			Assert.IsTrue(MyTask.IsCompleted, "Lock did not complete");
			CollectionAssert.AreEqual(new Type[] { key }, MyLock.KeysHeld);
		}

		[Test, Timeout(1000)]
		public void LockTwo([Values(-1, 0, 1)] int first, [Values(-1, 0, 1)] int second)
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			//****************************************

			var MyTask1 = MyLock.Lock(first);
			var MyTask2 = MyLock.Lock(second);

			//****************************************

			Assert.IsTrue(MyTask1.IsCompleted, "First lock did not complete");

			Assert.AreEqual(first != second, MyTask2.IsCompleted, "Second lock was not as expected");
			CollectionAssert.Contains(MyLock.KeysHeld, first, "First key missing");
			CollectionAssert.Contains(MyLock.KeysHeld, second, "Second key missing");
		}

		[Test, Timeout(1000)]
		public async Task LockRelease()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			//****************************************

			var MyResult = await MyLock.Lock(42);

			MyResult.Dispose();

			//****************************************

			CollectionAssert.IsEmpty(MyLock.KeysHeld);
		}

		[Test, Timeout(1000)]
		public async Task LockCancel()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task<IDisposable> SecondTask;
			//****************************************

			var FirstLock = await MyLock.Lock(42);

			using (var MySource = new CancellationTokenSource())
			{
				SecondTask = MyLock.Lock(42, MySource.Token);

				MySource.Cancel();
			}

			//****************************************

			try
			{
				await SecondTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task LockNoTimeout()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task<IDisposable> SecondTask;
			//****************************************

			using (await MyLock.Lock(42))
			{
				SecondTask = MyLock.Lock(42, TimeSpan.FromMilliseconds(50.0));
			}

			//****************************************

			(await SecondTask).Dispose();

			CollectionAssert.IsEmpty(MyLock.KeysHeld);
		}

		[Test, Timeout(1000)]
		public async Task LockInstant()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			//****************************************

			using (await MyLock.Lock(42, TimeSpan.FromMilliseconds(50.0)))
			{
			}

			//****************************************

			CollectionAssert.IsEmpty(MyLock.KeysHeld);
		}

		[Test, Timeout(1000)]
		public async Task LockTimeout()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task<IDisposable> SecondTask;
			//****************************************

			using (await MyLock.Lock(42))
			{
				SecondTask = MyLock.Lock(42, TimeSpan.FromMilliseconds(50.0));

				Thread.Sleep(100);
			}

			//****************************************

			try
			{
				await SecondTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task LockCancelLock()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task<IDisposable> SecondTask;
			//****************************************

			var FirstLock = await MyLock.Lock(42);

			using (var MySource = new CancellationTokenSource())
			{
				SecondTask = MyLock.Lock(42, MySource.Token);

				MySource.Cancel();
			}

			var ThirdLockTask = MyLock.Lock(42);

			Assert.IsFalse(ThirdLockTask.IsCompleted, "Third lock completed early");

			FirstLock.Dispose();

			var ThirdLock = await ThirdLockTask;

			//****************************************

			try
			{
				await SecondTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task Dispose()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			//****************************************

			await MyLock.Dispose();
		}

		[Test, Timeout(1000)]
		public async Task DisposeLock()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			//****************************************

			await MyLock.Dispose();

			try
			{
				await MyLock.Lock(42);

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task LockDispose()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task MyDisposeTask;
			//****************************************

			using (await MyLock.Lock(42))
			{
				MyDisposeTask = MyLock.Dispose();
			}

			await MyDisposeTask;
		}

		[Test, Timeout(1000)]
		public async Task LockLockDispose()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task MyDisposeTask;
			Task<IDisposable> MyInnerTask;
			//****************************************

			using (await MyLock.Lock(42))
			{
				MyInnerTask = MyLock.Lock(42);

				MyDisposeTask = MyLock.Dispose();
			}

			await MyDisposeTask;

			//****************************************

			try
			{
				(await MyInnerTask).Dispose();

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task LockDisposeLock()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task MyDisposeTask;
			Task<IDisposable> MyInnerTask;
			//****************************************

			using (await MyLock.Lock(42))
			{
				MyDisposeTask = MyLock.Dispose();

				try
				{
					MyInnerTask = MyLock.Lock(42);

					Assert.Fail("Should never reach this point");
				}
				catch (ObjectDisposedException)
				{
				}
			}

			await MyDisposeTask;
		}

		[Test, Timeout(2000), Repeat(10)]
		public async Task ConcurrentFast()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			int Resource = 0;
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise

						using (var MyWait = await MyLock.Lock(count % 10))
						{
							Interlocked.Increment(ref Resource);

							await Task.Delay(10);
						}

						return;
					})
			);

			//****************************************

			Assert.AreEqual(100, Resource, "Block not entered");

			CollectionAssert.IsEmpty(MyLock.KeysHeld, "Lock still held");
		}
			
		[Test, Timeout(1000), Repeat(4)]
		public async Task ConcurrentSlow()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			int Resource = 0;
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						if (count % 10 == 0)
							Thread.Sleep(1);

						await Task.Yield(); // Yield, so it doesn't serialise

						using (var MyWait = await MyLock.Lock(count % 10))
						{
							Interlocked.Increment(ref Resource);

							await Task.Delay(10);
						}

						return;
					})
			);

			//****************************************

			Assert.AreEqual(100, Resource, "Block not entered");

			CollectionAssert.IsEmpty(MyLock.KeysHeld, "Lock still held");
		}

		[Test, Timeout(1000), Repeat(10)]
		public async Task ConcurrentDispose()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			Task MyDisposeTask;
			//****************************************

			var WorkTask = ConcurrentLockRelease(MyLock, 42, 0, CancellationToken.None);

			await Task.Delay(50);

			MyDisposeTask = MyLock.Dispose();

			try
			{
				await WorkTask;

				Assert.Fail("Should not reach this point");
			}
			catch (ObjectDisposedException)
			{
			}

			await MyDisposeTask;
		}

		[Test, Timeout(2000), Repeat(2)]
		public async Task ConcurrentMany()
		{	//****************************************
			var MyLock = new AsyncKeyedLock<int>();
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				var WorkTask1 = ConcurrentLockRelease(MyLock, 42, 0, MySource.Token);
				var WorkTask2 = ConcurrentLockRelease(MyLock, 42, 1, MySource.Token);
				var WorkTask3 = ConcurrentLockRelease(MyLock, 42, 2, MySource.Token);

				await Task.Delay(500);

				MySource.Cancel();

				await WorkTask1;
				await WorkTask2;
				await WorkTask3;
			}

			await MyLock.Dispose();

			//****************************************

			CollectionAssert.IsEmpty(MyLock.KeysHeld);
		}

		//****************************************

		private async Task ConcurrentLockRelease<TKey>(AsyncKeyedLock<TKey> keyedLock, TKey key, int delay, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				using (await keyedLock.Lock(key))
				{
					if (delay == 0)
						await Task.Yield();
					else
						await Task.Delay(delay);
				}
			}
		}
	}
}
