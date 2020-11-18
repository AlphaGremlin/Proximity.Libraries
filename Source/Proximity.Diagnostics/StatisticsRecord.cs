using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Diagnostics
{
	/// <summary>
	/// Describes a statistics value
	/// </summary>
	public readonly struct StatisticsRecord
	{
		internal StatisticsRecord(long value, TimeSpan elapsed)
		{
			Value = value;
			Elapsed = elapsed;
		}

		//****************************************

		/// <summary>
		/// Gets the average values per hour for this record
		/// </summary>
		public double PerHour => Value / Elapsed.TotalHours;

		/// <summary>
		/// Gets the average values per millisecond for this record
		/// </summary>
		public double PerMillisecond => Value / Elapsed.TotalMilliseconds;

		/// <summary>
		/// Gets the average values per minute for this record
		/// </summary>
		public double PerMinute => Value / Elapsed.TotalMinutes;

		/// <summary>
		/// Gets the average values per second for this record
		/// </summary>
		public double PerSecond => Value / Elapsed.TotalSeconds;

		/// <summary>
		/// Gets the total value of this record
		/// </summary>
		public long Value { get; }

		/// <summary>
		/// Gets the total time elapsed when building this record
		/// </summary>
		public TimeSpan Elapsed { get; }
	}
}
