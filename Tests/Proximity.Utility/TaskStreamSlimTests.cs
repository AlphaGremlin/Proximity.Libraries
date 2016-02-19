/****************************************\
 TaskStreamSlimTests.cs
 Created: 2016-02-17
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of TaskStreamSlimTests.
	/// </summary>
	[TestFixture()]
	public class TaskStreamSlimTests
	{
		public TaskStreamSlimTests()
		{
		}

		//****************************************

		[Test(), Timeout(1000)]
		public async Task QueueSingle()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.Queue(() => { TaskRan = true; });

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}
		
		[Test(), Timeout(1000)]
		public async Task QueueSingleException()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			//****************************************

			var MyTask = MyQueue.Queue(() => { Assert.Pass("Success"); });

			await MyTask;

			//****************************************

			Assert.Fail("Should not run");
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleInValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			//****************************************

			await MyQueue.Queue((value) => { Assert.AreEqual(42, value); }, 42);
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleInOutValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			//****************************************

			var MyResult = await MyQueue.Queue((value) => value, 42);

			//****************************************

			Assert.AreEqual(42, MyResult, "Value is incorrect");
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleOutValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			//****************************************

			var MyResult = await MyQueue.Queue(() => 42);

			//****************************************

			Assert.AreEqual(42, MyResult, "Value is incorrect");
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleTask()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return TaskSource.Task; });

			TaskSource.SetResult(VoidStruct.Empty);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleTaskInValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { Assert.AreEqual(42, value); TaskRan = true; return (Task)TaskSource.Task; }, 42);

			TaskSource.SetResult(VoidStruct.Empty);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleTaskInOutValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var TaskSource = new TaskCompletionSource<int>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { Assert.AreEqual(42, value); TaskRan = true; return TaskSource.Task; }, 42);

			TaskSource.SetResult(30);

			var MyResult = await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
			Assert.AreEqual(30, MyResult, "Value is incorrect");
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleTaskOutValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var TaskSource = new TaskCompletionSource<int>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return TaskSource.Task; });

			TaskSource.SetResult(30);

			var MyResult = await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
			Assert.AreEqual(30, MyResult, "Value is incorrect");
		}

		[Test(), Timeout(1000)]
		public async Task QueueMultiple()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			int RunCounter = 0;
			Task LatestTask = null;
			//****************************************

			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue(() => { RunCounter++; });

			await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter);
		}

		[Test(), Timeout(1000)]
		public async Task QueueMultipleInValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			int RunCounter = 0;
			Task LatestTask = null;
			//****************************************

			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue((value) => { Assert.AreEqual(30, value); RunCounter++; }, 30);
			
			await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter, "Not all tasks executed");
		}

		[Test(), Timeout(1000)]
		public async Task QueueMultipleOutValue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			int RunCounter = 0;
			Task<int> LatestTask = null;
			//****************************************

			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue((value) => { RunCounter++; return value; }, Index);

			var MyResult = await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter, "Not all tasks executed");
			Assert.AreEqual(99, MyResult, "Result is not as expected");
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleCancel()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyQueue.Queue(() => SpinUntilCancelPlusOne(MySource.Token), MySource.Token);

				MySource.Cancel();
			}

			try
			{
				await MyTask;

				Assert.Fail("Task succeeded");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test(), Timeout(1000)]
		public async Task QueueSingleInValueCancel()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyQueue.Queue((value) => { Assert.AreEqual(42, value); SpinUntilCancelPlusOne(MySource.Token); }, 42, MySource.Token);

				MySource.Cancel();
			}

			try
			{
				await MyTask;

				Assert.Fail("Task succeeded");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test(), Timeout(1000)]
		public async Task QueueTwoCancelSecond()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			Task FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			bool SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
				SecondTask = MyQueue.Queue(() => { SecondRan = true; }, MySource.Token);

				MySource.Cancel();
			}

			//****************************************

			try
			{
				await SecondTask;

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
			}

			Assert.IsFalse(FirstTask.IsCompleted, "Task completed");
			Assert.IsFalse(SecondRan, "Second Task ran");

			FirstSource.SetResult(VoidStruct.Empty);

			await FirstTask;
		}

		[Test(), Timeout(1000)]
		public async Task QueueTwoExceptionSecond()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			Task FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
			SecondTask = MyQueue.Queue(() => { Assert.Pass("Success"); });

			//****************************************

			Assert.IsFalse(FirstTask.IsCompleted, "Task completed");
			Assert.IsFalse(SecondTask.IsCompleted, "Second Task ran");

			FirstSource.SetResult(VoidStruct.Empty);

			await SecondTask;
			
			Assert.Fail("Should not run");
		}

		[Test(), Timeout(1000)]
		public async Task QueueTwoInValueCancelSecond()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			Task FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			bool SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask((value) => { Assert.AreEqual(12, value); return FirstSource.Task; }, 12);
				SecondTask = MyQueue.Queue((value) => { Assert.AreEqual(24, value); SecondRan = true; }, 24, MySource.Token);

				MySource.Cancel();
			}

			//****************************************

			try
			{
				await SecondTask;

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
			}

			Assert.IsFalse(FirstTask.IsCompleted, "Task completed");
			Assert.IsFalse(SecondRan, "Second Task ran");

			FirstSource.SetResult(VoidStruct.Empty);

			await FirstTask;
		}

		[Test(), Timeout(1000)]
		public async Task QueueThreeCancelSecond()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			Task FirstTask, SecondTask, ThirdTask;
			TaskCompletionSource<VoidStruct> FirstSource, ThirdSource;
			bool SecondRan = false, ThirdRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();
			ThirdSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
				SecondTask = MyQueue.Queue(() => { SecondRan = true; }, MySource.Token);
				ThirdTask = MyQueue.QueueTask(() => { ThirdRan = true; return ThirdSource.Task; });

				MySource.Cancel();
			}

			ThirdSource.SetResult(VoidStruct.Empty);

			//****************************************

			try
			{
				await SecondTask;

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
			}

			Thread.Sleep(500); // No way to know when the third task will run or not

			Assert.IsFalse(SecondRan, "Second Task was executed");
			Assert.IsFalse(ThirdRan, "Third Task was executed early");

			FirstSource.SetResult(VoidStruct.Empty);

			await ThirdTask;
		}

		[Test(), Timeout(1000)]
		public async Task QueueComplete()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => (Task)TaskSource.Task);

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(VoidStruct.Empty);

			//****************************************

			await CompletionTask;
		}

		[Test(), Timeout(1000)]
		public async Task QueueCompleteMultiple()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			Task LatestTask = null;
			int RunCounter = 0;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => (Task)TaskSource.Task);

			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue(() => { RunCounter++; });

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(VoidStruct.Empty);

			//****************************************

			await CompletionTask;

			Assert.AreEqual(100, RunCounter);
		}

		[Test(), Timeout(2000)]
		public async Task ConcurrentQueue()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			int RunCounter = 0;
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 10).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise

						for (int Index = 0; Index < 20; Index++)
						{
							var MyTask = MyQueue.Queue(() => { RunCounter++; });

							await Task.Yield();
						}

						return;
					})
			);

			//****************************************

			await MyQueue.Complete();

			Assert.AreEqual(20 * 10, RunCounter, "Did not run");
		}

		[Test(), Timeout(2000)]
		public async Task ConcurrentLockCheck()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var MyLock = new AsyncSemaphore();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			int RunCounter = 0;
			var AllTasks = new List<Task>();
			//****************************************

			AllTasks.Add(MyQueue.QueueTask(() => (Task)TaskSource.Task));

			for (int Index = 0; Index < 100; Index++)
			{
				AllTasks.Add(MyQueue.QueueTask(async () =>
					{
						var MyTask = MyLock.Wait();

						if (!MyTask.IsCompleted)
							Assert.Fail("Failure");

						await Task.Yield();

						RunCounter++;

						Assert.AreEqual(0, MyLock.WaitingCount, "Someone was waiting");

						MyTask.Result.Dispose();
					}));
			}

			TaskSource.SetResult(VoidStruct.Empty);

			//****************************************

			await MyQueue.Complete();

			await Task.WhenAll(AllTasks);

			Assert.AreEqual(100, RunCounter, "Did not run");
		}

		[Test(), Timeout(2000)]
		public async Task ConcurrentOrderCheck()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			int RunCounter = 0;
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 4).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						int InnerCount = 0;

						for (int Index = 0; Index < 100; Index++)
						{
							var MyTask = MyQueue.Queue((value) => { RunCounter++; Assert.AreEqual(InnerCount++, value); }, Index);

							await Task.Yield();
						}

						return;
					})
			);

			//****************************************

			await MyQueue.Complete();

			Assert.AreEqual(4 * 100, RunCounter, "Did not run");
		}

		[Test(), Timeout(2000)]
		public async Task ConcurrentLockExtended()
		{	//****************************************
			var MyQueue = new TaskStreamSlim();
			var MyLock = new AsyncSemaphore();
			var AllTasks = new List<Task>();
			int RunCounter = 0;
			var MySpinWait = new SpinWait();
			//****************************************

			var EndTime = DateTime.Now.AddSeconds(1.0);

			do
			{
				if (MyQueue.PendingActions < 100)
				{
					AllTasks.Add(MyQueue.QueueTask(async () =>
					{
						var MyTask = MyLock.Wait();

						if (!MyTask.IsCompleted)
							Assert.Fail("Failure");

						await Task.Yield();

						RunCounter++;

						Assert.AreEqual(0, MyLock.WaitingCount, "Someone was waiting");

						MyTask.Result.Dispose();
					}));
				}

				MySpinWait.SpinOnce();

			} while (EndTime > DateTime.Now);

			//****************************************

			await MyQueue.Complete();

			await Task.WhenAll(AllTasks);
		}

		//****************************************

		private void SpinUntilCancelPlusOne(CancellationToken token)
		{
			var MySpinWait = new SpinWait();

			while (!token.IsCancellationRequested)
				MySpinWait.SpinOnce();

			Thread.Sleep(1000);
		}
	}
}
