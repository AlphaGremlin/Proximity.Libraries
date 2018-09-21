/****************************************\
 StatisticManager.cs
 Created: 2014-04-09
\****************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Diagnostics
{
	/// <summary>
	/// Provides statistics management routines
	/// </summary>
	public sealed class StatisticManager
	{	//****************************************
		private readonly Dictionary<string, Statistic> _Statistics = new Dictionary<string, Statistic>();
		
		private readonly PrecisionTimer _Timer = new PrecisionTimer();
		
		private readonly TimeSpan _ShortInterval, _LongInterval;
		//****************************************
		
		/// <summary>
		/// Creates a new Statistics manager with the default Short and Long intervals
		/// </summary>
		public StatisticManager()
		{
			_ShortInterval = new TimeSpan(0, 0, 1);
			_LongInterval = new TimeSpan(0, 1, 0);
		}
		
		/// <summary>
		/// Creates a new Statistics manager with the provided Short and Long intervals
		/// </summary>
		/// <param name="shortInterval"></param>
		/// <param name="longInterval"></param>
		public StatisticManager(TimeSpan shortInterval, TimeSpan longInterval)
		{
			_ShortInterval = shortInterval;
			_LongInterval = longInterval;
		}
		
		//****************************************
		
		/// <summary>
		/// Creates a new Statistic for monitoring
		/// </summary>
		/// <param name="name">The name to refer to this Statistic</param>
		/// <returns>The new Statistic object</returns>
		public Statistic Create(string name)
		{	//****************************************
			var MyStatistic = new Statistic(this, name);
			//****************************************
			
			_Statistics.Add(name, MyStatistic);
			
			return MyStatistic;
		}
		
		/// <summary>
		/// Creates a set of new Statistic for monitoring
		/// </summary>
		/// <param name="names">The names to refer to the new Statistics</param>
		public void Create(params string[] names)
		{
			foreach(var MyName in names)
			{
				_Statistics.Add(MyName, new Statistic(this, MyName));
			}
		}
		
		/// <summary>
		/// Adds to a Statistic
		/// </summary>
		/// <param name="name">The name of the Statistic to increase</param>
		/// <param name="count">The number of events to add to the Statistic</param>
		public void Add(string name, int count)
		{	//****************************************
			var CurrentTime = _Timer.GetTime();
			Statistic MyStatistic;
			//****************************************
			
			if (_Statistics.TryGetValue(name, out MyStatistic))
			{
				MyStatistic.Add(CurrentTime, count);
			}
		}
		
		/// <summary>
		/// Adds to a set of Statistics
		/// </summary>
		/// <param name="count">The number of events to add to the Statistics</param>
		/// <param name="names">An array of Statistic names to increase</param>
		public void Add(int count, params string[] names)
		{	//****************************************
			var CurrentTime = _Timer.GetTime();
			Statistic MyStatistic;
			//****************************************
			
			foreach(var MyName in names)
			{
				if (_Statistics.TryGetValue(MyName, out MyStatistic))
				{
					MyStatistic.Add(CurrentTime, count);
				}
			}
		}
		
		/// <summary>
		/// Increments a Statistic
		/// </summary>
		/// <param name="name">The name of the Statistic to increase</param>
		public void Increment(string name)
		{	//****************************************
			var CurrentTime = _Timer.GetTime();
			Statistic MyStatistic;
			//****************************************
			
			if (_Statistics.TryGetValue(name, out MyStatistic))
			{
				MyStatistic.Add(CurrentTime, 1);
			}
		}
		
		/// <summary>
		/// Increments a set of Statistics
		/// </summary>
		/// <param name="names">An array of Statistic names to increase</param>
		public void Increment(params string[] names)
		{	//****************************************
			var CurrentTime = _Timer.GetTime();
			Statistic MyStatistic;
			//****************************************
			
			foreach(var MyName in names)
			{
				if (_Statistics.TryGetValue(MyName, out MyStatistic))
				{
					MyStatistic.Add(CurrentTime, 1);
				}
			}
		}
		
		/// <summary>
		/// Resets all the total statistics to zero
		/// </summary>
		public void Reset()
		{
			foreach(var MyStatistic in _Statistics.Values)
				MyStatistic.Reset();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the named Statistic
		/// </summary>
		public Statistic this[string name]
		{
			get
			{
				Statistic MyStatistic;
				
				if (_Statistics.TryGetValue(name, out MyStatistic))
					return MyStatistic;
				
				return null;
			}
		}
		
		/// <summary>
		/// Gets the exact time, down to the tick
		/// </summary>
		public DateTime NowExact
		{
			get { return _Timer.GetTime(); }
		}
		
		/// <summary>
		/// Gets the short time between statistics intervals
		/// </summary>
		public TimeSpan ShortInterval
		{
			get { return _ShortInterval; }
		}
		
		/// <summary>
		/// Gets the long time between statistics intervals
		/// </summary>
		public TimeSpan LongInterval
		{
			get { return _LongInterval; }
		}
		
		/// <summary>
		/// Gets a collection of all statistics
		/// </summary>
		public ICollection<Statistic> Statistics
		{
			get { return _Statistics.Values; }
		}
	}
}
