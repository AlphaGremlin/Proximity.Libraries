using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//****************************************

namespace Proximity.Threading.Tests
{
	/// <summary>
	/// Tests the functionality of the asynchronous switch lock
	/// </summary>
	public sealed class AsyncSwitchLockTests
	{
		[Test, MaxTime(1000)]
		public async Task SingleLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			var Resource = false;
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
		
		[Test, MaxTime(1000)]
		public async Task SingleRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			var Resource = false;
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
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task ConcurrentLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			var Resource = 0;
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
		
		[Test, MaxTime(1000), Repeat(10)]
		public async Task ConcurrentRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			var Resource = 0;
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
		
		[Test, MaxTime(1000)]
		public async Task LeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyRight;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight();
				
				Assert.IsFalse(MyRight.IsCompleted, "Right completed");
			}
			
			//****************************************
			
			(await MyRight).Dispose();

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft();
				
				Assert.IsFalse(MyLeft.IsCompleted, "Left completed");
			}

			//****************************************

			(await MyLeft).Dispose();

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftRightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight();
				
				MyLeft = MyLock.LockLeft();
				
				Assert.IsFalse(MyLeft.IsCompleted, "Left completed");
			}
			
			Assert.IsFalse(MyLeft.IsCompleted, "Left not completed");

			(await MyRight).Dispose();

			//****************************************

			(await MyLeft).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, MaxTime(1000)]
		public async Task LeftRightLeftUnfair()
		{	//****************************************
			var MyLock = new AsyncSwitchLock(true);
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
			//****************************************

			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight();

				MyLeft = MyLock.LockLeft();

				Assert.IsTrue(MyLeft.IsCompleted, "Left not completed");
			}

			(await MyLeft).Dispose();

			(await MyRight).Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightLeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft();
				
				MyRight = MyLock.LockRight();
				
				Assert.IsFalse(MyRight.IsCompleted, "Right completed");
			}
			
			Assert.IsFalse(MyRight.IsCompleted, "Right completed");

			(await MyLeft).Dispose();

			//****************************************

			(await MyRight).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, MaxTime(1000)]
		public async Task RightLeftRightUnfair()
		{	//****************************************
			var MyLock = new AsyncSwitchLock(true);
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
			//****************************************

			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft();

				MyRight = MyLock.LockRight();

				Assert.IsTrue(MyRight.IsCompleted, "Right not completed");
			}

			(await MyRight).Dispose();

			(await MyLeft).Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftRightLeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight1, MyRight2;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight1 = MyLock.LockRight();
				
				MyLeft = MyLock.LockLeft();
				
				MyRight2 = MyLock.LockRight();
			}
			
			var Results = await MyRight1.ThenWaitOn<AsyncSwitchLock.Instance>(MyRight2);

			Results[0].Dispose();
			Results[1].Dispose();

			(await MyLeft).Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightLeftRightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyRight, MyLeft1, MyLeft2;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft1 = MyLock.LockLeft();
				
				MyRight = MyLock.LockRight();
				
				MyLeft2 = MyLock.LockLeft();
			}
			
			var Results = await MyLeft1.ThenWaitOn<AsyncSwitchLock.Instance>(MyLeft2);

			Results[0].Dispose();
			Results[1].Dispose();

			(await MyRight).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftCancelRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyRight;
			//****************************************
			
			var MyLeft = await MyLock.LockLeft();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyRight = MyLock.LockRight(MySource.Token);
				
				MySource.Cancel();
				
				MyLeft.Dispose();
			}

			try
			{
				(await MyRight).Dispose();

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightCancelLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft;
			//****************************************
			
			var MyRight = await MyLock.LockRight();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyLeft = MyLock.LockLeft(MySource.Token);
				
				MySource.Cancel();
				
				MyRight.Dispose();
			}

			try
			{
				(await MyLeft).Dispose();

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftCancelRightRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyRight1, MyRight2;
			//****************************************
			
			var MyLeft = await MyLock.LockLeft();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyRight1 = MyLock.LockRight(MySource.Token);
				MyRight2 = MyLock.LockRight();
				
				MySource.Cancel();
				
				MyLeft.Dispose();
			}

			try
			{
				(await MyRight1).Dispose();

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}

			(await MyRight2).Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightCancelLeftLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft1, MyLeft2;
			//****************************************
			
			var MyRight = await MyLock.LockRight();
			
			using (var MySource = new CancellationTokenSource())
			{
				MyLeft1 = MyLock.LockLeft(MySource.Token);
				MyLeft2 = MyLock.LockLeft();
				
				MySource.Cancel();
				
				MyRight.Dispose();
			}

			try
			{
				(await MyLeft1).Dispose();

				Assert.Fail("Did not cancel");
			}
			catch (OperationCanceledException)
			{
			}

			(await MyLeft2).Dispose();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftCancelRightLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
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
			
			(await MyLeft).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftRightCancelLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
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
		
		[Test, MaxTime(1000)]
		public async Task RightCancelLeftRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
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
			
			(await MyRight).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightLeftCancelRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft, MyRight;
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
		
		[Test, MaxTime(1000)]
		public async Task LeftTimeoutRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyRight;
			//****************************************
			
			using (await MyLock.LockLeft())
			{
				MyRight = MyLock.LockRight(TimeSpan.FromMilliseconds(50));

				try
				{
					(await MyRight).Dispose();

					Assert.Fail("Should not reach here");
				}
				catch (TimeoutException)
				{
				}
			}

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightTimeoutLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft;
			//****************************************
			
			using (await MyLock.LockRight())
			{
				MyLeft = MyLock.LockLeft(TimeSpan.FromMilliseconds(50));

				try
				{
					(await MyLeft).Dispose();

					Assert.Fail("Should not reach here");
				}
				catch (TimeoutException)
				{
				}

			}

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task NoTimeoutLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyLeft;
			//****************************************
			
			MyLeft = MyLock.LockLeft(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyLeft.IsCompleted, "Is not completed");
			
			(await MyLeft).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task NoTimeoutRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			ValueTask<AsyncSwitchLock.Instance> MyRight;
			//****************************************
			
			MyRight = MyLock.LockRight(TimeSpan.FromMilliseconds(50));
			
			//****************************************
			
			Assert.IsTrue(MyRight.IsCompleted, "Is not completed");
			
			(await MyRight).Dispose();
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test]
		public async Task StackBlow()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			var Depth = 0;
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
				
				ResultTask = Results[^1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
			}
			
			await ResultTask;
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = MyLock.LockLeft();
			
			var MyDispose = MyLock.DisposeAsync();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			(await MyLeft).Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftDisposeLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = MyLock.LockLeft();
			
			var MyDispose = MyLock.DisposeAsync();
			
			try
			{
				(await MyLock.LockLeft()).Dispose();

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			(await MyLeft).Dispose();

			await MyDispose;

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, MaxTime(1000)]
		public async Task LeftLeftDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************

			var MyLeft1 = MyLock.LockLeft();

			var MyLeft2 = MyLock.LockLeft();

			var MyDispose = MyLock.DisposeAsync();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			(await MyLeft1).Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			(await MyLeft2).Dispose();

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task LeftRightDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = MyLock.LockLeft();
			
			var MyRight = MyLock.LockRight();
			
			_ = MyLock.DisposeAsync();
			
			try
			{
				(await MyRight).Dispose();
				
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
		
		[Test, MaxTime(1000)]
		public async Task RightDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = MyLock.LockRight();
			
			var MyDispose = MyLock.DisposeAsync();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");
			
			(await MyRight).Dispose();
			
			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightDisposeRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = MyLock.LockRight();
			
			var MyDispose = MyLock.DisposeAsync();

			try
			{
				(await MyLock.LockRight()).Dispose();

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed");

			(await MyRight).Dispose();

			await MyDispose;
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, MaxTime(1000)]
		public async Task RightRightDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************

			var MyRight1 = MyLock.LockRight();

			var MyRight2 = MyLock.LockRight();

			var MyDispose = MyLock.DisposeAsync();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			(await MyRight1).Dispose();

			Assert.IsFalse(MyDispose.IsCompleted, "Dispose completed early");

			(await MyRight2).Dispose();

			//****************************************

			await MyDispose;

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightLeftDispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = MyLock.LockRight();
			
			var MyLeft = MyLock.LockLeft();
			
			_ = MyLock.DisposeAsync();
			
			try
			{
				(await MyLeft).Dispose();
				
				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}
			
			(await MyRight).Dispose();
			
			//****************************************
			
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}

		[Test, MaxTime(1000)]
		public async Task Dispose()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************

			await MyLock.DisposeAsync();

			//****************************************

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task DisposeLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyDispose = MyLock.DisposeAsync();

			Assert.IsTrue(MyDispose.IsCompleted, "Dispose did not complete");

			try
			{
				(await MyLock.LockLeft()).Dispose();
				
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
		
		[Test, MaxTime(1000)]
		public async Task DisposeRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************

			var MyDispose = MyLock.DisposeAsync();

			Assert.IsTrue(MyDispose.IsCompleted, "Dispose did not complete");

			try
			{
				(await MyLock.LockRight()).Dispose();

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
		
		[Test, MaxTime(1000)]
		public async Task LeftDisposeContinueWithLeft()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyRight = await MyLock.LockRight();
			
			var MyLeftTask = MyLock.LockLeft().AsTask();
			
			var MyInnerTask = MyLeftTask.ContinueWith((task) => MyLock.LockLeft().AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
			
			var MyDispose = MyLock.DisposeAsync();
			
			MyRight.Dispose();

			await MyDispose;

			//****************************************

			try
			{
				await MyLeftTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}

			try
			{
				await MyInnerTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}

			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
		}
		
		[Test, MaxTime(1000)]
		public async Task RightDisposeContinueWithRight()
		{	//****************************************
			var MyLock = new AsyncSwitchLock();
			//****************************************
			
			var MyLeft = await MyLock.LockLeft();
			
			var MyRightTask = MyLock.LockRight().AsTask();
			
			var MyInnerTask = MyRightTask.ContinueWith((task) => MyLock.LockRight().AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
			
			var MyDispose = MyLock.DisposeAsync();
			
			MyLeft.Dispose();

			await MyDispose;

			//****************************************

			try
			{
				await MyRightTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}

			try
			{
				await MyInnerTask;

				Assert.Fail("Should never reach this point");
			}
			catch (ObjectDisposedException)
			{
			}

			Assert.AreEqual(0, MyLock.WaitingRight, "Rights still waiting");
			Assert.AreEqual(0, MyLock.WaitingLeft, "Lefts still waiting");
			Assert.IsFalse(MyLock.IsRight, "Rights still running");
			Assert.IsFalse(MyLock.IsLeft, "Lefts still running");
		}
	}
}
