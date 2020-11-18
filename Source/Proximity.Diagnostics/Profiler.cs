using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Proximity.Diagnostics
{
	/// <summary>
	/// Profiles performance statistics over time
	/// </summary>
	public sealed class Profiler
	{ //****************************************
		private readonly Stopwatch _Timer;
		private TimeSpan _StartTime;

		private readonly TimeSpan[] _Intervals;
		private readonly Dictionary<TimeSpan, int> _IntervalLookup;
		private readonly Dictionary<string, Section> _Sections = new Dictionary<string, Section>();

		private readonly SectionState _Blank;
		//****************************************

		/// <summary>
		/// Creates a new Profiler with default 0, 15, 5 and 1 minute intervals
		/// </summary>
		public Profiler() : this(TimeSpan.Zero, new TimeSpan(0, 15, 0), new TimeSpan(0, 5, 0), new TimeSpan(0, 1, 0))
		{
		}

		/// <summary>
		/// Creates a new Profiler
		/// </summary>
		/// <param name="intervals">The time intervals to track each value over</param>
		public Profiler(params TimeSpan[] intervals) : this((IEnumerable<TimeSpan>)intervals)
		{
		}

		/// <summary>
		/// Creates a new Profiler
		/// </summary>
		/// <param name="intervals">The time intervals to track each value over</param>
		public Profiler(IEnumerable<TimeSpan> intervals)
		{
			_Intervals = intervals.ToArray();
			_IntervalLookup = _Intervals.Select((interval, index) => (interval, index)).ToDictionary(pair => pair.interval, pair => pair.index);

			_StartTime = new TimeSpan(DateTime.Now.Ticks);
			_Timer = Stopwatch.StartNew();

			_Blank = new SectionState(_Intervals.Length);
		}

		//****************************************

		/// <summary>
		/// Registers named sections for profiling
		/// </summary>
		/// <param name="names">The section names to register</param>
		public void Add(params string[] names)
		{
			foreach (var Name in names)
				Add(Name);
		}

		/// <summary>
		/// Registers a named section for profiling
		/// </summary>
		/// <param name="name">The section name to register</param>
		public void Add(string name) => _Sections.Add(name, new Section(name, this));

		/// <summary>
		/// Resets all sections to zero
		/// </summary>
		public void Reset()
		{
			_Timer.Restart();
			_StartTime = new TimeSpan(DateTime.Now.Ticks);

			var Ticks = GetTicks();

			foreach (var Statistic in _Sections.Values)
				Statistic.Reset(Ticks);
		}

		/// <summary>
		/// Starts profiling a section of code
		/// </summary>
		/// <param name="name">The named section to profile</param>
		/// <returns>An instance that should be disposed once profiling is complete</returns>
		public ProfilerInstance Profile(string name)
		{
			if (name is null || !_Sections.TryGetValue(name, out var Section))
				throw new ArgumentOutOfRangeException(nameof(name));

			return new ProfilerInstance(Section, GetTicks());
		}

		/// <summary>
		/// Retrieves the current values of a metric in all time intervals
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <returns>The values of the metric</returns>
		public IReadOnlyList<ProfilerRecord> Get(string name)
		{
			var CurrentValues = new ProfilerRecord[Intervals.Count];

			Get(name, CurrentValues);

			return CurrentValues;
		}

		/// <summary>
		/// Retrieves the current values of a metric in all time intervals
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="values">Receives the values of the metric</param>
		public void Get(string name, Span<ProfilerRecord> values)
		{
			if (name is null || !_Sections.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (values.Length < Intervals.Count)
				throw new ArgumentOutOfRangeException(nameof(values));

			var Records = Statistic.Records;
			var CurrentTicks = GetTicks();

			values = values.Slice(0, Intervals.Count);

			for (var Index = 0; Index < values.Length; Index++)
			{
				var (LastTicks, Current, Previous) = Records[Index];
				var IntervalTicks = Intervals[Index];

				var NextInterval = LastTicks + IntervalTicks;

				// Determine when the currently active interval ends
				if (NextInterval > CurrentTicks)
					// The current interval has yet to elapse, so we return the result from the previous interval
					values[Index] = Previous;
				else if (NextInterval + IntervalTicks > CurrentTicks)
					// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
					values[Index] = Current;
				else
					// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
					values[Index] = default;
			}
		}

		/// <summary>
		/// Retrieves the current value of a metric
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="interval">The time interval we're interested in</param>
		/// <returns>The value of the metric in the given time interval</returns>
		public ProfilerRecord Get(string name, TimeSpan interval)
		{
			if (name is null || !_Sections.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (!_IntervalLookup.TryGetValue(interval, out var Index))
				throw new ArgumentOutOfRangeException(nameof(interval));

			var (LastTicks, Current, Previous) = Statistic.Records[Index];
			var CurrentTicks = GetTicks();

			var NextInterval = LastTicks + interval;

			// Determine when the currently active interval ends
			if (NextInterval > CurrentTicks)
				// The current interval has yet to elapse, so we return the result from the previous interval
				return Previous;

			if (NextInterval + interval > CurrentTicks)
				// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
				return Current;

			// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
			return default;
		}

		/// <summary>
		/// Retrieves the active value of a metric
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="interval">The time interval we're interested in</param>
		/// <returns>The active value of the metric in the given time interval</returns>
		public ProfilerRecord GetLatest(string name, TimeSpan interval)
		{
			if (name is null || !_Sections.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (!_IntervalLookup.TryGetValue(interval, out var Index))
				throw new ArgumentOutOfRangeException(nameof(interval));

			var (LastTicks, Current, _) = Statistic.Records[Index];
			var CurrentTicks = GetTicks();

			var NextInterval = LastTicks + interval;

			if (NextInterval > CurrentTicks)
				return Current;

			// The current interval has elapsed, so the next interval is zero
			return default;
		}

		//****************************************

		private TimeSpan GetTicks() => _StartTime + _Timer.Elapsed;

		//****************************************

		/// <summary>
		/// Gets the list of intervals supported by this Profiler
		/// </summary>
		public IReadOnlyList<TimeSpan> Intervals => _Intervals;

		//****************************************

		/// <summary>
		/// Represents a profiling instance
		/// </summary>
		public readonly struct ProfilerInstance : IDisposable
		{ //****************************************
			private readonly Section _Section;
			private readonly TimeSpan _StartTime;
			//****************************************

			internal ProfilerInstance(Section section, TimeSpan startTime)
			{
				_Section = section;
				_StartTime = startTime;
			}

			//****************************************

			/// <summary>
			/// Completes the profiling instance
			/// </summary>
			public void Dispose() => _Section.Finish(_StartTime);

			//****************************************

			/// <summary>
			/// Gets the details of the Section being profiled
			/// </summary>
			public string Section => _Section.Name;
		}

		internal sealed class Section
		{ //****************************************
			private readonly Profiler _Profiler;

			private SectionState _Records;
			//****************************************

			internal Section(string name, Profiler profiler)
			{
				Name = name;
				_Profiler = profiler;
				_Records = profiler._Blank;
			}

			//****************************************

			internal void Finish(TimeSpan startTime)
			{
				var Ticks = _Profiler.GetTicks();
				var Elapsed = Ticks - startTime;

				SectionState OldRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
				}
				while (Interlocked.CompareExchange(ref _Records, OldRecords.Add(Ticks, _Profiler, Elapsed.Ticks), OldRecords) != OldRecords);
			}

			internal void Reset(TimeSpan ticks)
			{
				SectionState OldRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
				}
				while (Interlocked.CompareExchange(ref _Records, OldRecords.Reset(ticks, _Profiler), OldRecords) != OldRecords);
			}

			//****************************************

			public string Name { get; }

			public SectionState Records => Volatile.Read(ref _Records);
		}

		internal sealed class SectionState
		{ //****************************************
			private readonly ImmutableArray<TimeSpan> _LastTicks;
			private readonly ImmutableArray<ProfilerRecord> _Current, _Previous;
			//****************************************

			public SectionState(int intervals)
			{
				var Blank = ImmutableArray.CreateBuilder<TimeSpan>(intervals);
				var BlankEntries = ImmutableArray.CreateBuilder<ProfilerRecord>(intervals);

				for (var Index = 0; Index < intervals; Index++)
				{
					Blank.Add(TimeSpan.Zero);
					BlankEntries.Add(ProfilerRecord.Empty);
				}

				_LastTicks = Blank.ToImmutable();
				_Current = _Previous = BlankEntries.ToImmutable();
			}

			private SectionState(ImmutableArray<TimeSpan> lastTicks, ImmutableArray<ProfilerRecord> current, ImmutableArray<ProfilerRecord> previous)
			{
				_LastTicks = lastTicks;
				_Current = current;
				_Previous = previous;
			}

			//****************************************

			internal SectionState Add(TimeSpan ticks, Profiler profiler, long elapsed)
			{
				ImmutableArray<TimeSpan>.Builder? LastTicks = null;
				var Current = _Current.ToBuilder();
				ImmutableArray<ProfilerRecord>.Builder? Previous = null;

				var Intervals = profiler._Intervals;

				for (var Index = 0; Index < Intervals.Length; Index++)
				{
					var IntervalTicks = Intervals[Index];
					var IntervalEnds = _LastTicks[Index] + IntervalTicks;

					if (IntervalEnds <= ticks)
					{
						// This interval has elapsed and needs rolling over
						if (LastTicks == null)
						{
							LastTicks = _LastTicks.ToBuilder();
							Previous = _Previous.ToBuilder();
						}

						// Round to the nearest Interval
						LastTicks[Index] = RoundTo(ticks, IntervalTicks);
						// If it's been more than one interval since we last ticked over, the previous should be zero
						Previous![Index] = IntervalEnds + IntervalTicks <= ticks ? ProfilerRecord.Empty : Current[Index];
						// The current interval peak becomes the value of the previous interval
						Current[Index] = new ProfilerRecord(1, elapsed, elapsed, elapsed);
					}
					else
					{
						Current[Index] = Current[Index].Add(elapsed);
					}
				}

				return new SectionState(LastTicks?.ToImmutable() ?? _LastTicks, Current.ToImmutable(), Previous?.ToImmutable() ?? _Previous);
			}

			internal SectionState Reset(TimeSpan ticks, Profiler profiler)
			{
				// Every Last Tick should be 'now'
				var LastTicks = ImmutableArray.CreateBuilder<TimeSpan>(_LastTicks.Length);
				var Intervals = profiler._Intervals;

				for (var Index = 0; Index < Intervals.Length; Index++)
					LastTicks.Add(RoundTo(ticks, Intervals[Index]));

				var Blank = profiler._Blank;

				return new SectionState(
					LastTicks.ToImmutable(),
					Blank._Current,
					Blank._Previous
					);
			}

			//****************************************

			public (TimeSpan ticks, ProfilerRecord current, ProfilerRecord previous) this[int index] => (_LastTicks[index], _Current[index], _Previous[index]);

			//****************************************

			private static TimeSpan RoundTo(TimeSpan value, TimeSpan nearest)
			{
				return new TimeSpan(value.Ticks - (value.Ticks % nearest.Ticks));
			}
		}
	}
}
