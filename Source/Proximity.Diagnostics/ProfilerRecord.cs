using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Diagnostics
{
	/// <summary>
	/// Represents a profiler record
	/// </summary>
	public readonly struct ProfilerRecord
	{ //****************************************
		private readonly long _Elapsed;
		private readonly long _Minimum, _Maximum;
		//****************************************

		internal ProfilerRecord(int samples, long elapsed, long minimum, long maximum)
		{
			Samples = samples;
			_Elapsed = elapsed;
			_Minimum = minimum;
			_Maximum = maximum;
		}

		//****************************************

		internal ProfilerRecord Add(long elapsed) => new ProfilerRecord(Samples + 1, _Elapsed + elapsed, Math.Min(_Minimum, elapsed), Math.Max(_Maximum, elapsed));

		//****************************************

		/// <summary>
		/// Gets the number of samples included in this interval
		/// </summary>
		public int Samples { get; }

		/// <summary>
		/// Gets the total amount of time elapsed in this interval
		/// </summary>
		public TimeSpan Elapsed => new TimeSpan(_Elapsed);

		/// <summary>
		/// Gets the largest sample in this interval
		/// </summary>
		public TimeSpan Maximum => new TimeSpan(_Maximum);

		/// <summary>
		/// Gets the smallest sample in this interval
		/// </summary>
		public TimeSpan Minimum => new TimeSpan(_Minimum);

		//****************************************

		internal static ProfilerRecord Empty { get; } = new ProfilerRecord(0, 0, long.MaxValue, 0);
	}
}
