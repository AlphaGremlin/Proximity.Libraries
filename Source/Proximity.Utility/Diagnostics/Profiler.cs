/****************************************\
 Profiler.cs
 Created: 2009-09-27
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
//****************************************

namespace Proximity.Utility.Diagnostics
{
	/// <summary>
	/// Provides methods for profiling a program
	/// </summary>
	public sealed class Profiler
	{	//****************************************
		private readonly PrecisionTimer _Timer = new PrecisionTimer();
		
		private readonly ConcurrentDictionary<string, ProfileSection> _Sections = new ConcurrentDictionary<string, ProfileSection>();
		
		private readonly TimeSpan _ShortInterval, _LongInterval;
		//****************************************
		
		/// <summary>
		/// Creates a new Profiler
		/// </summary>
		public Profiler()
		{
			_ShortInterval = new TimeSpan(0, 0, 1);
			_LongInterval = new TimeSpan(0, 1, 0);
		}
		
		/// <summary>
		/// Creates a new Profiler with the provided Short and Long intervals
		/// </summary>
		/// <param name="shortInterval"></param>
		/// <param name="longInterval"></param>
		public Profiler(TimeSpan shortInterval, TimeSpan longInterval)
		{
			_ShortInterval = shortInterval;
			_LongInterval = longInterval;
		}
		
		//****************************************
		
		/// <summary>
		/// Starts a profiling section
		/// </summary>
		/// <param name="name">The name of the section to profile</param>
		/// <returns>An object that represents this profiling event</returns>
		/// <remarks>To finish profiling, call Dispose on the given object, or call in a Using block</remarks>
		public SectionInstance StartSection(string name)
		{
			return _Sections.GetOrAdd(name, CreateSection).Start();
		}
		
		/// <summary>
		/// Gets a profiling section
		/// </summary>
		/// <param name="name">The name of the section being profiled</param>
		/// <returns>The Section object that represents this section</returns>
		public ProfileSection GetSection(string name)
		{
			return _Sections.GetOrAdd(name, CreateSection);
		}
		
		//****************************************
		
		private ProfileSection CreateSection(string name)
		{
			return new ProfileSection(this, name);
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the named Profile Section, or Null if the section does not exist
		/// </summary>
		public ProfileSection this[string name]
		{
			get
			{
				ProfileSection MySection;
				
				if (_Sections.TryGetValue(name, out MySection))
					return MySection;
				
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
		/// Gets a list of all current sections
		/// </summary>
		public ICollection<ProfileSection> Sections
		{
			get { return _Sections.Values; }
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
		
		//****************************************

		/// <summary>
		/// Represents a profiling instance
		/// </summary>
		public struct SectionInstance : IDisposable
		{	//****************************************
			private readonly ProfileSection _Section;
			private readonly DateTime _StartTime;
			//****************************************
			
			internal SectionInstance(ProfileSection section, DateTime startTime)
			{
				_Section = section;
				_StartTime = startTime;
			}
			
			//****************************************
			
			/// <summary>
			/// Completes the profiling instance
			/// </summary>
			public void Dispose()
			{
				_Section.Finish(_StartTime);
			}
			
			//****************************************
			
			/// <summary>
			/// Gets the details of the Section being profiled
			/// </summary>
			public ProfileSection Section
			{
				get { return _Section; }
			}
		}
	}
}
