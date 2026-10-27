using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Tests the functionality of the task extensions
/// </summary>
[TestFixture]
public class ValueTaskWhenTests
{
	[Test, CancelAfter(1000)]
	public async Task Wait(CancellationToken token)
	{
		var TaskSource = new TaskCompletionSource();

		var WhenTask = new ValueTask(TaskSource.Task).WaitAsync(token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		TaskSource.SetResult();

		await WhenTask;
	}

	[Test, CancelAfter(1000)]
	public async Task WaitCancel(CancellationToken token)
	{
		using var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var TaskSource = new TaskCompletionSource();

		var WhenTask = new ValueTask(TaskSource.Task).WaitAsync(CancelSource.Token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		try
		{
			CancelSource.Cancel();

			await WhenTask;

			Assert.Fail("Should not complete");
		}
		catch (OperationCanceledException e)
		{
			Assert.IsTrue(CancelSource.Token == e.CancellationToken);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task WaitSourceCancel(CancellationToken token)
	{
		using var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);

		var TaskSource = new TaskCompletionSource();

		var WhenTask = new ValueTask(TaskSource.Task).WaitAsync(token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		try
		{
			CancelSource.Cancel();

			TaskSource.TrySetCanceled(CancelSource.Token);

			await WhenTask;

			Assert.Fail("Should not complete");
		}
		catch (OperationCanceledException e)
		{
			Assert.IsTrue(CancelSource.Token == e.CancellationToken);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task WaitThrow(CancellationToken token)
	{
		var TaskSource = new TaskCompletionSource();

		var WhenTask = new ValueTask(TaskSource.Task).WaitAsync(token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		TaskSource.SetException(new SuccessException("Exception was passed through successfully"));

		await WhenTask;

		Assert.Fail("Should not reach this point");
	}

	[Test, CancelAfter(1000)]
	public async Task WaitTimeout(CancellationToken token)
	{
		var TaskSource = new TaskCompletionSource();

		var WhenTask = new ValueTask(TaskSource.Task).WaitAsync(100, token);

		try
		{
			await WhenTask;

			Assert.Fail("Should not complete");
		}
		catch (TimeoutException)
		{
		}
	}

	//****************************************

	[Test, CancelAfter(1000)]
	public async Task WaitResult(CancellationToken token)
	{
		var TaskSource = new TaskCompletionSource<int>();

		var WhenTask = new ValueTask<int>(TaskSource.Task).WaitAsync(token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		TaskSource.SetResult(100);

		var Result = await WhenTask;

		Assert.That(Result, Is.EqualTo(100));
	}

	[Test, CancelAfter(1000)]
	public async Task WaitResultCancel(CancellationToken token)
	{
		using var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var TaskSource = new TaskCompletionSource<int>();

		var WhenTask = new ValueTask<int>(TaskSource.Task).WaitAsync(CancelSource.Token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		try
		{
			CancelSource.Cancel();

			_ = await WhenTask;

			Assert.Fail("Should not complete");
		}
		catch (OperationCanceledException e)
		{
			Assert.IsTrue(CancelSource.Token == e.CancellationToken);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task WaitResultSourceCancel(CancellationToken token)
	{
		using var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);

		var TaskSource = new TaskCompletionSource<int>();

		var WhenTask = new ValueTask<int>(TaskSource.Task).WaitAsync(token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		try
		{
			CancelSource.Cancel();

			TaskSource.TrySetCanceled(CancelSource.Token);

			_ = await WhenTask;

			Assert.Fail("Should not complete");
		}
		catch (OperationCanceledException e)
		{
			Assert.IsTrue(CancelSource.Token == e.CancellationToken);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task WaitResultThrow(CancellationToken token)
	{
		var TaskSource = new TaskCompletionSource<int>();

		var WhenTask = new ValueTask<int>(TaskSource.Task).WaitAsync(token);

		Assert.That(WhenTask.IsCompleted, Is.False);

		TaskSource.SetException(new SuccessException("Exception was passed through successfully"));

		_ = await WhenTask;

		Assert.Fail("Should not reach this point");
	}

	[Test, CancelAfter(1000)]
	public async Task WhenResultTimeout(CancellationToken token)
	{
		var TaskSource = new TaskCompletionSource<int>();

		var WhenTask = new ValueTask<int>(TaskSource.Task).WaitAsync(100, token);

		try
		{
			_ = await WhenTask;

			Assert.Fail("Should not complete");
		}
		catch (TimeoutException)
		{
		}
	}
}
