using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Proximity.Diagnostics.Tests;

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

		Assert.That(Results[0].Samples, Is.EqualTo(1));
		Assert.That(Results[1].Samples, Is.Zero);
		Assert.That(Results[2].Samples, Is.Zero);
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

		Assert.That(Results[0].Samples, Is.Zero);
		Assert.That(Results[1].Samples, Is.EqualTo(1));
		Assert.That(Results[2].Samples, Is.Zero);
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

		Assert.That(Results[0].Samples, Is.EqualTo(1));
		Assert.That(Results[1].Samples, Is.EqualTo(2));
		Assert.That(Results[2].Samples, Is.Zero);
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

		Assert.That(Results[0].Samples, Is.Zero);
		Assert.That(Results[1].Samples, Is.Zero);
		Assert.That(Results[2].Samples, Is.EqualTo(1));
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

		Assert.That(Result.Samples, Is.EqualTo(1));
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

		Assert.That(Result.Samples, Is.EqualTo(2));
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
