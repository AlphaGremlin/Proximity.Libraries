using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Tests the functionality of the ActionFlag class
/// </summary>
[TestFixture]
public sealed class ActionFlagTests
{   //****************************************
	private int _Counter;
	//****************************************

	[SetUp()]
	public void Reset()
	{
		_Counter = 0;
	}

	//****************************************

	[Test, CancelAfter(1000)]
	public async Task SetNoWait(CancellationToken token)
	{
		var MyFlag = new ActionFlag(() => Task.CompletedTask);

		await MyFlag.SetAndWait().WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task SetAndWait(CancellationToken token)
	{
		var MyFlag = new ActionFlag(WaitHalfSecond);

		var StartTime = DateTime.Now;

		await MyFlag.SetAndWait().WaitAsync(token);

		Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(400.0, 600.0));

		Assert.That(_Counter, Is.EqualTo(1), "Counter is not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task SetAndWaitDelay(CancellationToken token)
	{
		var MyFlag = new ActionFlag(WaitHalfSecond, new TimeSpan(0, 0, 0, 0, 100));

		var StartTime = DateTime.Now;

		var MyTask = MyFlag.SetAndWait().WaitAsync(token);

		Thread.Sleep(50);

		Assert.That(_Counter, Is.Zero, "Raised early");

		await MyTask;

		Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(500.0, 700.0));

		Assert.That(_Counter, Is.EqualTo(1), "Counter is not as expected");
	}

	[Test, CancelAfter(2500)]
	public async Task SetAndWaitTwice(CancellationToken token)
	{
		var MyFlag = new ActionFlag(WaitHalfSecond);

		var StartTime = DateTime.Now;

		MyFlag.Set();

		await Task.Delay(200, token);

		await MyFlag.SetAndWait().WaitAsync(token);

		Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(900.0, 1100.0));

		Assert.That(_Counter, Is.EqualTo(2), "Counter is not as expected");
	}

	[Test, CancelAfter(2500)]
	public async Task SetAndWaitTwiceDelay(CancellationToken token)
	{
		var MyFlag = new ActionFlag(WaitHalfSecond, new TimeSpan(0, 0, 0, 0, 100));

		var StartTime = DateTime.Now;

		MyFlag.Set();

		await Task.Delay(200, token);

		await MyFlag.SetAndWait().WaitAsync(token);

		Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(1100.0, 1300.0));

		Assert.That(_Counter, Is.EqualTo(2), "Counter is not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task SetRuns()
	{
		var MyFlag = new ActionFlag(WaitFiftyMs);
		var EndTime = DateTime.Now.AddSeconds(1.0);

		while (DateTime.Now < EndTime)
		{
			MyFlag.Set();

			Interlocked.Exchange(ref _Counter, 0);

			Thread.Sleep(0);
		}

		await MyFlag.SetAndWait();

		Assert.That(_Counter, Is.Not.Zero);
	}

	//****************************************

	private Task WaitFiftyMs()
	{
		Interlocked.Increment(ref _Counter);

		return Task.Delay(50);
	}

	private Task WaitHalfSecond()
	{
		Interlocked.Increment(ref _Counter);

		return Task.Delay(500);
	}
}
