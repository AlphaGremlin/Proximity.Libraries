using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
//****************************************

namespace Proximity.Utility.Diagnostics
{
	/// <summary>
	/// Provides Profiling services for a section of code
	/// </summary>
	public sealed class ProfileSection
	{	//****************************************
		private readonly Profiler _Owner;
		private long _Elapsed;
		private long _ShortRecent, _ShortLast;
		private long _LongRecent, _LongLast;
		private long _NextShortUpdate, _NextLongUpdate;
		//****************************************
		
		internal ProfileSection(Profiler owner, string name)
		{
			_Owner = owner;
			Name = name;
			ResetTime = _Owner.NowExact;
			
			_NextShortUpdate = _NextLongUpdate = ResetTime.Ticks;
		}

		//****************************************

		internal Profiler.SectionInstance Start() => new Profiler.SectionInstance(this, _Owner.NowExact);

		internal void Finish(DateTime startTime)
		{	//****************************************
			var Now = _Owner.NowExact;
			var MyTicks = Now.Subtract(startTime).Ticks;
			//****************************************
			
			Synchronise(Now);
			
			Interlocked.Add(ref _Elapsed, MyTicks);
			Interlocked.Add(ref _ShortRecent, MyTicks);
			Interlocked.Add(ref _LongRecent, MyTicks);
		}
		
		//****************************************
		
		/// <summary>
		/// Retrieves the current statistics as at this time
		/// </summary>
		/// <returns>An immutable record of the statistics at this point in time</returns>
		public Record GetCurrent()
		{
			Synchronise(_Owner.NowExact);
			
			return new Record(this);
		}
		
		/// <summary>
		/// Resets the Elapsed counter for this Section back to zero
		/// </summary>
		public void Reset()
		{
			ResetTime = _Owner.NowExact;
			
			Interlocked.Exchange(ref _Elapsed, 0L);
		}

		/// <summary>
		/// Provides a text representation of this Section
		/// </summary>
		/// <returns>A string describing the section</returns>
		public override string ToString() => $"{Name}: {new TimeSpan(_Elapsed).TotalMilliseconds,4:F1}";

		//****************************************

		private void Synchronise(DateTime currentTime)
		{	//****************************************
			long MyUpdateTime;
			var Now = currentTime.Ticks;
			//****************************************
		
			MyUpdateTime = _NextShortUpdate;
			
			// If the Short Time has elapsed, try and update it
			if (MyUpdateTime < Now && Interlocked.CompareExchange(ref _NextShortUpdate, Now + _Owner.ShortInterval.Ticks, MyUpdateTime) == MyUpdateTime)
			{
				// If the Short Time has elapsed more than once, Last should be zero
				if (MyUpdateTime + _Owner.ShortInterval.Ticks < Now)
				{
					Interlocked.Exchange(ref _ShortRecent, 0);
					Interlocked.Exchange(ref _ShortLast, 0);
				}
				else
				{
					_ShortLast = Interlocked.Exchange(ref _ShortRecent, 0);
				}
			}
			
			//****************************************
			
			MyUpdateTime = _NextLongUpdate;
			
			// If the Long Time has elapsed, try and update it
			if (MyUpdateTime < Now && Interlocked.CompareExchange(ref _NextLongUpdate, Now + _Owner.LongInterval.Ticks, MyUpdateTime) == MyUpdateTime)
			{
				// If the Long Time has elapsed more than once, Last should be zero
				if (MyUpdateTime + _Owner.LongInterval.Ticks < Now)
				{
					Interlocked.Exchange(ref _LongRecent, 0);
					Interlocked.Exchange(ref _LongLast, 0);
				}
				else
				{
					_LongLast = Interlocked.Exchange(ref _LongRecent, 0);
				}
			}
		}

		//****************************************

		/// <summary>
		/// Gets the name of this Section
		/// </summary>
		public string Name { get; }

		//****************************************

		/// <summary>
		/// Represents a record of profiler statistics as at a point in time
		/// </summary>
		public sealed class Record
		{	//****************************************
			private readonly Profiler _Owner;

			//****************************************

			internal Record(ProfileSection section)
			{
				_Owner = section._Owner;
				CurrentTime = _Owner.NowExact;
				
				ResetLast = section.ResetTime;
				Elapsed = new TimeSpan(section._Elapsed);
				ElapsedShort = new TimeSpan(section._ShortLast);
				ElapsedLong = new TimeSpan(section._LongLast);
			}

			//****************************************

			/// <summary>
			/// Gets the time at which these statistics were last reset
			/// </summary>
			public DateTime ResetLast { get; }

			/// <summary>
			/// Gets the time at which these statistics are current
			/// </summary>
			public DateTime CurrentTime { get; }

			/// <summary>
			/// Gets the amount of time elapsed since the last reset
			/// </summary>
			public TimeSpan ElapsedSinceReset => CurrentTime.Subtract(ResetLast);

			/// <summary>
			/// Gets the total events since the last reset
			/// </summary>
			public TimeSpan Elapsed { get; }

			/// <summary>
			/// Gets the total time elapsed during the last short interval
			/// </summary>
			public TimeSpan ElapsedShort { get; }

			/// <summary>
			/// Gets the total time elapsed during the last long interval
			/// </summary>
			public TimeSpan ElapsedLong { get; }

			/// <summary>
			/// Gets the average time spent per second since the last reset
			/// </summary>
			public TimeSpan Average => new TimeSpan(Elapsed.Ticks / CurrentTime.Subtract(ResetLast).Ticks);

			/// <summary>
			/// Gets the average time spent per second in the last short interval
			/// </summary>
			public TimeSpan AverageShort => new TimeSpan(ElapsedShort.Ticks / _Owner.ShortInterval.Ticks);

			/// <summary>
			/// Gets the average time spent per second in the last long interval
			/// </summary>
			public TimeSpan AverageLong => new TimeSpan(ElapsedLong.Ticks / _Owner.LongInterval.Ticks);
		}

		/// <summary>
		/// Gets the last time this Section was reset
		/// </summary>
		public DateTime ResetTime { get; private set; }
	}
	
}
