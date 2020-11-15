using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Proximity.Threading.Tests
{
	[TestFixture]
	public class TaskInterleaveTests
	{
		[Test]
		public async Task CancelEnumerable()
		{
			var FirstSource = new TaskCompletionSource<int>();
			var CancelSource = new CancellationTokenSource();

			await using var Enumerator = new[] { FirstSource.Task }.Interleave(CancelSource.Token).GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);

			CancelSource.Cancel();

			try
			{
				await Waiter;

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test]
		public async Task CancelEnumerableWith()
		{
			var FirstSource = new TaskCompletionSource<int>();
			var CancelSource = new CancellationTokenSource();

			await using var Enumerator = new[] { FirstSource.Task }.Interleave().WithCancellation(CancelSource.Token).GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();

			CancelSource.Cancel();

			try
			{
				await Waiter;

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test]
		public async Task CancelEnumerator()
		{
			var FirstSource = new TaskCompletionSource<int>();
			var CancelSource = new CancellationTokenSource();

			await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator(CancelSource.Token);

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);

			CancelSource.Cancel();

			try
			{
				await Waiter;

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test]
		public async Task Empty()
		{
			await using var Enumerator = Array.Empty<Task<int>>().Interleave().GetAsyncEnumerator();

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task Exception()
		{
			var FirstTask = Task.FromException<int>(new ApplicationException("Test"));

			await using var Enumerator = new[] { FirstTask }.Interleave().GetAsyncEnumerator();

			Assert.IsTrue(await Enumerator.MoveNextAsync());

			try
			{
				await Enumerator.Current;

				Assert.Fail("Did not throw");
			}
			catch (ApplicationException)
			{
			}

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task ExceptionAsync()
		{
			var FirstSource = new TaskCompletionSource<int>();

			await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			FirstSource.SetException(new ApplicationException("Test"));

			Assert.IsTrue(await Waiter);

			try
			{
				await Enumerator.Current;

				Assert.Fail("Did not throw");
			}
			catch (ApplicationException)
			{
			}

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task Multiple()
		{
			var FirstTask = Task.FromResult(42);
			var SecondTask = Task.FromResult(108);

			await using var Enumerator = new[] { FirstTask, SecondTask }.Interleave().GetAsyncEnumerator();

			Assert.IsTrue(await Enumerator.MoveNextAsync());
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(FirstTask.Result, Enumerator.Current.Result);

			Assert.IsTrue(await Enumerator.MoveNextAsync());
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(SecondTask.Result, Enumerator.Current.Result);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task MultipleAsync()
		{
			var FirstSource = new TaskCompletionSource<int>();
			var SecondSource = new TaskCompletionSource<int>();

			await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			FirstSource.SetResult(42);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(FirstSource.Task.Result, Enumerator.Current.Result);

			Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			SecondSource.SetResult(108);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(SecondSource.Task.Result, Enumerator.Current.Result);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task MultipleAsyncOutOfOrder()
		{
			var FirstSource = new TaskCompletionSource<int>();
			var SecondSource = new TaskCompletionSource<int>();

			await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			SecondSource.SetResult(108);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(SecondSource.Task.Result, Enumerator.Current.Result);

			Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			FirstSource.SetResult(42);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(FirstSource.Task.Result, Enumerator.Current.Result);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task MultipleAsyncIndexOutOfOrder()
		{
			var FirstSource = new TaskCompletionSource<int>();
			var SecondSource = new TaskCompletionSource<int>();

			await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.InterleaveIndex().GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			SecondSource.SetResult(108);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.result.Status);
			Assert.AreEqual(SecondSource.Task.Result, Enumerator.Current.result.Result);
			Assert.AreEqual(1, Enumerator.Current.index);

			Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			FirstSource.SetResult(42);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.result.Status);
			Assert.AreEqual(FirstSource.Task.Result, Enumerator.Current.result.Result);
			Assert.AreEqual(0, Enumerator.Current.index);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task MultipleSimultaneousAsync()
		{
			var FirstSource = new TaskCompletionSource<int>();
			var SecondSource = new TaskCompletionSource<int>();

			await using var Enumerator = new[] { FirstSource.Task, SecondSource.Task }.Interleave().GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			FirstSource.SetResult(42);
			SecondSource.SetResult(108);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(FirstSource.Task.Result, Enumerator.Current.Result);

			Assert.IsTrue(await Enumerator.MoveNextAsync());
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(SecondSource.Task.Result, Enumerator.Current.Result);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task Single()
		{
			var FirstTask = Task.FromResult(42);

			await using var Enumerator = new[] { FirstTask }.Interleave().GetAsyncEnumerator();

			Assert.IsTrue(await Enumerator.MoveNextAsync());
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(FirstTask.Result, Enumerator.Current.Result);
			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task SingleAsync()
		{
			var FirstSource = new TaskCompletionSource<int>();

			await using var Enumerator = new[] { FirstSource.Task }.Interleave().GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			FirstSource.SetResult(42);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.Status);
			Assert.AreEqual(FirstSource.Task.Result, Enumerator.Current.Result);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task SingleAsyncIndex()
		{
			var FirstSource = new TaskCompletionSource<int>();

			await using var Enumerator = new[] { FirstSource.Task }.InterleaveIndex().GetAsyncEnumerator();

			var Waiter = Enumerator.MoveNextAsync();
			Assert.IsFalse(Waiter.IsCompleted);
			FirstSource.SetResult(42);

			Assert.IsTrue(await Waiter);
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.result.Status);
			Assert.AreEqual(FirstSource.Task.Result, Enumerator.Current.result.Result);
			Assert.AreEqual(0, Enumerator.Current.index);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}

		[Test]
		public async Task SingleIndex()
		{
			var FirstTask = Task.FromResult(42);

			await using var Enumerator = new[] { FirstTask }.InterleaveIndex().GetAsyncEnumerator();

			Assert.IsTrue(await Enumerator.MoveNextAsync());
			Assert.AreEqual(TaskStatus.RanToCompletion, Enumerator.Current.result.Status);
			Assert.AreEqual(FirstTask.Result, Enumerator.Current.result.Result);
			Assert.AreEqual(0, Enumerator.Current.index);

			Assert.IsFalse(await Enumerator.MoveNextAsync());
		}
	}
}
