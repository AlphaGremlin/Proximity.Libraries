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
		//****************************************

		/// <summary>
		/// Creates a new Profiler
		/// </summary>
		public Profiler()
		{
			ShortInterval = new TimeSpan(0, 0, 1);
			LongInterval = new TimeSpan(0, 1, 0);
		}
		
		/// <summary>
		/// Creates a new Profiler with the provided Short and Long intervals
		/// </summary>
		/// <param name="shortInterval"></param>
		/// <param name="longInterval"></param>
		public Profiler(TimeSpan shortInterval, TimeSpan longInterval)
		{
			ShortInterval = shortInterval;
			LongInterval = longInterval;
		}

		//****************************************

		/// <summary>
		/// Starts a profiling section
		/// </summary>
		/// <param name="name">The name of the section to profile</param>
		/// <returns>An object that represents this profiling event</returns>
		/// <remarks>To finish profiling, call Dispose on the given object, or call in a Using block</remarks>
		public SectionInstance StartSection(string name) => _Sections.GetOrAdd(name, CreateSection).Start();

		/// <summary>
		/// Gets a profiling section
		/// </summary>
		/// <param name="name">The name of the section being profiled</param>
		/// <returns>The Section object that represents this section</returns>
		public ProfileSection GetSection(string name) => _Sections.GetOrAdd(name, CreateSection);

		//****************************************

		private ProfileSection CreateSection(string name) => new ProfileSection(this, name);

		//****************************************

		/// <summary>
		/// Gets the named Profile Section, or Null if the section does not exist
		/// </summary>
		public ProfileSection this[string name]
		{
			get
			{
				if (_Sections.TryGetValue(name, out var Section))
					return Section;
				
				return null;
			}
		}

		/// <summary>
		/// Gets the exact time, down to the tick
		/// </summary>
		public DateTime NowExact => _Timer.GetTime();

		/// <summary>
		/// Gets a list of all current sections
		/// </summary>
		public ICollection<ProfileSection> Sections => _Sections.Values;

		/// <summary>
		/// Gets the short time between statistics intervals
		/// </summary>
		public TimeSpan ShortInterval { get; }

		/// <summary>
		/// Gets the long time between statistics intervals
		/// </summary>
		public TimeSpan LongInterval { get; }

		//****************************************

		/// <summary>
		/// Represents a profiling instance
		/// </summary>
		public readonly struct SectionInstance : IDisposable
		{ //****************************************
			private readonly DateTime _StartTime;
			//****************************************
			
			internal SectionInstance(ProfileSection section, DateTime startTime)
			{
				Section = section;
				_StartTime = startTime;
			}

			//****************************************

			/// <summary>
			/// Completes the profiling instance
			/// </summary>
			public void Dispose() => Section.Finish(_StartTime);

			//****************************************

			/// <summary>
			/// Gets the details of the Section being profiled
			/// </summary>
			public ProfileSection Section { get; }
		}
	}
}
