﻿using System;
//****************************************

namespace Proximity.Logging
{
	/// <summary>
	/// Identifies the severity of an entry in the log
	/// </summary>
	public enum Severity
	{
		/// <summary>
		/// No severity specified
		/// </summary>
		None = 0,
		/// <summary>
		/// Debug level
		/// </summary>
		Debug,
		/// <summary>
		/// Verbose level
		/// </summary>
		Verbose,
		/// <summary>
		/// Information level
		/// </summary>
		Info,
		/// <summary>
		/// Milestone level
		/// </summary>
		Milestone,
		/// <summary>
		/// Warning level
		/// </summary>
		Warning,
		/// <summary>
		/// Error level
		/// </summary>
		Error,
		/// <summary>
		/// Critical error level
		/// </summary>
		Critical
	}
}