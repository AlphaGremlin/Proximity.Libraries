using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Proximity.Diagnostics.Tests
{
	[TestFixture]
	public class StatisticsTests
	{	//****************************************
		private static readonly TimeSpan ShortInterval = new TimeSpan(10 * TimeSpan.TicksPerMillisecond);
		private static readonly TimeSpan MediumInterval = new TimeSpan(20 * TimeSpan.TicksPerMillisecond);
		private static readonly TimeSpan LongInterval = new TimeSpan(30 * TimeSpan.TicksPerMillisecond);

		private static readonly TimeSpan[] Intervals = new[] { ShortInterval, MediumInterval, LongInterval };
		//****************************************
		private readonly Stopwatch _Stopwatch = Stopwatch.StartNew();
		//****************************************

		[Test]
		public void AddShort()
		{
			var Stats = new Statistics(Intervals);
			Stats.Add("Test");
			Stats.Reset();

			//****************************************

			Stats.Increment("Test");

			WaitFor(ShortInterval);

			//****************************************

			var Results = Stats.GetRaw("Test");

			Assert.AreEqual(1, Results[0]);
			Assert.AreEqual(0, Results[1]);
			Assert.AreEqual(0, Results[2]);
		}

		[Test]
		public void AddMedium()
		{
			var Stats = new Statistics(Intervals);
			Stats.Add("Test");
			Stats.Reset();

			//****************************************

			Stats.Increment("Test");

			WaitFor(MediumInterval);

			//****************************************

			var Results = Stats.GetRaw("Test");

			Assert.AreEqual(0, Results[0]);
			Assert.AreEqual(1, Results[1]);
			Assert.AreEqual(0, Results[2]);
		}

		[Test]
		public void AddMediumMulti()
		{
			var Stats = new Statistics(Intervals);
			Stats.Add("Test");
			Stats.Reset();

			//****************************************

			Stats.Increment("Test");

			WaitFor(ShortInterval);

			Stats.Increment("Test");

			WaitFor(ShortInterval);

			//****************************************

			var Results = Stats.GetRaw("Test");

			Assert.AreEqual(1, Results[0]);
			Assert.AreEqual(2, Results[1]);
			Assert.AreEqual(0, Results[2]);
		}

		[Test]
		public void AddLong()
		{
			var Stats = new Statistics(Intervals);
			Stats.Add("Test");
			Stats.Reset();

			//****************************************

			Stats.Increment("Test");

			// Need to wait 40ms for the medium interval to tick over
			WaitFor(LongInterval + ShortInterval);

			//****************************************

			var Results = Stats.GetRaw("Test");

			Assert.AreEqual(0, Results[0]);
			Assert.AreEqual(0, Results[1]);
			Assert.AreEqual(1, Results[2]);
		}

		//****************************************

		private void WaitFor(TimeSpan time)
		{
			var Target = _Stopwatch.Elapsed + time;

			do
			{
				Thread.Yield();
			}
			while (_Stopwatch.Elapsed < Target);
		}
	}
}
