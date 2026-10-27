using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Tests the functionality of the asynchronous counter
/// </summary>
[TestFixture]
public class AsyncCounterTests
{
	[Test]
	public void Increment()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Increment();

		//****************************************

		Assert.That(Counter.CurrentCount, Is.EqualTo(1), "Counter not incremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void Add()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Add(10);

		//****************************************

		Assert.That(Counter.CurrentCount, Is.EqualTo(10), "Counter not incremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void AddDecrement()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Add(10);

		var Task = Counter.Decrement().AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to decrement");

		Assert.That(Counter.CurrentCount, Is.EqualTo(9), "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void Decrement()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Increment();

		var Task = Counter.Decrement().AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to decrement");

		Assert.That(Counter.CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void DecrementInitial()
	{ //****************************************
		var Counter = new AsyncCounter(1);
		//****************************************

		var Task = Counter.Decrement().AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to decrement");

		Assert.That(Counter.CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void DecrementToZero()
	{ //****************************************
		var Counter = new AsyncCounter(1);
		//****************************************

		var Task = Counter.DecrementToZero().AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to decrement");
		Assert.That(Task.Result, Is.EqualTo(1));

		Assert.That(Counter.CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void DecrementToZeroInitial()
	{ //****************************************
		var Counter = new AsyncCounter(10);
		//****************************************

		var Task = Counter.DecrementToZero().AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to decrement");
		Assert.That(Task.Result, Is.EqualTo(10));

		Assert.That(Counter.CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementIncrement(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Task = Counter.Decrement(CancellationToken.None);

		Assert.That(Task.IsCompleted, Is.False, "Decremented too early");

		Counter.Increment();

		//****************************************

		await Task.WaitAsync(token);

		Assert.That(Counter.CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementAdd(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Task = Counter.Decrement(CancellationToken.None);

		Assert.That(Task.IsCompleted, Is.False, "Decremented too early");

		Counter.Add(10);

		//****************************************

		await Task.WaitAsync(token);

		Assert.That(Counter.CurrentCount, Is.EqualTo(9), "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementToZeroAdd(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Task = Counter.DecrementToZero(CancellationToken.None).AsTask();

		Assert.That(Task.IsCompleted, Is.False, "Decremented too early");

		Counter.Add(10);

		//****************************************

		var Result = await Task.WaitAsync(token);

		Assert.That(Result, Is.EqualTo(10));

		Assert.That(Counter.CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementCancel(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		Task Task;
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		Task = Counter.Decrement(CancelSource.Token).AsTask();

		Assert.IsFalse(Task.IsCompleted, "Decremented too early");

		CancelSource.Cancel();

		// Cancellation happens in the background
		await Task.Delay(100, token);

		Assert.IsTrue(Task.IsCanceled, "Wait not cancelled");

		// Since there are no waiters that aren't cancelled, this should succeed immediately
		Counter.Increment();

		//****************************************

		Assert.That(Counter.CurrentCount, Is.EqualTo(1), "Counter still decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementMultiCancel(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Task1 = Counter.Decrement(CancelSource.Token);
		var Task2 = Counter.Decrement(CancellationToken.None);

		Assert.IsFalse(Task1.IsCompleted, "Decremented too early");
		Assert.IsFalse(Task2.IsCompleted, "Decremented too early");

		CancelSource.Cancel();

		Assert.IsTrue(Task1.IsCanceled, "Wait not cancelled");
		Assert.IsFalse(Task2.IsCanceled, "Wait cancelled");

		Counter.Increment();

		await Task2.WaitAsync(token);

		//****************************************

		Assert.That(Counter.CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counter.WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementTimeout(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		try
		{
			await Counter.Decrement(TimeSpan.FromMilliseconds(10), token);

			Assert.Fail("Task did not cancel");
		}
		catch (TimeoutException)
		{
		}

		//****************************************

		//MyCounter.Dispose();

		//Assert.AreEqual(0, MyCounter.WaitingCount, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test]
	public void DecrementTimeoutNone()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Increment();

		var Task = Counter.Decrement(TimeSpan.FromMilliseconds(50));

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to decrement");

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementTimeoutDelayed(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Task = Counter.Decrement(TimeSpan.FromMilliseconds(50), CancellationToken.None);

		Counter.Increment();

		//****************************************

		await Task.WaitAsync(token);

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeDecrement(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.DisposeAsync().Ignore();

		await Assert.ThrowsAsync<ObjectDisposedException>(() => Counter.Decrement().AsTask().WaitAsync(token));

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test]
	public void DisposeIncrement()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.DisposeAsync().Ignore();

		try
		{
			Counter.Increment();

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test]
	public void IncrementDisposeTryDecrement()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Increment();

		Counter.DisposeAsync().Ignore();

		Assert.That(Counter.TryDecrement(), Is.True);

		Assert.That(Counter.TryDecrement(), Is.False);

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test]
	public void DisposeTryDecrement()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.DisposeAsync().Ignore();

		Assert.That(Counter.TryDecrement(), Is.False);

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeTryDecrementAsync(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.DisposeAsync().Ignore();

		Assert.That(await Counter.TryDecrementAsync(CancellationToken.None).WaitAsync(token), Is.False);

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementDispose(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Task = Counter.Decrement(CancellationToken.None);

		Counter.DisposeAsync().Ignore();

		try
		{
			await Task.WaitAsync(token);

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.Zero, "Count not zero");
	}

	[Test]
	public void IncrementDispose()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Increment();

		var DisposeTask = Counter.DisposeAsync();

		Assert.That(DisposeTask.IsCompleted, Is.False, "Dispose completed early");

		//****************************************

		Assert.That(Counter.WaitingCount, Is.Zero, "Waiter still registered");
		Assert.That(Counter.CurrentCount, Is.EqualTo(1), "Count not one");
	}

	[Test, CancelAfter(1000)]
	public async Task StackBlow(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		Task ResultTask;
		Task[] Results;
		//****************************************

		Results = Enumerable.Range(1, 40000).Select(
			async count =>
			{
				await Counter.Decrement();

				Counter.Increment();
			}).ToArray();

		ResultTask = Results[^1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);

		Counter.Increment();

		await ResultTask.WaitAsync(token);
	}

	[Test]
	public void TryDecrement()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Result = Counter.TryDecrement();

		//****************************************

		Assert.That(Result, Is.False, "Decrement succeeded unexpectedly");
	}

	[Test]
	public void TryDecrementSuccess()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		Counter.Increment();

		var Result = Counter.TryDecrement();

		//****************************************

		Assert.That(Result, Is.True, "Decrement failed unexpectedly");
	}

	[Test]
	public void TryDecrementAny()
	{ //****************************************
		var Counters = new[] { new AsyncCounter(), new AsyncCounter() };
		//****************************************

		var Result = AsyncCounter.TryDecrementAny(Counters, out var Counter);

		//****************************************

		Assert.That(Result, Is.False, "Decremented");

		Assert.That(Counter, Is.Null, "Return an unexpected counter");
	}

	[Test]
	public void TryDecrementAnySuccess([Values(0, 1)] int index)
	{ //****************************************
		var Counters = new[] { new AsyncCounter(), new AsyncCounter() };
		//****************************************

		Counters[index].Increment();

		var Result = AsyncCounter.TryDecrementAny(Counters, out var Counter);

		//****************************************

		Assert.That(Result, Is.True, "Did not decrement");

		Assert.That(Counter, Is.SameAs(Counters[index]), "Did not return the expected counter");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementAny([Values(0, 1)] int index, CancellationToken token)
	{ //****************************************
		var Counters = new[] { new AsyncCounter(0), new AsyncCounter(0) };
		//****************************************

		var Task = AsyncCounter.DecrementAny(Counters).AsTask();

		Assert.That(Task.IsCompleted, Is.False, "Task unexpectedly completed");

		Counters[index].Increment();

		var Result = await Task.WaitAsync(token);

		//****************************************

		Assert.That(Result, Is.EqualTo(Counters[index]), "Decremented unexpected counter");

		Assert.That(Counters[0].CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counters[0].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Counters[1].CurrentCount, Is.Zero, "Counter decremented");
		Assert.That(Counters[1].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void DecrementAnyInitial()
	{ //****************************************
		var Counters = new[] { new AsyncCounter(1), new AsyncCounter(1) };
		//****************************************

		var Task = AsyncCounter.DecrementAny(Counters).AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to decrement");
		Assert.That(Task.Result, Is.EqualTo(Counters[0]), "Decremented unexpected counter");

		Assert.That(Counters[0].CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counters[0].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Counters[1].CurrentCount, Is.EqualTo(1), "Counter decremented");
		Assert.That(Counters[1].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementAnyMulti(CancellationToken token)
	{ //****************************************
		var Counters = new[] { new AsyncCounter(0), new AsyncCounter(0) };
		//****************************************

		var Task1 = AsyncCounter.DecrementAny(Counters).AsTask();
		var Task2 = AsyncCounter.DecrementAny(Counters).AsTask();

		Assert.That(Task1.IsCompleted, Is.False, "Task 1 unexpectedly completed");
		Assert.That(Task2.IsCompleted, Is.False, "Task 2 unexpectedly completed");

		Counters[0].Increment();

		await Task.WhenAny(Task1, Task2).WaitAsync(token); // No guarantee which peek will finish first

		Counters[1].Increment();

		await Task.WhenAll(Task1, Task2).WaitAsync(token);

		//****************************************

		Assert.That(Counters, Has.Member(Task1.Result), "Decremented unexpected counter");
		Assert.That(Counters, Has.Member(Task2.Result), "Decremented unexpected counter");

		Assert.That(Task2.Result, Is.Not.SameAs(Task1.Result), "Decremented the same counter");

		Assert.That(Counters[0].CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counters[0].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Counters[1].CurrentCount, Is.Zero, "Counter decremented");
		Assert.That(Counters[1].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementAnyCancel(CancellationToken token)
	{ //****************************************
		var Counters = new[] { new AsyncCounter(0), new AsyncCounter(0) };
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var DecrementTask = AsyncCounter.DecrementAny(Counters, CancelSource.Token).AsTask();

		Assert.IsFalse(DecrementTask.IsCompleted, "Decremented too early");

		CancelSource.Cancel();

		await Task.Delay(100, token);

		Assert.IsTrue(DecrementTask.IsCanceled, "Wait not cancelled");

		// Since there are no waiters that aren't cancelled, these should succeed immediately
		Counters[0].Increment();
		Counters[1].Increment();

		Assert.That(Counters[0].CurrentCount, Is.EqualTo(1), "Counter decremented");
		Assert.That(Counters[0].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Counters[1].CurrentCount, Is.EqualTo(1), "Counter decremented");
		Assert.That(Counters[1].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task DecrementAnyDispose(CancellationToken token)
	{ //****************************************
		var Counters = new[] { new AsyncCounter(0), new AsyncCounter(0) };
		//****************************************

		var Task = AsyncCounter.DecrementAny(Counters).AsTask();

		Counters[0].DisposeAsync().Ignore();
		Counters[1].DisposeAsync().Ignore();

		// Wait for the DecrementAny task to cancel
		try
		{
			await Task.WaitAsync(token);

			Assert.Fail("Did not throw disposed");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Counters[0].CurrentCount, Is.Zero, "Counter not decremented");
		Assert.That(Counters[0].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Counters[1].CurrentCount, Is.Zero, "Counter decremented");
		Assert.That(Counters[1].WaitingCount, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test]
	public void TryPeek()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Result = Counter.TryPeekDecrement();

		//****************************************

		Assert.That(Result, Is.False, "Peek succeeded unexpectedly");
	}

	[Test]
	public void Peek()
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Task = Counter.PeekDecrement();

		//****************************************

		Assert.That(Task.IsCompleted, Is.False, "Peek succeeded unexpectedly");
	}

	[Test, CancelAfter(1000)]
	public async Task PeekDispose(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Task = Counter.PeekDecrement().AsTask();

		await Counter.DisposeAsync().WaitAsync(token);

		//****************************************

		Assert.That(await Task.WaitAsync(token), Is.False);
	}

	[Test, CancelAfter(1000)]
	public async Task PeekCancel(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var Task = Counter.PeekDecrement(CancelSource.Token).AsTask();

		CancelSource.Cancel();

		//****************************************

		try
		{
			await Task.WaitAsync(token);

			Assert.Fail("Should never reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task PeekDisposeContinueWithPeek(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var OuterTask = Counter.PeekDecrement().AsTask();

		var InnerTask = OuterTask.ContinueWith((task) => Counter.PeekDecrement().AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();

		var Dispose = Counter.DisposeAsync();

		//****************************************

		Assert.That(await OuterTask.WaitAsync(token), Is.False);

		Assert.That(await InnerTask.WaitAsync(token), Is.False);

		await Dispose.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task Consume(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consume = Consumer(Counter, token);

		var Incrementer = Increment(Counter, 10, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = await Incrementer.WaitAsync(token);
		var TotalConsumed = await Consume.WaitAsync(token);

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeDual(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consume1 = Consumer(Counter, token);
		var Consume2 = Consumer(Counter, token);

		var Incrementer = Increment(Counter, 10, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = await Incrementer.WaitAsync(token);
		var TotalConsumed = (await Task.WhenAll(Consume1, Consume2)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeDualPeek(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consume1 = Consumer(Counter, token);
		var Consume2 = ConsumerPeek(Counter, token);

		var Incrementer = Increment(Counter, 10, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = await Incrementer.WaitAsync(token);
		var TotalConsumed = (await Task.WhenAll(Consume1, Consume2)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeDualIncrement(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consume1 = Consumer(Counter, token);
		var Consume2 = Consumer(Counter, token);

		var Increment1 = Increment(Counter, 10, token);
		var Increment2 = Increment(Counter, 10, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = (await Task.WhenAll(Increment1, Increment2)).Sum();
		var TotalConsumed = (await Task.WhenAll(Consume1, Consume2)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeDualIncrementPeek(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consume1 = Consumer(Counter, token);
		var Consume2 = ConsumerPeek(Counter, token);

		var Increment1 = Increment(Counter, 10, token);
		var Increment2 = Increment(Counter, 10, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = (await Task.WhenAll(Increment1, Increment2)).Sum();
		var TotalConsumed = (await Task.WhenAll(Consume1, Consume2)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeLots(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consumers = Enumerable.Range(0, 10).Select(count => Consumer(Counter, token)).ToArray();

		var Incrementer = Increment(Counter, 50, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = await Incrementer.WaitAsync(token);
		var TotalConsumed = (await Task.WhenAll(Consumers)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeLotsDualIncrement(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consumers = Enumerable.Range(0, 10).Select(count => Consumer(Counter, token)).ToArray();

		var Increment1 = Increment(Counter, 50, token);
		var Increment2 = Increment(Counter, 50, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = (await Task.WhenAll(Increment1, Increment2)).Sum();
		var TotalConsumed = (await Task.WhenAll(Consumers)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, Repeat(2), CancelAfter(2000)]
	public async Task ConsumeLotsIncrementLots(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consumers = Enumerable.Range(0, 5).Select(count => Consumer(Counter, token)).ToArray();

		var Incrementers = Enumerable.Range(0, 5).Select(count => Increment(Counter, 50, token)).ToArray();

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = (await Task.WhenAll(Incrementers)).Sum();
		var TotalConsumed = (await Task.WhenAll(Consumers)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, Repeat(2), CancelAfter(1000)]
	public async Task ConsumeLotsIncrementLotsPeek(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consumers = Enumerable.Range(0, 5).Select(count => count % 2 == 0 ? Consumer(Counter, token) : ConsumerPeek(Counter, token)).ToArray();

		var Incrementers = Enumerable.Range(0, 5).Select(count => Increment(Counter, 50, token)).ToArray();

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = (await Task.WhenAll(Incrementers)).Sum();
		var TotalConsumed = (await Task.WhenAll(Consumers)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeDualCancel(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consume1 = Consumer(Counter, token);
		var Consume2 = ConsumerCancel(Counter, token);

		var Incrementer = Increment(Counter, 10, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = await Incrementer.WaitAsync(token);
		var TotalConsumed = (await Task.WhenAll(Consume1, Consume2)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	[Test, CancelAfter(1000)]
	public async Task ConsumeDualCancelPeek(CancellationToken token)
	{ //****************************************
		var Counter = new AsyncCounter();
		//****************************************

		var Consume1 = Consumer(Counter, token);
		var Consume2 = ConsumerCancelPeek(Counter, token);

		var Incrementer = Increment(Counter, 10, token);

		await Task.Delay(500, token);

		Counter.DisposeAsync().Ignore();

		var TotalAdded = await Incrementer.WaitAsync(token);
		var TotalConsumed = (await Task.WhenAll(Consume1, Consume2)).Sum();

		//****************************************

		Assert.That(TotalConsumed + Counter.CurrentCount, Is.EqualTo(TotalAdded), $"Counts do not match: {TotalConsumed} + {Counter.CurrentCount} != {TotalAdded}");
	}

	//****************************************

	private static async Task<int> Increment(AsyncCounter counter, int maximum, CancellationToken token)
	{
		var TotalIncrement = 0;
		var Wait = new SpinWait();

		try
		{
			for (; !counter.IsDisposed; token.ThrowIfCancellationRequested())
			{
				while (counter.CurrentCount < maximum)
				{
					if (!counter.TryIncrement())
						return TotalIncrement;

					TotalIncrement++;

					if (TotalIncrement % 16 == 0)
						await Task.Yield();
				}

				Wait.SpinOnce();
			}
		}
		catch (ObjectDisposedException)
		{
			Console.WriteLine("Disposed Incrementer");
		}

		return TotalIncrement;
	}

	private static async Task<int> Consumer(AsyncCounter counter, CancellationToken token)
	{
		var TotalDecrement = 0;

		while (await counter.TryDecrementAsync(token))
		{
			TotalDecrement++;

			if (TotalDecrement % 16 == 0)
				await Task.Yield();
		}

		return TotalDecrement;
	}

	private static async Task<int> ConsumerPeek(AsyncCounter counter, CancellationToken token)
	{
		var TotalDecrement = 0;

		while (await counter.PeekDecrement(token))
		{
			if (!counter.TryDecrement())
				continue;

			TotalDecrement++;

			if (TotalDecrement % 16 == 0)
				await Task.Yield();
		}

		return TotalDecrement;
	}

	private static async Task<int> ConsumerCancel(AsyncCounter counter, CancellationToken token)
	{
		var TotalDecrement = 0;

		for (; ; )
		{
			using var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);

			CancelSource.CancelAfter(50);

			try
			{
				while (await counter.TryDecrementAsync(token))
				{
					TotalDecrement++;

					if (TotalDecrement % 16 == 0)
						await Task.Yield();
				}

				return TotalDecrement;
			}
			catch (OperationCanceledException)
			{
				token.ThrowIfCancellationRequested();
			}
		}
	}

	private static async Task<int> ConsumerCancelPeek(AsyncCounter counter, CancellationToken token)
	{
		var TotalDecrement = 0;

		for (; ; )
		{
			using var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);

			CancelSource.CancelAfter(50);

			try
			{
				while (await counter.PeekDecrement(token))
				{
					if (!counter.TryDecrement())
						continue;

					TotalDecrement++;

					if (TotalDecrement % 16 == 0)
						await Task.Yield();
				}

				return TotalDecrement;
			}
			catch (OperationCanceledException)
			{
				token.ThrowIfCancellationRequested();
			}
		}
	}
}
