using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace Proximity.Diagnostics.Tests
{
	[TestFixture]
	public class ProfilerTests
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
			var Profile = new Profiler(Intervals);
			Profile.Add("Test");
			Profile.Reset();

			//****************************************

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			WaitFor(ShortInterval);

			//****************************************

			var Results = Profile.Get("Test");

			Assert.AreEqual(1, Results[0].Samples);
			Assert.AreEqual(0, Results[1].Samples);
			Assert.AreEqual(0, Results[2].Samples);
		}

		[Test]
		public void AddMedium()
		{
			var Profile = new Profiler(Intervals);
			Profile.Add("Test");
			Profile.Reset();

			//****************************************

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			WaitFor(MediumInterval);

			//****************************************

			var Results = Profile.Get("Test");

			Assert.AreEqual(0, Results[0].Samples);
			Assert.AreEqual(1, Results[1].Samples);
			Assert.AreEqual(0, Results[2].Samples);
		}

		[Test]
		public void AddMediumMulti()
		{
			var Profile = new Profiler(Intervals);
			Profile.Add("Test");
			Profile.Reset();

			//****************************************

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			WaitFor(ShortInterval);

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			//****************************************

			var Results = Profile.Get("Test");

			Assert.AreEqual(1, Results[0].Samples);
			Assert.AreEqual(2, Results[1].Samples);
			Assert.AreEqual(0, Results[2].Samples);
		}

		[Test]
		public void AddLong()
		{
			var Profile = new Profiler(Intervals);
			Profile.Add("Test");
			Profile.Reset();

			//****************************************

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			// Need to wait 40ms for the medium interval to tick over
			WaitFor(LongInterval + ShortInterval);

			//****************************************

			var Results = Profile.Get("Test");

			Assert.AreEqual(0, Results[0].Samples);
			Assert.AreEqual(0, Results[1].Samples);
			Assert.AreEqual(1, Results[2].Samples);
		}

		[Test]
		public void AllTime1()
		{
			var Profile = new Profiler(new[] { TimeSpan.Zero });
			Profile.Add("Test");
			Profile.Reset();

			//****************************************

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			//****************************************

			var Result = Profile.Get("Test")[0];

			Assert.AreEqual(1, Result.Samples);
		}

		[Test]
		public void AllTime2()
		{
			var Profile = new Profiler(new[] { TimeSpan.Zero });
			Profile.Add("Test");
			Profile.Reset();

			//****************************************

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			using (Profile.Profile("Test"))
			{
				Thread.Yield();
			}

			//****************************************

			var Result = Profile.Get("Test")[0];

			Assert.AreEqual(2, Result.Samples);
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
