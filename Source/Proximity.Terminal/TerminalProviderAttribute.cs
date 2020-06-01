using System;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Identifies a class that provides terminal commands
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class TerminalProviderAttribute : Attribute
	{
		/// <summary>
		/// Identifies a static class that provides terminal commands
		/// </summary>
		public TerminalProviderAttribute()
		{
		}
		
		/// <summary>
		/// Identifies a reference class that provides terminal commands
		/// </summary>
		/// <param name="typeSet">The name to group instances of this class registered with the Terminal</param>
		public TerminalProviderAttribute(string typeSet)
		{
			TypeSet = typeSet;
		}

		/// <summary>
		/// Identifies a reference class that provides terminal commands
		/// </summary>
		/// <param name="typeSet">The name to group instances of this class registered with the Terminal</param>
		/// <param name="isDefault">Whether this instance acts as the default target for this instance type</param>
		public TerminalProviderAttribute(string typeSet, bool isDefault)
		{
			TypeSet = typeSet;
			IsDefault = isDefault;
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the name to group instances of this class registered with the Terminal
		/// </summary>
		public string? TypeSet { get; set; }

		/// <summary>
		/// Gets/Sets whether an instance of this type acts as the default target for this type set
		/// </summary>
		/// <remarks>Only one instance can be registered as the default at any one time</remarks>
		public bool IsDefault { get; set; }
	}
}
