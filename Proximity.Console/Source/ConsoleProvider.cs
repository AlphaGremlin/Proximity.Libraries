/****************************************\
 ConsoleProvider.cs
 Created: 31-01-2008
\****************************************/
using System;
//****************************************

namespace Proximity.Console
{
	/// <summary>
	/// Defines a class that provides information for the console
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ConsoleProviderAttribute : Attribute
	{
		/// <summary>
		/// Defines a class that provides information for the console
		/// </summary>
		public ConsoleProviderAttribute()
		{
		}
	}
}
