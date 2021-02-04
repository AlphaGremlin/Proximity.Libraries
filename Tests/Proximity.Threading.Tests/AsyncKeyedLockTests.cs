using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//****************************************

namespace Proximity.Threading.Tests
{
	[TestFixture, Timeout(1000)]
	public class AsyncKeyedLockTests
	{
		[Test]
		public async Task LockNull()
		{	//****************************************
			var Lock = new AsyncKeyedLock<object>();
			//****************************************

			try
			{
				await Lock.Lock(null);

				Assert.Fail("Should not reach this point");
			}
			catch (ArgumentNullException)
			{
			}
		}

		[Test]
		public void LockStruct([Values(-1, 0, 1, int.MaxValue)] int key)
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			//****************************************

			var MyTask = Lock.Lock(key);

			//****************************************

			Assert.IsTrue(MyTask.IsCompleted, "Lock did not complete");
			CollectionAssert.AreEqual(new [] { key }, Lock.KeysHeld);
		}

		[Test]
		public void LockClass([Values(typeof(int), typeof(long), typeof(string), typeof(Version))] Type key)
		{	//****************************************
			var Lock = new AsyncKeyedLock<Type>();
			//****************************************

			var MyTask = Lock.Lock(key);

			//****************************************

			Assert.IsTrue(MyTask.IsCompleted, "Lock did not complete");
			CollectionAssert.AreEqual(new [] { key }, Lock.KeysHeld);
		}

		[Test]
		public void LockTwo([Values(-1, 0, 1)] int first, [Values(-1, 0, 1)] int second)
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			//****************************************

			var MyTask1 = Lock.Lock(first);
			var MyTask2 = Lock.Lock(second);

			//****************************************

			Assert.IsTrue(MyTask1.IsCompleted, "First lock did not complete");

			Assert.AreEqual(first != second, MyTask2.IsCompleted, "Second lock was not as expected");
			CollectionAssert.Contains(Lock.KeysHeld, first, "First key missing");
			CollectionAssert.Contains(Lock.KeysHeld, second, "Second key missing");
		}

		[Test]
		public async Task LockRelease()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			//****************************************

			var MyResult = await Lock.Lock(42);

			MyResult.Dispose();

			//****************************************

			CollectionAssert.IsEmpty(Lock.KeysHeld);
		}

		[Test]
		public async Task LockCancel()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask<AsyncKeyedLock<int>.Instance> SecondTask;
			//****************************************

			var FirstLock = await Lock.Lock(42);

			using (var MySource = new CancellationTokenSource())
			{
				SecondTask = Lock.Lock(42, MySource.Token);

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

		[Test]
		public async Task LockNoMaxTime()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask<AsyncKeyedLock<int>.Instance> SecondTask;
			//****************************************

			using (await Lock.Lock(42))
			{
				SecondTask = Lock.Lock(42, TimeSpan.FromMilliseconds(50.0));
			}

			//****************************************

			(await SecondTask).Dispose();

			CollectionAssert.IsEmpty(Lock.KeysHeld);
		}

		[Test]
		public async Task LockInstant()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			//****************************************

			using (await Lock.Lock(42, TimeSpan.FromMilliseconds(50.0)))
			{
			}

			//****************************************

			CollectionAssert.IsEmpty(Lock.KeysHeld);
		}

		[Test]
		public async Task LockMaxTime()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask<AsyncKeyedLock<int>.Instance> SecondTask;
			//****************************************

			using (await Lock.Lock(42))
			{
				SecondTask = Lock.Lock(42, TimeSpan.FromMilliseconds(50.0));

				Thread.Sleep(100);
			}

			//****************************************

			try
			{
				await SecondTask;

				Assert.Fail("Should not reach this point");
			}
			catch (TimeoutException)
			{
			}
		}

		[Test]
		public async Task LockCancelLock()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask<AsyncKeyedLock<int>.Instance> SecondTask;
			//****************************************

			var FirstLock = await Lock.Lock(42);

			using (var MySource = new CancellationTokenSource())
			{
				SecondTask = Lock.Lock(42, MySource.Token);

				MySource.Cancel();
			}

			var ThirdLockTask = Lock.Lock(42);

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

		[Test]
		public async Task Dispose()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			//****************************************

			await Lock.DisposeAsync();
		}

		[Test]
		public async Task DisposeLock()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			//****************************************

			await Lock.DisposeAsync();

			try
			{
				await Lock.Lock(42);

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test]
		public async Task LockDispose()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask MyDisposeTask;
			//****************************************

			using (await Lock.Lock(42))
			{
				MyDisposeTask = Lock.DisposeAsync();
			}

			await MyDisposeTask;
		}

		[Test]
		public async Task LockLockDispose()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask MyDisposeTask;
			ValueTask<AsyncKeyedLock<int>.Instance> MyInnerTask;
			//****************************************

			using (await Lock.Lock(42))
			{
				MyInnerTask = Lock.Lock(42);

				MyDisposeTask = Lock.DisposeAsync();
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

		[Test]
		public async Task LockDisposeLock()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask MyDisposeTask;
			//****************************************

			using (await Lock.Lock(42))
			{
				MyDisposeTask = Lock.DisposeAsync();

				try
				{
					await Lock.Lock(42);

					Assert.Fail("Should never reach this point");
				}
				catch (ObjectDisposedException)
				{
				}
			}

			await MyDisposeTask;
		}

		[Test, MaxTime(2000), Repeat(10)]
		public async Task ConcurrentFast()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			var Resource = 0;
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise

						using (var MyWait = await Lock.Lock(count % 10))
						{
							Interlocked.Increment(ref Resource);

							await Task.Delay(10);
						}

						return;
					})
			);

			//****************************************

			Assert.AreEqual(100, Resource, "Block not entered");

			CollectionAssert.IsEmpty(Lock.KeysHeld, "Lock still held");
		}
			
		[Test, Repeat(4)]
		public async Task ConcurrentSlow()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			var Resource = 0;
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						if (count % 10 == 0)
							Thread.Sleep(1);

						await Task.Yield(); // Yield, so it doesn't serialise

						using (var MyWait = await Lock.Lock(count % 10))
						{
							Interlocked.Increment(ref Resource);

							await Task.Delay(10);
						}

						return;
					})
			);

			//****************************************

			Assert.AreEqual(100, Resource, "Block not entered");

			CollectionAssert.IsEmpty(Lock.KeysHeld, "Lock still held");
		}

		[Test, Repeat(10)]
		public async Task ConcurrentDispose()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			ValueTask MyDisposeTask;
			//****************************************

			var WorkTask = ConcurrentLockRelease(Lock, 42, 0, CancellationToken.None);

			await Task.Delay(50);

			MyDisposeTask = Lock.DisposeAsync();

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

		[Test, MaxTime(2000), Repeat(2)]
		public async Task ConcurrentMany()
		{	//****************************************
			var Lock = new AsyncKeyedLock<int>();
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				var WorkTask1 = ConcurrentLockRelease(Lock, 42, 0, MySource.Token);
				var WorkTask2 = ConcurrentLockRelease(Lock, 42, 1, MySource.Token);
				var WorkTask3 = ConcurrentLockRelease(Lock, 42, 2, MySource.Token);

				await Task.Delay(500);

				MySource.Cancel();

				await WorkTask1;
				await WorkTask2;
				await WorkTask3;
			}

			await Lock.DisposeAsync();

			//****************************************

			CollectionAssert.IsEmpty(Lock.KeysHeld);
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
