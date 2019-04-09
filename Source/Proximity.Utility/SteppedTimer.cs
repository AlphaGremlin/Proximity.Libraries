using System;
using System.Diagnostics;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Provides a precision timing system
	/// </summary>
	public class SteppedTimer
	{	//****************************************
		private Stopwatch _Timer;
		private long _Remainder, _LastTicks, _Frequency;

		private DateTime _Now;
		private TimeSpan _Elapsed;
		//****************************************
		
		/// <summary>
		/// Creates a new Stepped Timer
		/// </summary>
		public SteppedTimer()
		{
			_Timer = new Stopwatch();
			_LastTicks = 0;
			_Frequency = Stopwatch.Frequency;
			
			_Timer.Start();
			_Now = DateTime.Now;
			_Elapsed = TimeSpan.Zero;
		}
		
		//****************************************
		
		/// <summary>
		/// Steps the timer, updating <see cref="Now" /> and <see cref="Elapsed" />
		/// </summary>
		public void Step()
		{	//****************************************
			long TotalTicks = _Timer.ElapsedTicks;
			long ElapsedTicks = TotalTicks - _LastTicks;
			//****************************************

			// Calculate a TimeSpan representing the time between Now and the last Step
			// As the Stopwatch has a higher frequency per second than TimeSpan,
			// we keep a remainder so as not to lose fractions of a second
			_Elapsed = TimeSpan.FromTicks(Math.DivRem(ElapsedTicks * TimeSpan.TicksPerSecond + _Remainder, _Frequency, out _Remainder));
			
			_LastTicks = TotalTicks;
			
			_Now = _Now.Add(_Elapsed);
		}
		
		/// <summary>
		/// Stops the timer
		/// </summary>
		public void Close()
		{
			_Timer.Stop();
			_Timer = null;
		}

		//****************************************

		/// <summary>
		/// Gets the last time <see cref="Step"/> was called
		/// </summary>
		public DateTime Now => _Now;

		/// <summary>
		/// Gets the time between the two previous calls to <see cref="Step"/>
		/// </summary>
		public TimeSpan Elapsed => _Elapsed;

		/*
		/// <summary>
		/// Gets the exact time at the moment
		/// </summary>
		public DateTime NowExact
		{
			get { return _Now.AddTicks(((_Timer.ElapsedTicks - _LastTicks) * TimeSpan.TicksPerSecond + _Remainder) / _Frequency); }
		}
		*/

		//****************************************

		/// <summary>
		/// Gets whether the High Precision hardware timer is being used
		/// </summary>
		public static bool IsHighPrecision => Stopwatch.IsHighResolution;
	}
}