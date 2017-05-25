/****************************************\
 AsyncSwitchLockTests.cs
 Created: 2014-02-26
\****************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests the functionality of the asynchronous switch lock
	/// </summary>
	public sealed class AsyncSwitchLockTests
	{
		[Test, Timeout(1000)]
		public async Task SingleLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			bool Resource = false;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task SingleRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			bool Resource = false;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				Resource = true;
			}
			
			//****************************************
			
			Assert.IsTrue(Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ConcurrentLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			int Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (await MyLock.LockLeft())
						{
							Interlocked.Increment(ref Resource);
						}
						
						return;
					})
			);
			
			//****************************************
			
			Assert.AreEqual(100, Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000), Repeat(10)]
		public async Task ConcurrentRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			int Resource = 0;
			//****************************************
			
			await Task.WhenAll(
				Enumerable.Range(1, 100).Select(
					async count =>
					{
						await Task.Yield(); // Yield, so it doesn't serialise
						
						using (await MyLock.LockRight())
						{
							Interlocked.Increment(ref Resource);
						}
						
						return;
					})
			);
			
			//****************************************
			
			Assert.AreEqual(100, Resource, "Block not entered");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyRight;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight();
				
				Assert.IsFalse(MyRight.IsCompleted, "Right completed");
			}
			
			//****************************************
			
			await MyRight;
			
			MyRight.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft();
				
				Assert.IsFalse(MyLeft.IsCompleted, "Left completed");
			}
			
			//****************************************
			
			await MyLeft;
			
			MyLeft.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftRightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft, MyRight;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight();
				
				MyLeft = MyLock.LockLeft();
				
				Assert.IsFalse(MyLeft.IsCompleted, "Left completed");
			}
			
			Assert.IsFalse(MyLeft.IsCompleted, "Left not completed");
			
			MyRight.Result.Dispose();
			
			//****************************************
			
			(await MyLeft).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, Timeout(1000)]
		public async Task LeftRightLeftUnfair()
		{	//****************************************
			var MyLock = new AsyncSwitchLock(true);
			Task<IDisposable> MyLeft, MyRight;
			//****************************************

			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight();

				MyLeft = MyLock.LockLeft();

				Assert.IsTrue(MyLeft.IsCompleted, "Left not completed");
			}

			MyLeft.Result.Dispose();

			MyRight.Result.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightLeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft, MyRight;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft();
				
				MyRight = MyLock.LockRight();
				
				Assert.IsFalse(MyRight.IsCompleted, "Right completed");
			}
			
			Assert.IsFalse(MyRight.IsCompleted, "Right completed");
			
			MyLeft.Result.Dispose();
			
			//****************************************
			
			(await MyRight).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, Timeout(1000)]
		public async Task RightLeftRightUnfair()
		{	//****************************************
			var MyLock = new AsyncSwitchLock(true);
			Task<IDisposable> MyLeft, MyRight;
			//****************************************

			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft();

				MyRight = MyLock.LockRight();

				Assert.IsTrue(MyRight.IsCompleted, "Right not completed");
			}

			MyRight.Result.Dispose();

			MyLeft.Result.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftRightLeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft, MyRight1, MyRight2;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight1 = MyLock.LockRight();
				
				MyLeft = MyLock.LockLeft();
				
				MyRight2 = MyLock.LockRight();
			}
			
			await Task.WhenAll(MyRight1, MyRight2);
			
			MyRight1.Result.Dispose();
			MyRight2.Result.Dispose();
			MyLeft.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightLeftRightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyRight, MyLeft1, MyLeft2;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft1 = MyLock.LockLeft();
				
				MyRight = MyLock.LockRight();
				
				MyLeft2 = MyLock.LockLeft();
			}
			
			await Task.WhenAll(MyLeft1, MyLeft2);
			
			MyLeft1.Result.Dispose();
			MyLeft2.Result.Dispose();
			MyRight.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftCancelRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyRight;
			//****************************************
			
			var MyLeft = await MyLock.LockLeft();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyRight = MyLock.LockRight(MySource.Token);
				
				MySource.Cancel();
				
				MyLeft.Dispose();
			}
			
			//****************************************
			
			Assert.IsTrue(MyRight.IsCanceled, "Did not cancel");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightCancelLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft;
			//****************************************
			
			var MyRight = await MyLock.LockRight();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyLeft = MyLock.LockLeft(MySource.Token);
				
				MySource.Cancel();
				
				MyRight.Dispose();
			}
			
			//****************************************
			
			Assert.IsTrue(MyLeft.IsCanceled, "Did not cancel");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftCancelRightRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyRight1, MyRight2;
			//****************************************
			
			var MyLeft = await MyLock.LockLeft();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyRight1 = MyLock.LockRight(MySource.Token);
				MyRight2 = MyLock.LockRight();
				
				MySource.Cancel();
				
				MyLeft.Dispose();
			}
			
			//****************************************
			
			Assert.IsTrue(MyRight1.IsCanceled, "Did not cancel");
			Assert.IsFalse(MyRight2.IsCanceled, "Cancelled");
			
			MyRight2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightCancelLeftLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft1, MyLeft2;
			//****************************************
			
			var MyRight = await MyLock.LockRight();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyLeft1 = MyLock.LockLeft(MySource.Token);
				MyLeft2 = MyLock.LockLeft();
				
				MySource.Cancel();
				
				MyRight.Dispose();
			}
			
			//****************************************
			
			Assert.IsTrue(MyLeft1.IsCanceled, "Did not cancel");
			Assert.IsFalse(MyLeft2.IsCanceled, "Cancelled");
			
			MyLeft2.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftCancelRightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft, MyRight;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				using (await MyLock.LockLeft())
				{
					MyRight = MyLock.LockRight(MySource.Token);
					
					MyLeft = MyLock.LockLeft();
					
					MySource.Cancel();
				}
			}
			
			//****************************************
			
			await MyLeft;
			
			MyLeft.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftRightCancelLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft, MyRight;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				MySource.Cancel();
				
				using (await MyLock.LockLeft())
				{
					MyRight = MyLock.LockRight();
					
					MyLeft = MyLock.LockLeft(MySource.Token);
				}
			}
			
			//****************************************
			
			try
			{
				await MyLeft;
				
				Assert.Fail("Should not reach here");
			}
			catch (OperationCanceledException)
			{
			}
			
			(await MyRight).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightCancelLeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft, MyRight;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				using (await MyLock.LockRight())
				{
					MyLeft = MyLock.LockLeft(MySource.Token);
					
					MyRight = MyLock.LockRight();
					
					MySource.Cancel();
				}
			}
			
			//****************************************
			
			await MyRight;
			
			MyRight.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightLeftCancelRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft, MyRight;
			//****************************************
			
			using (var MySource = new CancellationTokenSource())
			{
				MySource.Cancel();
				
				using (await MyLock.LockRight())
				{
					MyLeft = MyLock.LockLeft();
					
					MyRight = MyLock.LockRight(MySource.Token);
				}
			}
			
			//****************************************
			
			try
			{
				await MyRight;
				
				Assert.Fail("Should not reach here");
			}
			catch (OperationCanceledException)
			{
			}
			
			(await MyLeft).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task LeftTimeoutRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyRight;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************
			
			Assert.IsTrue(MyRight.IsCanceled, "Did not cancel");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightTimeoutLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft(TimeSpan.FromMilliseconds(50));
				
				Thread.Sleep(100);
			}
			
			//****************************************
			
			Assert.IsTrue(MyLeft.IsCanceled, "Did not cancel");
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void NoTimeoutLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyLeft;
			//****************************************
			
			MyLeft = MyLock.LockLeft(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyLeft.IsCompleted, "Is not completed");
			
			MyLeft.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void NoTimeoutRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			Task<IDisposable> MyRight;
			//****************************************
			
			MyRight = MyLock.LockRight(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyRight.IsCompleted, "Is not completed");
			
			MyRight.Result.Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test]
		public async Task StackBlow()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			int Depth = 0;
			Task ResultTask;
			Task[] Results;
			//****************************************

			using (await MyLock.LockLeft())
			{
				Results = Enumerable.Range(1, 40000).Select(
					async count => 
					{
						using (await (count % 2 == 0 ? MyLock.LockLeft() : MyLock.LockRight()))
						{
							Depth++;
						}
					}).ToArray();
				
				ResultTask = Results[Results.Length -1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
			}
			
			await ResultTask;
		}
		
		[Test, Timeout(1000)]
		public async Task LeftDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = MyLock.LockLeft();
			
			var MyDispose = MyLock.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyLeft.Result.Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void LeftDisposeLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = MyLock.LockLeft();
			
			MyLock.Dispose();
			
			try
			{
				MyLock.LockLeft().Result.Dispose();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			MyLeft.Result.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, Timeout(1000)]
		public async Task LeftLeftDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************

			var MyLeft1 = MyLock.LockLeft();

			var MyLeft2 = MyLock.LockLeft();

			var MyDispose = MyLock.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			MyLeft1.Result.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			MyLeft2.Result.Dispose();

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void LeftRightDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = MyLock.LockLeft();
			
			var MyRight = MyLock.LockRight();
			
			MyLock.Dispose();
			
			try
			{
				MyRight.Result.Dispose();
				
				Assert.Fail("Should never reach this point");
			}
			catch (AggregateException e)
			{
				Assert.IsInstanceOf(typeof(ObjectDisposedException), e.InnerException, "Inner exception not as expected");
			}
			
			MyLeft.Result.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public async Task RightDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = MyLock.LockRight();
			
			var MyDispose = MyLock.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			MyRight.Result.Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void RightDisposeRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = MyLock.LockRight();
			
			MyLock.Dispose();
			
			try
			{
				MyLock.LockRight().Result.Dispose();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			MyRight.Result.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, Timeout(1000)]
		public async Task RightRightDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************

			var MyRight1 = MyLock.LockRight();

			var MyRight2 = MyLock.LockRight();

			var MyDispose = MyLock.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			MyRight1.Result.Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			MyRight2.Result.Dispose();

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void RightLeftDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = MyLock.LockRight();
			
			var MyLeft = MyLock.LockLeft();
			
			MyLock.Dispose();
			
			try
			{
				MyLeft.Result.Dispose();
				
				Assert.Fail("Should never reach this point");
			}
			catch (AggregateException e)
			{
				Assert.IsInstanceOf(typeof(ObjectDisposedException), e.InnerException, "Inner exception not as expected");
			}
			
			MyRight.Result.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, Timeout(1000)]
		public async Task Dispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************

			await MyLock.Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void DisposeLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			MyLock.Dispose();
			
			try
			{
				MyLock.LockLeft().Result.Dispose();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void DisposeRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			MyLock.Dispose();
			
			try
			{
				MyLock.LockRight().Result.Dispose();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, Timeout(1000)]
		public void LeftDisposeContinueWithLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = MyLock.LockRight().Result;
			
			var MyLeftTask = MyLock.LockLeft();
			
			var MyInnerTask = MyLeftTask.ContinueWith((task) => MyLock.LockLeft(), TaskContinuationOptions.ExecuteSynchronously);
			
			MyLock.Dispose();
			
			MyRight.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
			
			Assert.IsTrue(MyLeftTask.IsFaulted, "Left did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyLeftTask.Exception.InnerException, "Left is not disposed");
			Assert.IsTrue(MyInnerTask.IsFaulted, "Inner Left did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyInnerTask.Exception.InnerException, "Inner Left is not disposed");
		}
		
		[Test, Timeout(1000)]
		public void RightDisposeContinueWithRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = MyLock.LockLeft().Result;
			
			var MyRightTask = MyLock.LockRight();
			
			var MyInnerTask = MyRightTask.ContinueWith((task) => MyLock.LockRight(), TaskContinuationOptions.ExecuteSynchronously);
			
			MyLock.Dispose();
			
			MyLeft.Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			
			Assert.IsTrue(MyRightTask.IsFaulted, "Right did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyRightTask.Exception.InnerException, "Right is not disposed");
			Assert.IsTrue(MyInnerTask.IsFaulted, "Inner Right did not fault");
			Assert.IsInstanceOf(typeof(ObjectDisposedException), MyInnerTask.Exception.InnerException, "Inner Right is not disposed");
		}
	}
}
