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
	public class ValueTaskExtensionTests
	{
		[Test, MaxTime(1000)]
		public async Task When()
		{
			using var CancelSource = new CancellationTokenSource();

			var MyTask = new ValueTask(Task.Delay(100));

			await MyTask.When(CancelSource.Token);
		}
		
		[Test, MaxTime(1000)]
		public async Task WhenCancel()
		{
			using var CancelSource = new CancellationTokenSource(50);

			var MyTask = new ValueTask(Task.Delay(100));

			try
			{
				await MyTask.When(CancelSource.Token);

				Assert.Fail("Should not complete");
			}
			catch (OperationCanceledException e)
			{
				Assert.IsTrue(CancelSource.Token == e.CancellationToken);
			}
		}
		
		[Test, MaxTime(1000)]
		public async Task WhenResult()
		{
			int Result;
			
			using (var CancelSource = new CancellationTokenSource())
			{
				var MyTask = new ValueTask<int>(Task.Delay(100).ContinueWith(task => 100));
				
				Result = await MyTask.When(CancelSource.Token);
			}
			
			Assert.AreEqual(100, Result);
		}
		
		[Test, MaxTime(1000)]
		public async Task WhenResultCancel()
		{
			using var CancelSource = new CancellationTokenSource(50);

			var MyTask = new ValueTask<int>(Task.Delay(100).ContinueWith(task => 100));

			try
			{
				_ = await MyTask.When(CancelSource.Token);

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
			using var CancelSource1 = new CancellationTokenSource(50);
			using var CancelSource2 = new CancellationTokenSource();

			var MyTask = new ValueTask(Task.Delay(1000, CancelSource1.Token));

			try
			{
				await MyTask.When(CancelSource2.Token);

				Assert.Fail("Should not complete");
			}
			catch (OperationCanceledException e)
			{
				Assert.IsTrue(CancelSource1.Token == e.CancellationToken);
			}
		}
		
		[Test, MaxTime(1000)]
		public async Task WhenSourceResultCancel()
		{
			int Result;

			using var CancelSource1 = new CancellationTokenSource(50);
			using var CancelSource2 = new CancellationTokenSource();

			var MyTask = new ValueTask<int>(Task.Delay(1000, CancelSource1.Token).ContinueWith(task => 100, CancelSource1.Token));

			try
			{
				Result = await MyTask.When(CancelSource2.Token);

				Assert.Fail("Should not complete");
			}
			catch (OperationCanceledException e)
			{
				Assert.IsTrue(CancelSource1.Token == e.CancellationToken);
			}
		}
	}
}
