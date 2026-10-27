using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Diagnostics.Tests;

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

		Assert.That(Results[0], Is.EqualTo(1));
		Assert.That(Results[1], Is.Zero);
		Assert.That(Results[2], Is.Zero);
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

		Assert.That(Results[0], Is.Zero);
		Assert.That(Results[1], Is.EqualTo(1));
		Assert.That(Results[2], Is.Zero);
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

		Assert.That(Results[0], Is.EqualTo(1));
		Assert.That(Results[1], Is.EqualTo(2));
		Assert.That(Results[2], Is.Zero);
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

		Assert.That(Results[0], Is.Zero);
		Assert.That(Results[1], Is.Zero);
		Assert.That(Results[2], Is.EqualTo(1));
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
