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
		/// <param name="typeName">The name to group instances of this class registered with the Terminal</param>
		public TerminalProviderAttribute(string typeName)
		{
			TypeName = typeName;
		}

		/// <summary>
		/// Identifies a reference class that provides terminal commands
		/// </summary>
		/// <param name="typeName">The name to group instances of this class registered with the Terminal</param>
		/// <param name="isDefault">Whether this instance acts as the default target for this instance type</param>
		public TerminalProviderAttribute(string typeName, bool isDefault)
		{
			TypeName = typeName;
			IsDefault = isDefault;
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the name to group instances of this class registered with the Terminal
		/// </summary>
		public string TypeName { get; set; }

		/// <summary>
		/// Gets/Sets whether this instance acts as the default target for this instance type
		/// </summary>
		/// <remarks>Only one class can be registered as the default at any one time</remarks>
		public bool IsDefault { get; set; }
	}
}
