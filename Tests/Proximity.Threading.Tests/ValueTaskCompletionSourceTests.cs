using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Threading.Tests;

[TestFixture]
public class ValueTaskCompletionSourceTests
{
	[Test]
	public async Task Complete()
	{
		using var TaskSource = ValueTaskCompletionSource.New();

		var Task = TaskSource.Task;

		Assert.That(Task.IsCompleted, Is.False);

		Assert.That(TaskSource.TrySetResult(), Is.True);

		await Task;
	}

	[Test]
	public async Task CompleteResult()
	{
		using var TaskSource = ValueTaskCompletionSource.New<int>();

		var Task = TaskSource.Task;

		Assert.That(Task.IsCompleted, Is.False);

		Assert.That(TaskSource.TrySetResult(123), Is.True);

		var Result = await Task;

		Assert.That(Result, Is.EqualTo(123));
	}

	[Test]
	public async Task Exception()
	{
		using var TaskSource = ValueTaskCompletionSource.New();

		var Task = TaskSource.Task;

		Assert.That(Task.IsCompleted, Is.False);

		Assert.That(TaskSource.TrySetException(new ApplicationException()), Is.True);

		try
		{
			await Task;

			Assert.Fail("Should not reach here");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test]
	public async Task ExceptionResult()
	{
		using var TaskSource = ValueTaskCompletionSource.New<int>();

		var Task = TaskSource.Task;

		Assert.That(Task.IsCompleted, Is.False);

		Assert.That(TaskSource.TrySetException(new ApplicationException()), Is.True);

		try
		{
			var Result = await Task;

			Assert.Fail("Should not reach here");
		}
		catch (ApplicationException)
		{
		}
	}
}
