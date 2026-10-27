using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Threading.Tests;

[TestFixture]
public class AsyncAutoResetEventTests
{
	[Test]
	public void DisposeSet()
	{
		var Event = new AsyncAutoResetEvent();

		Event.Dispose();

		try
		{
			Event.Set();

			Assert.Fail("Did not throw");
		}
		catch (ObjectDisposedException)
		{
		}
	}

	[Test]
	public void DisposeTryWait()
	{
		var Event = new AsyncAutoResetEvent();

		Event.Dispose();

		try
		{
			_ = Event.TryWait();

			Assert.Fail("Did not throw");
		}
		catch (ObjectDisposedException)
		{
		}
	}

	[Test]
	public void DisposeWait()
	{
		var Event = new AsyncAutoResetEvent();

		Event.Dispose();

		try
		{
			_ = Event.Wait();

			Assert.Fail("Did not throw");
		}
		catch (ObjectDisposedException)
		{
		}
	}

	[Test]
	public void Preset()
	{
		var Event = new AsyncAutoResetEvent(true);

		Assert.That(Event.IsSet, Is.True);
	}

	[Test]
	public void Set()
	{
		var Event = new AsyncAutoResetEvent();

		Assert.That(Event.IsSet, Is.False);

		Event.Set();

		Assert.That(Event.IsSet, Is.True);
	}

	[Test]
	public void SetReset()
	{
		var Event = new AsyncAutoResetEvent();

		Event.Set();

		Assert.That(Event.IsSet, Is.True);

		Event.Reset();

		Assert.That(Event.IsSet, Is.False);
	}

	[Test]
	public void SetResetWait()
	{
		var Event = new AsyncAutoResetEvent();

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
		var Event = new AsyncAutoResetEvent();

		Event.Set();

		Assert.That(Event.TryWait(), Is.True);
	}

	[Test]
	public void SetWait()
	{
		var Event = new AsyncAutoResetEvent();

		Event.Set();

		var Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.True);
	}

	[Test]
	public void SetWaitWait()
	{
		var Event = new AsyncAutoResetEvent();

		Event.Set();

		var Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.True);

		Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.False);
	}

	[Test]
	public void TryWait()
	{
		var Event = new AsyncAutoResetEvent();

		Assert.That(Event.TryWait(), Is.False);
	}

	[Test]
	public void Wait()
	{
		var Event = new AsyncAutoResetEvent();

		var Task = Event.Wait();

		Assert.That(Task.IsCompleted, Is.False);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitCancel(CancellationToken token)
	{
		var Event = new AsyncAutoResetEvent();

		using var TokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

		var Task = Event.Wait(TokenSource.Token);

		TokenSource.Cancel();

		try
		{
			await Task;

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task WaitSet(CancellationToken token)
	{
		var Event = new AsyncAutoResetEvent();

		var Task = Event.Wait(token);

		Event.Set();

		await Task;
	}

	[Test, CancelAfter(1000)]
	public async Task WaitSetWait(CancellationToken token)
	{
		var Event = new AsyncAutoResetEvent();

		var Task = Event.Wait(token);

		Event.Set();

		await Task;

		Task = Event.Wait(token);

		Assert.That(Task.IsCompleted, Is.False);
	}

	[Test, CancelAfter(1000)]
	public async Task WaitWaitSet(CancellationToken token)
	{
		var Event = new AsyncAutoResetEvent();

		var Task1 = Event.Wait(token);
		var Task2 = Event.Wait(token);

		Event.Set();

		await Task1;
		await Task2;
	}
}
