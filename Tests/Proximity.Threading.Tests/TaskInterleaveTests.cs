using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Threading.Tests;

[TestFixture]
public class TaskInterleaveTests
{
	[Test]
	public async Task CancelEnumerable()
	{
		var FirstSource = new TaskCompletionSource<int>();
		var CancelSource = new CancellationTokenSource();

		await using var Enumerator = new[] { FirstSource.Task }.Interleave(CancelSource.Token).GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);

		CancelSource.Cancel();

		try
		{
			await Waiter;

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
		}
	}

	[Test]
	public async Task CancelEnumerableWith()
	{
		var FirstSource = new TaskCompletionSource<int>();
		var CancelSource = new CancellationTokenSource();

		await using var Enumerator = new[] { FirstSource.Task }.Interleave().WithCancellation(CancelSource.Token).GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();

		CancelSource.Cancel();

		try
		{
			await Waiter;

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
		}
	}

	[Test]
	public async Task CancelEnumerator()
	{
		var FirstSource = new TaskCompletionSource<int>();
		var CancelSource = new CancellationTokenSource();

		await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator(CancelSource.Token);

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);

		CancelSource.Cancel();

		try
		{
			await Waiter;

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
		}
	}

	[Test]
	public async Task Empty()
	{
		await using var Enumerator = Array.Empty<Task<int>>().Interleave().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task Exception()
	{
		var FirstTask = Task.FromException<int>(new ApplicationException("Test"));

		await using var Enumerator = new[] { FirstTask }.Interleave().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);

		try
		{
			await Enumerator.Current;

			Assert.Fail("Did not throw");
		}
		catch (ApplicationException)
		{
		}

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task ExceptionAsync()
	{
		var FirstSource = new TaskCompletionSource<int>();

		await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.SetException(new ApplicationException("Test"));

		Assert.That(await Waiter, Is.True);

		try
		{
			await Enumerator.Current;

			Assert.Fail("Did not throw");
		}
		catch (ApplicationException)
		{
		}

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task Multiple()
	{
		var FirstTask = Task.FromResult(42);
		var SecondTask = Task.FromResult(108);

		await using var Enumerator = new[] { FirstTask, SecondTask }.Interleave().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstTask.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondTask.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleAsync()
	{
		var FirstSource = new TaskCompletionSource<int>();
		var SecondSource = new TaskCompletionSource<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.SetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		SecondSource.SetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleAsyncOutOfOrder()
	{
		var FirstSource = new TaskCompletionSource<int>();
		var SecondSource = new TaskCompletionSource<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		SecondSource.SetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondSource.Task.Result));

		Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.SetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleAsyncIndexOutOfOrder()
	{
		var FirstSource = new TaskCompletionSource<int>();
		var SecondSource = new TaskCompletionSource<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.InterleaveIndex().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		SecondSource.SetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.result.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(SecondSource.Task.Result));
		Assert.That(Enumerator.Current.index, Is.EqualTo(1));

		Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.SetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.result.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(FirstSource.Task.Result));
		Assert.That(Enumerator.Current.index, Is.Zero);

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleSimultaneousAsync()
	{
		var FirstSource = new TaskCompletionSource<int>();
		var SecondSource = new TaskCompletionSource<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.SetResult(42);
		SecondSource.SetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task Single()
	{
		var FirstTask = Task.FromResult(42);

		await using var Enumerator = new[] { FirstTask }.Interleave().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstTask.Result));
		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task SingleAsync()
	{
		var FirstSource = new TaskCompletionSource<int>();

		await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.SetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task SingleAsyncIndex()
	{
		var FirstSource = new TaskCompletionSource<int>();

		await using var Enumerator = new[] { FirstSource.Task }.InterleaveIndex().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.SetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.result.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(FirstSource.Task.Result));
		Assert.That(Enumerator.Current.index, Is.Zero);

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task SingleIndex()
	{
		var FirstTask = Task.FromResult(42);

		await using var Enumerator = new[] { FirstTask }.InterleaveIndex().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.result.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(FirstTask.Result));
		Assert.That(Enumerator.Current.index, Is.Zero);

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}
}
