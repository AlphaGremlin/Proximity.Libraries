using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//****************************************

namespace Proximity.Threading.Tests
{
	/// <summary>
	/// Description of TaskStreamTests.
	/// </summary>
	[TestFixture()]
	public class TaskQueueTests
	{
		[Test(), MaxTime(1000)]
		public async Task QueueSingle()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskRan = false;
			//****************************************

			var MyTask = MyQueue.Queue(() => { TaskRan = true; });

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleException()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			var MyTask = MyQueue.Queue(() => { Assert.Pass("Success"); });

			await MyTask;

			//****************************************

			Assert.Fail("Should not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			await MyQueue.Queue((value) => { Assert.AreEqual(42, value); }, 42);
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInValueToken()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask MyTask;
			//****************************************

			using var MySource = new CancellationTokenSource();

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

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			var MyResult = await MyQueue.Queue((value) => value, 42);

			//****************************************

			Assert.AreEqual(42, MyResult, "Value is incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleInOutValueToken()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask<int> MyTask;
			//****************************************

			using var MySource = new CancellationTokenSource();

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

		[Test(), MaxTime(1000)]
		public async Task QueueSingleOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			var MyResult = await MyQueue.Queue(() => 42);

			//****************************************

			Assert.AreEqual(42, MyResult, "Value is incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleOutValueToken()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask<int> MyTask;
			//****************************************

			using var MySource = new CancellationTokenSource();

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

		[Test(), MaxTime(1000)]
		public async Task QueueSingleToken()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask MyTask;
			//****************************************

			using var MySource = new CancellationTokenSource();

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

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTask()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return TaskSource.Task; });

			TaskSource.SetResult(default);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskCancel()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask((value) => { Assert.AreEqual(42, value); TaskRan = true; return (Task)TaskSource.Task; }, 42);

			TaskSource.SetResult(default);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskInValueCancel()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskRan = false;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskRan = true; return Task.CompletedTask; });

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskException()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { Assert.Pass("Completed"); return Task.CompletedTask; });

			await MyTask;

			//****************************************

			Assert.Fail("Should not reach this point");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleTaskExceptionImmediate()
		{ //****************************************
			var MyQueue = new TaskQueue();
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => { TaskSource.SetException(new ApplicationException()); return TaskSource.Task; });

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
		public async Task QueueSingleValueTask()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			//****************************************

			async ValueTask Callback()
			{
				TaskRan = true;
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

			TaskSource.SetResult(default);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleValueTaskCancel()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			async ValueTask Callback()
			{
				TaskRan = true;
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		public async Task QueueSingleValueTaskCancelImmediate()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			//****************************************

			async ValueTask Callback() 
			{
				TaskRan = true; 
				TaskSource.SetCanceled(); 
				await TaskSource.Task; 
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		public async Task QueueSingleValueTaskInValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			//****************************************

			async ValueTask Callback(int value)
			{ 
				Assert.AreEqual(42, value);
				TaskRan = true;
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback, 42);

			TaskSource.SetResult(default);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleValueTaskInValueCancel()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			async ValueTask Callback(int value)
			{
				TaskRan = true; 
				Assert.AreEqual(42, value);
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback, 42);

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
		public async Task QueueSingleValueTaskInValueCancelImmediate()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			//****************************************

			async ValueTask Callback(int value)
			{ 
				TaskRan = true; 
				Assert.AreEqual(42, value);
				TaskSource.SetCanceled();
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback, 42);

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
		public async Task QueueSingleValueTaskInOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
			//****************************************

			async ValueTask<int> Callback(int value)
			{ 
				TaskRan = true;
				Assert.AreEqual(42, value);
				TaskRan = true;
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback, 42);

			TaskSource.SetResult(30);

			var MyResult = await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
			Assert.AreEqual(30, MyResult, "Value is incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleValueTaskInOutValueCancel()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			async ValueTask<int> Callback(int value)
			{
				TaskRan = true;
				Assert.AreEqual(42, value);
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback, 42);

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
		public async Task QueueSingleValueTaskInOutValueCancelImmediate()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
			//****************************************

			async ValueTask<int> Callback(int value)
			{
				TaskRan = true;
				Assert.AreEqual(42, value);
				TaskSource.SetCanceled();
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback, 42);

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
		public async Task QueueSingleValueTaskOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
			//****************************************

			async ValueTask<int> Callback()
			{
				TaskRan = true;
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

			TaskSource.SetResult(30);

			var MyResult = await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
			Assert.AreEqual(30, MyResult, "Value is incorrect");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleValueTaskOutValueCancel()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			async ValueTask<int> Callback()
			{
				TaskRan = true;
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		public async Task QueueSingleValueTaskOutValueCancelImmediate()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
			//****************************************

			async ValueTask<int> Callback()
			{
				TaskRan = true;
				TaskSource.SetCanceled();
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		public async Task QueueSingleValueTaskFinished()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskRan = false;
			//****************************************

			ValueTask Callback()
			{
				TaskRan = true;
				return default;
			}

			var MyTask = MyQueue.QueueTask(Callback);

			await MyTask;

			//****************************************

			Assert.IsTrue(TaskRan, "Task did not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleValueTaskException()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			async ValueTask Callback()
			{
				TaskRan = true;
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		public async Task QueueSingleValueTaskExceptionOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			var TaskRan = false;
			var MySpinWait = new SpinWait();
			//****************************************

			async ValueTask<int> Callback()
			{
				TaskRan = true;
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		public async Task QueueSingleValueTaskExceptionRaised()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			static ValueTask Callback()
			{
				Assert.Pass("Completed");
				return default;
			}

			var MyTask = MyQueue.QueueTask(Callback);

			await MyTask;

			//****************************************

			Assert.Fail("Should not reach this point");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueSingleValueTaskExceptionImmediate()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			async ValueTask Callback()
			{
				TaskSource.SetException(new ApplicationException());
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		public async Task QueueSingleValueTaskExceptionImmediateOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			//****************************************

			async ValueTask<int> Callback()
			{
				TaskSource.SetException(new ApplicationException());
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var RunCounter = 0;
			ValueTask LatestTask = default;
			//****************************************

			for (var Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue(() => { RunCounter++; });

			await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter);
		}

		[Test(), MaxTime(1000)]
		public async Task QueueMultipleInValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var RunCounter = 0;
			ValueTask LatestTask = default;
			//****************************************

			for (var Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue((value) => { Assert.AreEqual(30, value); RunCounter++; }, 30);

			await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter, "Not all tasks executed");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueMultipleOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var RunCounter = 0;
			ValueTask<int> LatestTask = default;
			//****************************************

			for (var Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue((value) => { RunCounter++; return value; }, Index);

			var MyResult = await LatestTask;

			//****************************************

			Assert.AreEqual(100, RunCounter, "Not all tasks executed");
			Assert.AreEqual(99, MyResult, "Result is not as expected");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueWaitQueue()
		{ //****************************************
			var MyQueue = new TaskQueue();
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
		{ //****************************************
			var MyQueue = new TaskQueue();
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			var SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
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

			FirstSource.SetResult(default);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecondInValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			var SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
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

			FirstSource.SetResult(default);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecondOutValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask<int> FirstTask, SecondTask;
			TaskCompletionSource<int> FirstSource;
			var SecondRan = false;
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
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			var SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
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

			FirstSource.SetResult(default);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoCancelSecondTaskInValue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			var SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
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

			FirstSource.SetResult(default);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoExceptionSecond()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
			SecondTask = MyQueue.Queue(() => { Assert.Pass("Success"); });

			//****************************************

			Assert.IsFalse(FirstTask.IsCompleted, "Task completed");
			Assert.IsFalse(SecondTask.IsCompleted, "Second Task ran");

			FirstSource.SetResult(default);

			await SecondTask;

			Assert.Fail("Should not run");
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTwoInValueCancelSecond()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask;
			TaskCompletionSource<VoidStruct> FirstSource;
			var SecondRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask((value) => { Assert.AreEqual(12, value); return (Task)FirstSource.Task; }, 12);
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

			FirstSource.SetResult(default);

			await FirstTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueThree()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask, ThirdTask;
			TaskCompletionSource<VoidStruct> FirstSource, SecondSource, ThirdSource;
			bool SecondRan = false, ThirdRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();
			SecondSource = new TaskCompletionSource<VoidStruct>();
			ThirdSource = new TaskCompletionSource<VoidStruct>();

			FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
			SecondTask = MyQueue.QueueTask(() => { SecondRan = true; return (Task)SecondSource.Task; });
			ThirdTask = MyQueue.QueueTask(() => { ThirdRan = true; return (Task)ThirdSource.Task; });

			await Task.Yield();

			Assert.IsFalse(SecondRan, "Second Task was executed early");
			Assert.IsFalse(ThirdRan, "Third Task was executed early");

			FirstSource.SetResult(default);

			await FirstTask;

			Assert.IsFalse(ThirdRan, "Third Task was executed early");

			SecondSource.SetResult(default);

			await SecondTask;

			ThirdSource.SetResult(default);

			await ThirdTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueThreeCancelSecond()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask, ThirdTask;
			TaskCompletionSource<VoidStruct> FirstSource, ThirdSource;
			bool SecondRan = false, ThirdRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();
			ThirdSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
				SecondTask = MyQueue.Queue(() => { SecondRan = true; }, MySource.Token);
				ThirdTask = MyQueue.QueueTask(() => { ThirdRan = true; return (Task)ThirdSource.Task; });

				await Task.Yield();

				MySource.Cancel();

				ThirdSource.SetResult(default);

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

			FirstSource.SetResult(default);

			await ThirdTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueThreeCancelSecondTask()
		{ //****************************************
			var MyQueue = new TaskQueue();
			ValueTask FirstTask, SecondTask, ThirdTask;
			TaskCompletionSource<VoidStruct> FirstSource, ThirdSource;
			bool SecondRan = false, ThirdRan = false;
			//****************************************

			FirstSource = new TaskCompletionSource<VoidStruct>();
			ThirdSource = new TaskCompletionSource<VoidStruct>();

			using (var MySource = new CancellationTokenSource())
			{
				FirstTask = MyQueue.QueueTask(() => (Task)FirstSource.Task);
				SecondTask = MyQueue.QueueTask(() => { SecondRan = true; return Task.CompletedTask; }, MySource.Token);
				ThirdTask = MyQueue.QueueTask(() => { ThirdRan = true; return (Task)ThirdSource.Task; });

				await Task.Yield();

				MySource.Cancel();

				ThirdSource.SetResult(default);

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

			FirstSource.SetResult(default);

			await ThirdTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueComplete()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			var MyTask = MyQueue.Queue(() => { });

			var CompletionTask = MyQueue.Complete();

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueOutValueComplete()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			var MyTask = MyQueue.Queue(() => 42);

			var CompletionTask = MyQueue.Complete();

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTaskComplete()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => (Task)TaskSource.Task);

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(default);

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueTaskOutValueComplete()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			var MyTask = MyQueue.QueueTask(() => TaskSource.Task);

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(default);

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueValueTaskComplete()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			async ValueTask Callback()
			{
				await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(default);

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueValueTaskOutValueComplete()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<int>();
			//****************************************

			async ValueTask<int> Callback()
			{
				return await TaskSource.Task;
			}

			var MyTask = MyQueue.QueueTask(Callback);

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(42);

			//****************************************

			await CompletionTask;
		}

		[Test(), MaxTime(1000)]
		public async Task QueueCompleteMultiple()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			ValueTask LatestTask = default;
			var RunCounter = 0;
			//****************************************

			var MyTask = MyQueue.QueueTask(() => (Task)TaskSource.Task);

			for (var Index = 0; Index < 100; Index++)
				LatestTask = MyQueue.Queue(() => { RunCounter++; });

			var CompletionTask = MyQueue.Complete();

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(default);

			//****************************************

			await CompletionTask;

			Assert.AreEqual(100, RunCounter);
		}

		[Test(), MaxTime(1000)]
		public void Complete()
		{ //****************************************
			var MyQueue = new TaskQueue();
			//****************************************

			var CompletionTask = MyQueue.Complete();

			Assert.IsTrue(CompletionTask.IsCompleted, "Task is not completed");
		}

		[Test()]//, MaxTime(1000)]
		public async Task CompleteQueueValueTask()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			//****************************************

			async ValueTask Callback()
			{
				await TaskSource.Task;
			}

			var MyTask1 = MyQueue.QueueTask(Callback);

			var CompletionTask = MyQueue.Complete();

			var MyTask2 = MyQueue.QueueTask(Callback);

			Assert.IsFalse(CompletionTask.IsCompleted, "Task completed early");

			TaskSource.SetResult(default);

			//****************************************

			await CompletionTask;

			await MyTask2;
		}

		[Test(), MaxTime(2000)]
		public async Task ConcurrentQueue()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var RunCounter = 0;
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 10).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise

						for (var Index = 0; Index < 20; Index++)
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

		[Test(), MaxTime(2000), Repeat(10)]
		public async Task ConcurrentLockCheck()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var MyLock = new AsyncSemaphore();
			var TaskSource = new TaskCompletionSource<VoidStruct>();
			var RunCounter = 0;
			var AllTasks = new List<Task>();
			//****************************************

			AllTasks.Add(MyQueue.QueueTask(() => (Task)TaskSource.Task).AsTask());

			async ValueTask Callback(int index)
			{
				var MyTask = MyLock.Take();

				if (!MyTask.IsCompleted)
					Assert.Fail("Failure on run {0}", index);

				await Task.Yield();

				RunCounter++;

				Assert.AreEqual(0, MyLock.WaitingCount, "Someone was waiting on run {0}", index);

				MyTask.Result.Dispose();
			}

			for (var Index = 0; Index < 100; Index++)
			{
				AllTasks.Add(MyQueue.QueueTask(Callback, Index).AsTask());
			}

			TaskSource.SetResult(default);

			//****************************************

			await MyQueue.Complete();

			await Task.WhenAll(AllTasks);

			Assert.AreEqual(100, RunCounter, "Did not run");
		}

		[Test(), MaxTime(2000), Repeat(4)]
		public async Task ConcurrentOrderCheck()
		{ //****************************************
			var MyQueue = new TaskQueue();
			var RunCounter = 0;
			var AllTasks = new ConcurrentBag<Task>();
			//****************************************

			await Task.WhenAll(
				Enumerable.Range(1, 4).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						var InnerCount = 0;

						for (var Index = 0; Index < 100; Index++)
						{
							AllTasks.Add(MyQueue.Queue((value) => { RunCounter++; Assert.AreEqual(InnerCount++, value); }, Index).AsTask());

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
		{ //****************************************
			var MyQueue = new TaskQueue();
			var MyLock = new AsyncSemaphore();
			var AllTasks = new List<Task>();
			var RunCounter = 0;
			var MySpinWait = new SpinWait();
			//****************************************

			var EndTime = DateTime.Now.AddSeconds(1.0);

			async ValueTask Callback()
			{
				var MyTask = MyLock.Take();

				if (!MyTask.IsCompleted)
					Assert.Fail("Failure");

				await Task.Yield();

				RunCounter++;

				Assert.AreEqual(0, MyLock.WaitingCount, "Someone was waiting");

				MyTask.Result.Dispose();
			}

			do
			{
				if (MyQueue.PendingActions < 100)
				{
					AllTasks.Add(MyQueue.QueueTask(Callback).AsTask());
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
