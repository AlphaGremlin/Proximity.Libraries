using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//****************************************

namespace Proximity.Threading.Tests
{
	/// <summary>
	/// Tests the functionality of the task extensions
	/// </summary>
	[TestFixture]
	public class ValueTaskWhenTests
	{
		[Test, MaxTime(1000)]
		public async Task When()
		{
			using var CancelSource = new CancellationTokenSource();
			var TaskSource = new TaskCompletionSource<VoidStruct>();

			var WhenTask = new ValueTask(TaskSource.Task).When(CancelSource.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			TaskSource.SetResult(default);

			await WhenTask;
		}
		
		[Test, MaxTime(1000)]
		public async Task WhenCancel()
		{
			using var CancelSource = new CancellationTokenSource();
			var TaskSource = new TaskCompletionSource<VoidStruct>();

			var WhenTask = new ValueTask(TaskSource.Task).When(CancelSource.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			try
			{
				CancelSource.Cancel();

				await WhenTask;

				Assert.Fail("Should not complete");
			}
			catch (OperationCanceledException e)
			{
				Assert.IsTrue(CancelSource.Token == e.CancellationToken);
			}
		}

		[Test, MaxTime(1000)]
		public async Task WhenSourceCancel()
		{
			using var CancelSource1 = new CancellationTokenSource();
			using var CancelSource2 = new CancellationTokenSource();

			var TaskSource = new TaskCompletionSource<VoidStruct>();

			var WhenTask = new ValueTask(TaskSource.Task).When(CancelSource2.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			try
			{
				CancelSource1.Cancel();

				TaskSource.TrySetCanceled(CancelSource1.Token);

				await WhenTask;

				Assert.Fail("Should not complete");
			}
			catch (OperationCanceledException e)
			{
				Assert.IsTrue(CancelSource1.Token == e.CancellationToken);
			}
		}

		[Test, MaxTime(1000)]
		public async Task WhenThrow()
		{
			using var CancelSource = new CancellationTokenSource();
			var TaskSource = new TaskCompletionSource<VoidStruct>();

			var WhenTask = new ValueTask(TaskSource.Task).When(CancelSource.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			TaskSource.SetException(new SuccessException("Exception was passed through successfully"));

			await WhenTask;

			Assert.Fail("Should not reach this point");
		}

		[Test, MaxTime(1000)]
		public async Task WhenTimeout()
		{
			var TaskSource = new TaskCompletionSource<VoidStruct>();

			var WhenTask = new ValueTask(TaskSource.Task).When(100);

			try
			{
				await WhenTask;

				Assert.Fail("Should not complete");
			}
			catch (TimeoutException)
			{
			}
		}

		//****************************************

		[Test, MaxTime(1000)]
		public async Task WhenResult()
		{
			using var CancelSource = new CancellationTokenSource();
			var TaskSource = new TaskCompletionSource<int>();

			var WhenTask = new ValueTask<int>(TaskSource.Task).When(CancelSource.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			TaskSource.SetResult(100);

			var Result = await WhenTask;

			Assert.AreEqual(100, Result);
		}
		
		[Test, MaxTime(1000)]
		public async Task WhenResultCancel()
		{
			using var CancelSource = new CancellationTokenSource();
			var TaskSource = new TaskCompletionSource<int>();

			var WhenTask = new ValueTask<int>(TaskSource.Task).When(CancelSource.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			try
			{
				CancelSource.Cancel();

				_ = await WhenTask;

				Assert.Fail("Should not complete");
			}
			catch (OperationCanceledException e)
			{
				Assert.IsTrue(CancelSource.Token == e.CancellationToken);
			}
		}
		
		[Test, MaxTime(1000)]
		public async Task WhenResultSourceCancel()
		{
			using var CancelSource1 = new CancellationTokenSource();
			using var CancelSource2 = new CancellationTokenSource();

			var TaskSource = new TaskCompletionSource<int>();

			var WhenTask = new ValueTask<int>(TaskSource.Task).When(CancelSource2.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			try
			{
				CancelSource1.Cancel();

				TaskSource.TrySetCanceled(CancelSource1.Token);

				_ = await WhenTask;

				Assert.Fail("Should not complete");
			}
			catch (OperationCanceledException e)
			{
				Assert.IsTrue(CancelSource1.Token == e.CancellationToken);
			}
		}

		[Test, MaxTime(1000)]
		public async Task WhenResultThrow()
		{
			using var CancelSource = new CancellationTokenSource();
			var TaskSource = new TaskCompletionSource<int>();

			var WhenTask = new ValueTask<int>(TaskSource.Task).When(CancelSource.Token);

			Assert.IsFalse(WhenTask.IsCompleted);

			TaskSource.SetException(new SuccessException("Exception was passed through successfully"));

			_ = await WhenTask;

			Assert.Fail("Should not reach this point");
		}

		[Test, MaxTime(1000)]
		public async Task WhenResultTimeout()
		{
			var TaskSource = new TaskCompletionSource<int>();

			var WhenTask = new ValueTask<int>(TaskSource.Task).When(100);

			try
			{
				_ = await WhenTask;

				Assert.Fail("Should not complete");
			}
			catch (TimeoutException)
			{
			}
		}
	}
}
