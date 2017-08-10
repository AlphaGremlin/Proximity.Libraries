/****************************************\
 TaskExtensionTests.cs
 Created: 2014-12-03
\****************************************/
using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests the functionality of the task extensions
	/// </summary>
	[TestFixture]
	public class TaskExtensionTests
	{
		[Test, Timeout(1000)]
		public async Task When()
		{
			using (var CancelSource = new CancellationTokenSource())
			{
				var MyTask = Task.Delay(100);
				
				await MyTask.When(CancelSource.Token);
			}
		}
		
		[Test, Timeout(1000)]
		public void WhenCancel()
		{
			using (var CancelSource = new CancellationTokenSource(50))
			{
				var MyTask = Task.Delay(100);

				Assert.ThrowsAsync<TaskCanceledException>(() => MyTask.When(CancelSource.Token));
			}
		}
		
		[Test, Timeout(1000)]
		public async Task WhenResult()
		{
			int Result;
			
			using (var CancelSource = new CancellationTokenSource())
			{
				var MyTask = Task.Delay(100).ContinueWith(task => 100);
				
				Result = await MyTask.When(CancelSource.Token);
			}
			
			Assert.AreEqual(100, Result);
		}
		
		[Test, Timeout(1000)]
		public void WhenResultCancel()
		{
			using (var CancelSource = new CancellationTokenSource(50))
			{
				var MyTask = Task.Delay(100).ContinueWith(task => 100);

				Assert.ThrowsAsync<TaskCanceledException>(() => MyTask.When(CancelSource.Token));
			}
		}
		
		[Test, Timeout(1000)]
		public async Task WhenSourceCancel()
		{
			using (var CancelSource1 = new CancellationTokenSource(50))
			using (var CancelSource2 = new CancellationTokenSource())
			{
				var MyTask = Task.Delay(100, CancelSource1.Token);
				
				try
				{
					await MyTask.When(CancelSource2.Token);
					
					Assert.Fail("Should not complete");
				}
				catch (TaskCanceledException e)
				{
					Assert.IsTrue(CancellationToken.None == e.CancellationToken);
					
					// Does not work, since TaskCompletionSource doesn't take a Cancellation Token
					// Assert.IsTrue(CancelSource1.Token == e.CancellationToken);
				}
			}
		}
		
		[Test, Timeout(1000)]
		public async Task WhenSourceResultCancel()
		{
			int Result;
			
			using (var CancelSource1 = new CancellationTokenSource(50))
			using (var CancelSource2 = new CancellationTokenSource())
			{
				var MyTask = Task.Delay(100, CancelSource1.Token).ContinueWith(task => 100, CancelSource1.Token);
				
				try
				{
					Result = await MyTask.When(CancelSource2.Token);
					
					Assert.Fail("Should not complete");
				}
				catch (TaskCanceledException e)
				{
					Assert.IsTrue(CancellationToken.None == e.CancellationToken);
					
					// Does not work, since TaskCompletionSource doesn't take a Cancellation Token
					// Assert.IsTrue(CancelSource1.Token == e.CancellationToken);
				}
			}
		}
	}
}
