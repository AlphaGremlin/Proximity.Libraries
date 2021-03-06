using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//****************************************

namespace Proximity.Threading.Tests
{
	/// <summary>
	/// Tests the functionality of the asynchronous blocking collection
	/// </summary>
	[TestFixture]
	public class AsyncCollectionTests
	{
		[Test]
		public void Add()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Add(42);

			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to add");
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.Take();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to take");
			
			Assert.AreEqual(42, MyTask.Result, "Result not as expected");
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
		}
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task TakeAdd()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Take();
			
			Assert.IsFalse(MyTask.IsCompleted, "Took too early");
			
			await MyCollection.Add(42);
			
			var MyResult = await MyTask;
			
			//****************************************
			
			Assert.AreEqual(42, MyResult, "Result not as expected");
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddMaximum()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.Add(84);
			
			//****************************************
			
			Assert.IsFalse(MyTask.IsCompleted, "Added over the maximum");
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			
			Assert.AreEqual(1, MyCollection.WaitingToAdd, "Waiting adders not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddMaximumCancel()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			
			ValueTask<bool> MyTask;
			//****************************************
			
			await MyCollection.Add(42);
			
			using (var MyCancelSource = new CancellationTokenSource())
			{
				MyTask = MyCollection.Add(84, MyCancelSource.Token);
				
				MyCancelSource.Cancel();
			}
			
			try
			{
				await MyTask;
				
				Assert.Fail("Should not reach here");
			}
			catch (OperationCanceledException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddMaximumMaxTime()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			
			ValueTask<bool> MyTask;
			//****************************************
			
			await MyCollection.Add(42);
			
			MyTask = MyCollection.Add(84, TimeSpan.FromMilliseconds(50));
			
			try
			{
				await MyTask;
				
				Assert.Fail("Should not reach here");
			}
			catch (TimeoutException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
		}
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task AddMaximumTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.Add(84);
			
			var MyResult = await MyCollection.Take();
			
			await MyTask;
			
			//****************************************
			
			Assert.AreEqual(42, MyResult, "Result was not as expected");
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			Assert.AreEqual(0, MyCollection.WaitingToAdd, "Waiting adders not as expected");
		}
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task TakeAddMaximum()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			var MyTask = MyCollection.Take();
			
			await MyCollection.Add(42);
			
			await MyCollection.Add(84);
			
			var MyResult = await MyTask;
			
			//****************************************
			
			Assert.AreEqual(42, MyResult, "Result was not as expected");
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			Assert.AreEqual(0, MyCollection.WaitingToAdd, "Waiting adders not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeCancel()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask = MyCollection.Take(MySource.Token);
			
				Assert.IsFalse(MyTask.IsCompleted, "Took too early");
				
				MySource.Cancel();

				try
				{
					_ = await MyTask;

					Assert.Fail("Wait not cancelled");
				}
				catch (OperationCanceledException)
				{
				}
			}
			
			await MyCollection.Add(42);
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.WaitingToTake, "Tasks unexpectedly waiting");
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeMaxTime()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Take(TimeSpan.FromMilliseconds(50));
			
			Thread.Sleep(100);

			//****************************************

			try
			{
				_ = await MyTask;

				Assert.Fail("Wait not cancelled");
			}
			catch (TimeoutException)
			{
			}
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeMultiCancel()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************

			using var MySource = new CancellationTokenSource();
			
			var MyTask1 = MyCollection.Take(MySource.Token);
			var MyTask2 = MyCollection.Take();
			
			Assert.IsFalse(MyTask1.IsCompleted, "Took too early");
			Assert.IsFalse(MyTask2.IsCompleted, "Took too early");
				
			MySource.Cancel();

			try
			{
				_ = await MyTask1;

				Assert.Fail("Wait not cancelled");
			}
			catch (OperationCanceledException)
			{
			}

			await MyCollection.Add(42);
				
			var Result = await MyTask2;
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.Count, "Item not removed");
			Assert.AreEqual(0, MyCollection.WaitingToTake, "Tasks unexpectedly waiting");
			Assert.AreEqual(42, Result, "Result not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddCancel()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask = MyCollection.Add(84, MySource.Token);
			
				Assert.IsFalse(MyTask.IsCompleted, "Added too early");
				
				MySource.Cancel();

				try
				{
					await MyTask;

					Assert.Fail("Wait not cancelled");
				}
				catch (OperationCanceledException)
				{
				}
			}
			
			var Result = await MyCollection.Take();
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.WaitingToAdd, "Tasks unexpectedly waiting");
			Assert.AreEqual(42, Result, "Result not as expected");
		}
		
		[Test, MaxTime(1000)]
		public void AddComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.AddComplete(42);
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to add");
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Not completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddCompleteTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			await MyCollection.AddComplete(42);
			
			var MyTask = MyCollection.Take();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to take");
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
			
			Assert.IsTrue(MyCollection.IsCompleted, "Not completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddCompleteAdd()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			await MyCollection.AddComplete(42);
			
			Assert.IsFalse(await MyCollection.Add(84));
			
			//****************************************
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Not completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddCompleteTakeTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			await MyCollection.AddComplete(42);
			
			var Result = await MyCollection.Take();
			
			try
			{
				_ = await MyCollection.Take();
				
				Assert.Fail("Should not reach here");
			}
			catch (InvalidOperationException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
			Assert.AreEqual(42, Result, "Result not as expected");

			Assert.IsTrue(MyCollection.IsCompleted, "Not completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddMaximumAddCompleteTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.AddComplete(84);
			
			var MyResult = await MyCollection.Take();
			
			await MyTask;
			
			//****************************************
			
			Assert.AreEqual(42, MyResult, "Result was not as expected");
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			Assert.AreEqual(0, MyCollection.WaitingToAdd, "Waiting adders not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding is not completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddMaximumAddCompleteTakeTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.AddComplete(84);

			_ = await MyCollection.Take();
			
			await MyTask;
			
			var MyResult = await MyCollection.Take();
			
			//****************************************
			
			Assert.AreEqual(84, MyResult, "Result was not as expected");
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
			Assert.AreEqual(0, MyCollection.WaitingToAdd, "Waiting adders not as expected");
			
			Assert.IsTrue(MyCollection.IsCompleted, "Collection is not completed");
		}
		
		//****************************************
		
		[Test, MaxTime(1000)]
		public async Task TakeThenComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Take();
			
			Assert.IsFalse(MyTask.IsCompleted, "Took too early");
			
			_ = MyCollection.CompleteAdding();
			
			//****************************************
			
			try
			{
				_ = await MyTask;
				
				Assert.Fail("Should not reach here");
			}
			catch (InvalidOperationException)
			{
			}
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsTrue(MyCollection.IsCompleted, "Collection not completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddMaximumThenComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.Add(84);

			_ = MyCollection.CompleteAdding();
			
			//****************************************
			
			Assert.IsFalse(await MyTask);
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsFalse(MyCollection.IsCompleted, "Collection completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task AddMaximumThenCompleteTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			_ = MyCollection.Add(84);
			
			_ = MyCollection.CompleteAdding();
			
			var MyResult = await MyCollection.Take();
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
			Assert.AreEqual(42, MyResult, "Result not as expected");

			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsTrue(MyCollection.IsCompleted, "Collection not completed");
		}
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task TakeAddThenComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Take();
			
			await MyCollection.Add(42);
			
			_ = MyCollection.CompleteAdding();
			
			var MyResult = await MyTask;
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
			Assert.AreEqual(42, MyResult, "Result not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsTrue(MyCollection.IsCompleted, "Collection not completed");
		}
		
		[Test]
		public void TryTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			Assert.IsFalse(MyCollection.TryTake(out _), "Take succeeded unexpectedly");
		}
		
		[Test, MaxTime(1000)]
		public async Task TryTakeSuccess()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			await MyCollection.Add(42);
			
			//****************************************
			
			Assert.IsTrue(MyCollection.TryTake(out var MyResult), "Take failed unexpectedly");
			Assert.AreEqual(42, MyResult, "Result was not as expected");
		}
		
		//****************************************

		[Test, MaxTime(1000), Repeat(4)]
		public async Task ConsumePreFilled()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************

			var ProducerTask = Producer(MyCollection, 20, (index) => index);

			var MyTask = Consumer(MyCollection);

			await ProducerTask;

			_ = MyCollection.CompleteAdding();

			_ = await MyTask;
		}

		[Test, MaxTime(1000), Repeat(4)]
		public async Task Consume()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = Consumer(MyCollection);
			
			foreach(var Item in Enumerable.Range(1, 10))
			{
				await MyCollection.Add(Item);
			}

			_ = MyCollection.CompleteAdding();

			_ = await MyTask;
		}

		[Test, MaxTime(1000), Repeat(4)]
		public async Task ConsumeLimit()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(10);
			//****************************************

			var MyTask = ConsumerWithWait(MyCollection, TimeSpan.Zero);

			foreach (var Item in Enumerable.Range(1, 100))
			{
				await MyCollection.Add(Item);
			}

			_ = MyCollection.CompleteAdding();

			_ = await MyTask;
		}

		[Test, MaxTime(1000), Repeat(4)]
		public async Task ConsumeLimitPreFilled()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(10);
			//****************************************

			var ProducerTask = Producer(MyCollection, 100, (index) => index);

			var MyTask = ConsumerWithWait(MyCollection, TimeSpan.Zero);

			await ProducerTask;

			_ = MyCollection.CompleteAdding();

			_ = await MyTask;
		}

		[Test, MaxTime(1000), Repeat(4)]
		public async Task ConsumeLimitPreFilledComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(10);
			//****************************************

			var ProducerTask = ProducerWithAddComplete(MyCollection, 10, (index) => index);

			var MyTask = ConsumerWithWait(MyCollection, TimeSpan.Zero);

			await ProducerTask;

			_ = await MyTask;
		}

		[Test, MaxTime(1000), Repeat(4)]
		public async Task ConsumeLimitOverFilledComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(10);
			//****************************************

			var ProducerTask = ProducerWithAddComplete(MyCollection, 100, (index) => index);

			var MyTask = ConsumerWithWait(MyCollection, TimeSpan.Zero);

			await ProducerTask;

			_ = await MyTask;
		}

		[Test, MaxTime(1000), Repeat(10)]
		public async Task ConsumeEmpty()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = Consumer(MyCollection);

			_ = MyCollection.CompleteAdding();

			_ = await MyTask;
		}
		
		//****************************************
		
		[Test, MaxTime(1000)]
		public void TryAdd()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			Assert.IsTrue(MyCollection.TryAdd(42), "Failed to add");
		}
		
		[Test, MaxTime(1000)]
		public void TryAddMaximum()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************

			_ = MyCollection.TryAdd(42);
			
			//****************************************
			
			Assert.IsFalse(MyCollection.TryAdd(84), "Add unexpectedly succeeded");
		}
		
		[Test, MaxTime(1000)]
		public void TryAddCompleted()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************

			_ = MyCollection.CompleteAdding();
			
			//****************************************
			
			Assert.IsFalse(MyCollection.TryAdd(42), "Add unexpectedly succeeded");
		}
		
		[Test, MaxTime(1000)]
		public void TryTakeFromAnyEmpty()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			var MyResult = AsyncCollection<int>.TryTakeFromAny(MyCollections);
			
			//****************************************
			
			Assert.IsFalse(MyResult.HasItem, "Take succeeded unexpectedly");
		}
		
		[Test, MaxTime(1000)]
		public async Task TryTakeFromAny([Values(0, 1)] int index)
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			await MyCollections[index].Add(42);
			
			var MyResult = AsyncCollection<int>.TryTakeFromAny(MyCollections);
			
			//****************************************
			
			Assert.IsTrue(MyResult.HasItem, "Take failed unexpectedly");
			Assert.AreSame(MyCollections[index], MyResult.Source, "Collection was not as expected");
			Assert.AreEqual(42, MyResult.Item, "Item was not as expected");
		}
		
		//****************************************
		
		[Test, MaxTime(1000)]
		public void TakeFromAny()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			var MyTask = AsyncCollection<int>.TakeFromAny(MyCollections);
			
			//****************************************
			
			Assert.IsFalse(MyTask.IsCompleted, "Task completed unexpectedly");
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeFromAnyInitial([Values(0, 1)] int index)
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			await MyCollections[index].Add(42);
			
			var MyTask = AsyncCollection<int>.TakeFromAny(MyCollections);
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Task completed unexpectedly");
			
			Assert.IsTrue(MyTask.Result.HasItem, "Take result failed unexpectedly");
			Assert.AreSame(MyCollections[index], MyTask.Result.Source, "Collection was not as expected");
			Assert.AreEqual(42, MyTask.Result.Item, "Item was not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeFromAnySuccess([Values(0, 1)] int index)
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			var MyTask = AsyncCollection<int>.TakeFromAny(MyCollections);
			
			await MyCollections[index].Add(42);
			
			//****************************************
			
			var MyResult = await MyTask;

			await Task.Yield();

			Thread.Sleep(100); // Cancellations happen after the task is completed

			Assert.IsTrue(MyResult.HasItem, "Take result failed unexpectedly");
			Assert.AreSame(MyCollections[index], MyResult.Source, "Collection was not as expected");
			Assert.AreEqual(42, MyResult.Item, "Item was not as expected");
			
			Assert.AreEqual(0, MyCollections[0].Count, "Item not removed");
			Assert.AreEqual(0, MyCollections[0].WaitingToTake, "Tasks unexpectedly waiting");
			
			Assert.AreEqual(0, MyCollections[1].Count, "Counter decremented");
			Assert.AreEqual(0, MyCollections[1].WaitingToTake, "Tasks unexpectedly waiting");
		}
		
		[Test]//, MaxTime(1000)]
		public async Task TakeFromAnyAddComplete()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			var MyTask = AsyncCollection<int>.TakeFromAny(MyCollections);
			
			await MyCollections[0].AddComplete(42);
			
			//****************************************
			
			var MyResult = await MyTask;

			await Task.Yield();

			Thread.Sleep(100); // Cancellations happen after the task is completed
			
			Assert.IsTrue(MyResult.HasItem, "Take result failed unexpectedly");
			Assert.AreSame(MyCollections[0], MyResult.Source, "Collection was not as expected");
			Assert.AreEqual(42, MyResult.Item, "Item was not as expected");
			
			Assert.AreEqual(0, MyCollections[0].Count, "Item not removed");
			Assert.AreEqual(0, MyCollections[0].WaitingToTake, "Tasks unexpectedly waiting");
			
			Assert.AreEqual(0, MyCollections[1].Count, "Counter decremented");
			Assert.AreEqual(0, MyCollections[1].WaitingToTake, "Tasks unexpectedly waiting");
			
			Assert.IsTrue(MyCollections[0].IsCompleted, "Not completed");
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeFromAnyCancel()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			ValueTask<CollectionTakeResult<int>> MyTask;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				MyTask = AsyncCollection<int>.TakeFromAny(MyCollections, MySource.Token);
				
				Assert.IsFalse(MyTask.IsCompleted, "Added too early");
				
				MySource.Cancel();
			}
			
			try
			{
				var MyResult = await MyTask;
				
				Assert.Fail("Task succeeded unexpectedly");
			}
			catch (OperationCanceledException)
			{
			}
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeFromAnyComplete()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			ValueTask<CollectionTakeResult<int>> MyTask;
			//****************************************

			MyTask = AsyncCollection<int>.TakeFromAny(MyCollections);
			
			Assert.IsFalse(MyTask.IsCompleted, "Added too early");

			_ = MyCollections[0].CompleteAdding();
			_ = MyCollections[1].CompleteAdding();

			try
			{
				var MyResult = await MyTask;
				
				Assert.Fail("Task succeeded unexpectedly");
			}
			catch (InvalidOperationException)
			{
			}
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeFromAnySingle()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			var MyTask1 = AsyncCollection<int>.TakeFromAny(MyCollections).AsTask();
			var MyTask2 = AsyncCollection<int>.TakeFromAny(MyCollections).AsTask();
			
			await MyCollections[0].Add(42);
			
			var MyTask = await Task.WhenAny(MyTask1, MyTask2);
			
			//****************************************
			
			Assert.IsTrue(MyTask.Result.HasItem, "No item");
			CollectionAssert.Contains(MyCollections, MyTask.Result.Source, "Result is not an expected collection");
			Assert.AreEqual(42, MyTask.Result.Item, "Result item is not as expected");
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeFromAnyMulti()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************
			
			var MyTask1 = AsyncCollection<int>.TakeFromAny(MyCollections);
			var MyTask2 = AsyncCollection<int>.TakeFromAny(MyCollections);
			
			await MyCollections[0].Add(42);
			await MyCollections[1].Add(84);
			
			var MyResult1 = await MyTask1;
			var MyResult2 = await MyTask2;
			
			//****************************************
			
			Assert.IsTrue(MyResult1.HasItem, "No item");
			CollectionAssert.Contains(MyCollections, MyResult1.Source, "Result is not an expected collection");
			
			
			Assert.IsTrue(MyResult2.HasItem, "No item");
			CollectionAssert.Contains(MyCollections, MyResult2.Source, "Result is not an expected collection");
			
			Assert.AreNotSame(MyResult1.Source, MyResult2.Source, "Same collection for both takes");
			Assert.AreNotEqual(MyResult1.Item, MyResult2.Item, "Same result for both takes");
		}
		
		[Test, MaxTime(1000)]
		public async Task TakeFromAnyMultiCancel()
		{	//****************************************
			var MyCollections = new AsyncCollection<int>[] { new AsyncCollection<int>(), new AsyncCollection<int>() };
			//****************************************

			using var MySource = new CancellationTokenSource();

			var MyTask1 = AsyncCollection<int>.TakeFromAny(MyCollections, MySource.Token);
			var MyTask2 = AsyncCollection<int>.TakeFromAny(MyCollections);

			MySource.Cancel();

			await MyCollections[0].Add(42);
			await MyCollections[1].Add(84);

			try
			{
				var MyResult1 = await MyTask1;

				Assert.Fail("Task succeeded unexpectedly");
			}
			catch (OperationCanceledException)
			{
			}

			var MyResult2 = await MyTask2;

			//****************************************

			Assert.IsTrue(MyResult2.HasItem, "No item");
			CollectionAssert.Contains(MyCollections, MyResult2.Source, "Result is not an expected collection");

			Assert.IsFalse(MyCollections[0].Count == 0 && MyCollections[1].Count == 0, "All items were removed");
		}
		
		//****************************************
		
		[Test, MaxTime(1000)]
		public void TryPeek()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			Assert.IsFalse(MyCollection.TryPeek(), "Peek succeeded unexpectedly");
		}
		
		[Test, MaxTime(1000)]
		public void TryPeekSuccess()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************

			_ = MyCollection.TryAdd(42);
			
			Assert.IsTrue(MyCollection.TryPeek(), "Peek failed unexpectedly");
		}
		
		[Test, MaxTime(1000)]
		public void Peek()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Peek();
			
			//****************************************
			
			Assert.IsFalse(MyTask.IsCompleted, "Peek succeeded unexpectedly");
		}
		
		[Test, MaxTime(1000)]
		public void PeekSuccess()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************

			_ = MyCollection.TryAdd(42);
			
			var MyTask = MyCollection.Peek();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Peek failed unexpectedly");
		}
		
		//****************************************

		private async Task Producer<TItem>(AsyncCollection<TItem> collection, int count, Func<int, TItem> producer)
		{
			await Task.Yield();

			for (var Index = 0; Index < count; Index++)
			{
				await collection.Add(producer(Index));

				await Task.Yield();
			}
		}

		private async Task ProducerWithAddComplete<TItem>(AsyncCollection<TItem> collection, int count, Func<int, TItem> producer)
		{
			for (var Index = 0; Index < count; Index++)
			{
				if (Index == count - 1)
					await collection.AddComplete(producer(Index));
				else
					await collection.Add(producer(Index));

				await Task.Yield();
			}
		}

		private async Task<int> Consumer<TItem>(AsyncCollection<TItem> collection)
		{
			var TotalItems = 0;
			
			try
			{
				await foreach(var MyResult in collection.GetConsumingAsyncEnumerable())
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

		private async Task<int> ConsumerWithWait<TItem>(AsyncCollection<TItem> collection, TimeSpan delay)
		{
			var TotalItems = 0;

			try
			{
				await foreach (var MyResult in collection.GetConsumingAsyncEnumerable())
				{
					TotalItems++;

					if (delay == TimeSpan.Zero)
						await Task.Yield();
					else
						await Task.Delay(delay);
				}
			}
			catch (InvalidOperationException)
			{
				// Cancelled
			}

			return TotalItems;
		}
	}
}
