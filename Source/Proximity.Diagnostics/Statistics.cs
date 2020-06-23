using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Proximity.Diagnostics
{
	/// <summary>
	/// Tracks statistics over time
	/// </summary>
	public sealed class Statistics
	{ //****************************************
		private readonly Stopwatch _Timer;
		private readonly DateTime _StartTime;

		private readonly TimeSpan[] _Intervals;
		private readonly Dictionary<TimeSpan, int> _IntervalLookup;
		private readonly Dictionary<string, Statistic> _Statistics = new Dictionary<string, Statistic>();
		//****************************************

		/// <summary>
		/// Creates a new Statistics tracker with default 15, 5 and 1 minute intervals
		/// </summary>
		public Statistics() : this(new TimeSpan(0, 15, 0), new TimeSpan(0, 5, 0), new TimeSpan(0, 1, 0))
		{
		}

		/// <summary>
		/// Creates a new Statistics tracker
		/// </summary>
		/// <param name="intervals">The time intervals to track each value over</param>
		public Statistics(params TimeSpan[] intervals) : this((IEnumerable<TimeSpan>)intervals)
		{
		}

		/// <summary>
		/// Creates a new Statistics tracker
		/// </summary>
		/// <param name="intervals">The time intervals to track each value over</param>
		public Statistics(IEnumerable<TimeSpan> intervals)
		{
			_Intervals = intervals.ToArray();
			_IntervalLookup = _Intervals.Select((interval, index) => (interval, index)).ToDictionary(pair => pair.interval, pair => pair.index);

			_StartTime = DateTime.Now;
			_Timer = Stopwatch.StartNew();
		}

		//****************************************

		/// <summary>
		/// Registers a metric for tracking
		/// </summary>
		/// <param name="names">The metric names to track</param>
		public void Add(params string[] names)
		{
			foreach (var Name in names)
				Add(Name);
		}

		/// <summary>
		/// Registers a metric for monitoring
		/// </summary>
		/// <param name="name">The metric name to track</param>
		public void Add(string name)
		{
			_Statistics.Add(name, new Statistic(_Intervals.Length));
		}

		/// <summary>
		/// Resets all metrics to zero
		/// </summary>
		public void Reset()
		{
			var Ticks = GetTicks();

			foreach (var Statistic in _Statistics.Values)
				Statistic.Reset(Ticks, _Intervals);
		}

		/// <summary>
		/// Increments a metric
		/// </summary>
		/// <param name="name">The metric name to update</param>
		/// <remarks>Metrics used with <see cref="Increase"/> or <see cref="Increment"/> should not be used with <see cref="Peak"/></remarks>
		public void Increment(string name) => Increase(name, 1);

		/// <summary>
		/// Increases a metric
		/// </summary>
		/// <param name="name">The metric name to update</param>
		/// <param name="value">The value to increase the metric by</param>
		/// <remarks>Metrics used with <see cref="Increase"/> or <see cref="Increment"/> should not be used with <see cref="Peak"/></remarks>
		public void Increase(string name, long value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			if (value == 0)
				return;

			if (name is null || !_Statistics.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			Statistic.Add(GetTicks(), _Intervals, value);
		}

		/// <summary>
		/// Sets the peak of a metric
		/// </summary>
		/// <param name="name">The metric name to update</param>
		/// <param name="value">The peak value to apply</param>
		/// <remarks>Metrics used with <see cref="Peak"/> should not be used with <see cref="Increase"/> or <see cref="Increment"/></remarks>
		public void Peak(string name, long value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			if (value == 0)
				return;

			if (name is null || !_Statistics.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			Statistic.Peak(GetTicks(), _Intervals, value);
		}

		/// <summary>
		/// Retrieves the current values of a metric in all time intervals
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <returns>The values of the metric</returns>
		public IReadOnlyList<long> Get(string name)
		{
			var CurrentValues = new long[Intervals.Count];

			Get(name, CurrentValues);

			return CurrentValues;
		}

		/// <summary>
		/// Retrieves the current values of a metric in all time intervals
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="values">Receives the values of the metric</param>
		public void Get(string name, Span<long> values)
		{
			if (name is null || !_Statistics.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (values.Length < Intervals.Count)
				throw new ArgumentOutOfRangeException(nameof(values));

			var Records = Statistic.Records;
			var CurrentTicks = GetTicks();

			values = values.Slice(0, Intervals.Count);

			for (var Index = 0; Index < values.Length; Index++)
			{
				var (LastTicks, Current, Previous) = Records[Index];
				var IntervalTicks = Intervals[Index].Ticks;

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
					values[Index] = 0;
			}
		}

		/// <summary>
		/// Retrieves the current value of a metric
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="interval">The time interval we're interested in</param>
		/// <returns>The value of the metric in the given time interval</returns>
		public long Get(string name, TimeSpan interval)
		{
			if (name is null || !_Statistics.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (!_IntervalLookup.TryGetValue(interval, out var Index))
				throw new ArgumentOutOfRangeException(nameof(interval));

			var (LastTicks, Current, Previous) = Statistic.Records[Index];
			var CurrentTicks = GetTicks();

			var NextInterval = LastTicks + interval.Ticks;

			// Determine when the currently active interval ends
			if (NextInterval > CurrentTicks)
				// The current interval has yet to elapse, so we return the result from the previous interval
				return Previous;

			if (NextInterval + interval.Ticks > CurrentTicks)
				// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
				return Current;

			// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
			return 0;
		}

		/// <summary>
		/// Retrieves the active value of a metric
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="interval">The time interval we're interested in</param>
		/// <returns>The active value of the metric in the given time interval</returns>
		public long GetLatest(string name, TimeSpan interval)
		{
			if (name is null || !_Statistics.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (!_IntervalLookup.TryGetValue(interval, out var Index))
				throw new ArgumentOutOfRangeException(nameof(interval));

			var (LastTicks, Current, _) = Statistic.Records[Index];
			var CurrentTicks = GetTicks();

			var NextInterval = LastTicks + interval.Ticks;

			if (NextInterval > CurrentTicks)
				return Current;

			// The current interval has elapsed, so the next interval is zero
			return 0;
		}

		//****************************************

		private long GetTicks() => _StartTime.Ticks + _Timer.Elapsed.Ticks;

		//****************************************

		/// <summary>
		/// Gets the list of intervals supported by this Statistics tracker
		/// </summary>
		public IReadOnlyList<TimeSpan> Intervals => _Intervals;

		//****************************************

		private sealed class Statistic
		{ //****************************************
			private StatisticsRecord _Records;
			//****************************************

			internal Statistic(int intervals)
			{
				_Records = new StatisticsRecord(intervals);
			}

			//****************************************

			internal void Add(long ticks, IReadOnlyList<TimeSpan> intervals, long value)
			{
				StatisticsRecord OldRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
				}
				while (Interlocked.CompareExchange(ref _Records, OldRecords.Add(ticks, intervals, value), OldRecords) != OldRecords);
			}

			internal void Peak(long ticks, IReadOnlyList<TimeSpan> intervals, long value)
			{
				StatisticsRecord OldRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
				}
				while (Interlocked.CompareExchange(ref _Records, OldRecords.Peak(ticks, intervals, value), OldRecords) != OldRecords);
			}

			internal void Reset(long ticks, IReadOnlyList<TimeSpan> intervals)
			{
				StatisticsRecord OldRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
				}
				while (Interlocked.CompareExchange(ref _Records, OldRecords.Reset(ticks, intervals), OldRecords) != OldRecords);
			}

			//****************************************

			public StatisticsRecord Records => Volatile.Read(ref _Records);
		}

		private sealed class StatisticsRecord
		{ //****************************************
			private readonly ImmutableArray<long> _LastTicks, _Current, _Previous;
			//****************************************

			public StatisticsRecord(int intervals)
			{
				var Blank = ImmutableArray.CreateBuilder<long>(intervals);

				for (var Index = 0; Index < intervals; Index++)
					Blank.Add(0);

				_LastTicks = _Current = _Previous = Blank.ToImmutable();
			}

			private StatisticsRecord(ImmutableArray<long> lastTicks, ImmutableArray<long> current, ImmutableArray<long> previous)
			{
				_LastTicks = lastTicks;
				_Current = current;
				_Previous = previous;
			}

			//****************************************

			internal StatisticsRecord Add(long ticks, IReadOnlyList<TimeSpan> intervals, long value)
			{
				ImmutableArray<long>.Builder? LastTicks = null;
				var Current = _Current.ToBuilder();
				ImmutableArray<long>.Builder? Previous = null;

				for (var Index = 0; Index < _LastTicks.Length; Index++)
				{
					var IntervalTicks = intervals[Index].Ticks;
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
						LastTicks[Index] = ticks - (ticks % IntervalTicks);
						// If it's been more than one interval since we last ticked over, the previous should be zero
						Previous![Index] = IntervalEnds + IntervalTicks <= ticks ? 0 : Current[Index];
						// The current interval peak becomes the value of the previous interval
						Current[Index] = value;
					}
					else
					{
						Current[Index] += value;
					}
				}

				return new StatisticsRecord(LastTicks?.ToImmutable() ?? _LastTicks, Current.ToImmutable(), Previous?.ToImmutable() ?? _Previous);
			}

			internal StatisticsRecord Peak(long ticks, IReadOnlyList<TimeSpan> intervals, long value)
			{
				ImmutableArray<long>.Builder? LastTicks = null;
				ImmutableArray<long>.Builder? Current = null;
				ImmutableArray<long>.Builder? Previous = null;

				for (var Index = 0; Index < _LastTicks.Length; Index++)
				{
					var IntervalTicks = intervals[Index].Ticks;
					var IntervalEnds = _LastTicks[Index] + IntervalTicks;

					if (IntervalEnds <= ticks)
					{
						// This interval has elapsed and needs rolling over
						if (LastTicks == null)
						{
							LastTicks = _LastTicks.ToBuilder();
							Previous = _Previous.ToBuilder();
						}

						if (Current == null)
							Current = _Current.ToBuilder();

						// Round to the nearest Interval
						LastTicks[Index] = ticks - (ticks % IntervalTicks);
						// If it's been more than one interval since we last ticked over, the previous should be zero
						Previous![Index] = IntervalEnds + IntervalTicks <= ticks ? 0 : Current[Index];
						// The current interval peak becomes the value of the previous interval
						Current[Index] = value;
					}
					else
					{
						if (_Current[Index] >= value)
							continue;

						if (Current == null)
							Current = _Current.ToBuilder();

						Current[Index] = value;
					}
				}

				if (LastTicks == null && Current == null && Previous == null)
					return this;

				return new StatisticsRecord(LastTicks?.ToImmutable() ?? _LastTicks, Current?.ToImmutable() ?? _Current, Previous?.ToImmutable() ?? _Previous);
			}

			internal StatisticsRecord Reset(long ticks, IReadOnlyList<TimeSpan> intervals)
			{
				// Every Last Tick should be 'now'
				var LastTicks = ImmutableArray.CreateBuilder<long>(_LastTicks.Length);

				for (var Index = 0; Index < intervals.Count; Index++)
					LastTicks.Add(ticks - (ticks % intervals[Index].Ticks));

				// Current and Previous should be zero
				var Blank = ImmutableArray.CreateBuilder<long>(_LastTicks.Length);

				for (var Index = 0; Index < _LastTicks.Length; Index++)
					Blank.Add(0);

				var Counters = Blank.ToImmutable();

				return new StatisticsRecord(
					LastTicks.ToImmutable(),
					Counters,
					Counters
					);
			}

			//****************************************

			public (long ticks, long current, long previous) this[int index] => (_LastTicks[index], _Current[index], _Previous[index]);
		}
	}
}
