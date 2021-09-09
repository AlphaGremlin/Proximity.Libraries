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
		private readonly Dictionary<string, Statistic> _Statistics = new();
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
			_Intervals = (intervals ?? throw new ArgumentNullException(nameof(intervals))).ToArray();

			foreach (var Interval in _Intervals)
			{
				if (Interval < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(intervals));
			}

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
			_Statistics.Add(name, new Statistic(_Intervals));
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
				Statistic.Reset(Ticks);
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

			Statistic.Add(GetTicks(), value);
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

			Statistic.Peak(GetTicks(), value);
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
				values[Index] = GetCurrentValue(Records[Index], CurrentTicks).Value;
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

			return GetCurrentValue(Statistic.Records[Index], GetTicks()).Value;
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
				values[Index] = GetCurrentValue(Records[Index], CurrentTicks);
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

			return GetCurrentValue(Statistic.Records[Index], GetTicks());
		}

		//****************************************

		private StatisticsRecord GetCurrentValue(in StatisticsState state, TimeSpan time)
		{
			var Wait = new SpinWait();

			StatisticsState State;
			long Current;

			for (; ; )
			{
				State = state; // Can't use Volatile.Read since it needs 'ref', not 'in'
				Thread.MemoryBarrier();
				Current = State.Current;

				if (Current != -1)
					break;

				Wait.SpinOnce();
			}

			var Interval = State.Interval;

			if (Interval == TimeSpan.Zero)
				return new StatisticsRecord(Current, time - State.Time);

			var NextInterval = State.Time + Interval;

			// Determine when the currently active interval ends
			if (NextInterval > time)
				// The current interval has yet to elapse, so we return the result from the previous interval
				return new StatisticsRecord(State.Previous, Interval);
			else if (NextInterval + Interval > time)
				// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
				return new StatisticsRecord(Current, Interval);
			else
				// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
				return new StatisticsRecord(0, Interval);
		}

		private TimeSpan GetTicks() => _Timer.Elapsed;

		//****************************************

		/// <summary>
		/// Gets the list of intervals supported by this Statistics tracker
		/// </summary>
		public IReadOnlyList<TimeSpan> Intervals => _Intervals;

		/// <summary>
		/// Gets the time the statistics were last reset
		/// </summary>
		public DateTime LastReset => new(_StartTime.Ticks, DateTimeKind.Local);

		/// <summary>
		/// Gets the registered statistic names
		/// </summary>
		public IReadOnlyCollection<string> Metrics => _Statistics.Keys;

		//****************************************

		private readonly struct Statistic
		{ //****************************************
			private readonly StatisticsState[] _Statistics;
			//****************************************

			internal Statistic(TimeSpan[] intervals)
			{
				_Statistics = new StatisticsState[intervals.Length];

				for (var Index = 0; Index < intervals.Length; Index++)
				{
					_Statistics[Index] = new StatisticsState(intervals[Index], TimeSpan.Zero);
				}
			}

			//****************************************

			internal void Add(TimeSpan time, long value)
			{
				for (var Index = 0; Index < _Statistics.Length; Index++)
				{
					StatisticsState.Add(ref _Statistics[Index], time, value);
				}
			}

			internal void Peak(TimeSpan time, long value)
			{
				for (var Index = 0; Index < _Statistics.Length; Index++)
				{
					StatisticsState.Peak(ref _Statistics[Index], time, value);
				}
			}

			internal void Reset(TimeSpan time)
			{
				for (var Index = 0; Index < _Statistics.Length; Index++)
				{
					StatisticsState.Reset(ref _Statistics[Index], time);
				}
			}

			//****************************************

			public ReadOnlySpan<StatisticsState> Records => _Statistics;
		}

		private sealed class StatisticsState
		{ //****************************************
			private readonly TimeSpan _Interval;
			private readonly TimeSpan _Time;

			private long _Current;
			private readonly long _Previous;
			//****************************************

			public StatisticsState(TimeSpan interval, TimeSpan time, long current = 0, long previous = 0)
			{
				_Interval = interval;
				_Current = current;
				_Previous = previous;

				if (interval == TimeSpan.Zero)
					_Time = time;
				else
					_Time = RoundTo(time, interval);
			}

			//****************************************

			public TimeSpan Time => _Time;

			public TimeSpan Interval => _Interval;

			public long Current => _Current;

			public long Previous => _Previous;

			//****************************************

			internal static void Reset(ref StatisticsState state, TimeSpan time)
			{
				var NewState = new StatisticsState(state.Interval, time);

				Interlocked.Exchange(ref state, NewState);
			}

			internal static void Add(ref StatisticsState state, TimeSpan time, long value)
			{
				StatisticsState State;
				var Wait = new SpinWait();

				for (; ; )
				{
					State = Volatile.Read(ref state);

					var Interval = State._Interval;

					if (Interval == TimeSpan.Zero)
					{
						Interlocked.Add(ref State._Current, value);

						return;
					}

					var Finish = State._Time + Interval;

					if (time < Finish)
					{
						// Still within the time interval
						if (Interlocked.Add(ref State._Current, value) >= value)
							return;

						// If the result is less than our input value, this state has been expired (added to -1)
						// Restore the expired state. Since our previous addition opens a window where another thread can successfully add,
						// we take the old current value and use it for our own addition
						value = Interlocked.Exchange(ref State._Current, -1) + 1;

						// Wait a moment for the other thread to finish replacing the state
						Wait.SpinOnce();
					}
					else if (time < Finish + Interval)
					{
						// We're within the next time interval, flag the state as expired so we can lock in that interval
						var Previous = Interlocked.Exchange(ref State._Current, -1);

						if (Previous != -1)
						{
							var NewState = new StatisticsState(Interval, time, value, Previous);

							// Replace the current state with a new state
							if (Interlocked.CompareExchange(ref state, NewState, State) == State)
								return;
						}
						else
						{
							// Wait a moment for the other thread to finish replacing the state
							Wait.SpinOnce();
						}

						// Another thread is performing a replacement, wait and try again
					}
					else
					{
						// Two intervals have passed since this state began recording, so we replace with a zero previous record
						var NewState = new StatisticsState(Interval, time, value);

						// Replace the current state with a new state
						if (Interlocked.CompareExchange(ref state, NewState, State) == State)
							return;

						// Another thread is performing a replacement, wait and try again
					}
				}
			}

			internal static void Peak(ref StatisticsState state, TimeSpan time, long value)
			{
				StatisticsState State;
				var Wait = new SpinWait();

				for (; ; )
				{
					State = Volatile.Read(ref state);

					var Interval = State._Interval;

					if (Interval == TimeSpan.Zero)
					{
						var OldValue = Volatile.Read(ref State._Current);

						if (OldValue >= value || Interlocked.CompareExchange(ref State._Current, value, OldValue) >= value)
							return;

						continue;
					}

					var Finish = State._Time + Interval;

					if (time < Finish)
					{
						// Still within the time interval
						var OldValue = Volatile.Read(ref State._Current);

						if (OldValue == -1)
						{
							// Expired. Wait a moment for the other thread to finish replacing the state
							Wait.SpinOnce();
						}
						else
						{
							if (OldValue >= value || Interlocked.CompareExchange(ref State._Current, value, OldValue) >= value)
								return;

							// Another thread replaced our value with something greater than the previous peak, but less than our new peak
						}
					}
					else if (time < Finish + Interval)
					{
						// We're within the next time interval, flag the state as expired so we can lock in that interval
						var Previous = Interlocked.Exchange(ref State._Current, -1);

						if (Previous != -1)
						{
							var NewState = new StatisticsState(Interval, time, value, Previous);

							// Replace the current state with a new state
							if (Interlocked.CompareExchange(ref state, NewState, State) == State)
								return;
						}
						else
						{
							// Wait a moment for the other thread to finish replacing the state
							Wait.SpinOnce();
						}

						// Another thread is performing a replacement, wait and try again
					}
					else
					{
						// Two intervals have passed since this state began recording, so we replace with a zero previous record
						var NewState = new StatisticsState(Interval, time, value);

						// Replace the current state with a new state
						if (Interlocked.CompareExchange(ref state, NewState, State) == State)
							return;

						// Another thread is performing a replacement, wait and try again
					}
				}
			}

			//****************************************

			private static TimeSpan RoundTo(TimeSpan value, TimeSpan nearest)
			{
				return new TimeSpan(value.Ticks - (value.Ticks % nearest.Ticks));
			}
		}
	}
}
