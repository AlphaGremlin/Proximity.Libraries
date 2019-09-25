/****************************************\
 TaskStreamTests.cs
 Created: 2016-02-17
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of TaskStreamTests.
	/// </summary>
	[TestFixture()]
	public class TaskStreamTests
	{
		public TaskStreamTests()
		{
		}

		//****************************************

		[Test(), MaxTime(1000)]
		public async Task QueueSingle()
		{	//****************************************
			var MyQueue = new TaskStream();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.Queue(() => { TaskRan = true; });

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleException()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			var MyTask = MyQueue.Queue(() => { Assert.Pass("Success"); });

			await MyTask;

			//****************************************

			Assert.Fail("Should not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			await MyQueue.Queue((value) => { Assert.AreEqual(42, value); }, 42);
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInValueToken()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyQueue.Queue((value) => { Assert.AreEqual(42, value); SpinUntilCancel(MySource.Token); }, 42, MySource.Token);

				await Task.Yield();

				MySource.Cancel();

				try
				{
					await MyTask;

					Assert.Fail("Task succeeded");
				}
				catch (OperationCanceledException)
				{
				}
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			var MyResult = await MyQueue.Queue((value) => value, 42);

			//****************************************

			Assert.AreEqual(42, MyResult, "Value is incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInOutValueToken()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyQueue.Queue((value) => { Assert.AreEqual(42, value); SpinUntilCancel(MySource.Token); return 30; }, 42, MySource.Token);

				await Task.Yield();

				MySource.Cancel();

				try
				{
					await MyTask;

					Assert.Fail("Task succeeded");
				}
				catch (OperationCanceledException)
				{
				}
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			var MyResult = await MyQueue.Queue(() => 42);

			//****************************************

			Assert.AreEqual(42, MyResult, "Value is incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleOutValueToken()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyQueue.Queue((value) => { SpinUntilCancel(MySource.Token); return 42; }, MySource.Token);

				await Task.Yield();

				MySource.Cancel();

				try
				{
					await MyTask;

					Assert.Fail("Task succeeded");
				}
				catch (OperationCanceledException)
				{
				}
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleToken()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task MyTask;
			//****************************************

			using (var MySource = new CancellationTokenSource())
			{
				MyTask = MyQueue.Queue(() => SpinUntilCancel(MySource.Token), MySource.Token);

				await Task.Yield();

				MySource.Cancel();

				try
				{
					await MyTask;

					Assert.Fail("Task succeeded");
				}
				catch (OperationCanceledException)
				{
				}
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTask()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return TaskSource.Task; });

			TaskSource.SetResult(VoidStruct.Empty);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskCancel()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return (Task)TaskSource.Task; });

			while (!TaskRan)
			{
				MySpinWait.SpinOnce();
			}

			TaskSource.SetCanceled();

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskCancelImmediate()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; TaskSource.SetCanceled(); return (Task)TaskSource.Task; });

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskInValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { Assert.AreEqual(42, value); TaskRan = true; return (Task)TaskSource.Task; }, 42);

			TaskSource.SetResult(VoidStruct.Empty);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskInValueCancel()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); return (Task)TaskSource.Task; }, 42);

			while (!TaskRan)
			{
				MySpinWait.SpinOnce();
			}

			TaskSource.SetCanceled();

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskInValueCancelImmediate()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); TaskSource.SetCanceled(); return (Task)TaskSource.Task; }, 42);

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskInOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskInOutValueCancel()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); return TaskSource.Task; }, 42);

			while (!TaskRan)
			{
				MySpinWait.SpinOnce();
			}

			TaskSource.SetCanceled();

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskInOutValueCancelImmediate()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); TaskSource.SetCanceled(); return TaskSource.Task; }, 42);

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskOutValueCancel()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return TaskSource.Task; });

			while (!TaskRan)
			{
				MySpinWait.SpinOnce();
			}

			TaskSource.SetCanceled();

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskOutValueCancelImmediate()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; TaskSource.SetCanceled(); return TaskSource.Task; });

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskFinished()
		{	//****************************************
			var MyQueue = new TaskStream();
			bool TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return Task.CompletedTask; });

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskException()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return (Task)TaskSource.Task; });

			while (!TaskRan)
			{
				MySpinWait.SpinOnce();
			}

			TaskSource.SetException(new ApplicationException());

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (ApplicationException)
			{
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskExceptionOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			bool TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return TaskSource.Task; });

			while (!TaskRan)
			{
				MySpinWait.SpinOnce();
			}

			TaskSource.SetException(new ApplicationException());

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (ApplicationException)
			{
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskExceptionRaised()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { Assert.Pass("Completed"); return Task.CompletedTask; });

			await MyTask;

			//****************************************

			Assert.Fail("Should not reach this point");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskExceptionImmediate()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskSource.SetException(new ApplicationException()); return (Task)TaskSource.Task; });

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (ApplicationException)
			{
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskExceptionImmediateOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskSource.SetException(new ApplicationException()); return (Task)TaskSource.Task; });

			//****************************************

			try
			{
				await MyTask;

				Assert.Fail("Should not reach this point");
			}
			catch (ApplicationException)
			{
			}
		}

		[Test(), MaxTime(1000)]
		public async Task QueueMultiple()
		{	//****************************************
			var MyQueue = new TaskStream();
			int RunCounter = 0;
			Task LatestTask = null;
			//****************************************

			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue(() => { RunCounter++; });

			await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter);
		}

		[Test(), MaxTime(1000)]
		public async Task QueueMultipleInValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			int RunCounter = 0;
			Task LatestTask = null;
			//****************************************

			for (int Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue((value) => { Assert.AreEqual(30, value); RunCounter++; }, 30);

			await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter, "Not all tasks executed");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueMultipleOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(1000)]
		public async Task QueueWaitQueue()
		{	//****************************************
			var MyQueue = new TaskStream();
			bool FirstRan = false, SecondRan = false;
			//****************************************

			await MyQueue.Queue(() => { FirstRan = true; });

			await MyQueue.Queue(() => { SecondRan = true; });

			//****************************************

			Assert.IsTrue(FirstRan, "First task did not run");
			Assert.IsTrue(SecondRan, "Second Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueResultWaitQueueResult()
		{	//****************************************
			var MyQueue = new TaskStream();
			bool FirstRan = false, SecondRan = false;
			//****************************************

			var FirstResult = await MyQueue.Queue(() => { FirstRan = true; return 1; });

			var SecondResult = await MyQueue.Queue(() => { SecondRan = true; return 2; });

			//****************************************

			Assert.IsTrue(FirstRan, "First Task did not run");
			Assert.IsTrue(SecondRan, "Second Task did not run");

			Assert.AreEqual(1, FirstResult, "First Task result incorrect");
			Assert.AreEqual(2, SecondResult, "Second Task result incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecond()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecondInValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			bool SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
				SecondTask = MyQueue.Queue((value) => { SecondRan = true; }, 42, MySource.Token);

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

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecondOutValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task<int> FirstTask, SecondTask;
			TaskCompletionSource<int> FirstSource;
			bool SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<int>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
				SecondTask = MyQueue.Queue(() => { SecondRan = true; return 2; }, MySource.Token);

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

			FirstSource.SetResult(1);

			var MyResult = await FirstTask;

			Assert.AreEqual(1, MyResult, "First Task result incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecondTask()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			bool SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
				SecondTask = MyQueue.QueueTask(() => { SecondRan = true; return Task.CompletedTask; }, MySource.Token);

				await Task.Yield();

				MySource.Cancel();

				//****************************************

				try
				{
					await SecondTask;

					Assert.Fail("Second Task succeeded");
				}
				catch (OperationCanceledException)
				{
				}
			}

			Assert.IsFalse(FirstTask.IsCompleted, "Task completed");
			Assert.IsFalse(SecondRan, "Second Task ran");

			FirstSource.SetResult(VoidStruct.Empty);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecondTaskInValue()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			bool SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
				SecondTask = MyQueue.QueueTask((value) => { SecondRan = true; return Task.CompletedTask; }, 42, MySource.Token);

				await Task.Yield();

				MySource.Cancel();

				//****************************************

				try
				{
					await SecondTask;

					Assert.Fail("Second Task succeeded");
				}
				catch (OperationCanceledException)
				{
				}
			}

			Assert.IsFalse(FirstTask.IsCompleted, "Task completed");
			Assert.IsFalse(SecondRan, "Second Task ran");

			FirstSource.SetResult(VoidStruct.Empty);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoExceptionSecond()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(1000)]
		public async Task QueueTwoInValueCancelSecond()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			bool SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask((value) => { Assert.AreEqual(12, value); return FirstSource.Task; }, 12);
				SecondTask = MyQueue.Queue((value) => { Assert.AreEqual(24, value); SecondRan = true; }, 24, MySource.Token);

				await Task.Yield();

				MySource.Cancel();

				//****************************************

				try
				{
					await SecondTask;

					Assert.Fail("Second Task succeeded");
				}
				catch (OperationCanceledException)
				{
				}
			}

			Assert.IsFalse(FirstTask.IsCompleted, "Task completed");
			Assert.IsFalse(SecondRan, "Second Task ran");

			FirstSource.SetResult(VoidStruct.Empty);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueThree()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task FirstTask, SecondTask, ThirdTask;
			TaskCompletionSource<VoidStruct> FirstSource, SecondSource, ThirdSource;
			bool SecondRan = false, ThirdRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();
			SecondSource = new TaskCompletionSource<VoidStruct>();
			ThirdSource = new TaskCompletionSource<VoidStruct>();

			FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
			SecondTask = MyQueue.QueueTask(() => { SecondRan = true; return SecondSource.Task; });
			ThirdTask = MyQueue.QueueTask(() => { ThirdRan = true; return ThirdSource.Task; });

			await Task.Yield();

			Assert.IsFalse(SecondRan, "Second Task was executed early");
			Assert.IsFalse(ThirdRan, "Third Task was executed early");

			FirstSource.SetResult(VoidStruct.Empty);

			await FirstTask;

			Assert.IsFalse(ThirdRan, "Third Task was executed early");

			SecondSource.SetResult(VoidStruct.Empty);

			await SecondTask;

			ThirdSource.SetResult(VoidStruct.Empty);

			await ThirdTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueThreeCancelSecond()
		{	//****************************************
			var MyQueue = new TaskStream();
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

				await Task.Yield();

				MySource.Cancel();

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
			}

			Thread.Sleep(500); // No way to know when the third task will run or not

			Assert.IsFalse(SecondRan, "Second Task was executed");
			Assert.IsFalse(ThirdRan, "Third Task was executed early");

			FirstSource.SetResult(VoidStruct.Empty);

			await ThirdTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueThreeCancelSecondTask()
		{	//****************************************
			var MyQueue = new TaskStream();
			Task FirstTask, SecondTask, ThirdTask;
			TaskCompletionSource<VoidStruct> FirstSource, ThirdSource;
			bool SecondRan = false, ThirdRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();
			ThirdSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => FirstSource.Task);
				SecondTask = MyQueue.QueueTask(() => { SecondRan = true; return Task.CompletedTask; }, MySource.Token);
				ThirdTask = MyQueue.QueueTask(() => { ThirdRan = true; return ThirdSource.Task; });

				await Task.Yield();

				MySource.Cancel();

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
			}

			Thread.Sleep(500); // No way to know when the third task will run or not

			Assert.IsFalse(SecondRan, "Second Task was executed");
			Assert.IsFalse(ThirdRan, "Third Task was executed early");

			FirstSource.SetResult(VoidStruct.Empty);

			await ThirdTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueComplete()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			var MyTask = MyQueue.Queue(() => { });

			var CompletionTask = MyQueue.Complete();

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueOutValueComplete()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			var MyTask = MyQueue.Queue(() => 42);

			var CompletionTask = MyQueue.Complete();

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTaskComplete()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => (Task)TaskSource.Task);

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(VoidStruct.Empty);

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTaskOutValueComplete()
		{	//****************************************
			var MyQueue = new TaskStream();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => TaskSource.Task);

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(VoidStruct.Empty);

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueCompleteMultiple()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(1000)]
		public void Complete()
		{	//****************************************
			var MyQueue = new TaskStream();
			//****************************************

			var CompletionTask = MyQueue.Complete();

			Assert.IsTrue(CompletionTask.IsCompleted, "Task is not completed");
		}

		[Test(), MaxTime(2000)]
		public async Task ConcurrentQueue()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(2000)]
		public async Task ConcurrentLockCheck()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		[Test(), MaxTime(2000), Repeat(4)]
		public async Task ConcurrentOrderCheck()
		{	//****************************************
			var MyQueue = new TaskStream();
			int RunCounter = 0;
			var AllTasks = new ConcurrentBag<Task>();
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 4).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						int InnerCount = 0;

						for (int Index = 0; Index < 100; Index++)
						{
							AllTasks.Add(MyQueue.Queue((value) => { RunCounter++; Assert.AreEqual(InnerCount++, value); }, Index));

							await Task.Yield();
						}
					})
			);

			//****************************************

			await MyQueue.Complete();

			await Task.WhenAll(AllTasks);

			Assert.AreEqual(4 * 100, RunCounter, "Did not run");
		}

		[Test(), MaxTime(2000)]
		public async Task ConcurrentLockExtended()
		{	//****************************************
			var MyQueue = new TaskStream();
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

		private void SpinUntilCancel(CancellationToken token)
		{
			var MySpinWait = new SpinWait();

			while (!token.IsCancellationRequested)
				MySpinWait.SpinOnce();

			token.ThrowIfCancellationRequested();
		}
	}
}
