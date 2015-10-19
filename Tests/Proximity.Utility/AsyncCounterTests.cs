/****************************************\
 AsyncCounterTests.cs
 Created: 2014-07-09
\****************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests the functionality of the asynchronous counter
	/// </summary>
	[TestFixture]
	public class AsyncCounterTests
	{
		[Test]
		public void Increment()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			//****************************************
			
			Assert.AreEqual(1, MyCounter.CurrentCount, "Counter not incremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void Decrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			var MyTask = MyCounter.Decrement();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test]
		public void DecrementInitial()
		{	//****************************************
			var MyCounter = new AsyncCounter(1);
			//****************************************
			
			var MyTask = MyCounter.Decrement();
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementIncrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement();
			
			Assert.IsFalse(MyTask.IsCompleted, "Decremented too early");
			
			MyCounter.Increment();
			
			//****************************************
			
			MyTask.Wait();
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			Task MyTask;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyCounter.Decrement(MySource.Token);
			
				Assert.IsFalse(MyTask.IsCompleted, "Decremented too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			}
			
			MyCounter.Increment();
			
			Thread.Sleep(100); // Increment happens on another thread and there's nothing to wait on
			
			//****************************************
			
			Assert.AreEqual(1, MyCounter.CurrentCount, "Counter still decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementMultiCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				var MyTask1 = MyCounter.Decrement(MySource.Token);
				var MyTask2 = MyCounter.Decrement();
			
				Assert.IsFalse(MyTask1.IsCompleted, "Decremented too early");
				Assert.IsFalse(MyTask2.IsCompleted, "Decremented too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask1.IsCanceled, "Wait not cancelled");
				Assert.IsFalse(MyTask2.IsCanceled, "Wait cancelled");
				
				MyCounter.Increment();
				
				MyTask2.Wait();
				
				Assert.IsTrue(MyTask2.IsCompleted, "Still waiting to decrement");
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyCounter.CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounter.WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeout()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			Thread.Sleep(100);
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			
			MyCounter.Dispose();
			
			Assert.AreEqual(0, MyCounter.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyCounter.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeoutNone()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			
			Assert.AreEqual(0, MyCounter.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyCounter.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DecrementTimeoutDelayed()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.Decrement(TimeSpan.FromMilliseconds(50));
			
			MyCounter.Increment();
			
			//****************************************
			
			MyTask.Wait();
			
			//****************************************
			
			Assert.AreEqual(0, MyCounter.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyCounter.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DisposeDecrement()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			MyLock.Dispose();
			
			try
			{
				var MyTask = MyLock.Decrement();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyLock.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DisposeIncrement()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			MyLock.Dispose();
			
			try
			{
				MyLock.Increment();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyLock.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void DecrementDispose()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			var MyTask = MyLock.Decrement();
			
			MyLock.Dispose();
			
			try
			{
				MyTask.Wait();
				
				Assert.Fail("Should never reach this point");
			}
			catch (AggregateException e)
			{
				Assert.IsInstanceOf(typeof(ObjectDisposedException), e.InnerException, "Inner exception not as expected");
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(0, MyLock.CurrentCount, "Count not zero");
		}
		
		[Test, Timeout(1000)]
		public void IncrementDispose()
		{	//****************************************
			var MyLock = new AsyncCounter();
			//****************************************
			
			MyLock.Increment();
			
			MyLock.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingCount, "Waiter still registered");
			Assert.AreEqual(1, MyLock.CurrentCount, "Count not one");
		}
		
		[Test]
		public async Task StackBlow()
		{	//****************************************
			var MyLock = new AsyncCounter();
			Task ResultTask;
			Task[] Results;
			//****************************************

			Results = Enumerable.Range(1, 40000).Select(
				async count => 
				{
					await MyLock.Decrement();
					
					MyLock.Increment();
				}).ToArray();
			
			ResultTask = Results[Results.Length -1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);

			MyLock.Increment();
			
			await ResultTask;
		}
		
		[Test, Timeout(1000)]
		public void TryDecrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyResult = MyCounter.TryDecrement();
			
			//****************************************
			
			Assert.IsFalse(MyResult, "Decrement succeeded unexpectedly");
		}
		
		[Test, Timeout(1000)]
		public void TryDecrementSuccess()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			MyCounter.Increment();
			
			var MyResult = MyCounter.TryDecrement();
			
			//****************************************
			
			Assert.IsTrue(MyResult, "Decrement failed unexpectedly");
		}
		
		[Test, Timeout(1000)]
		public void TryDecrementAny()
		{	//****************************************
			var MyCounters = new AsyncCounter[] { new AsyncCounter(), new AsyncCounter() };
			//****************************************
			
			var MyResult = AsyncCounter.TryDecrementAny(MyCounters);
			
			//****************************************
			
			Assert.IsNull(MyResult, "Return an unexpected counter");
		}
		
		[Test, Timeout(1000)]
		public void TryDecrementAnySuccess([Values(0, 1)] int index)
		{	//****************************************
			var MyCounters = new AsyncCounter[] { new AsyncCounter(), new AsyncCounter() };
			//****************************************
			
			MyCounters[index].Increment();
			
			var MyResult = AsyncCounter.TryDecrementAny(MyCounters);
			
			//****************************************
			
			Assert.AreSame(MyCounters[index], MyResult, "Did not return the expected counter");
		}
		
		[Test, Timeout(1000)]
		public void DecrementAny([Values(0, 1)] int index)
		{	//****************************************
			var MyCounters = new AsyncCounter[] { new AsyncCounter(0), new AsyncCounter(0) };
			//****************************************
			
			var MyTask = AsyncCounter.DecrementAny(MyCounters);
			
			Assert.IsFalse(MyTask.IsCompleted, "Task unexpectedly completed");
			
			MyCounters[index].Increment();
			
			Thread.Sleep(100); // Increment happens on another thread and there's nothing to wait on
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			Assert.AreEqual(MyCounters[index], MyTask.Result, "Decremented unexpected counter");
			
			Assert.AreEqual(0, MyCounters[0].CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounters[0].WaitingCount, "Tasks unexpectedly waiting");
			
			Assert.AreEqual(0, MyCounters[1].CurrentCount, "Counter decremented");
			Assert.AreEqual(0, MyCounters[1].WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementAnyInitial()
		{	//****************************************
			var MyCounters = new AsyncCounter[] { new AsyncCounter(1), new AsyncCounter(1) };
			//****************************************
			
			var MyTask = AsyncCounter.DecrementAny(MyCounters);
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			Assert.AreEqual(MyCounters[0], MyTask.Result, "Decremented unexpected counter");
			
			Assert.AreEqual(0, MyCounters[0].CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounters[0].WaitingCount, "Tasks unexpectedly waiting");
			
			Assert.AreEqual(1, MyCounters[1].CurrentCount, "Counter decremented");
			Assert.AreEqual(0, MyCounters[1].WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementAnyMulti()
		{	//****************************************
			var MyCounters = new AsyncCounter[] { new AsyncCounter(0), new AsyncCounter(0) };
			//****************************************
			
			var MyTask1 = AsyncCounter.DecrementAny(MyCounters);
			var MyTask2 = AsyncCounter.DecrementAny(MyCounters);
			
			Assert.IsFalse(MyTask1.IsCompleted, "Task 1 unexpectedly completed");
			Assert.IsFalse(MyTask2.IsCompleted, "Task 2 unexpectedly completed");
			
			MyCounters[0].Increment();
			
			Thread.Sleep(100); // Increment happens on another thread and there's nothing to wait on
			
			MyCounters[1].Increment();
			
			Thread.Sleep(100); // Increment happens on another thread and there's nothing to wait on
			
			//****************************************
			
			Assert.IsTrue(MyTask1.IsCompleted, "Still waiting to decrement");
			Assert.IsTrue(MyTask2.IsCompleted, "Still waiting to decrement");
			
			CollectionAssert.Contains(MyCounters, MyTask1.Result, "Decremented unexpected counter");
			CollectionAssert.Contains(MyCounters, MyTask2.Result, "Decremented unexpected counter");
			
			Assert.AreNotSame(MyTask1.Result, MyTask2.Result, "Decremented the same counter");
			
			Assert.AreEqual(0, MyCounters[0].CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounters[0].WaitingCount, "Tasks unexpectedly waiting");
			
			Assert.AreEqual(0, MyCounters[1].CurrentCount, "Counter decremented");
			Assert.AreEqual(0, MyCounters[1].WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementAnyCancel()
		{	//****************************************
			var MyCounters = new AsyncCounter[] { new AsyncCounter(0), new AsyncCounter(0) };
			Task<AsyncCounter> MyTask;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				MyTask = AsyncCounter.DecrementAny(MyCounters, MySource.Token);
				
				Assert.IsFalse(MyTask.IsCompleted, "Decremented too early");
				
				MySource.Cancel();
				
				Assert.IsTrue(MyTask.IsCanceled, "Wait not cancelled");
			}
			
			MyCounters[0].Increment();
			MyCounters[1].Increment();
			
			Thread.Sleep(100); // Increment happens on another thread and there's nothing to wait on
			
			Assert.AreEqual(1, MyCounters[0].CurrentCount, "Counter decremented");
			Assert.AreEqual(0, MyCounters[0].WaitingCount, "Tasks unexpectedly waiting");
			
			Assert.AreEqual(1, MyCounters[1].CurrentCount, "Counter decremented");
			Assert.AreEqual(0, MyCounters[1].WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void DecrementAnyDispose()
		{	//****************************************
			var MyCounters = new AsyncCounter[] { new AsyncCounter(0), new AsyncCounter(0) };
			//****************************************
			
			var MyTask = AsyncCounter.DecrementAny(MyCounters);
			
			MyCounters[0].Dispose();
			
			Thread.Sleep(100); // Disposal happens on another thread and there's nothing to wait on
			
			//****************************************
			
			Assert.IsTrue(MyTask.IsCompleted, "Still waiting to decrement");
			Assert.IsTrue(MyTask.IsFaulted, "Did not throw");
			//Assert.AreEqual(MyCounters[0], MyTask.Result, "Decremented unexpected counter");
			
			Assert.AreEqual(0, MyCounters[0].CurrentCount, "Counter not decremented");
			Assert.AreEqual(0, MyCounters[0].WaitingCount, "Tasks unexpectedly waiting");
			
			Assert.AreEqual(0, MyCounters[1].CurrentCount, "Counter decremented");
			Assert.AreEqual(0, MyCounters[1].WaitingCount, "Tasks unexpectedly waiting");
		}
		
		[Test, Timeout(1000)]
		public void TryPeek()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyResult = MyCounter.TryPeekDecrement();
			
			//****************************************
			
			Assert.IsFalse(MyResult, "Peek succeeded unexpectedly");
		}
		
		[Test, Timeout(1000)]
		public void Peek()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************
			
			var MyTask = MyCounter.PeekDecrement();
			
			//****************************************
			
			Assert.IsFalse(MyTask.IsCompleted, "Peek succeeded unexpectedly");
		}

		[Test, Timeout(1000)]
		public async Task PeekDispose()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************

			var MyTask = MyCounter.PeekDecrement();

			MyCounter.Dispose();

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task PeekCancelDispose()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyCounter.PeekDecrement(MySource.Token);

				MyCounter.Dispose();
			}

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task PeekCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyCounter.PeekDecrement(MySource.Token);

				MySource.Cancel();
			}

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should never reach this point");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task PeekDisposeContinueWithPeek()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			//****************************************

			var MyTask = MyCounter.PeekDecrement();

			var MyInnerTask = MyTask.ContinueWith((task) => MyCounter.PeekDecrement(), TaskContinuationOptions.ExecuteSynchronously);

			MyCounter.Dispose();

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}

			try
			{
				await MyInnerTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test, Timeout(2000)]
		public async Task ConsumeDual()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consume1 = Consumer(MyCounter, MySource.Token);
				var Consume2 = Consumer(MyCounter, MySource.Token);

				var Increment1 = Increment(MyCounter, 10, MySource.Token);

				MyResults = await Task.WhenAll(Consume1, Consume2, Increment1);
			}

			//****************************************

			Assert.AreEqual(MyResults[2], MyResults.Take(2).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Take(2).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeDualIncrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consume1 = Consumer(MyCounter, MySource.Token);
				var Consume2 = Consumer(MyCounter, MySource.Token);

				var Increment1 = Increment(MyCounter, 10, MySource.Token);
				var Increment2 = Increment(MyCounter, 10, MySource.Token);

				MyResults = await Task.WhenAll(Consume1, Consume2, Increment1, Increment2);
			}

			//****************************************

			Assert.AreEqual(MyResults.Skip(2).Sum(), MyResults.Take(2).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Take(2).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeLots()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consumers = Enumerable.Range(0, 10).Select(count => Consumer(MyCounter, MySource.Token));

				var Increment1 = Increment(MyCounter, 50, MySource.Token);

				MyResults = await Task.WhenAll(new [] { Increment1 }.Concat(Consumers));
			}

			//****************************************

			Assert.AreEqual(MyResults[0], MyResults.Skip(1).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Skip(1).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeLotsIncrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consumers = Enumerable.Range(0, 10).Select(count => Consumer(MyCounter, MySource.Token));

				var Increment1 = Increment(MyCounter, 50, MySource.Token);

				MyResults = await Task.WhenAll(new[] { Increment1 }.Concat(Consumers));
			}

			//****************************************

			Assert.AreEqual(MyResults[0], MyResults.Skip(1).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Skip(1).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeLotsDualIncrement()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consumers = Enumerable.Range(0, 10).Select(count => Consumer(MyCounter, MySource.Token));

				var Increment1 = Increment(MyCounter, 50, MySource.Token);

				var Increment2 = Increment(MyCounter, 50, MySource.Token);

				MyResults = await Task.WhenAll(new[] { Increment1, Increment2 }.Concat(Consumers));
			}

			//****************************************

			Assert.AreEqual(MyResults.Take(2).Sum(), MyResults.Skip(2).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Skip(2).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeLotsIncrementLots()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consumers = Enumerable.Range(0, 5).Select(count => Consumer(MyCounter, MySource.Token));

				var Incrementers = Enumerable.Range(0, 5).Select(count => Increment(MyCounter, 50, MySource.Token));

				MyResults = await Task.WhenAll(Incrementers.Concat(Consumers));
			}

			//****************************************

			Assert.AreEqual(MyResults.Take(5).Sum(), MyResults.Skip(5).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Skip(5).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeDualCancel()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consume1 = Consumer(MyCounter, MySource.Token);
				var Consume2 = ConsumerCancel(MyCounter, MySource.Token);

				var Increment1 = Increment(MyCounter, 10, MySource.Token);

				MyResults = await Task.WhenAll(Consume1, Consume2, Increment1);
			}

			//****************************************

			Assert.AreEqual(MyResults[2], MyResults.Take(2).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Take(2).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeDualDispose()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consume1 = Consumer(MyCounter, CancellationToken.None);
				var Consume2 = Consumer(MyCounter, CancellationToken.None);

				var Increment1 = Increment(MyCounter, 10, CancellationToken.None);

				MySource.Token.Register(MyCounter.Dispose);

				MyResults = await Task.WhenAll(Consume1, Consume2, Increment1);
			}

			//****************************************

			Assert.AreEqual(MyResults[2], MyResults.Take(2).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Take(2).Sum(), MyCounter.CurrentCount);
		}

		[Test, Timeout(2000)]
		public async Task ConsumeDualCancelDispose()
		{	//****************************************
			var MyCounter = new AsyncCounter();
			int[] MyResults;
			//****************************************

			using (var MySource = new CancellationTokenSource(1000))
			{
				var Consume1 = Consumer(MyCounter, CancellationToken.None);
				var Consume2 = ConsumerCancel(MyCounter, MySource.Token);

				var Increment1 = Increment(MyCounter, 10, CancellationToken.None);

				MySource.Token.Register(MyCounter.Dispose);

				MyResults = await Task.WhenAll(Consume1, Consume2, Increment1);
			}

			//****************************************

			Assert.AreEqual(MyResults[2], MyResults.Take(2).Sum() + MyCounter.CurrentCount, "Counts do not match: {0} + {1}", MyResults.Take(2).Sum(), MyCounter.CurrentCount);
		}

		//****************************************

		private async Task<int> Increment(AsyncCounter counter, int threshold, CancellationToken token)
		{
			int TotalIncrement = 0;

			try
			{
				while (!token.IsCancellationRequested)
				{
					while (counter.CurrentCount < threshold)
					{
						counter.Increment();

						TotalIncrement++;

						if (TotalIncrement % 16 == 0)
							await Task.Yield();
					}

					await Task.Yield();
				}
			}
			catch (ObjectDisposedException)
			{
				System.Diagnostics.Debug.WriteLine("Disposed Incrementer");
			}

			return TotalIncrement;
		}

		private async Task<int> Consumer(AsyncCounter counter, CancellationToken token)
		{
			int TotalDecrement = 0;
			
			try
			{
				while (!token.IsCancellationRequested)
				{
					await counter.Decrement(token);

					TotalDecrement++;

					if (TotalDecrement % 16 == 0)
						await Task.Yield();
				}
			}
			catch (OperationCanceledException)
			{
				System.Diagnostics.Debug.WriteLine("Cancelled Consumer");
			}
			catch (ObjectDisposedException)
			{
				System.Diagnostics.Debug.WriteLine("Disposed Consumer");
			}

			return TotalDecrement;
		}

		private async Task<int> ConsumerCancel(AsyncCounter counter, CancellationToken token)
		{
			int TotalDecrement = 0;

			while (!token.IsCancellationRequested)
			{
				using (var MySource = new CancellationTokenSource(50))
				{
					TotalDecrement += await Consumer(counter, MySource.Token);
				}
			}

			return TotalDecrement;
		}
	}
}
