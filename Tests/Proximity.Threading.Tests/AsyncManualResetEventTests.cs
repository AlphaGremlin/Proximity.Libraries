using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Threading.Tests;

[TestFixture]
public class AsyncManualResetEventTests
{
	[Test]
	public void DisposeSet()
	{
		var Event = new AsyncManualResetEvent();

		Event.Dispose();

		ClassicAssert.Throws<ObjectDisposedException>(() => Event.Set(), "Did not report disposed");
	}

	[Test]
	public void DisposeTryWait()
	{
		var Event = new AsyncManualResetEvent();

		Event.Dispose();

		ClassicAssert.Throws<ObjectDisposedException>(() => Event.TryWait(), "Did not report disposed");
	}

	[Test]
	public void DisposeTryWaitBlocking()
	{
		var Event = new AsyncManualResetEvent();

		Event.Dispose();

		ClassicAssert.Throws<ObjectDisposedException>(() => Event.TryWait(Timeout.InfiniteTimeSpan), "Did not report disposed");
	}

	[Test]
	public void DisposeWait()
	{
		var Event = new AsyncManualResetEvent();

		Event.Dispose();

		ClassicAssert.Throws<ObjectDisposedException>(() => Event.Wait().Ignore(), "Did not report disposed");
	}

	[Test]
	public void Preset()
	{
		var Event = new AsyncManualResetEvent(true);

		Assert.That(Event.IsSet, Is.True);
	}

	[Test]
	public void Set()
	{
		var Event = new AsyncManualResetEvent();

		Assert.That(Event.IsSet, Is.False);

		Event.Set();

		Assert.That(Event.IsSet, Is.True);
	}

	[Test]
	public void SetReset()
	{
		var Event = new AsyncManualResetEvent();

		Event.Set();

		Assert.That(Event.IsSet, Is.True);

		Event.Reset();

		Assert.That(Event.IsSet, Is.False);
	}

	[Test]
	public void SetResetWait()
	{
		var Event = new AsyncManualResetEvent();

		Event.Set();

		Assert.That(Event.IsSet, Is.True);

		Event.Reset();

		Assert.That(Event.IsSet, Is.False);

		var Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.False);
	}

	[Test]
	public void SetTryWait()
	{
		var Event = new AsyncManualResetEvent();

		Event.Set();

		Assert.That(Event.TryWait(), Is.True);
	}

	[Test]
	public void SetWait()
	{
		var Event = new AsyncManualResetEvent();

		Event.Set();

		var Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.True);
	}

	[Test]
	public void SetWaitWait()
	{
		var Event = new AsyncManualResetEvent();

		Event.Set();

		var Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.True);

		Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.True);
	}

	[Test]
	public void TryWait()
	{
		var Event = new AsyncManualResetEvent();

		Assert.That(Event.TryWait(), Is.False);
	}

	[Test, CancelAfter(1000)]
	public async Task TryWaitDispose(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var WaitTask = Task.Run(() => Event.TryWait(Timeout.InfiniteTimeSpan, CancellationToken.None));

		Event.Dispose();

		await Assert.ThrowsAsync<ObjectDisposedException>(() => WaitTask, "Did not report disposed").WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task TryWaitTimeout(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var WaitTask = Task.Run(() => Event.TryWait(new TimeSpan(0, 0, 0, 0, 100), CancellationToken.None), token);

		Assert.That(await WaitTask, Is.False);
	}

	[Test]
	public void Wait()
	{
		var Event = new AsyncManualResetEvent();

		var Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.False);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitCancel(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		using var CancelSource = new CancellationTokenSource();

		var WaitTask = Event.Wait(CancelSource.Token).AsTask();

		CancelSource.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() => WaitTask, "Did not cancel").WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitDispose(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var WaitTask = Event.Wait(Timeout.InfiniteTimeSpan, CancellationToken.None).AsTask();

		Event.Dispose();

		await Assert.ThrowsAsync<ObjectDisposedException>(() => WaitTask, "Did not report disposed").WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitTimeout(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var Task = Event.Wait(new TimeSpan(0, 0, 0, 0, 100), CancellationToken.None);

		Assert.That(await Task.WaitAsync(token), Is.False);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitSet(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var Task = Event.Wait(CancellationToken.None);

		Event.Set();

		await Task.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitSetWait(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var Task = Event.Wait(CancellationToken.None);

		Event.Set();

		await Task.WaitAsync(token);

		Task = Event.Wait(CancellationToken.None);

		Assert.That(Task.IsCompleted, Is.True);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitWaitSet(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var Task1 = Event.Wait(CancellationToken.None);
		var Task2 = Event.Wait(CancellationToken.None);

		Event.Set();

		await Task1.WaitAsync(token);
		await Task2.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task RepeatedWaitCancel(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var CancelSource = new CancellationTokenSource();

		var InitialCapacity = Event.Capacity;

		for (var Count = 0; Count < 10000; Count++)
		{
			var Task = Event.Wait(CancelSource.Token);

			CancelSource.Cancel();

			try
			{
				await Task.WaitAsync(token);
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}

			CancelSource = new CancellationTokenSource();
		}

		Assert.That(Event.Capacity, Is.EqualTo(InitialCapacity));
	}

	[Test, CancelAfter(1000)]
	public async Task RepeatedWaitCancelWithWait(CancellationToken token)
	{
		var Event = new AsyncManualResetEvent();

		var CancelSource = new CancellationTokenSource();

		var InitialCapacity = Event.Capacity;

		var Waiter = Event.Wait(CancellationToken.None);

		for (var Count = 0; Count < 10000; Count++)
		{
			var Task2 = Event.Wait(CancelSource.Token);

			CancelSource.Cancel();

			try
			{
				await Task2.WaitAsync(token);
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}

			CancelSource.Dispose();
			CancelSource = new CancellationTokenSource();
		}

		GC.KeepAlive(Waiter);

		CancelSource.Dispose();

		Assert.That(Event.Capacity, Is.EqualTo(InitialCapacity * 2));
	}
}
