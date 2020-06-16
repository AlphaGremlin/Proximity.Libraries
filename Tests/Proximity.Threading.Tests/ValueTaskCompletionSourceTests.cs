using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Proximity.Threading.Tests
{
	[TestFixture]
	public class ValueTaskCompletionSourceTests
	{
		[Test]
		public async Task Complete()
		{
			using var TaskSource = ValueTaskCompletionSource.New();

			var Task = TaskSource.Task;

			Assert.IsFalse(Task.IsCompleted);

			Assert.IsTrue(TaskSource.TrySetResult());

			await Task;
		}

		[Test]
		public async Task CompleteResult()
		{
			using var TaskSource = ValueTaskCompletionSource.New<int>();

			var Task = TaskSource.Task;

			Assert.IsFalse(Task.IsCompleted);

			Assert.IsTrue(TaskSource.TrySetResult(123));

			var Result = await Task;

			Assert.AreEqual(123, Result);
		}

		[Test]
		public async Task Exception()
		{
			using var TaskSource = ValueTaskCompletionSource.New();

			var Task = TaskSource.Task;

			Assert.IsFalse(Task.IsCompleted);

			Assert.IsTrue(TaskSource.TrySetException(new ApplicationException()));

			try
			{
				await Task;

				Assert.Fail("Should not reach here");
			}
			catch (ApplicationException)
			{
			}
		}
		[Test]
		public async Task ExceptionResult()
		{
			using var TaskSource = ValueTaskCompletionSource.New<int>();

			var Task = TaskSource.Task;

			Assert.IsFalse(Task.IsCompleted);

			Assert.IsTrue(TaskSource.TrySetException(new ApplicationException()));

			try
			{
				var Result = await Task;

				Assert.Fail("Should not reach here");
			}
			catch (ApplicationException)
			{
			}
		}
	}
}
