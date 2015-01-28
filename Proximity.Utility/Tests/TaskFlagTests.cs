﻿/****************************************\
 TaskFlagTests.cs
 Created: 2014-01-24
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
	/// Tests the functionality of the TaskFlag class
	/// </summary>
	[TestFixture()]
	public sealed class TaskFlagTests
	{	//****************************************
		private int _Counter;
		//****************************************
		
		[SetUp()]
		public void Reset()
		{
			_Counter = 0;
		}
		
		//****************************************
		
		[Test(), Timeout(1500)]
		public async Task SetAndWait()
		{
			var MyFlag = new TaskFlag(WaitHalfSecond);
			
			var StartTime = DateTime.Now;
			
			await MyFlag.SetAndWait();
			
			Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(400.0, 600.0));
			
			Assert.AreEqual(1, _Counter, "Counter is not as expected");
		}
		
		[Test(), Timeout(1500)]
		public async Task SetAndWaitDelay()
		{
			var MyFlag = new TaskFlag(WaitHalfSecond, new TimeSpan(0, 0, 0, 0, 100));
			
			var StartTime = DateTime.Now;
			
			var MyTask = MyFlag.SetAndWait();
			
			Thread.Sleep(50);
			
			Assert.AreEqual(0, _Counter, "Raised early");
			
			await MyTask;
			
			Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(500.0, 700.0));
			
			Assert.AreEqual(1, _Counter, "Counter is not as expected");
		}
		
		[Test(), Timeout(2500)]
		public async Task SetAndWaitTwice()
		{
			var MyFlag = new TaskFlag(WaitHalfSecond);
			
			var StartTime = DateTime.Now;
			
			MyFlag.Set();
			
			Thread.Sleep(200);
			
			await MyFlag.SetAndWait();
			
			Assert.That(DateTime.Now.Subtract(StartTime).TotalMilliseconds, Is.InRange(900.0, 1100.0));
			
			Assert.AreEqual(2, _Counter, "Counter is not as expected");
		}
		
		[Test(), Timeout(1500)]
		public async Task SetRuns()
		{
			var MyFlag = new TaskFlag(WaitFiftyMs);
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
		
		private void WaitFiftyMs()
		{
			Interlocked.Increment(ref _Counter);
			
			Thread.Sleep(50);
		}
		
		private void WaitHalfSecond()
		{
			Interlocked.Increment(ref _Counter);
			
			Thread.Sleep(500);
		}
	}
}
