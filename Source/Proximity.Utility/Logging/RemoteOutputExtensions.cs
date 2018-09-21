﻿/****************************************\
 RemoteOutput.cs
 Created: 2-06-2009
\****************************************/
#if !NETSTANDARD1_3 && !NETSTANDARD2_0
using System;
using Proximity.Utility.Logging.Outputs;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// Extension methods for the Remote Output
	/// </summary>
	public static class RemoteOutputExtensions
	{
		/// <summary>
		/// Creates a Log Output that can be attached in a child AppDomain to route logging data to a parent AppDomain
		/// </summary>
		/// <param name="receiver">A Remote Output receiver passed from the parent AppDomain</param>
		/// <returns>A Log Output to pass to <see cref="LogManager.AddOutput"/></returns>
		public static LogOutput ToSender(this RemoteOutput receiver)
		{
			return new RemoteOutput.SendOutput(receiver);
		}
	}
}
#endif