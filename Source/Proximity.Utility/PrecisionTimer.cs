/****************************************\
 PrecisionTimer.cs
 Created: 2010-09-18
\****************************************/
using System;
using System.Diagnostics;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Provides a precision timing system
	/// </summary>
	public class PrecisionTimer
	{	//****************************************
		private Stopwatch _Timer;

		private DateTime _Now, _LastNow;
		//****************************************
		
		/// <summary>
		/// Creates a new Precision Timer
		/// </summary>
		public PrecisionTimer()
		{
			_Timer = new Stopwatch();
			
			_Now = DateTime.Now;
			_Timer.Start();
		}
		
		//****************************************
				
		/// <summary>
		/// Retrieves the current time at the highest precision possible
		/// </summary>
		/// <returns>The current time</returns>
		public DateTime GetTime()
		{
			lock (_Timer)
			{
				DateTime NewNow = _Now.AddTicks(_Timer.Elapsed.Ticks);
				
				// Timer can get out of sync, reset it after 1 minute of inactivity
				if (NewNow.Subtract(_LastNow).TotalMinutes > 1.0)
				{
					_Timer.Reset();
					_Now = DateTime.Now;
					_Timer.Start();
				}
				else if (NewNow < _LastNow) // Gone back in time! This is a bug in the HAL/BIOS
				{
					// Probably doesn't fix it, but it should keep things close to reasonable
					_Timer.Reset();
					_Now = _LastNow = DateTime.Now;
					_Timer.Start();
					
					return _Now;
				}
				
				_LastNow = NewNow;
				
				return NewNow;
			}
		}
		
		/*
		/// <summary>
		/// Retrieves the current time at the highest precision possible
		/// </summary>
		/// <returns>The current time</returns>
		public DateTime GetTime()
		{	//****************************************
			long TotalTicks = _Timer.ElapsedTicks;
			long ElapsedTicks = TotalTicks - _LastTicks;
			//****************************************

			// Update our current time with the amount of elapsed ticks
			// As the Stopwatch has a higher frequency per second than TimeSpan,
			// we keep a remainder so as not to lose fractions of a second
			_Now = _Now.AddTicks(Math.DivRem(ElapsedTicks * TimeSpan.TicksPerSecond + _Remainder, _Frequency, out _Remainder));
			
			_LastTicks = TotalTicks;
			
			return _Now;
		}
		*/
		
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
		/// Gets whether the High Precision hardware timer is being used
		/// </summary>
		public static bool IsHighPrecision
		{
			get { return Stopwatch.IsHighResolution; }
		}
	}
}
