using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Threading.Tests;

[TestFixture]
public class ValueTaskInterleaveTests
{
	[Test]
	public async Task CancelEnumerable()
	{
		var FirstSource = ValueTaskCompletionSource.New<int>();
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
		var FirstSource = ValueTaskCompletionSource.New<int>();
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
		var FirstSource = ValueTaskCompletionSource.New<int>();
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
		await using var Enumerator = Array.Empty<ValueTask<int>>().Interleave().GetAsyncEnumerator();

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
		var FirstSource = ValueTaskCompletionSource.New<int>();

		await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.TrySetException(new ApplicationException("Test"));

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
		var FirstTask = new ValueTask<int>(42);
		var SecondTask = new ValueTask<int>(108);

		await using var Enumerator = new[] { FirstTask, SecondTask }.Interleave().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstTask.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondTask.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleAsync()
	{
		var FirstSource = ValueTaskCompletionSource.New<int>();
		var SecondSource = ValueTaskCompletionSource.New<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.TrySetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		SecondSource.TrySetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleAsyncOutOfOrder()
	{
		var FirstSource = ValueTaskCompletionSource.New<int>();
		var SecondSource = ValueTaskCompletionSource.New<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		SecondSource.TrySetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondSource.Task.Result));

		Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.TrySetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleAsyncIndexOutOfOrder()
	{
		var FirstSource = ValueTaskCompletionSource.New<int>();
		var SecondSource = ValueTaskCompletionSource.New<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.InterleaveIndex().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		SecondSource.TrySetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.result.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(SecondSource.Task.Result));
		Assert.That(Enumerator.Current.index, Is.EqualTo(1));

		Waiter = Enumerator.MoveNextAsync().AsTask();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.TrySetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.result.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(FirstSource.Task.Result));
		Assert.That(Enumerator.Current.index, Is.Zero);

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task MultipleSimultaneousAsync()
	{
		var FirstSource = ValueTaskCompletionSource.New<int>();
		var SecondSource = ValueTaskCompletionSource.New<int>();

		await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.TrySetResult(42);
		SecondSource.TrySetResult(108);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(SecondSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task Single()
	{
		var FirstTask = new ValueTask<int>(42);

		await using var Enumerator = new[] { FirstTask }.Interleave().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstTask.Result));
		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task SingleAsync()
	{
		var FirstSource = ValueTaskCompletionSource.New<int>();

		await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.TrySetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.Result, Is.EqualTo(FirstSource.Task.Result));

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task SingleAsyncIndex()
	{
		var FirstSource = ValueTaskCompletionSource.New<int>();

		await using var Enumerator = new[] { FirstSource.Task }.InterleaveIndex().GetAsyncEnumerator();

		var Waiter = Enumerator.MoveNextAsync();
		Assert.That(Waiter.IsCompleted, Is.False);
		FirstSource.TrySetResult(42);

		Assert.That(await Waiter, Is.True);
		Assert.That(Enumerator.Current.result.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(FirstSource.Task.Result));
		Assert.That(Enumerator.Current.index, Is.Zero);

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}

	[Test]
	public async Task SingleIndex()
	{
		var FirstTask = new ValueTask<int>(42);

		await using var Enumerator = new[] { FirstTask }.InterleaveIndex().GetAsyncEnumerator();

		Assert.That(await Enumerator.MoveNextAsync(), Is.True);
		Assert.That(Enumerator.Current.result.IsCompleted, Is.True);
		Assert.That(Enumerator.Current.result.Result, Is.EqualTo(FirstTask.Result));
		Assert.That(Enumerator.Current.index, Is.Zero);

		Assert.That(await Enumerator.MoveNextAsync(), Is.False);
	}
}
