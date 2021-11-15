﻿/****************************************\
 Statistic.cs
 Created: 2014-04-09
\****************************************/
using System;
using System.Collections.Generic;
using System.Threading;
//****************************************

namespace Proximity.Utility.Diagnostics
{
	/// <summary>
	/// Provides tracking services for a statistic
	/// </summary>
	public sealed class Statistic
	{	//****************************************
		private readonly string _Name;
		private readonly StatisticManager _Owner;
		
		private DateTime _ResetLast;
		private long _NextShortUpdate, _NextLongUpdate;
		
		private int _Total;
		private int _ShortRecent, _ShortLast;
		private int _LongRecent, _LongLast;
		//****************************************
		
		internal Statistic(StatisticManager owner, string name)
		{
			_Owner = owner;
			_Name = name;
			
			_ResetLast = _Owner.NowExact;
			_NextShortUpdate = _NextLongUpdate = _ResetLast.Ticks;
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
		
		//****************************************
		
		internal void Add(DateTime currentTime, int count)
		{
			Synchronise(currentTime);
			
			Interlocked.Add(ref _Total, count);
			Interlocked.Add(ref _ShortRecent, count);
			Interlocked.Add(ref _LongRecent, count);
		}
		
		internal void Reset()
		{
			_ResetLast = _Owner.NowExact;
		
			Interlocked.Exchange(ref _Total, 0);
			
			Interlocked.Exchange(ref _NextShortUpdate, 0);
			Interlocked.Exchange(ref _ShortRecent, 0);
			Interlocked.Exchange(ref _ShortLast, 0);
			
			Interlocked.Exchange(ref _NextLongUpdate, 0);
			Interlocked.Exchange(ref _LongRecent, 0);
			Interlocked.Exchange(ref _LongLast, 0);
		}
		
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
		/// Gets the unique name of this Statistic
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
		
		//****************************************
		
		/// <summary>
		/// Represents a record of statistics as at a point in time
		/// </summary>
		public sealed class Record
		{	//****************************************
			private readonly StatisticManager _Owner;
			private readonly DateTime _CurrentTime, _ResetLast;
			
			private readonly int _Total, _TotalShort, _TotalLong;
			//****************************************
			
			internal Record(Statistic statistic)
			{
				_Owner = statistic._Owner;
				_CurrentTime = _Owner.NowExact;
				
				_ResetLast = statistic._ResetLast;
				_Total = statistic._Total;
				_TotalShort = statistic._ShortLast;
				_TotalLong = statistic._LongLast;
			}
			
			//****************************************
			
			/// <summary>
			/// Gets the time at which these statistics are current
			/// </summary>
			public DateTime CurrentTime
			{
				get { return _CurrentTime; }
			}
			
			/// <summary>
			/// Gets the time at which these statistics were last reset
			/// </summary>
			public DateTime ResetLast
			{
				get { return _ResetLast; }
			}
			
			/// <summary>
			/// Gets the amount of time elapsed since the last reset
			/// </summary>
			public TimeSpan Elapsed
			{
				get { return _CurrentTime.Subtract(_ResetLast); }
			}
			
			/// <summary>
			/// Gets the total events since the last reset
			/// </summary>
			public int Total
			{
				get { return _Total; }
			}
			
			/// <summary>
			/// Gets the total events during the last short interval
			/// </summary>
			public int TotalShort
			{
				get { return _TotalShort; }
			}
			
			/// <summary>
			/// Gets the total events during the last long interval
			/// </summary>
			public int TotalLong
			{
				get { return _TotalLong; }
			}
			
			/// <summary>
			/// Gets the average events per second since the last reset
			/// </summary>
			public double Average
			{
				get { return (double)_Total / _CurrentTime.Subtract(_ResetLast).TotalSeconds; }
			}
			
			/// <summary>
			/// Gets the average events per second in the last short interval
			/// </summary>
			public double AverageShort
			{
				get { return (double)_TotalShort / _Owner.ShortInterval.TotalSeconds; }
			}
			
			/// <summary>
			/// Gets the average events per second in the last long interval
			/// </summary>
			public double AverageLong
			{
				get { return (double)_TotalLong / _Owner.LongInterval.TotalSeconds; }
			}
		}
	}
}