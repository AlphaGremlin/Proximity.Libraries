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
		/// <param name="instanceType">The name to identify instances of this class registered with the Terminal</param>
		public TerminalProviderAttribute(string instanceType)
		{
			InstanceType = instanceType;
		}
		
		/// <summary>
		/// Identifies a reference class that provides terminal commands
		/// </summary>
		/// <param name="instanceType">The name to identify instances of this class registered with the Terminal</param>
		/// <param name="isDefault">Whether this instance acts as the default target for this instance type</param>
		public TerminalProviderAttribute(string instanceType, bool isDefault)
		{
			InstanceType = instanceType;
			IsDefault = isDefault;
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the name to identify instances of this class registered with the Terminal
		/// </summary>
		public string InstanceType { get; set; }

		/// <summary>
		/// Gets/Sets whether this instance acts as the default target for this instance type
		/// </summary>
		public bool IsDefault { get; set; }
	}
}
