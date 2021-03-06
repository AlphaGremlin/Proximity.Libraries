using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//****************************************

namespace Proximity.Threading.Tests
{
	/// <summary>
	/// Tests the functionality of the ActionFlag class
	/// </summary>
	[TestFixture()]
	public sealed class ActionFlagTests
	{	//****************************************
		private int _Counter;
		//****************************************
		
		[SetUp()]
		public void Reset()
		{
			_Counter = 0;
		}
		
		//****************************************
		
		[Test(), MaxTime(1500)]
		public async Task SetNoWait()
		{
			var MyFlag = new ActionFlag(() => Task.CompletedTask);
			
			await MyFlag.SetAndWait();
		}
		
		[Test(), MaxTime(1500)]
		public async Task SetAndWait()
		{
			var MyFlag = new ActionFlag(WaitHalfSecond);
			
			var StartTime = DateTime.Now;
			
			await MyFlag.SetAndWait();
			
			Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(400.0, 600.0));
			
			Assert.AreEqual(1, _Counter, "Counter is not as expected");
		}
		
		[Test(), MaxTime(1500)]
		public async Task SetAndWaitDelay()
		{
			var MyFlag = new ActionFlag(WaitHalfSecond, new TimeSpan(0, 0, 0, 0, 100));
			
			var StartTime = DateTime.Now;
			
			var MyTask = MyFlag.SetAndWait();
			
			Thread.Sleep(50);
			
			Assert.AreEqual(0, _Counter, "Raised early");
			
			await MyTask;
			
			Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(500.0, 700.0));
			
			Assert.AreEqual(1, _Counter, "Counter is not as expected");
		}
		
		[Test(), MaxTime(2500)]
		public async Task SetAndWaitTwice()
		{
			var MyFlag = new ActionFlag(WaitHalfSecond);
			
			var StartTime = DateTime.Now;
			
			MyFlag.Set();
			
			Thread.Sleep(200);
			
			await MyFlag.SetAndWait();
			
			Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(900.0, 1100.0));
			
			Assert.AreEqual(2, _Counter, "Counter is not as expected");
		}
		
		[Test(), MaxTime(2500)]
		public async Task SetAndWaitTwiceDelay()
		{
			var MyFlag = new ActionFlag(WaitHalfSecond, new TimeSpan(0, 0, 0, 0, 100));
			
			var StartTime = DateTime.Now;
			
			MyFlag.Set();
			
			Thread.Sleep(200);
			
			await MyFlag.SetAndWait();
			
			Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(1100.0, 1300.0));
			
			Assert.AreEqual(2, _Counter, "Counter is not as expected");
		}
		
		[Test(), MaxTime(1500)]
		public async Task SetRuns()
		{
			var MyFlag = new ActionFlag(WaitFiftyMs);
			var EndTime = DateTime.Now.AddSeconds(1.0);
			
			while (DateTime.Now < EndTime)
			{
				MyFlag.Set();
				
				Interlocked.Exchange(ref _Counter, 0);
				
				Thread.Sleep(0);
			}
			
			await MyFlag.SetAndWait();
			
			Assert.AreNotEqual(0, _Counter);
		}

		//****************************************
		
		private Task WaitFiftyMs()
		{
			Interlocked.Increment(ref _Counter);
			
			return Task.Delay(50);
		}
		
		private Task WaitHalfSecond()
		{
			Interlocked.Increment(ref _Counter);
			
			return Task.Delay(500);
		}
	}
}
