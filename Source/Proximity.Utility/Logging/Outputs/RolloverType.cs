/****************************************\
 RolloverType.cs
 Created: 2014-07-29
\****************************************/
#if !MOBILE && !PORTABLE
using System;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Identifies the condition under which the log file rolls over
	/// </summary>
	public enum RolloverType
	{
		/// <summary>
		/// A new log file is created only when the process begins
		/// </summary>
		/// <remarks>The file suffix is the date and time the file was created</remarks>
		Startup,
		/// <summary>
		/// A new log file is created when the size reaches the threshold
		/// </summary>
		/// <remarks>The file suffix is the date and time the file was created</remarks>
		Size,
		/// <summary>
		/// A new log file is created when the local time ticks over to a new day
		/// </summary>
		/// <remarks>The file suffix is the date the file was created</remarks>
		Daily,
		/// <summary>
		/// A new log file is created when the local time ticks over to a new week (Sunday morning)
		/// </summary>
		/// <remarks>The file suffix is the date the file was created</remarks>
		Weekly,
		/// <summary>
		/// A new log file is created when the local time ticks over to a new month
		/// </summary>
		/// <remarks>The file suffix is the date the file was created</remarks>
		Monthly,
		/// <summary>
		/// A new log file is only created once
		/// </summary>
		/// <remarks>There is no file suffix or extension added</remarks>
		Fixed
	}
}
#endif