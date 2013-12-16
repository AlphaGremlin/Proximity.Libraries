/****************************************\
 Profiler.cs
 Created: 2009-09-27
\****************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Provides methods for profiling a program
	/// </summary>
	public sealed class Profiler
	{	//****************************************
		private readonly Stopwatch _SystemTimer;
		private long _SystemTimeRem, _SystemTimeTicks, _SystemTimeFreq;
		private DateTime _Now;
		
		private readonly Dictionary<string, Section> _Sections = new Dictionary<string, Section>();
		//****************************************
		
		/// <summary>
		/// Creates a new Profiler
		/// </summary>
		public Profiler()
		{
			_SystemTimer = new Stopwatch();
			_SystemTimeTicks = 0;
			_SystemTimeFreq = Stopwatch.Frequency;
				
			_SystemTimer.Start();
			_Now = DateTime.Now;
		}
		
		//****************************************
		
		/// <summary>
		/// Starts a profiling section
		/// </summary>
		/// <param name="name">The name of the section to profile</param>
		/// <returns>A Section object that represents this section</returns>
		/// <remarks>To finish a section, call Dispose, or call in a Using block</remarks>
		public Section StartSection(string name)
		{	//****************************************
			Section MySection;
			//****************************************
			
			if (!_Sections.TryGetValue(name, out MySection))
				_Sections.Add(name, MySection = new Section(this, name));
			
			MySection.Start();
			
			return MySection;
		}
		
		/// <summary>
		/// Gets a profiling section
		/// </summary>
		/// <param name="name">The name of the section being profiled</param>
		/// <returns>The Section object that represents this section</returns>
		public Section GetSection(string name)
		{	//****************************************
			Section MySection;
			//****************************************
			
			if (!_Sections.TryGetValue(name, out MySection))
				_Sections.Add(name, MySection = new Section(this, name));
			
			return MySection;
		}
		
		//****************************************
		
		private void Step()
		{	//****************************************
			long TotalTicks = _SystemTimer.ElapsedTicks;
			long ElapsedTicks = TotalTicks - _SystemTimeTicks;
			TimeSpan ElapsedTime;
			//****************************************

			ElapsedTime = new TimeSpan(Math.DivRem(ElapsedTicks * TimeSpan.TicksPerSecond + _SystemTimeRem, _SystemTimeFreq, out _SystemTimeRem));
			
			_SystemTimeTicks = TotalTicks;
			
			_Now = _Now.Add(ElapsedTime);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the exact time, down to the tick
		/// </summary>
		public DateTime NowExact
		{
			get { return _Now.AddTicks(((_SystemTimer.ElapsedTicks - _SystemTimeTicks) * TimeSpan.TicksPerSecond + _SystemTimeRem) / _SystemTimeFreq); }
		}
		
		/// <summary>
		/// Gets a list of all current sections
		/// </summary>
		public ICollection<Section> Sections
		{
			get { return (ICollection<Section>)_Sections.Values; }
		}
				
		//****************************************

		/// <summary>
		/// Provides Profiling services for a section of code
		/// </summary>
		public class Section : IDisposable
		{	//****************************************
			private Profiler _Owner;
			private string _Name;
			
			private TimeSpan _Elapsed;
			
			private DateTime _StartTime;
			//****************************************
			
			internal Section(Profiler owner, string name)
			{
				_Owner = owner;
				_Name = name;
			}
			
			//****************************************
			
			void IDisposable.Dispose()
			{
				_Elapsed = _Elapsed.Add(_Owner.NowExact.Subtract(_StartTime));
			}
			
			//****************************************
			
			internal void Start()
			{
				_StartTime = _Owner.NowExact;
			}
			
			//****************************************
			
			/// <summary>
			/// Provides a text representation of this Section
			/// </summary>
			/// <returns>A string describing the section</returns>
			public override string ToString()
			{
				return string.Format("{0}: {1,4:F1}", _Name, _Elapsed.TotalMilliseconds);
			}
			
			//****************************************
			
			/// <summary>
			/// Gets the name of this Section
			/// </summary>
			public string Name
			{
				get { return _Name; }
			}
			
			/// <summary>
			/// Gets the total time elapsed during the lifetime of this Section
			/// </summary>
			public TimeSpan Elapsed
			{
				get { return _Elapsed; }
			}
		}
	}
}
