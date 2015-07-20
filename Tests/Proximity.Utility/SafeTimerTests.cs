/****************************************\
 SafeTimerTests.cs
 Created: 2012-07-26
\****************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Tests for the SafeTimer
	/// </summary>
	[TestFixture()]
	public class SafeTimerTests
	{
		[Test()]
		public void DelayedCallback()
		{	//****************************************
			var Counter = 0;
			//****************************************
			
			SafeTimer.DelayedCallback<object>((state) => { Interlocked.Increment(ref Counter); }, null, 500);
			
			Thread.Sleep(1500);
			
			Assert.AreEqual(1, Counter, "Delayed Callback should have run once, ran {0} times", Counter);
		}
	}
}
