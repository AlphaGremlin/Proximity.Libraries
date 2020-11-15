using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Proximity.Diagnostics
{
	/// <summary>
	/// Tracks statistics over time
	/// </summary>
	public sealed class Statistics
	{ //****************************************
		private readonly Stopwatch _Timer;
		private TimeSpan _StartTime;

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

			_StartTime = new TimeSpan(DateTime.Now.Ticks);
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
			_Timer.Restart();
			_StartTime = new TimeSpan(DateTime.Now.Ticks);

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
		public IReadOnlyList<long> GetRaw(string name)
		{
			var CurrentValues = new long[Intervals.Count];

			GetRaw(name, CurrentValues);

			return CurrentValues;
		}

		/// <summary>
		/// Retrieves the current values of a metric in all time intervals
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="values">Receives the values of the metric</param>
		public void GetRaw(string name, Span<long> values)
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
				var IntervalTicks = Intervals[Index];

				if (IntervalTicks == TimeSpan.Zero)
				{
					values[Index] = Current;

					continue;
				}

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
		public long GetRaw(string name, TimeSpan interval)
		{
			if (name is null || !_Statistics.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (!_IntervalLookup.TryGetValue(interval, out var Index))
				throw new ArgumentOutOfRangeException(nameof(interval));

			var (LastTicks, Current, Previous) = Statistic.Records[Index];
			var CurrentTicks = GetTicks();

			var NextInterval = LastTicks + interval;

			if (interval == TimeSpan.Zero)
				return Current;

			// Determine when the currently active interval ends
			if (NextInterval > CurrentTicks)
				// The current interval has yet to elapse, so we return the result from the previous interval
				return Previous;

			if (NextInterval + interval > CurrentTicks)
				// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
				return Current;

			// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
			return 0;
		}

		/// <summary>
		/// Retrieves the current values of a metric in all time intervals
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <returns>The values of the metric</returns>
		public IReadOnlyList<StatisticsRecord> Get(string name)
		{
			var CurrentValues = new StatisticsRecord[Intervals.Count];

			Get(name, CurrentValues);

			return CurrentValues;
		}

		/// <summary>
		/// Retrieves the current values of a metric in all time intervals
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="values">Receives the values of the metric</param>
		public void Get(string name, Span<StatisticsRecord> values)
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
				var IntervalTicks = Intervals[Index];

				if (IntervalTicks == TimeSpan.Zero)
				{
					values[Index] = new StatisticsRecord(Current, CurrentTicks - LastTicks);

					continue;
				}

				var NextInterval = LastTicks + IntervalTicks;

				// Determine when the currently active interval ends
				if (NextInterval > CurrentTicks)
					// The current interval has yet to elapse, so we return the result from the previous interval
					values[Index] = new StatisticsRecord(Previous, IntervalTicks);
				else if (NextInterval + IntervalTicks > CurrentTicks)
					// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
					values[Index] = new StatisticsRecord(Current, IntervalTicks);
				else
					// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
					values[Index] = new StatisticsRecord(0, IntervalTicks);
			}
		}

		/// <summary>
		/// Retrieves the current value of a metric
		/// </summary>
		/// <param name="name">The metric name to retrieve</param>
		/// <param name="interval">The time interval we're interested in</param>
		/// <returns>The value of the metric in the given time interval</returns>
		public StatisticsRecord Get(string name, TimeSpan interval)
		{
			if (name is null || !_Statistics.TryGetValue(name, out var Statistic))
				throw new ArgumentOutOfRangeException(nameof(name));

			if (!_IntervalLookup.TryGetValue(interval, out var Index))
				throw new ArgumentOutOfRangeException(nameof(interval));

			var (LastTicks, Current, Previous) = Statistic.Records[Index];
			var CurrentTicks = GetTicks();

			if (interval == TimeSpan.Zero)
				return new StatisticsRecord(Current, CurrentTicks - LastTicks);

			var NextInterval = LastTicks + interval;

			// Determine when the currently active interval ends
			if (NextInterval > CurrentTicks)
				// The current interval has yet to elapse, so we return the result from the previous interval
				return new StatisticsRecord(Previous, interval);

			if (NextInterval + interval > CurrentTicks)
				// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
				return new StatisticsRecord(Current, interval);

			// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
			return new StatisticsRecord(0, interval);
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

			var NextInterval = LastTicks + interval;

			if (NextInterval > CurrentTicks)
				return Current;

			// The current interval has elapsed, so the next interval is zero
			return 0;
		}

		//****************************************

		private TimeSpan GetTicks() => _Timer.Elapsed;

		//****************************************

		/// <summary>
		/// Gets the list of intervals supported by this Statistics tracker
		/// </summary>
		public IReadOnlyList<TimeSpan> Intervals => _Intervals;

		/// <summary>
		/// Gets the time the statistics were last reset
		/// </summary>
		public DateTime LastReset => new DateTime(_StartTime.Ticks, DateTimeKind.Local);

		/// <summary>
		/// Gets the registered statistic names
		/// </summary>
		public IReadOnlyCollection<string> Metrics => _Statistics.Keys;

		//****************************************

		private sealed class Statistic
		{ //****************************************
			private StatisticsState _Records;
			//****************************************

			internal Statistic(int intervals)
			{
				_Records = new StatisticsState(intervals);
			}

			//****************************************

			internal void Add(TimeSpan ticks, IReadOnlyList<TimeSpan> intervals, long value)
			{
				StatisticsState OldRecords, NewRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
					NewRecords = OldRecords.Prepare(ticks, intervals);
				}
				while (Interlocked.CompareExchange(ref _Records, NewRecords, OldRecords) != OldRecords);

				NewRecords.Add(value);
			}

			internal void Peak(TimeSpan ticks, IReadOnlyList<TimeSpan> intervals, long value)
			{
				StatisticsState OldRecords, NewRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
					NewRecords = OldRecords.Prepare(ticks, intervals);
				}
				while (Interlocked.CompareExchange(ref _Records, NewRecords, OldRecords) != OldRecords);

				NewRecords.Peak(value);
			}

			internal void Reset(TimeSpan ticks, IReadOnlyList<TimeSpan> intervals)
			{
				StatisticsState OldRecords;

				do
				{
					OldRecords = Volatile.Read(ref _Records);
				}
				while (Interlocked.CompareExchange(ref _Records, OldRecords.Reset(ticks, intervals), OldRecords) != OldRecords);
			}

			//****************************************

			public StatisticsState Records => Volatile.Read(ref _Records);
		}

		private sealed class StatisticsState
		{ //****************************************
			private readonly ImmutableArray<TimeSpan> _LastTicks;
			private readonly ImmutableArray<StrongBox<long>> _Current, _Previous;
			//****************************************

			public StatisticsState(int intervals)
			{
				var BlankTicks = ImmutableArray.CreateBuilder<TimeSpan>(intervals);
				var BlankCurrent = ImmutableArray.CreateBuilder<StrongBox<long>>(intervals);
				var BlankPrevious = ImmutableArray.CreateBuilder<StrongBox<long>>(intervals);

				for (var Index = 0; Index < intervals; Index++)
				{
					BlankTicks.Add(default);
					BlankCurrent.Add(new StrongBox<long>(0));
					BlankPrevious.Add(new StrongBox<long>(0));
				}

				_LastTicks = BlankTicks.ToImmutable();
				_Current = BlankCurrent.ToImmutable();
				_Previous = BlankPrevious.ToImmutable();
			}

			private StatisticsState(ImmutableArray<TimeSpan> lastTicks, ImmutableArray<StrongBox<long>> current, ImmutableArray<StrongBox<long>> previous)
			{
				_LastTicks = lastTicks;
				_Current = current;
				_Previous = previous;
			}

			//****************************************

			internal StatisticsState Prepare(TimeSpan ticks, IReadOnlyList<TimeSpan> intervals)
			{
				ImmutableArray<TimeSpan>.Builder? LastTicks = null;
				ImmutableArray<StrongBox<long>>.Builder? Current = null, Previous = null;

				for (var Index = 0; Index < _LastTicks.Length; Index++)
				{
					var IntervalTicks = intervals[Index];

					if (IntervalTicks == TimeSpan.Zero)
						continue;

					var IntervalEnds = _LastTicks[Index] + IntervalTicks;

					if (IntervalEnds <= ticks)
					{
						// This interval has elapsed and needs rolling over
						if (LastTicks == null)
						{
							LastTicks = _LastTicks.ToBuilder();
							Previous = _Previous.ToBuilder();
							Current = _Current.ToBuilder();
						}

						// Round to the nearest Interval
						LastTicks[Index] = RoundTo(ticks, IntervalTicks);
						// If it's been more than one interval since we last ticked over, the previous should be zero
						Previous![Index] = IntervalEnds + IntervalTicks <= ticks ? new StrongBox<long>(0) : Current![Index];
						// The current interval peak becomes the value of the previous interval
						Current![Index] = new StrongBox<long>(0);
					}
				}

				if (LastTicks == null)
					return this;

				return new StatisticsState(LastTicks.ToImmutable(), Current!.ToImmutable(), Previous!.ToImmutable());
			}

			internal void Add(long value)
			{
				for (var Index = 0; Index < _Current.Length; Index++)
				{
					Interlocked.Add(ref _Current[Index].Value, value);
				}
			}

			internal void Peak(long value)
			{
				for (var Index = 0; Index < _Current.Length; Index++)
				{
					ref var Value = ref _Current[Index].Value;

					long OldValue;

					do
					{
						OldValue = Value;

						if (OldValue >= value)
							return;
					}
					while (Interlocked.CompareExchange(ref Value, value, OldValue) < value);
				}
			}

			internal StatisticsState Reset(TimeSpan ticks, IReadOnlyList<TimeSpan> intervals)
			{
				var LastTicks = ImmutableArray.CreateBuilder<TimeSpan>(_LastTicks.Length);
				var BlankCurrent = ImmutableArray.CreateBuilder<StrongBox<long>>(_LastTicks.Length);
				var BlankPrevious = ImmutableArray.CreateBuilder<StrongBox<long>>(_LastTicks.Length);

				for (var Index = 0; Index < intervals.Count; Index++)
				{
					var IntervalTicks = intervals[Index];

					// Every Last Tick should be 'now'
					LastTicks.Add(IntervalTicks == TimeSpan.Zero ? ticks : RoundTo(ticks, IntervalTicks));
					// Current and Previous should be zero
					BlankCurrent.Add(new StrongBox<long>(0));
					BlankPrevious.Add(new StrongBox<long>(0));
				}

				return new StatisticsState(
					LastTicks.ToImmutable(),
					BlankCurrent.ToImmutable(),
					BlankPrevious.ToImmutable()
					);
			}

			//****************************************

			public (TimeSpan ticks, long current, long previous) this[int index] => (_LastTicks[index], _Current[index].Value, _Previous[index].Value);

			//****************************************

			private static TimeSpan RoundTo(TimeSpan value, TimeSpan nearest)
			{
				return new TimeSpan(value.Ticks - (value.Ticks % nearest.Ticks));
			}
		}
	}
}
