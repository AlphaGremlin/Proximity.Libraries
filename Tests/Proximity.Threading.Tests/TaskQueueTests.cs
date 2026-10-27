using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Description of TaskStreamTests.
/// </summary>
[TestFixture()]
public class TaskQueueTests
{
	[Test, CancelAfter(1000)]
	public async Task QueueSingle(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskRan = false;
		//****************************************

		var Task = Queue.Queue(() => { TaskRan = true; }, CancellationToken.None);

		await Task.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleException(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		var Task = Queue.Queue(() => { Assert.Pass("Success"); }, CancellationToken.None);

		await Task.WaitAsync(token);

		//****************************************

		Assert.Fail("Should not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleInValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		await Queue.Queue((value) => { Assert.AreEqual(42, value); }, 42, CancellationToken.None).WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleInValueToken(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var QueuedTask = Queue.Queue((value) => { Assert.AreEqual(42, value); SpinUntilCancel(CancelSource.Token); }, 42, CancelSource.Token);

		await Task.Yield();

		CancelSource.Cancel();

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Task succeeded");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleInOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		var Result = await Queue.Queue((value) => value, 42, CancellationToken.None).WaitAsync(token);

		//****************************************

		Assert.That(Result, Is.EqualTo(42), "Value is incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleInOutValueToken(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var QueuedTask = Queue.Queue((value) => { Assert.AreEqual(42, value); SpinUntilCancel(CancelSource.Token); return 30; }, 42, CancelSource.Token);

		await Task.Yield();

		CancelSource.Cancel();

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Task succeeded");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		var Result = await Queue.Queue(() => 42, CancellationToken.None).WaitAsync(token);

		//****************************************

		Assert.That(Result, Is.EqualTo(42), "Value is incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleOutValueToken(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var QueuedTask = Queue.Queue((value) => { SpinUntilCancel(CancelSource.Token); return 42; }, CancelSource.Token);

		await Task.Yield();

		CancelSource.Cancel();

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Task succeeded");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleToken(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		using var CancelSource = new CancellationTokenSource();

		var QueuedTask = Queue.Queue(() => SpinUntilCancel(CancelSource.Token), CancelSource.Token);

		await Task.Yield();

		CancelSource.Cancel();

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Task succeeded");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTask(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; return TaskSource.Task; }, CancellationToken.None).WaitAsync(token);

		TaskSource.SetResult();

		await QueuedTask;

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; return TaskSource.Task; }, CancellationToken.None);

		while (!TaskRan)
		{
			SpinWait.SpinOnce();
		}

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; TaskSource.SetCanceled(); return TaskSource.Task; }, CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskInValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask((value) => { Assert.AreEqual(42, value); TaskRan = true; return TaskSource.Task; }, 42, CancellationToken.None);

		TaskSource.SetResult();

		await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskInValueCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		var QueuedTask = Queue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); return TaskSource.Task; }, 42, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskInValueCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		var QueedTask = Queue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); TaskSource.SetCanceled(); return TaskSource.Task; }, 42, CancellationToken.None);

		try
		{
			await QueedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskInOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask((value) => { Assert.AreEqual(42, value); TaskRan = true; return TaskSource.Task; }, 42, CancellationToken.None);

		TaskSource.SetResult(30);

		var Result = await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
		Assert.That(Result, Is.EqualTo(30), "Value is incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskInOutValueCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		var QueuedTask = Queue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); return TaskSource.Task; }, 42, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskInOutValueCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask((value) => { TaskRan = true; Assert.AreEqual(42, value); TaskSource.SetCanceled(); return TaskSource.Task; }, 42, CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; return TaskSource.Task; }, CancellationToken.None);

		TaskSource.SetResult(30);

		var Result = await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
		Assert.That(Result, Is.EqualTo(30), "Value is incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskOutValueCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; return TaskSource.Task; }, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskOutValueCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; TaskSource.SetCanceled(); return TaskSource.Task; }, CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskFinished(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskRan = false;
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; return Task.CompletedTask; }, CancellationToken.None);

		await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskException(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; return TaskSource.Task; }, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetException(new ApplicationException());

		//****************************************

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskExceptionOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskRan = true; return TaskSource.Task; }, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetException(new ApplicationException());

		//****************************************

		try
		{
			await QueuedTask;

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskExceptionRaised(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { Assert.Pass("Completed"); return Task.CompletedTask; }, CancellationToken.None);

		await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.Fail("Should not reach this point");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskExceptionImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskSource.SetException(new ApplicationException()); return TaskSource.Task; }, CancellationToken.None);

		//****************************************

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleTaskExceptionImmediateOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => { TaskSource.SetException(new ApplicationException()); return TaskSource.Task; }, CancellationToken.None);

		//****************************************

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTask(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		async ValueTask Callback()
		{
			TaskRan = true;
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		TaskSource.SetResult();

		await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		async ValueTask Callback()
		{
			TaskRan = true;
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		async ValueTask Callback()
		{
			TaskRan = true;
			TaskSource.SetCanceled(CancellationToken.None);
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskInValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		async ValueTask Callback(int value)
		{
			Assert.AreEqual(42, value);
			TaskRan = true;
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, 42, CancellationToken.None);

		TaskSource.SetResult();

		await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskInValueCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		async ValueTask Callback(int value)
		{
			TaskRan = true;
			Assert.AreEqual(42, value);
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, 42, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskInValueCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		//****************************************

		async ValueTask Callback(int value)
		{
			TaskRan = true;
			Assert.AreEqual(42, value);
			TaskSource.SetCanceled(CancellationToken.None);
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, 42, CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskInOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
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

		var QueuedTask = Queue.QueueAsync(Callback, 42, CancellationToken.None);

		TaskSource.SetResult(30);

		var MyResult = await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
		Assert.That(MyResult, Is.EqualTo(30), "Value is incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskInOutValueCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		async ValueTask<int> Callback(int value)
		{
			TaskRan = true;
			Assert.AreEqual(42, value);
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, 42, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskInOutValueCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		//****************************************

		async ValueTask<int> Callback(int value)
		{
			TaskRan = true;
			Assert.AreEqual(42, value);
			TaskSource.SetCanceled(CancellationToken.None);
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, 42, CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		//****************************************

		async ValueTask<int> Callback()
		{
			TaskRan = true;
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		TaskSource.SetResult(30);

		var MyResult = await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
		Assert.That(MyResult, Is.EqualTo(30), "Value is incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskOutValueCancel(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		async ValueTask<int> Callback()
		{
			TaskRan = true;
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetCanceled(CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskOutValueCancelImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		//****************************************

		async ValueTask<int> Callback()
		{
			TaskRan = true;
			TaskSource.SetCanceled(CancellationToken.None);
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskFinished(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskRan = false;
		//****************************************

		ValueTask Callback()
		{
			TaskRan = true;
			return default;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.That(TaskRan, Is.True, "Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskException(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		async ValueTask Callback()
		{
			TaskRan = true;
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		while (!TaskRan)
			SpinWait.SpinOnce();

		TaskSource.SetException(new ApplicationException());

		//****************************************

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskExceptionOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		var TaskRan = false;
		var SpinWait = new SpinWait();
		//****************************************

		async ValueTask<int> Callback()
		{
			TaskRan = true;
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		while (!TaskRan)
		{
			SpinWait.SpinOnce();
		}

		TaskSource.SetException(new ApplicationException());

		//****************************************

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskExceptionRaised(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		static ValueTask Callback()
		{
			Assert.Pass("Completed");
			return default;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		await QueuedTask.WaitAsync(token);

		//****************************************

		Assert.Fail("Should not reach this point");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskExceptionImmediate(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		//****************************************

		async ValueTask Callback()
		{
			TaskSource.SetException(new ApplicationException());
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		//****************************************

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueSingleValueTaskExceptionImmediateOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		//****************************************

		async ValueTask<int> Callback()
		{
			TaskSource.SetException(new ApplicationException());
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		//****************************************

		try
		{
			await QueuedTask.WaitAsync(token);

			Assert.Fail("Should not reach this point");
		}
		catch (ApplicationException)
		{
		}
	}

	[Test, CancelAfter(1000)]
	public async Task QueueMultiple(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var RunCounter = 0;
		ValueTask LatestTask = default;
		//****************************************

		for (var Index = 0; Index < 100; Index++)
			LatestTask = Queue.Queue(() => { RunCounter++; }, CancellationToken.None);

		await LatestTask.WaitAsync(token);

		//****************************************

		Assert.That(RunCounter, Is.EqualTo(100));
	}

	[Test, CancelAfter(1000)]
	public async Task QueueMultipleInValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var RunCounter = 0;
		ValueTask LatestTask = default;
		//****************************************

		for (var Index = 0; Index < 100; Index++)
			LatestTask = Queue.Queue((value) => { Assert.AreEqual(30, value); RunCounter++; }, 30, CancellationToken.None);

		await LatestTask.WaitAsync(token);

		//****************************************

		Assert.That(RunCounter, Is.EqualTo(100), "Not all tasks executed");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueMultipleOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var RunCounter = 0;
		ValueTask<int> LatestTask = default;
		//****************************************

		for (var Index = 0; Index < 100; Index++)
			LatestTask = Queue.Queue((value) => { RunCounter++; return value; }, Index, CancellationToken.None);

		var MyResult = await LatestTask.WaitAsync(token);

		//****************************************

		Assert.That(RunCounter, Is.EqualTo(100), "Not all tasks executed");
		Assert.That(MyResult, Is.EqualTo(99), "Result is not as expected");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueWaitQueue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		bool FirstRan = false, SecondRan = false;
		//****************************************

		await Queue.Queue(() => { FirstRan = true; }, CancellationToken.None).WaitAsync(token);

		await Queue.Queue(() => { SecondRan = true; }, CancellationToken.None).WaitAsync(token);

		//****************************************

		Assert.That(FirstRan, Is.True, "First task did not run");
		Assert.That(SecondRan, Is.True, "Second Task did not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueResultWaitQueueResult(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		bool FirstRan = false, SecondRan = false;
		//****************************************

		var FirstResult = await Queue.Queue(() => { FirstRan = true; return 1; }, CancellationToken.None).WaitAsync(token);

		var SecondResult = await Queue.Queue(() => { SecondRan = true; return 2; }, CancellationToken.None).WaitAsync(token);

		//****************************************

		Assert.That(FirstRan, Is.True, "First Task did not run");
		Assert.That(SecondRan, Is.True, "Second Task did not run");

		Assert.That(FirstResult, Is.EqualTo(1), "First Task result incorrect");
		Assert.That(SecondResult, Is.EqualTo(2), "Second Task result incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTwoCancelSecond(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask;
		TaskCompletionSource FirstSource;
		var SecondRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
			SecondTask = Queue.Queue(() => { SecondRan = true; }, CancelSource.Token);

			CancelSource.Cancel();
		}

		//****************************************

		try
		{
			await SecondTask.WaitAsync(token);

			Assert.Fail("Second Task succeeded");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		Assert.That(FirstTask.IsCompleted, Is.False, "Task completed");
		Assert.That(SecondRan, Is.False, "Second Task ran");

		FirstSource.SetResult();

		await FirstTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTwoCancelSecondInValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask;
		TaskCompletionSource FirstSource;
		var SecondRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
			SecondTask = Queue.Queue((value) => { SecondRan = true; }, 42, CancelSource.Token);

			CancelSource.Cancel();
		}

		//****************************************

		try
		{
			await SecondTask.WaitAsync(token);

			Assert.Fail("Second Task succeeded");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		Assert.That(FirstTask.IsCompleted, Is.False, "Task completed");
		Assert.That(SecondRan, Is.False, "Second Task ran");

		FirstSource.SetResult();

		await FirstTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTwoCancelSecondOutValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask<int> FirstTask, SecondTask;
		TaskCompletionSource<int> FirstSource;
		var SecondRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource<int>();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
			SecondTask = Queue.Queue(() => { SecondRan = true; return 2; }, CancelSource.Token);

			CancelSource.Cancel();
		}

		//****************************************

		try
		{
			await SecondTask.WaitAsync(token);

			Assert.Fail("Second Task succeeded");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		Assert.That(FirstTask.IsCompleted, Is.False, "Task completed");
		Assert.That(SecondRan, Is.False, "Second Task ran");

		FirstSource.SetResult(1);

		var Result = await FirstTask.WaitAsync(token);

		Assert.That(Result, Is.EqualTo(1), "First Task result incorrect");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTwoCancelSecondTask(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask;
		TaskCompletionSource FirstSource;
		var SecondRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
			SecondTask = Queue.QueueTask(() => { SecondRan = true; return Task.CompletedTask; }, CancelSource.Token);

			await Task.Yield();

			CancelSource.Cancel();

			//****************************************

			try
			{
				await SecondTask.WaitAsync(token);

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}
		}

		Assert.That(FirstTask.IsCompleted, Is.False, "Task completed");
		Assert.That(SecondRan, Is.False, "Second Task ran");

		FirstSource.SetResult();

		await FirstTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTwoCancelSecondTaskInValue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask;
		TaskCompletionSource FirstSource;
		var SecondRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
			SecondTask = Queue.QueueTask((value) => { SecondRan = true; return Task.CompletedTask; }, 42, CancelSource.Token);

			await Task.Yield();

			CancelSource.Cancel();

			//****************************************

			try
			{
				await SecondTask.WaitAsync(token);

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}
		}

		Assert.That(FirstTask.IsCompleted, Is.False, "Task completed");
		Assert.That(SecondRan, Is.False, "Second Task ran");

		FirstSource.SetResult();

		await FirstTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTwoExceptionSecond(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask;
		TaskCompletionSource FirstSource;
		//****************************************

		FirstSource = new TaskCompletionSource();

		FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
		SecondTask = Queue.Queue(() => { Assert.Pass("Success"); }, CancellationToken.None);

		//****************************************

		Assert.That(FirstTask.IsCompleted, Is.False, "Task completed");
		Assert.That(SecondTask.IsCompleted, Is.False, "Second Task ran");

		FirstSource.SetResult();

		await SecondTask.WaitAsync(token);

		Assert.Fail("Should not run");
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTwoInValueCancelSecond(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask;
		TaskCompletionSource FirstSource;
		var SecondRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask((value) => { Assert.AreEqual(12, value); return FirstSource.Task; }, 12, CancellationToken.None);
			SecondTask = Queue.Queue((value) => { Assert.AreEqual(24, value); SecondRan = true; }, 24, CancelSource.Token);

			await Task.Yield();

			CancelSource.Cancel();

			//****************************************

			try
			{
				await SecondTask.WaitAsync(token);

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}
		}

		Assert.That(FirstTask.IsCompleted, Is.False, "Task completed");
		Assert.That(SecondRan, Is.False, "Second Task ran");

		FirstSource.SetResult();

		await FirstTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueThree(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask, ThirdTask;
		TaskCompletionSource FirstSource, SecondSource, ThirdSource;
		bool SecondRan = false, ThirdRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();
		SecondSource = new TaskCompletionSource();
		ThirdSource = new TaskCompletionSource();

		FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
		SecondTask = Queue.QueueTask(() => { SecondRan = true; return SecondSource.Task; }, CancellationToken.None);
		ThirdTask = Queue.QueueTask(() => { ThirdRan = true; return ThirdSource.Task; }, CancellationToken.None);

		await Task.Yield();

		Assert.That(SecondRan, Is.False, "Second Task was executed early");
		Assert.That(ThirdRan, Is.False, "Third Task was executed early");

		FirstSource.SetResult();

		await FirstTask.WaitAsync(token);

		Assert.That(ThirdRan, Is.False, "Third Task was executed early");

		SecondSource.SetResult();

		await SecondTask.WaitAsync(token);

		ThirdSource.SetResult();

		await ThirdTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueThreeCancelSecond(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask, ThirdTask;
		TaskCompletionSource FirstSource, ThirdSource;
		bool SecondRan = false, ThirdRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();
		ThirdSource = new TaskCompletionSource();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
			SecondTask = Queue.Queue(() => { SecondRan = true; }, CancelSource.Token);
			ThirdTask = Queue.QueueTask(() => { ThirdRan = true; return ThirdSource.Task; }, CancellationToken.None);

			await Task.Yield();

			CancelSource.Cancel();

			ThirdSource.SetResult();

			//****************************************

			try
			{
				await SecondTask.WaitAsync(token);

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}
		}

		await Task.Delay(500, token); // No way to know when the third task will run or not

		Assert.That(SecondRan, Is.False, "Second Task was executed");
		Assert.That(ThirdRan, Is.False, "Third Task was executed early");

		FirstSource.SetResult();

		await ThirdTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueThreeCancelSecondTask(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		ValueTask FirstTask, SecondTask, ThirdTask;
		TaskCompletionSource FirstSource, ThirdSource;
		bool SecondRan = false, ThirdRan = false;
		//****************************************

		FirstSource = new TaskCompletionSource();
		ThirdSource = new TaskCompletionSource();

		using (var CancelSource = new CancellationTokenSource())
		{
			FirstTask = Queue.QueueTask(() => FirstSource.Task, CancellationToken.None);
			SecondTask = Queue.QueueTask(() => { SecondRan = true; return Task.CompletedTask; }, CancelSource.Token);
			ThirdTask = Queue.QueueTask(() => { ThirdRan = true; return ThirdSource.Task; }, CancellationToken.None);

			await Task.Yield();

			CancelSource.Cancel();

			ThirdSource.SetResult();

			//****************************************

			try
			{
				await SecondTask.WaitAsync(token);

				Assert.Fail("Second Task succeeded");
			}
			catch (OperationCanceledException)
			{
				Assert.That(token.IsCancellationRequested, Is.False);
			}
		}

		await Task.Delay(500, token); // No way to know when the third task will run or not

		Assert.That(SecondRan, Is.False, "Second Task was executed");
		Assert.That(ThirdRan, Is.False, "Third Task was executed early");

		FirstSource.SetResult();

		await ThirdTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueComplete(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		var QueuedTask = Queue.Queue(() => { }, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		//****************************************

		await CompletionTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueOutValueComplete(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		var QueuedTask = Queue.Queue(() => 42, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		//****************************************

		await CompletionTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTaskComplete(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => TaskSource.Task, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		Assert.That(CompletionTask.IsCompleted, Is.False, "Task completed early");

		TaskSource.SetResult();

		//****************************************

		await CompletionTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueTaskOutValueComplete(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		//****************************************

		var QueuedTask = Queue.QueueTask(() => TaskSource.Task, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		Assert.That(CompletionTask.IsCompleted, Is.False, "Task completed early");

		TaskSource.SetResult(1);

		//****************************************

		await CompletionTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueValueTaskComplete(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		//****************************************

		async ValueTask Callback()
		{
			await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		Assert.That(CompletionTask.IsCompleted, Is.False, "Task completed early");

		TaskSource.SetResult();

		//****************************************

		await CompletionTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueValueTaskOutValueComplete(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource<int>();
		//****************************************

		async ValueTask<int> Callback()
		{
			return await TaskSource.Task;
		}

		var QueuedTask = Queue.QueueAsync(Callback, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		Assert.That(CompletionTask.IsCompleted, Is.False, "Task completed early");

		TaskSource.SetResult(42);

		//****************************************

		await CompletionTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task QueueCompleteMultiple(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		ValueTask LatestTask = default;
		var RunCounter = 0;
		//****************************************

		var QueuedTask = Queue.QueueTask(() => TaskSource.Task, CancellationToken.None);

		for (var Index = 0; Index < 100; Index++)
			LatestTask = Queue.Queue(() => { RunCounter++; }, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		Assert.That(CompletionTask.IsCompleted, Is.False, "Task completed early");

		TaskSource.SetResult();

		//****************************************

		await CompletionTask.WaitAsync(token);

		Assert.That(RunCounter, Is.EqualTo(100));
	}

	[Test]
	public void Complete()
	{ //****************************************
		var Queue = new TaskQueue();
		//****************************************

		var CompletionTask = Queue.Complete(CancellationToken.None);

		Assert.That(CompletionTask.IsCompleted, Is.True, "Task is not completed");
	}

	[Test, CancelAfter(1000)]
	public async Task CompleteQueueValueTask(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var TaskSource = new TaskCompletionSource();
		//****************************************

		async ValueTask Callback()
		{
			await TaskSource.Task;
		}

		var Task1 = Queue.QueueAsync(Callback, CancellationToken.None);

		var CompletionTask = Queue.Complete(CancellationToken.None);

		var Task2 = Queue.QueueAsync(Callback, CancellationToken.None);

		Assert.That(CompletionTask.IsCompleted, Is.False, "Task completed early");

		TaskSource.SetResult();

		//****************************************

		await CompletionTask.WaitAsync(token);

		await Task2.WaitAsync(token);
	}

	[Test, CancelAfter(2000)]
	public async Task ConcurrentQueue(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var RunCounter = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 10).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					for (var Index = 0; Index < 20; Index++)
					{
						var QueuedTask = Queue.Queue(() => { RunCounter++; });

						await Task.Yield();
					}

					return;
				})
		);

		//****************************************

		await Queue.Complete().WaitAsync(token);

		Assert.That(RunCounter, Is.EqualTo(20 * 10), "Did not run");
	}

	[Test, CancelAfter(2000), Repeat(10)]
	public async Task ConcurrentLockCheck(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var Lock = new AsyncSemaphore();
		var TaskSource = new TaskCompletionSource();
		var RunCounter = 0;
		var AllTasks = new List<Task>();
		//****************************************

		AllTasks.Add(Queue.QueueTask(() => TaskSource.Task, CancellationToken.None).AsTask());

		async ValueTask Callback(int index)
		{
			var QueuedTask = Lock.Take(token);

			if (!QueuedTask.IsCompleted)
				Assert.Fail($"Failure on run {index}");

			await Task.Yield();

			RunCounter++;

			Assert.AreEqual(0, Lock.WaitingCount, "Someone was waiting on run {0}", index);

			QueuedTask.Result.Dispose();
		}

		for (var Index = 0; Index < 100; Index++)
		{
			AllTasks.Add(Queue.QueueAsync(Callback, Index, CancellationToken.None).AsTask());
		}

		TaskSource.SetResult();

		//****************************************

		await Queue.Complete(CancellationToken.None);

		await Task.WhenAll(AllTasks).WaitAsync(token);

		Assert.That(RunCounter, Is.EqualTo(100), "Did not run");
	}

	[Test, CancelAfter(2000), Repeat(4)]
	public async Task ConcurrentOrderCheck(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
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
						AllTasks.Add(Queue.Queue((value) => { RunCounter++; Assert.AreEqual(InnerCount++, value); }, Index).AsTask());

						await Task.Yield();
					}
				})
		);

		//****************************************

		await Queue.Complete(CancellationToken.None);

		await Task.WhenAll(AllTasks).WaitAsync(token);

		Assert.That(RunCounter, Is.EqualTo(4 * 100), "Did not run");
	}

	[Test, CancelAfter(2000)]
	public async Task ConcurrentLockExtended(CancellationToken token)
	{ //****************************************
		var Queue = new TaskQueue();
		var Lock = new AsyncSemaphore();
		var AllTasks = new List<Task>();
		var RunCounter = 0;
		var SpinWait = new SpinWait();
		//****************************************

		var EndTime = DateTime.Now.AddSeconds(1.0);

		async ValueTask Callback()
		{
			var QueuedTask = Lock.Take(token);

			if (!QueuedTask.IsCompleted)
				Assert.Fail("Failure");

			await Task.Yield();

			RunCounter++;

			Assert.AreEqual(0, Lock.WaitingCount, "Someone was waiting");

			QueuedTask.Result.Dispose();
		}

		do
		{
			if (Queue.PendingActions < 100)
			{
				AllTasks.Add(Queue.QueueAsync(Callback, CancellationToken.None).AsTask());
			}

			SpinWait.SpinOnce();

		} while (EndTime > DateTime.Now);

		//****************************************

		await Queue.Complete(CancellationToken.None);

		await Task.WhenAll(AllTasks).WaitAsync(token);
	}

	//****************************************

	private static void SpinUntilCancel(CancellationToken token)
	{
		var SpinWait = new SpinWait();

		while (!token.IsCancellationRequested)
			SpinWait.SpinOnce();

		token.ThrowIfCancellationRequested();
	}
}
