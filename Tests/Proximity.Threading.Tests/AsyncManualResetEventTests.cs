using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Proximity.Threading.Tests
{
	[TestFixture]
	public class AsyncManualResetEventTests
	{
		[Test]
		public void DisposeSet()
		{
			var Event = new AsyncManualResetEvent();

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
			var Event = new AsyncManualResetEvent();

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
			var Event = new AsyncManualResetEvent(true);

			Assert.IsTrue(Event.IsSet);
		}

		[Test]
		public void Set()
		{
			var Event = new AsyncManualResetEvent();

			Assert.IsFalse(Event.IsSet);

			Event.Set();

			Assert.IsTrue(Event.IsSet);
		}

		[Test]
		public void SetReset()
		{
			var Event = new AsyncManualResetEvent();

			Event.Set();

			Assert.IsTrue(Event.IsSet);

			Event.Reset();

			Assert.IsFalse(Event.IsSet);
		}

		[Test]
		public void SetResetWait()
		{
			var Event = new AsyncManualResetEvent();

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
			var Event = new AsyncManualResetEvent();

			Event.Set();

			Assert.IsTrue(Event.TryWait());
		}

		[Test]
		public void SetWait()
		{
			var Event = new AsyncManualResetEvent();

			Event.Set();

			var Task = Event.Wait();

			Assert.IsTrue(Task.IsCompleted);
		}

		[Test]
		public void SetWaitWait()
		{
			var Event = new AsyncManualResetEvent();

			Event.Set();

			var Task = Event.Wait();

			Assert.IsTrue(Task.IsCompleted);

			Task = Event.Wait();

			Assert.IsTrue(Task.IsCompleted);
		}

		[Test]
		public void TryWait()
		{
			var Event = new AsyncManualResetEvent();

			Assert.IsFalse(Event.TryWait());
		}

		[Test]
		public void Wait()
		{
			var Event = new AsyncManualResetEvent();

			var Task = Event.Wait();

			Assert.IsFalse(Task.IsCompleted);
		}

		[Test, Timeout(1000)]
		public async Task WaitCancel()
		{
			var Event = new AsyncManualResetEvent();

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
			var Event = new AsyncManualResetEvent();

			var Task = Event.Wait();

			Event.Set();

			await Task;
		}

		[Test, Timeout(1000)]
		public async Task WaitSetWait()
		{
			var Event = new AsyncManualResetEvent();

			var Task = Event.Wait();

			Event.Set();

			await Task;

			Task = Event.Wait();

			Assert.IsTrue(Task.IsCompleted);
		}

		[Test, Timeout(1000)]
		public async Task WaitWaitSet()
		{
			var Event = new AsyncManualResetEvent();

			var Task1 = Event.Wait();
			var Task2 = Event.Wait();

			Event.Set();

			await Task1;
			await Task2;
		}

		[Test]
		public async Task RepeatedWaitCancel()
		{
			var Event = new AsyncManualResetEvent();

			var CancelSource = new CancellationTokenSource();

			var InitialCapacity = Event.Capacity;

			for (var Count = 0; Count < 10000; Count++)
			{
				var Task = Event.Wait(CancelSource.Token);

				CancelSource.Cancel();

				try
				{
					await Task;
				}
				catch (OperationCanceledException)
				{
				}

				CancelSource = new CancellationTokenSource();
			}

			Assert.AreEqual(InitialCapacity, Event.Capacity);
		}

		[Test]
		public async Task RepeatedWaitCancelWithWait()
		{
			var Event = new AsyncManualResetEvent();

			var CancelSource = new CancellationTokenSource();

			var InitialCapacity = Event.Capacity;

			_ = Event.Wait();

			for (var Count = 0; Count < 10000; Count++)
			{
				var Task2 = Event.Wait(CancelSource.Token);

				CancelSource.Cancel();

				try
				{
					await Task2;
				}
				catch (OperationCanceledException)
				{
				}

				CancelSource = new CancellationTokenSource();
			}

			Assert.AreEqual(InitialCapacity * 2, Event.Capacity);
		}
	}
}
