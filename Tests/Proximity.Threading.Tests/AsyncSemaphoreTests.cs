using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Tests the functionality of the Asynchronous Semaphore
/// </summary>
[TestFixture]
public sealed class AsyncSemaphoreTests
{
	[Test, CancelAfter(1000)]
	public async Task Lock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		using (await Lock.Take(CancellationToken.None).WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public void LockLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		var Wait1 = Lock.Take(CancellationToken.None).AsTask();

		var Wait2 = Lock.Take(CancellationToken.None).AsTask();

		Assert.That(Wait2.IsCompleted, Is.False, "Nested lock taken");

		//****************************************

		Wait1.Result.Dispose();

		Wait2.Result.Dispose();

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task LockLockLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore(2);
		Task<AsyncSemaphore.Instance> MyWaiter1, MyWaiter2;
		//****************************************

		using (await Lock.Take(CancellationToken.None))
		{
			MyWaiter1 = Lock.Take(CancellationToken.None).AsTask();

			MyWaiter2 = Lock.Take(CancellationToken.None).AsTask();

			Assert.IsTrue(MyWaiter1.IsCompleted, "Nested lock not taken");

			Assert.IsFalse(MyWaiter2.IsCompleted, "Nested lock taken");
		}

		//****************************************

		await MyWaiter2;

		MyWaiter1.Result.Dispose();
		MyWaiter2.Result.Dispose();

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task LockCancelLockLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		var Resource = false;
		Task<AsyncSemaphore.Instance> MyWaiter1, MyWaiter2;
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		using (await Lock.Take(CancellationToken.None))
		{
			MyWaiter1 = Lock.Take(CancelSource.Token).AsTask();

			MyWaiter2 = Lock.Take(CancellationToken.None).AsTask();

			CancelSource.Cancel();

			// Cancellation happens off-thread
			await Task.Delay(100, token);

			Assert.IsTrue(MyWaiter1.IsCanceled, "Wait not cancelled");

			Assert.IsFalse(MyWaiter2.IsCompleted, "Nested lock taken");

			Resource = true;
		}

		//****************************************

		await MyWaiter2;

		Assert.That(Resource, Is.True, "Block not entered");

		MyWaiter2.Result.Dispose();

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task LockTimeoutLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		ValueTask<AsyncSemaphore.Instance> MyWait;
		//****************************************

		using (await Lock.Take(CancellationToken.None))
		{
			MyWait = Lock.Take(TimeSpan.FromMilliseconds(50), CancellationToken.None);

			await Task.Delay(100, token);
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

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test]
	public void TryTake()
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		Handle.Dispose();

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test]
	public void TryTakeTake()
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		Assert.That(Lock.TryTake(out _), Is.False, "Nested lock taken");

		Handle.Dispose();

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test]
	public void TryTakeTakeZero()
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		Assert.That(Lock.TryTake(out _, TimeSpan.Zero), Is.False, "Nested lock taken");

		Handle.Dispose();

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		var Wait = Lock.Take(CancellationToken.None);

		Assert.That(Wait.IsCompleted, Is.False, "Nested lock taken");

		Handle.Dispose();

		Handle = await Wait.WaitAsync(token);

		Handle.Dispose();

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeMaxTime(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		var StartTime = DateTime.Now;

		var Take = Task.Run(() => Lock.TryTake(out _, TimeSpan.FromMilliseconds(50)));

		Assert.That(await Take.WaitAsync(token), Is.False, "Nested lock taken");

		var EndTime = DateTime.Now;

		Handle.Dispose();

		//****************************************

		Assert.That((EndTime - StartTime).TotalMilliseconds, Is.GreaterThanOrEqualTo(50.0), "Finished too early");

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeNoMaxTime(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		AsyncSemaphore.Instance Handle = default;
		//****************************************

		var Take = Task.Run(() => Lock.TryTake(out Handle, TimeSpan.FromMilliseconds(50)));

		Assert.That(await Take.WaitAsync(token), Is.True, "Lock not taken");

		Handle.Dispose();

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeCancel(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		using (var CancelSource = new CancellationTokenSource())
		{
			CancelSource.CancelAfter(50);

			var StartTime = DateTime.Now;

			try
			{
				var Take = Task.Run(() => Lock.TryTake(out _, CancelSource.Token));

				await Take.WaitAsync(token);

				Assert.Fail("Nested lock taken");
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}

			var EndTime = DateTime.Now;

			Assert.GreaterOrEqual((EndTime - StartTime).TotalMilliseconds, 50.0, "Finished too early");
		}

		Handle.Dispose();

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task NoTimeoutLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		var Wait = Lock.Take(TimeSpan.FromMilliseconds(50));

		//****************************************

		Assert.That(Wait.IsCompleted, Is.True, "Is not completed");

		using (await Wait.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(2000)]
	public async Task ConcurrentHoldSingle(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.Take(CancellationToken.None))
					{
						Interlocked.Increment(ref Resource);

						await Task.Delay(1);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(2000), Repeat(4)]
	public async Task ConcurrentHoldMulti([Values(5, 10, 25, 50)] int startCount, CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore(startCount);
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.Take(CancellationToken.None))
					{
						Interlocked.Increment(ref Resource);

						await Task.Delay(10);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task ConcurrentAll(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore(100);
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.Take(CancellationToken.None))
					{
						Interlocked.Increment(ref Resource);

						await Task.Delay(10);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test]
	public async Task StackBlow()
	{ //****************************************
		var Lock = new AsyncSemaphore();
		var Depth = 0;
		Task ResultTask;
		Task[] Results;
		//****************************************

		using (await Lock.Take(CancellationToken.None))
		{
			Results = Enumerable.Range(1, 40000).Select(
				async count =>
				{
					using (await Lock.Take(CancellationToken.None))
					{
						Depth++;
					}
				}).ToArray();

			ResultTask = Results[Results.Length - 1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
		}

		await ResultTask;
	}

	[Test, CancelAfter(1000)]
	public async Task LockDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		var Wait = await Lock.Take(CancellationToken.None).WaitAsync(token);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		Wait.Dispose();

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task LockLockDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore(2);
		//****************************************

		var Wait1 = Lock.Take(CancellationToken.None).AsTask();

		var Wait2 = Lock.Take(CancellationToken.None).AsTask();

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Wait2.WaitAsync(token))
		{
		}

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Wait1.WaitAsync(token))
		{
		}

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeTakeDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore(1);
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		var DisposeTask = Task.Run(async () =>
			{
				await Task.Delay(50, token);

				await Lock.DisposeAsync();
			}, token);

		var WaitTask = Task.Run(() => Lock.TryTake(out _, Timeout.InfiniteTimeSpan));

		Assert.That(await WaitTask, Is.False, "Nested lock taken");

		Handle.Dispose();

		await DisposeTask;

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeTakeTimeoutDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore(1);
		//****************************************

		Assert.That(Lock.TryTake(out var Handle), Is.True, "Lock not taken");

		var DisposeTask = Task.Run(async () =>
		{
			await Task.Delay(50, token);

			await Lock.DisposeAsync();
		}, token);

		var WaitTask = Task.Run(() => Lock.TryTake(out _, TimeSpan.FromMilliseconds(100)), token);

		Assert.That(await WaitTask, Is.False, "Nested lock taken");

		Handle.Dispose();

		await DisposeTask;

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task LockMaxLockDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		var Wait1 = Lock.Take(CancellationToken.None).AsTask();

		var Wait2 = Lock.Take(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		try
		{
			using (await Wait2.WaitAsync(token))
			{
			}

			Assert.Fail("Did not dispose");
		}
		catch (ObjectDisposedException)
		{
		}

		using (await Wait1.WaitAsync(token))
		{
		}

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task Dispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		await Lock.DisposeAsync().WaitAsync(token);

		//****************************************

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		var Dispose = Lock.DisposeAsync();

		try
		{
			using (await Lock.Take(CancellationToken.None).WaitAsync(token))
			{
			}
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		await Dispose;

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeTryTake(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		var Dispose = Lock.DisposeAsync();

		Assert.That(Lock.TryTake(out _), Is.False, "Nested lock taken");

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeTryTakeZero(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		var Dispose = Lock.DisposeAsync();

		Assert.That(Lock.TryTake(out _, TimeSpan.Zero), Is.False, "Nested lock taken");

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingCount, Is.Zero, "Still waiting for Semaphore");
		Assert.That(Lock.CurrentCount, Is.EqualTo(Lock.MaxCount), "Semaphore still held");
	}

	[Test, CancelAfter(1000)]
	public async Task LockDisposeContinueWithLock(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSemaphore();
		//****************************************

		// Succeeds
		var Handle = await Lock.Take(CancellationToken.None).WaitAsync(token);

		// Should block, then throw once we dispose
		var TakeTask = Lock.Take(CancellationToken.None).AsTask();

		// Should throw immediately
		var InnerTakeTask = TakeTask.ContinueWith((task) => Lock.Take(CancellationToken.None).AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();

		// Dispose, causing TakeTask to throw
		var Dispose = Lock.DisposeAsync();

		//****************************************

		await Assert.ThrowsAsync<ObjectDisposedException>(() => TakeTask);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => InnerTakeTask);

		// Releases the last lock, so dispose can complete
		Handle.Dispose();

		await Dispose.WaitAsync(token);
	}
}
