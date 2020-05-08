using System;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Describes the binding of a property or method to a terminal command
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class TerminalBindingAttribute : Attribute
	{
		/// <summary>
		/// Creates a new terminal binding
		/// </summary>
		public TerminalBindingAttribute()
		{
		}
		
		/// <summary>
		/// Creates a new terminal binding
		/// </summary>
		/// <param name="description">The description to use for this binding</param>
		public TerminalBindingAttribute(string description)
		{
			Name = null;
			Description = description;
		}
		
		/// <summary>
		/// Creates a new terminal binding
		/// </summary>
		/// <param name="name">The name to use for this binding</param>
		/// <param name="description">The description to use for this binding</param>
		public TerminalBindingAttribute(string name, string description)
		{
			Name = name;
			Description = description;
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the name to use for this binding
		/// </summary>
		/// <remarks>If omitted, defaults to the name of the method or property</remarks>
		public string Name { get; set; }

		/// <summary>
		/// Gets/Sets the description of this binding
		/// </summary>
		/// <remarks>If the Provider specifies a resource manager, this is the index to the localised description text</remarks>
		public string Description { get; set; }
	}
}
