using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;
//****************************************

namespace Proximity.Threading.Tests;

/// <summary>
/// Tests the functionality of the asynchronous switch lock
/// </summary>
public sealed class AsyncSwitchLockTests
{
	[Test, CancelAfter(1000)]
	public async Task SingleLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		var Resource = false;
		//****************************************

		using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
		{
			Resource = true;
		}

		//****************************************

		Assert.That(Resource, Is.True, "Block not entered");

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task SingleRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		var Resource = false;
		//****************************************

		using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
		{
			Resource = true;
		}

		//****************************************

		Assert.That(Resource, Is.True, "Block not entered");

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task ConcurrentLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.LockLeft(CancellationToken.None))
					{
						Interlocked.Increment(ref Resource);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000), Repeat(10)]
	public async Task ConcurrentRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		var Resource = 0;
		//****************************************

		await Task.WhenAll(
			Enumerable.Range(1, 100).Select(
				async count =>
				{
					await Task.Yield(); // Yield, so it doesn't serialise

					using (await Lock.LockRight(CancellationToken.None))
					{
						Interlocked.Increment(ref Resource);
					}

					return;
				})
		).WaitAsync(token);

		//****************************************

		Assert.That(Resource, Is.EqualTo(100), "Block not entered");

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Right;
		//****************************************

		using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
		{
			Right = Lock.LockRight(CancellationToken.None);

			Assert.IsFalse(Right.IsCompleted, "Right completed");
		}

		//****************************************

		(await Right.WaitAsync(token)).Dispose();

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left;
		//****************************************

		using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
		{
			Left = Lock.LockLeft(CancellationToken.None);

			Assert.IsFalse(Left.IsCompleted, "Left completed");
		}

		//****************************************

		using (await Left.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftRightLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
		{
			Right = Lock.LockRight(CancellationToken.None);

			Left = Lock.LockLeft(CancellationToken.None);

			Assert.IsFalse(Left.IsCompleted, "Left completed");
		}

		Assert.That(Left.IsCompleted, Is.False, "Left not completed");

		using (await Right.WaitAsync(token))
		{
		}

		//****************************************

		using (await Left.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftRightLeftUnfair(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock(true);
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
		{
			Right = Lock.LockRight(CancellationToken.None);

			Left = Lock.LockLeft(CancellationToken.None);

			Assert.IsTrue(Left.IsCompleted, "Left not completed");
		}

		using (await Left.WaitAsync(token))
		{
		}

		using (await Right.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightLeftRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
		{
			Left = Lock.LockLeft(CancellationToken.None);

			Right = Lock.LockRight(CancellationToken.None);

			Assert.IsFalse(Right.IsCompleted, "Right completed");
		}

		Assert.That(Right.IsCompleted, Is.False, "Right completed");

		using (await Left.WaitAsync(token))
		{
		}

		using (await Right.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightLeftRightUnfair(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock(true);
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
		{
			Left = Lock.LockLeft(CancellationToken.None);

			Right = Lock.LockRight(CancellationToken.None);

			Assert.IsTrue(Right.IsCompleted, "Right not completed");
		}

		using (await Right.WaitAsync(token))
		{
		}

		using (await Left.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftRightLeftRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left, Right1, Right2;
		//****************************************

		using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
		{
			Right1 = Lock.LockRight(CancellationToken.None);

			Left = Lock.LockLeft(CancellationToken.None);

			Right2 = Lock.LockRight(CancellationToken.None);
		}

		var Results = await Right1.ThenWaitOn<AsyncSwitchLock.Instance>(Right2);

		Results[0].Dispose();
		Results[1].Dispose();

		using (await Left.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightLeftRightLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Right, Left1, Left2;
		//****************************************

		using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
		{
			Left1 = Lock.LockLeft(CancellationToken.None);

			Right = Lock.LockRight(CancellationToken.None);

			Left2 = Lock.LockLeft(CancellationToken.None);
		}

		var Results = await Left1.ThenWaitOn<AsyncSwitchLock.Instance>(Left2);

		Results[0].Dispose();
		Results[1].Dispose();

		using (await Right.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftCancelRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Right;
		//****************************************

		var Left = await Lock.LockLeft(CancellationToken.None).WaitAsync(token);

		using (var CancelSource = new CancellationTokenSource())
		{
			Right = Lock.LockRight(CancelSource.Token).WaitAsync(token);

			CancelSource.Cancel();

			Left.Dispose();
		}

		try
		{
			using (await Right.WaitAsync(token))
			{
			}

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightCancelLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left;
		//****************************************

		var Right = await Lock.LockRight(CancellationToken.None).WaitAsync(token);

		using (var CancelSource = new CancellationTokenSource())
		{
			Left = Lock.LockLeft(CancelSource.Token);

			CancelSource.Cancel();

			Right.Dispose();
		}

		try
		{
			using (await Left.WaitAsync(token))
			{
			}

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftCancelRightRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Right1, Right2;
		//****************************************

		var Left = await Lock.LockLeft(CancellationToken.None).WaitAsync(token);

		using (var CancelSource = new CancellationTokenSource())
		{
			Right1 = Lock.LockRight(CancelSource.Token);
			Right2 = Lock.LockRight(CancellationToken.None);

			CancelSource.Cancel();

			Left.Dispose();
		}

		try
		{
			using (await Right1.WaitAsync(token))
			{
			}

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		using (await Right2.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightCancelLeftLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left1, Left2;
		//****************************************

		var Right = await Lock.LockRight(CancellationToken.None).WaitAsync(token);

		using (var CancelSource = new CancellationTokenSource())
		{
			Left1 = Lock.LockLeft(CancelSource.Token);
			Left2 = Lock.LockLeft(CancellationToken.None);

			CancelSource.Cancel();

			Right.Dispose();
		}

		try
		{
			using (await Left1.WaitAsync(token))
			{
			}

			Assert.Fail("Did not cancel");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		using (await Left2.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftCancelRightLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (var CancelSource = new CancellationTokenSource())
		{
			using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
			{
				Right = Lock.LockRight(CancelSource.Token);

				Left = Lock.LockLeft(CancellationToken.None);

				CancelSource.Cancel();
			}
		}

		//****************************************

		using (await Left.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftRightCancelLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (var CancelSource = new CancellationTokenSource())
		{
			CancelSource.Cancel();

			using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
			{
				Right = Lock.LockRight(CancellationToken.None);

				Left = Lock.LockLeft(CancelSource.Token);
			}
		}

		//****************************************

		try
		{
			await Left;

			Assert.Fail("Should not reach here");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		using (await Right.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightCancelLeftRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (var CancelSource = new CancellationTokenSource())
		{
			using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
			{
				Left = Lock.LockLeft(CancelSource.Token);

				Right = Lock.LockRight(CancellationToken.None);

				CancelSource.Cancel();
			}
		}

		//****************************************

		using (await Right.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightLeftCancelRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left, Right;
		//****************************************

		using (var CancelSource = new CancellationTokenSource())
		{
			CancelSource.Cancel();

			using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
			{
				Left = Lock.LockLeft(CancellationToken.None);

				Right = Lock.LockRight(CancelSource.Token);
			}
		}

		//****************************************

		try
		{
			await Right;

			Assert.Fail("Should not reach here");
		}
		catch (OperationCanceledException)
		{
			Assert.That(token.IsCancellationRequested, Is.False);
		}

		using (await Left.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftTimeoutRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Right;
		//****************************************

		using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
		{
			Right = Lock.LockRight(TimeSpan.FromMilliseconds(50), CancellationToken.None);

			try
			{
				using (await Right.WaitAsync(token))
				{
				}

				Assert.Fail("Should not reach here");
			}
			catch (TimeoutException)
			{
			}
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightTimeoutLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		ValueTask<AsyncSwitchLock.Instance> Left;
		//****************************************

		using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
		{
			Left = Lock.LockLeft(TimeSpan.FromMilliseconds(50), CancellationToken.None);

			try
			{
				using (await Left.WaitAsync(token))
				{
				}

				Assert.Fail("Should not reach here");
			}
			catch (TimeoutException)
			{
			}

		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task NoTimeoutLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Left = Lock.LockLeft(TimeSpan.FromMilliseconds(50), CancellationToken.None);

		//****************************************

		Assert.That(Left.IsCompleted, Is.True, "Is not completed");

		using (await Left.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task NoTimeoutRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Right = Lock.LockRight(TimeSpan.FromMilliseconds(50), CancellationToken.None);

		//****************************************

		Assert.That(Right.IsCompleted, Is.True, "Is not completed");

		using (await Right.WaitAsync(token))
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task StackBlow(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		var Depth = 0;
		Task ResultTask;
		Task[] Results;
		//****************************************

		using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
		{
			Results = Enumerable.Range(1, 40000).Select(
				async count =>
				{
					using (await (count % 2 == 0 ? Lock.LockLeft(CancellationToken.None) : Lock.LockRight(CancellationToken.None)))
					{
						Depth++;
					}
				}).ToArray();

			ResultTask = Results[^1].ContinueWith(task => System.Diagnostics.Debug.WriteLine("Done to " + new System.Diagnostics.StackTrace(false).FrameCount.ToString()), TaskContinuationOptions.ExecuteSynchronously);
		}

		await ResultTask.WaitAsync(token);
	}

	[Test, CancelAfter(1000)]
	public async Task LeftDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Left = Lock.LockLeft(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Left.WaitAsync(token))
		{
		}

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftDisposeLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Left = Lock.LockLeft(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		try
		{
			using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
			{
			}

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		using (await Left.WaitAsync(token))
		{
		}

		await Dispose.WaitAsync(token);

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftLeftDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Left1 = Lock.LockLeft(CancellationToken.None);

		var Left2 = Lock.LockLeft(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Left1.WaitAsync(token))
		{
		}

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Left2.WaitAsync(token))
		{
		}

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftRightDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Left = Lock.LockLeft(CancellationToken.None);

		var Right = Lock.LockRight(CancellationToken.None);

		Lock.DisposeAsync().Ignore();

		try
		{
			using (await Right.WaitAsync(token))
			{
			}

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		using (await Left.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Right = Lock.LockRight(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Right.WaitAsync(token))
		{
		}

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightDisposeRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Right = Lock.LockRight(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		try
		{
			using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
			{
			}

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed");

		using (await Right.WaitAsync(token))
		{
		}

		await Dispose.WaitAsync(token);

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightRightDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Right1 = Lock.LockRight(CancellationToken.None);

		var Right2 = Lock.LockRight(CancellationToken.None);

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Right1.WaitAsync(token))
		{
		}

		Assert.That(Dispose.IsCompleted, Is.False, "Dispose completed early");

		using (await Right2.WaitAsync(token))
		{
		}

		//****************************************

		await Dispose.WaitAsync(token);

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightLeftDispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Right = Lock.LockRight(CancellationToken.None);

		var Left = Lock.LockLeft(CancellationToken.None);

		Lock.DisposeAsync().Ignore();

		try
		{
			using (await Left.WaitAsync(token))
			{
			}

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		using (await Right.WaitAsync(token))
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task Dispose(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		await Lock.DisposeAsync().WaitAsync(token);

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.True, "Dispose did not complete");

		try
		{
			using (await Lock.LockLeft(CancellationToken.None).WaitAsync(token))
			{
			}

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task DisposeRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Dispose = Lock.DisposeAsync();

		Assert.That(Dispose.IsCompleted, Is.True, "Dispose did not complete");

		try
		{
			using (await Lock.LockRight(CancellationToken.None).WaitAsync(token))
			{
			}

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		//****************************************

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task LeftDisposeContinueWithLeft(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Right = await Lock.LockRight(CancellationToken.None).WaitAsync(token);

		var LeftTask = Lock.LockLeft(CancellationToken.None).AsTask();

		var LeftInnerTask = LeftTask.ContinueWith((task) => Lock.LockLeft(CancellationToken.None).AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();

		var Dispose = Lock.DisposeAsync();

		Right.Dispose();

		await Dispose.WaitAsync(token);

		//****************************************

		try
		{
			await LeftTask.WaitAsync(token);

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		try
		{
			await LeftInnerTask.WaitAsync(token);

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
	}

	[Test, CancelAfter(1000)]
	public async Task RightDisposeContinueWithRight(CancellationToken token)
	{ //****************************************
		var Lock = new AsyncSwitchLock();
		//****************************************

		var Left = await Lock.LockLeft(CancellationToken.None);

		var RightTask = Lock.LockRight(CancellationToken.None).AsTask();

		var RightInnerTask = RightTask.ContinueWith((task) => Lock.LockRight(CancellationToken.None).AsTask(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();

		var Dispose = Lock.DisposeAsync();

		Left.Dispose();

		await Dispose.WaitAsync(token);

		//****************************************

		try
		{
			await RightTask.WaitAsync(token);

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		try
		{
			await RightInnerTask.WaitAsync(token);

			Assert.Fail("Should never reach this point");
		}
		catch (ObjectDisposedException)
		{
		}

		Assert.That(Lock.WaitingRight, Is.Zero, "Rights still waiting");
		Assert.That(Lock.WaitingLeft, Is.Zero, "Lefts still waiting");
		Assert.That(Lock.IsRight, Is.False, "Rights still running");
		Assert.That(Lock.IsLeft, Is.False, "Lefts still running");
	}
}
