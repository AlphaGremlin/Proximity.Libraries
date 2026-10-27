using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

[TestFixture, CancelAfter(1000)]
public class AsyncKeyedLockTests
{
	[Test]
	public async Task LockNull()
	{ //****************************************
		var Lock = new AsyncKeyedLock<object>();
		//****************************************

		await Assert.ThrowsAsync<ArgumentNullException>(() => Lock.Lock(null!).AsTask());
	}

	[Test]
	public void LockStruct([Values(-1, 0, 1, int.MaxValue)] int key)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		var LockTask = Lock.Lock(key);

		//****************************************

		Assert.That(LockTask.IsCompleted, Is.True, "Lock did not complete");
		Assert.That(Lock.KeysHeld, Is.EqualTo(new[] { key }).AsCollection);
	}

	[Test]
	public void LockClass([Values(typeof(int), typeof(long), typeof(string), typeof(Version))] Type key)
	{ //****************************************
		var Lock = new AsyncKeyedLock<Type>();
		//****************************************

		var LockTask = Lock.Lock(key);

		//****************************************

		Assert.That(LockTask.IsCompleted, Is.True, "Lock did not complete");
		Assert.That(Lock.KeysHeld, Is.EqualTo(new[] { key }).AsCollection);
	}

	[Test]
	public void LockTwo([Values(-1, 0, 1)] int first, [Values(-1, 0, 1)] int second)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		var LockTask1 = Lock.Lock(first);
		var LockTask2 = Lock.Lock(second);

		//****************************************

		Assert.That(LockTask1.IsCompleted, Is.True, "First lock did not complete");

		Assert.That(LockTask2.IsCompleted, Is.EqualTo(first != second), "Second lock was not as expected");
		Assert.That(Lock.KeysHeld, Has.Member(first), "First key missing");
		Assert.That(Lock.KeysHeld, Has.Member(second), "Second key missing");
	}

	[Test, CancelAfter(1000)]
	public async Task LockRelease(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		var Instance = await Lock.Lock(42, CancellationToken.None).WaitAsync(token);

		Instance.Dispose();

		//****************************************

		Assert.That(Lock.KeysHeld, Is.Empty);
	}

	[Test, CancelAfter(1000)]
	public async Task LockCancel(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var FirstLock = await Lock.Lock(42, CancellationToken.None).WaitAsync(token);

		var SecondTask = Lock.Lock(42, CancelSource.Token);

		CancelSource.Cancel();

		//****************************************

		try
		{
			await SecondTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		GC.KeepAlive(FirstLock);
	}

	[Test, CancelAfter(1000)]
	public async Task LockNoMaxTime(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		ValueTask<AsyncKeyedLock<int>.Instance> SecondTask;
		//****************************************

		using (await Lock.Lock(42, CancellationToken.None).WaitAsync(token))
		{
			SecondTask = Lock.Lock(42, TimeSpan.FromMilliseconds(50.0), CancellationToken.None);
		}

		//****************************************

		(await SecondTask.WaitAsync(token)).Dispose();

		Assert.That(Lock.KeysHeld, Is.Empty);
	}

	[Test, CancelAfter(1000)]
	public async Task LockInstant(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		using (await Lock.Lock(42, TimeSpan.FromMilliseconds(50.0), CancellationToken.None).WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.KeysHeld, Is.Empty);
	}

	[Test, CancelAfter(1000)]
	public async Task LockMaxTime(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		ValueTask<AsyncKeyedLock<int>.Instance> SecondTask;
		//****************************************

		using (await Lock.Lock(42, CancellationToken.None).WaitAsync(token))
		{
			SecondTask = Lock.Lock(42, TimeSpan.FromMilliseconds(50.0), CancellationToken.None);

			await Task.Delay(100, token);
		}

		//****************************************

		try
		{
			await SecondTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (TimeoutException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task LockCancelLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		ValueTask<AsyncKeyedLock<int>.Instance> SecondTask;
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var FirstLock = await Lock.Lock(42, CancellationToken.None);

		SecondTask = Lock.Lock(42, CancelSource.Token);

		CancelSource.Cancel();

		var ThirdLockTask = Lock.Lock(42, CancellationToken.None);

		Assert.That(ThirdLockTask.IsCompleted, Is.False, "Third lock completed early");

		FirstLock.Dispose();

		var ThirdLock = await ThirdLockTask.WaitAsync(token);

		//****************************************

		try
		{
			await SecondTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task Dispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		await Lock.DisposeAsync().WaitAsync(token);
		;
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		await Lock.DisposeAsync().WaitAsync(token);
		;

		try
		{
			await Lock.Lock(42, CancellationToken.None).WaitAsync(token);

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task LockDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		ValueTask Dispose;
		//****************************************

		using (await Lock.Lock(42, CancellationToken.None).WaitAsync(token))
		{
			Dispose = Lock.DisposeAsync();
		}

		await Dispose.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task LockLockDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		ValueTask DisposeTask;
		ValueTask<AsyncKeyedLock<int>.Instance> InnerLockTask;
		//****************************************

		using (await Lock.Lock(42, CancellationToken.None).WaitAsync(token))
		{
			InnerLockTask = Lock.Lock(42, CancellationToken.None);

			DisposeTask = Lock.DisposeAsync();
		}

		await DisposeTask.WaitAsync(token);

		//****************************************

		try
		{
			(await InnerLockTask.WaitAsync(token)).Dispose();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task LockDisposeLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		ValueTask DisposeTask;
		//****************************************

		using (await Lock.Lock(42, CancellationToken.None).WaitAsync(token))
		{
			DisposeTask = Lock.DisposeAsync();

			try
			{
				await Lock.Lock(42, CancellationToken.None).WaitAsync(token);

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		await DisposeTask;
	}

	[Test, CancelAfter(2000), Repeat(10)]
	public async Task ConcurrentFast(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.Lock(count % 10, CancellationToken.None))
					{
						Interlocked.Increment(ref Resource);

						await Task.Delay(10);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.KeysHeld, Is.Empty, "Lock still held");
	}

	[Test, CancelAfter(2000), Repeat(4)]
	public async Task ConcurrentSlow(CancellationToken token)
	{ //****************************************
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

					using (await Lock.Lock(count % 10, CancellationToken.None))
					{
						Interlocked.Increment(ref Resource);

						await Task.Delay(10);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.KeysHeld, Is.Empty, "Lock still held");
	}

	[Test, CancelAfter(2000), Repeat(10)]
	public async Task ConcurrentDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		ValueTask MyDisposeTask;
		//****************************************

		var WorkTask = ConcurrentLockRelease(Lock, 42, 0, CancellationToken.None);

		await Task.Delay(50, token);

		MyDisposeTask = Lock.DisposeAsync();

		try
		{
			await WorkTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		await MyDisposeTask;
	}

	[Test, CancelAfter(2000), Repeat(2)]
	public async Task ConcurrentMany(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncKeyedLock<int>();
		//****************************************

		using (var CancelSource = new CancellationTokenSource())
		{
			var WorkTask1 = ConcurrentLockRelease(Lock, 42, 0, CancelSource.Token);
			var WorkTask2 = ConcurrentLockRelease(Lock, 42, 1, CancelSource.Token);
			var WorkTask3 = ConcurrentLockRelease(Lock, 42, 2, CancelSource.Token);

			await Task.Delay(500, token);

			CancelSource.Cancel();

			await WorkTask1.WaitAsync(token);
			await WorkTask2.WaitAsync(token);
			await WorkTask3.WaitAsync(token);
		}

		await Lock.DisposeAsync().WaitAsync(token);

		//****************************************

		Assert.That(Lock.KeysHeld, Is.Empty);
	}

	//****************************************

	private async static Task ConcurrentLockRelease<TKey>(AsyncKeyedLock<TKey> keyedLock, TKey key, int delay, CancellationToken token) where TKey : notnull
	{
		while (!token.IsCancellationRequested)
		{
			using (await keyedLock.Lock(key, CancellationToken.None))
			{
				if (delay == 0)
					await Task.Yield();
				else
					await Task.Delay(delay, CancellationToken.None);
			}
		}
	}
}
