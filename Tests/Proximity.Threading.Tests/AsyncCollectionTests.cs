using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Tests the functionality of the asynchronous blocking collection
/// </summary>
[TestFixture]
public class AsyncCollectionTests
{
	[Test]
	public void Add()
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Collection.Add(42).AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to add");

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task AddTake(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		await Collection.Add(42, token);

		var Task = Collection.Take(token).AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to take");

		Assert.That(Task.Result, Is.EqualTo(42), "Result not as expected");

		Assert.That(Collection.Count, Is.Zero, "Count not as expected");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task TakeAdd(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Collection.Take(token).AsTask();

		Assert.That(Task.IsCompleted, Is.False, "Took too early");

		await Collection.Add(42, token);

		var Result = await Task;

		//****************************************

		Assert.That(Result, Is.EqualTo(42), "Result not as expected");

		Assert.That(Collection.Count, Is.Zero, "Count not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task AddMaximum(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.Add(42, token);

		var Task = Collection.Add(84, token);

		//****************************************

		Assert.That(Task.IsCompleted, Is.False, "Added over the maximum");

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");

		Assert.That(Collection.WaitingToAdd, Is.EqualTo(1), "Waiting adders not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task AddMaximumCancel(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);

		Task<bool> Task;
		//****************************************

		await Collection.Add(42, token);

		using (var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token))
		{
			Task = Collection.Add(84, CancelSource.Token).AsTask();

			CancelSource.Cancel();
		}

		try
		{
			await Task;

			Assert.Fail("Should not reach here");
		}
		catch (OperationCanceledException)
		{
		}

		//****************************************

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task AddMaximumMaxTime(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);

		Task<bool> Task;
		//****************************************

		await Collection.Add(42, token);

		Task = Collection.Add(84, TimeSpan.FromMilliseconds(50), token).AsTask();

		try
		{
			await Task;

			Assert.Fail("Should not reach here");
		}
		catch (TimeoutException)
		{
		}

		//****************************************

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task AddMaximumTake(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.Add(42, token);

		var Task = Collection.Add(84, token);

		var Result = await Collection.Take(token);

		await Task;

		//****************************************

		Assert.That(Result, Is.EqualTo(42), "Result was not as expected");

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");
		Assert.That(Collection.WaitingToAdd, Is.Zero, "Waiting adders not as expected");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task TakeAddMaximum(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		var Task = Collection.Take(token);

		await Collection.Add(42, token);

		await Collection.Add(84, token);

		var Result = await Task;

		//****************************************

		Assert.That(Result, Is.EqualTo(42), "Result was not as expected");

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");
		Assert.That(Collection.WaitingToAdd, Is.Zero, "Waiting adders not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeCancel(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		using (var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token))
		{
			var Task = Collection.Take(CancelSource.Token);

			Assert.IsFalse(Task.IsCompleted, "Took too early");

			CancelSource.Cancel();

			try
			{
				_ = await Task;

				Assert.Fail("Wait not cancelled");
			}
			catch (OperationCanceledException)
			{
			}
		}

		await Collection.Add(42, token);

		//****************************************

		Assert.That(Collection.WaitingToTake, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeMaxTime(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Collection.Take(TimeSpan.FromMilliseconds(50), token);

		Thread.Sleep(100);

		//****************************************

		try
		{
			_ = await Task;

			Assert.Fail("Wait not cancelled");
		}
		catch (TimeoutException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task TakeMultiCancel(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		using var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token);

		var Task1 = Collection.Take(CancelSource.Token).AsTask();
		var Task2 = Collection.Take(token).AsTask();

		Assert.That(Task1.IsCompleted, Is.False, "Took too early");
		Assert.That(Task2.IsCompleted, Is.False, "Took too early");

		CancelSource.Cancel();

		try
		{
			_ = await Task1;

			Assert.Fail("Wait not cancelled");
		}
		catch (OperationCanceledException)
		{
		}

		await Collection.Add(42, token);

		var Result = await Task2;

		//****************************************

		Assert.That(Collection.Count, Is.Zero, "Item not removed");
		Assert.That(Collection.WaitingToTake, Is.Zero, "Tasks unexpectedly waiting");
		Assert.That(Result, Is.EqualTo(42), "Result not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task AddCancel(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.Add(42, token);

		using (var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token))
		{
			var Task = Collection.Add(84, CancelSource.Token);

			Assert.IsFalse(Task.IsCompleted, "Added too early");

			CancelSource.Cancel();

			try
			{
				await Task;

				Assert.Fail("Wait not cancelled");
			}
			catch (OperationCanceledException)
			{
			}
		}

		var Result = await Collection.Take(token);

		//****************************************

		Assert.That(Collection.WaitingToAdd, Is.Zero, "Tasks unexpectedly waiting");
		Assert.That(Result, Is.EqualTo(42), "Result not as expected");
	}

	[Test, CancelAfter(1000)]
	public void AddComplete(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Collection.AddComplete(42, token);

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to add");

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");

		Assert.That(Collection.IsAddingCompleted, Is.True, "Not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task AddCompleteTake(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		await Collection.AddComplete(42, token);

		var Task = Collection.Take(token);

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Still waiting to take");

		Assert.That(Collection.Count, Is.Zero, "Count not as expected");

		Assert.That(Collection.IsCompleted, Is.True, "Not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task AddCompleteAdd(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		await Collection.AddComplete(42, token);

		Assert.That(await Collection.Add(84, token), Is.False);

		//****************************************

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");

		Assert.That(Collection.IsAddingCompleted, Is.True, "Not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task AddCompleteTakeTake(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		await Collection.AddComplete(42, token);

		var Result = await Collection.Take(token);

		try
		{
			_ = await Collection.Take(token);

			Assert.Fail("Should not reach here");
		}
		catch (InvalidOperationException)
		{
		}

		//****************************************

		Assert.That(Collection.Count, Is.Zero, "Count not as expected");
		Assert.That(Result, Is.EqualTo(42), "Result not as expected");

		Assert.That(Collection.IsCompleted, Is.True, "Not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task AddMaximumAddCompleteTake(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.Add(42, token);

		var Task = Collection.AddComplete(84, token).AsTask();

		var Result = await Collection.Take(token);

		await Task;

		//****************************************

		Assert.That(Result, Is.EqualTo(42), "Result was not as expected");

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");
		Assert.That(Collection.WaitingToAdd, Is.Zero, "Waiting adders not as expected");

		Assert.That(Collection.IsAddingCompleted, Is.True, "Adding is not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task AddMaximumAddCompleteTakeTake(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.Add(42, token);

		var Task = Collection.AddComplete(84, token).AsTask();

		_ = await Collection.Take(token);

		await Task;

		var Result = await Collection.Take(token);

		//****************************************

		Assert.That(Result, Is.EqualTo(84), "Result was not as expected");

		Assert.That(Collection.Count, Is.Zero, "Count not as expected");
		Assert.That(Collection.WaitingToAdd, Is.Zero, "Waiting adders not as expected");

		Assert.That(Collection.IsCompleted, Is.True, "Collection is not completed");
	}

	//****************************************

	[Test, CancelAfter(1000)]
	public async Task TakeThenComplete(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Collection.Take(token).AsTask();

		Assert.That(Task.IsCompleted, Is.False, "Took too early");

		Collection.CompleteAdding().Ignore();

		//****************************************

		try
		{
			_ = await Task;

			Assert.Fail("Should not reach here");
		}
		catch (InvalidOperationException)
		{
		}

		Assert.That(Collection.IsAddingCompleted, Is.True, "Adding not completed");
		Assert.That(Collection.IsCompleted, Is.True, "Collection not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task AddMaximumThenComplete(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.Add(42, token);

		var Task = Collection.Add(84, token);

		Collection.CompleteAdding().Ignore();

		//****************************************

		Assert.That(await Task, Is.False);

		Assert.That(Collection.Count, Is.EqualTo(1), "Count not as expected");

		Assert.That(Collection.IsAddingCompleted, Is.True, "Adding not completed");
		Assert.That(Collection.IsCompleted, Is.False, "Collection completed");
	}

	[Test, CancelAfter(1000)]
	public async Task AddMaximumThenCompleteTake(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.Add(42, token);

		Collection.Add(84, token).Ignore();

		Collection.CompleteAdding().Ignore();

		var Result = await Collection.Take(token);

		//****************************************

		Assert.That(Collection.Count, Is.Zero, "Count not as expected");
		Assert.That(Result, Is.EqualTo(42), "Result not as expected");

		Assert.That(Collection.IsAddingCompleted, Is.True, "Adding not completed");
		Assert.That(Collection.IsCompleted, Is.True, "Collection not completed");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task TakeAddThenComplete(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Collection.Take(token).AsTask();

		await Collection.Add(42, token);

		Collection.CompleteAdding().Ignore();

		var Result = await Task;

		//****************************************

		Assert.That(Collection.Count, Is.Zero, "Count not as expected");
		Assert.That(Result, Is.EqualTo(42), "Result not as expected");

		Assert.That(Collection.IsAddingCompleted, Is.True, "Adding not completed");
		Assert.That(Collection.IsCompleted, Is.True, "Collection not completed");
	}

	[Test]
	public void TryTake()
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		Assert.That(Collection.TryTake(out _), Is.False, "Take succeeded unexpectedly");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeSuccess(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		await Collection.Add(42, token);

		//****************************************

		Assert.That(Collection.TryTake(out var MyResult, token), Is.True, "Take failed unexpectedly");
		Assert.That(MyResult, Is.EqualTo(42), "Result was not as expected");
	}

	//****************************************

	[Test, CancelAfter(1000), Repeat(4)]
	public async Task ConsumePreFilled(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var ProducerTask = Producer(Collection, 20, (index) => index, token);

		var Task = Consumer(Collection, token);

		await ProducerTask;

		Collection.CompleteAdding().Ignore();

		await Task;
	}

	[Test, CancelAfter(1000), Repeat(4)]
	public async Task Consume(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Consumer(Collection, token);

		foreach (var Item in Enumerable.Range(1, 10))
		{
			await Collection.Add(Item, token);
		}

		Collection.CompleteAdding().Ignore();

		await Task;
	}

	[Test, CancelAfter(1000), Repeat(4)]
	public async Task ConsumeLimit(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(10);
		//****************************************

		var Task = ConsumerWithWait(Collection, TimeSpan.Zero, token);

		foreach (var Item in Enumerable.Range(1, 100))
		{
			await Collection.Add(Item, token);
		}

		Collection.CompleteAdding().Ignore();

		_ = await Task;
	}

	[Test, CancelAfter(1000), Repeat(4)]
	public async Task ConsumeLimitPreFilled(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(10);
		//****************************************

		var ProducerTask = Producer(Collection, 100, (index) => index, token);

		var ConsumeTask = ConsumerWithWait(Collection, TimeSpan.Zero, token);

		await ProducerTask;

		Collection.CompleteAdding().Ignore();

		await ConsumeTask;
	}

	[Test, CancelAfter(1000), Repeat(4)]
	public async Task ConsumeLimitPreFilledComplete(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(10);
		//****************************************

		var ProducerTask = ProducerWithAddComplete(Collection, 10, (index) => index, token);

		var ConsumeTask = ConsumerWithWait(Collection, TimeSpan.Zero, token);

		await ProducerTask;

		await ConsumeTask;
	}

	[Test, CancelAfter(1000), Repeat(4)]
	public async Task ConsumeLimitOverFilledComplete(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(10);
		//****************************************

		var ProducerTask = ProducerWithAddComplete(Collection, 100, (index) => index, token);

		var ConsumeTask = ConsumerWithWait(Collection, TimeSpan.Zero, token);

		await ProducerTask;

		await ConsumeTask;
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task ConsumeEmpty(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Consumer(Collection, token);

		Collection.CompleteAdding().Ignore();

		await Task;
	}

	//****************************************

	[Test]
	public void TryAdd()
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		Assert.That(Collection.TryAdd(42), Is.True, "Failed to add");
	}

	[Test]
	public void TryAddMaximum()
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		Collection.TryAdd(42);

		//****************************************

		Assert.That(Collection.TryAdd(84), Is.False, "Add unexpectedly succeeded");
	}

	[Test, CancelAfter(1000)]
	public async Task TryAddCompleted(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>(1);
		//****************************************

		await Collection.CompleteAdding().WaitAsync(token);

		//****************************************

		Assert.That(Collection.TryAdd(42), Is.False, "Add unexpectedly succeeded");
	}

	[Test]
	public void TryTakeFromAnyEmpty()
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		var Result = AsyncCollection<int>.TryTakeFromAny(Collections);

		//****************************************

		Assert.That(Result.HasItem, Is.False, "Take succeeded unexpectedly");
	}

	[Test, CancelAfter(1000)]
	public async Task TryTakeFromAny([Values(0, 1)] int index, CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		await Collections[index].Add(42, token);

		var Result = AsyncCollection<int>.TryTakeFromAny(Collections);

		//****************************************

		Assert.That(Result.HasItem, Is.True, "Take failed unexpectedly");
		Assert.That(Result.Source, Is.SameAs(Collections[index]), "Collection was not as expected");
		Assert.That(Result.Item, Is.EqualTo(42), "Item was not as expected");
	}

	//****************************************

	[Test, CancelAfter(1000)]
	public void TakeFromAny(CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		var Task = AsyncCollection<int>.TakeFromAny(Collections, token);

		//****************************************

		Assert.That(Task.IsCompleted, Is.False, "Task completed unexpectedly");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnyInitial([Values(0, 1)] int index, CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		await Collections[index].Add(42, token);

		var Task = AsyncCollection<int>.TakeFromAny(Collections, token).AsTask();

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Task completed unexpectedly");

		Assert.That(Task.Result.HasItem, Is.True, "Take result failed unexpectedly");
		Assert.That(Task.Result.Source, Is.SameAs(Collections[index]), "Collection was not as expected");
		Assert.That(Task.Result.Item, Is.EqualTo(42), "Item was not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnySuccess([Values(0, 1)] int index, CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		var TakeTask = AsyncCollection<int>.TakeFromAny(Collections, token).AsTask();

		await Collections[index].Add(42, token);

		//****************************************

		var Result = await TakeTask;

		await Task.Yield();

		Thread.Sleep(100); // Cancellations happen after the task is completed

		Assert.That(Result.HasItem, Is.True, "Take result failed unexpectedly");
		Assert.That(Result.Source, Is.SameAs(Collections[index]), "Collection was not as expected");
		Assert.That(Result.Item, Is.EqualTo(42), "Item was not as expected");

		Assert.That(Collections[0].Count, Is.Zero, "Item not removed");
		Assert.That(Collections[0].WaitingToTake, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Collections[1].Count, Is.Zero, "Counter decremented");
		Assert.That(Collections[1].WaitingToTake, Is.Zero, "Tasks unexpectedly waiting");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnyAddComplete(CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		var TakeTask = AsyncCollection<int>.TakeFromAny(Collections, token);

		await Collections[0].AddComplete(42, token);

		//****************************************

		var Result = await TakeTask;

		await Task.Yield();

		Thread.Sleep(100); // Cancellations happen after the task is completed

		Assert.That(Result.HasItem, Is.True, "Take result failed unexpectedly");
		Assert.That(Result.Source, Is.SameAs(Collections[0]), "Collection was not as expected");
		Assert.That(Result.Item, Is.EqualTo(42), "Item was not as expected");

		Assert.That(Collections[0].Count, Is.Zero, "Item not removed");
		Assert.That(Collections[0].WaitingToTake, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Collections[1].Count, Is.Zero, "Counter decremented");
		Assert.That(Collections[1].WaitingToTake, Is.Zero, "Tasks unexpectedly waiting");

		Assert.That(Collections[0].IsCompleted, Is.True, "Not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnyCancel(CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		Task<CollectionTakeResult<int>> Task;
		//****************************************

		using (var CancelSource = CancellationTokenSource.CreateLinkedTokenSource(token))
		{
			Task = AsyncCollection<int>.TakeFromAny(Collections, CancelSource.Token).AsTask();

			Assert.IsFalse(Task.IsCompleted, "Added too early");

			CancelSource.Cancel();
		}

		try
		{
			var Result = await Task;

			Assert.Fail("Task succeeded unexpectedly");
		}
		catch (OperationCanceledException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnyComplete(CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		Task<CollectionTakeResult<int>> Task;
		//****************************************

		Task = AsyncCollection<int>.TakeFromAny(Collections, token).AsTask();

		Assert.That(Task.IsCompleted, Is.False, "Added too early");

		Collections[0].CompleteAdding().Ignore();
		Collections[1].CompleteAdding().Ignore();

		try
		{
			var Result = await Task;

			Assert.Fail("Task succeeded unexpectedly");
		}
		catch (InvalidOperationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnySingle(CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		var Task1 = AsyncCollection<int>.TakeFromAny(Collections, token).AsTask();
		var Task2 = AsyncCollection<int>.TakeFromAny(Collections, token).AsTask();

		await Collections[0].Add(42, token);

		var WhenTask = await Task.WhenAny(Task1, Task2);

		//****************************************

		Assert.That(WhenTask.Result.HasItem, Is.True, "No item");
		Assert.That(Collections, Has.Member(WhenTask.Result.Source), "Result is not an expected collection");
		Assert.That(WhenTask.Result.Item, Is.EqualTo(42), "Result item is not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnyMulti(CancellationToken token)
	{ //****************************************
		var Collections = new[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		var Task1 = AsyncCollection<int>.TakeFromAny(Collections, token);
		var Task2 = AsyncCollection<int>.TakeFromAny(Collections, token);

		await Collections[0].Add(42, token);
		await Collections[1].Add(84, token);

		var Result1 = await Task1;
		var Result2 = await Task2;

		//****************************************

		Assert.That(Result1.HasItem, Is.True, "No item");
		Assert.That(Collections, Has.Member(Result1.Source), "Result is not an expected collection");


		Assert.That(Result2.HasItem, Is.True, "No item");
		Assert.That(Collections, Has.Member(Result2.Source), "Result is not an expected collection");

		Assert.That(Result2.Source, Is.Not.SameAs(Result1.Source), "Same collection for both takes");
		Assert.That(Result2.Item, Is.Not.EqualTo(Result1.Item), "Same result for both takes");
	}

	[Test, CancelAfter(1000)]
	public async Task TakeFromAnyMultiCancel(CancellationToken token)
	{ //****************************************
		var Collections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
		//****************************************

		using var Source = CancellationTokenSource.CreateLinkedTokenSource(token);

		var Task1 = AsyncCollection<int>.TakeFromAny(Collections, Source.Token);
		var Task2 = AsyncCollection<int>.TakeFromAny(Collections, token);

		Source.Cancel();

		await Collections[0].Add(42, token);
		await Collections[1].Add(84, token);

		try
		{
			var Result1 = await Task1;

			Assert.Fail("Task succeeded unexpectedly");
		}
		catch (OperationCanceledException)
		{
		}

		var Result2 = await Task2;

		//****************************************

		Assert.That(Result2.HasItem, Is.True, "No item");
		Assert.That(Collections, Has.Member(Result2.Source), "Result is not an expected collection");

		Assert.That(Collections[0].Count == 0 && Collections[1].Count == 0, Is.False, "All items were removed");
	}

	//****************************************

	[Test]
	public void TryPeek()
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		Assert.That(Collection.TryPeek(), Is.False, "Peek succeeded unexpectedly");
	}

	[Test]
	public void TryPeekSuccess()
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		_ = Collection.TryAdd(42);

		Assert.That(Collection.TryPeek(), Is.True, "Peek failed unexpectedly");
	}

	[Test, CancelAfter(1000)]
	public void Peek(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		var Task = Collection.Peek(token);

		//****************************************

		Assert.That(Task.IsCompleted, Is.False, "Peek succeeded unexpectedly");

		Task.Ignore();
	}

	[Test, CancelAfter(1000)]
	public void PeekSuccess(CancellationToken token)
	{ //****************************************
		var Collection = new AsyncCollection<int>();
		//****************************************

		Collection.TryAdd(42, token);

		var Task = Collection.Peek(token);

		//****************************************

		Assert.That(Task.IsCompleted, Is.True, "Peek failed unexpectedly");
	}

	//****************************************

	private static async Task Producer<TItem>(AsyncCollection<TItem> collection, int count, Func<int, TItem> producer, CancellationToken token)
	{
		await Task.Yield();

		for (var Index = 0; Index < count; Index++)
		{
			await collection.Add(producer(Index), token);

			await Task.Yield();
		}
	}

	private static async Task ProducerWithAddComplete<TItem>(AsyncCollection<TItem> collection, int count, Func<int, TItem> producer, CancellationToken token)
	{
		for (var Index = 0; Index < count; Index++)
		{
			if (Index == count - 1)
				await collection.AddComplete(producer(Index), token);
			else
				await collection.Add(producer(Index), token);

			await Task.Yield();
		}
	}

	private static async Task<int> Consumer<TItem>(AsyncCollection<TItem> collection, CancellationToken token)
	{
		var TotalItems = 0;

		try
		{
			await foreach (var MyResult in collection.GetConsumingAsyncEnumerable(token))
			{
				TotalItems++;
			}
		}
		catch (InvalidOperationException)
		{
			// Cancelled
		}

		return TotalItems;
	}

	private static async Task<int> ConsumerWithWait<TItem>(AsyncCollection<TItem> collection, TimeSpan delay, CancellationToken token)
	{
		var TotalItems = 0;

		try
		{
			await foreach (var MyResult in collection.GetConsumingAsyncEnumerable(token))
			{
				TotalItems++;

				if (delay == TimeSpan.Zero)
					await Task.Yield();
				else
					await Task.Delay(delay, token);
			}
		}
		catch (InvalidOperationException)
		{
			// Cancelled
		}

		return TotalItems;
	}
}
