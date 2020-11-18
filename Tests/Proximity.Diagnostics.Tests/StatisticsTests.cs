using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Proximity.Diagnostics.Tests
{
	[TestFixture]
	public class StatisticsTests
	{
		private static readonly TimeSpan ShortInterval = new TimeSpan(10 * TimeSpan.TicksPerMillisecond);

		private static readonly TimeSpan[] Intervals = new[] { ShortInterval, new TimeSpan(100 * TimeSpan.TicksPerMillisecond), new TimeSpan(250 * TimeSpan.TicksPerMillisecond) };

		private readonly Stopwatch _Stopwatch = Stopwatch.StartNew();

		[Test]
		public void Add()
		{
			var Stats = new Statistics(Intervals);

			Stats.Add("Test");

			Stats.Reset();

//			var StartTime = DateTime.Now;
//			var FirstInterval = ShortInterval - new TimeSpan(StartTime.Ticks % ShortInterval.Ticks);

//			WaitFor(FirstInterval);

			Stats.Increment("Test");

			WaitFor(ShortInterval);

			var Results = Stats.Get("Test");

			Assert.AreEqual(1, Results[0]);//, "Failed, interval {0}", FirstInterval);
			Assert.AreEqual(0, Results[1]);
			Assert.AreEqual(0, Results[2]);
		}

		private void WaitFor(TimeSpan time)
		{
			var Target = _Stopwatch.Elapsed + time;

			do
			{
				Thread.Yield();
			}
			while (_Stopwatch.Elapsed >= Target);
		}
	}
}
