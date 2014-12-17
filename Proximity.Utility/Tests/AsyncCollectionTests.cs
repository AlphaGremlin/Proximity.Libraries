/****************************************\
 AsyncCollectionTests.cs
 Created: 2014-07-09
\****************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Collections;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
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
		
		[Test, Timeout(1000)]
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
		
		[Test, Timeout(1000), Repeat(10)]
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
		
		[Test, Timeout(1000)]
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
		
		[Test, Timeout(1000), Repeat(10)]
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
		
		[Test, Timeout(1000), Repeat(10)]
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
		
		[Test, Timeout(1000)]
		public async Task TakeCancel()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask = MyCollection.Take(MySource.Token);
			
				Assert.IsFalse(MyTask.IsCompleted, "Took too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			}
			
			await MyCollection.Add(42);
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.WaitingToTake, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public async Task TakeMultiCancel()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask1 = MyCollection.Take(MySource.Token);
				var MyTask2 = MyCollection.Take();
			
				Assert.IsFalse(MyTask1.IsCompleted, "Took too early");
				Assert.IsFalse(MyTask2.IsCompleted, "Took too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask1.IsCanceled, "Wait not cancelled");
				Assert.IsFalse(MyTask2.IsCanceled, "Wait cancelled");
				
				await MyCollection.Add(42);
				
				var Result = await MyTask2;
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.Count, "Item not removed");
			Assert.AreEqual(0, MyCollection.WaitingToTake, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
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
				
				Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			}
			
			var MyResult = MyCollection.Take();
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.WaitingToAdd, "Tasks unexpectedly waiting");
		}
		
		//****************************************
		
		[Test, Timeout(1000)]
		public async Task TakeComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Take();
			
			Assert.IsFalse(MyTask.IsCompleted, "Took too early");
			
			MyCollection.CompleteAdding();
			
			//****************************************
			
			try
			{
				await MyTask;
				
				Assert.Fail("Should not reach here");
			}
			catch (OperationCanceledException)
			{
			}
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsTrue(MyCollection.IsCompleted, "Collection not completed");
		}
		
		[Test, Timeout(1000)]
		public async Task AddMaximumComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.Add(84);
			
			MyCollection.CompleteAdding();
			
			//****************************************
			
			try
			{
				await MyTask;
				
				Assert.Fail("Should not reach here");
			}
			catch (OperationCanceledException)
			{
			}
			
			Assert.AreEqual(1, MyCollection.Count, "Count not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsFalse(MyCollection.IsCompleted, "Collection completed");
		}
		
		[Test, Timeout(1000)]
		public async Task AddMaximumCompleteTake()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>(1);
			//****************************************
			
			await MyCollection.Add(42);
			
			var MyTask = MyCollection.Add(84);
			
			MyCollection.CompleteAdding();
			
			var MyResult = await MyCollection.Take();
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsTrue(MyCollection.IsCompleted, "Collection not completed");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task TakeAddComplete()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = MyCollection.Take();
			
			await MyCollection.Add(42);
			
			MyCollection.CompleteAdding();
			
			var MyResult = await MyTask;
			
			//****************************************
			
			Assert.AreEqual(0, MyCollection.Count, "Count not as expected");
			
			Assert.IsTrue(MyCollection.IsAddingCompleted, "Adding not completed");
			Assert.IsTrue(MyCollection.IsCompleted, "Collection not completed");
		}
		
		//****************************************
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task Consume()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = Consumer(MyCollection);
			
			foreach(var Item in Enumerable.Range(1, 10))
			{
				await MyCollection.Add(Item);
			}
			
			await Task.Delay(100);
			
			MyCollection.CompleteAdding();
			
			await MyTask;
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ConsumeEmpty()
		{	//****************************************
			var MyCollection = new AsyncCollection<int>();
			//****************************************
			
			var MyTask = Consumer(MyCollection);
			
			MyCollection.CompleteAdding();
			
			await MyTask;
		}
		
		//****************************************
		
		private async Task<int> Consumer<TItem>(AsyncCollection<TItem> collection)
		{
			int TotalItems = 0;
			
			try
			{
				foreach(var MyTask in collection.GetConsumingEnumerable())
				{
					var MyResult = await MyTask;
					
					TotalItems++;
				}
			}
			catch (OperationCanceledException)
			{
				// Cancelled
			}
			
			return TotalItems;
		}
		
		
	}
}
