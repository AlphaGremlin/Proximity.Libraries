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
		//****************************************

		/// <summary>
		/// Creates a new Profiler with default 0 (all-time), 15, 5 and 1 minute intervals
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
				values[Index] = GetCurrentValue(Records[Index], CurrentTicks);
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

			return GetCurrentValue(Statistic.Records[Index], GetTicks());
		}

		//****************************************

		private ProfilerRecord GetCurrentValue(in SectionState state, TimeSpan time)
		{
			var Wait = new SpinWait();

			SectionState State;
			ProfilerRecord Current;

			for (; ; Wait.SpinOnce())
			{
				State = state; // Can't use Volatile.Read since it needs 'ref', not 'in'

				lock (State)
				{
					if (State.IsExpired)
						continue;

					Current = State.Current;

					break;
				}
			}

			var Interval = State.Interval;

			if (Interval == TimeSpan.Zero)
				return Current;

			var NextInterval = State.Time + Interval;

			// Determine when the currently active interval ends
			if (NextInterval > time)
				// The current interval has yet to elapse, so we return the result from the previous interval
				return State.Previous;
			else if (NextInterval + Interval > time)
				// The current interval has elapsed, but hasn't rolled over, so we return the result for this interval
				return Current;
			else
				// The current interval has elapsed, and the next interval has also elapsed without a roll-over, meaning no events occurred
				return ProfilerRecord.Empty;
		}

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

			private readonly SectionState[] _Records;
			//****************************************

			internal Section(string name, Profiler profiler)
			{
				Name = name;
				_Profiler = profiler;

				var Intervals = profiler._Intervals;

				_Records = new SectionState[Intervals.Length];

				for (var Index = 0; Index < Intervals.Length; Index++)
				{
					_Records[Index] = new SectionState(Intervals[Index], TimeSpan.Zero);
				}
			}

			//****************************************

			internal void Finish(TimeSpan startTime)
			{
				var Ticks = _Profiler.GetTicks();
				var Elapsed = Ticks - startTime;

				for (var Index = 0; Index < _Records.Length; Index++)
				{
					SectionState.Finish(ref _Records[Index], Ticks, Elapsed);
				}
			}

			internal void Reset(TimeSpan time)
			{
				for (var Index = 0; Index < _Records.Length; Index++)
				{
					SectionState.Reset(ref _Records[Index], time);
				}
			}

			//****************************************

			public string Name { get; }

			public ReadOnlySpan<SectionState> Records => _Records;
		}

		internal sealed class SectionState
		{ //****************************************
			private readonly TimeSpan _Interval;
			private readonly TimeSpan _Time;

			private int _IsExpired;
			private ProfilerRecord _Current;
			private readonly ProfilerRecord _Previous;
			//****************************************

			public SectionState(TimeSpan interval, TimeSpan time, ProfilerRecord current = default, ProfilerRecord previous = default)
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

			public ProfilerRecord Current => _Current;

			public ProfilerRecord Previous => _Previous;

			public bool IsExpired => _IsExpired != 0;

			//****************************************

			internal static void Reset(ref SectionState state, TimeSpan time)
			{
				var NewState = new SectionState(state.Interval, time);

				Interlocked.Exchange(ref state, NewState);
			}

			internal static void Finish(ref SectionState state, TimeSpan time, TimeSpan elapsed)
			{
				SectionState State;
				var Elapsed = elapsed.Ticks;

				for (; ; )
				{
					State = Volatile.Read(ref state);

					var Interval = State._Interval;

					if (Interval == TimeSpan.Zero)
					{
						lock (State)
						{
							State._Current = State._Current.Add(Elapsed);
						}

						return;
					}

					var Finish = State._Time + Interval;

					if (time < Finish)
					{
						// Still within the time interval
						lock (State)
						{
							if (!State.IsExpired)
							{
								// Not expired, so update it and return
								State._Current = State._Current.Add(Elapsed);

								return;
							}
						}

						// Another thread is performing a replacement, try again
					}
					else if (time < Finish + Interval)
					{
						// We're within the next time interval, flag the state as expired so we can lock in that interval
						lock (State)
						{
							if (!State.IsExpired)
							{
								State._IsExpired = -1;

								var NewState = new SectionState(Interval, time, ProfilerRecord.Empty.Add(Elapsed), State._Current);

								// Replace the current state with a new state
								if (Interlocked.CompareExchange(ref state, NewState, State) == State)
									return;
							}
						}

						// Another thread is performing a replacement, try again
					}
					else
					{
						// Two intervals have passed since this state began recording, so we replace with a zero previous record
						var NewState = new SectionState(Interval, time, ProfilerRecord.Empty.Add(Elapsed));

						// Replace the current state with a new state
						if (Interlocked.CompareExchange(ref state, NewState, State) == State)
							return;

						// Another thread is performing a replacement, try again
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
