using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Proximity.Threading.Tests
{
	[TestFixture]
	public class AsyncAutoResetEventTests
	{
		[Test]
		public void DisposeSet()
		{
			var Event = new AsyncAutoResetEvent();

			Event.Dispose();

			try
			{
				Event.Set();

				Assert.Fail("Did not throw");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test]
		public void DisposeWait()
		{
			var Event = new AsyncAutoResetEvent();

			Event.Dispose();

			try
			{
				_ = Event.Wait();

				Assert.Fail("Did not throw");
			}
			catch (ObjectDisposedException)
			{
			}
		}

		[Test]
		public void Preset()
		{
			var Event = new AsyncAutoResetEvent(true);

			Assert.IsTrue(Event.IsSet);
		}

		[Test]
		public void Set()
		{
			var Event = new AsyncAutoResetEvent();

			Assert.IsFalse(Event.IsSet);

			Event.Set();

			Assert.IsTrue(Event.IsSet);
		}

		[Test]
		public void SetReset()
		{
			var Event = new AsyncAutoResetEvent();

			Event.Set();

			Assert.IsTrue(Event.IsSet);

			Event.Reset();

			Assert.IsFalse(Event.IsSet);
		}

		[Test]
		public void SetResetWait()
		{
			var Event = new AsyncAutoResetEvent();

			Event.Set();

			Assert.IsTrue(Event.IsSet);

			Event.Reset();

			Assert.IsFalse(Event.IsSet);

			var Task = Event.Wait();

			Assert.IsFalse(Task.IsCompleted);
		}

		[Test]
		public void SetTryWait()
		{
			var Event = new AsyncAutoResetEvent();

			Event.Set();

			Assert.IsTrue(Event.TryWait());
		}

		[Test]
		public void SetWait()
		{
			var Event = new AsyncAutoResetEvent();

			Event.Set();

			var Task = Event.Wait();

			Assert.IsTrue(Task.IsCompleted);
		}

		[Test]
		public void SetWaitWait()
		{
			var Event = new AsyncAutoResetEvent();

			Event.Set();

			var Task = Event.Wait();

			Assert.IsTrue(Task.IsCompleted);

			Task = Event.Wait();

			Assert.IsFalse(Task.IsCompleted);
		}

		[Test]
		public void TryWait()
		{
			var Event = new AsyncAutoResetEvent();

			Assert.IsFalse(Event.TryWait());
		}

		[Test]
		public void Wait()
		{
			var Event = new AsyncAutoResetEvent();

			var Task = Event.Wait();

			Assert.IsFalse(Task.IsCompleted);
		}

		[Test, Timeout(1000)]
		public async Task WaitCancel()
		{
			var Event = new AsyncAutoResetEvent();

			using var TokenSource = new CancellationTokenSource();

			var Task = Event.Wait(TokenSource.Token);

			TokenSource.Cancel();

			try
			{
				await Task;

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}
		}

		[Test, Timeout(1000)]
		public async Task WaitSet()
		{
			var Event = new AsyncAutoResetEvent();

			var Task = Event.Wait();

			Event.Set();

			await Task;
		}

		[Test, Timeout(1000)]
		public async Task WaitSetWait()
		{
			var Event = new AsyncAutoResetEvent();

			var Task = Event.Wait();

			Event.Set();

			await Task;

			Task = Event.Wait();

			Assert.IsFalse(Task.IsCompleted);
		}

		[Test, Timeout(1000)]
		public async Task WaitWaitSet()
		{
			var Event = new AsyncAutoResetEvent();

			var Task1 = Event.Wait();
			var Task2 = Event.Wait();

			Event.Set();

			await Task1;
			await Task2;
		}
	}
}
