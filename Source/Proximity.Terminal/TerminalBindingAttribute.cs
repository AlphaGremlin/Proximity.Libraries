/****************************************\
 TerminalBindingAttribute.cs
 Created: 2014-02-28
\****************************************/
using System;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Describes a terminal binding
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class TerminalBindingAttribute : Attribute
	{	//****************************************
		private string _Name;
		private string _Description;
		//****************************************
		
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
			_Name = null;
			_Description = description;
		}
		
		/// <summary>
		/// Creates a new terminal binding
		/// </summary>
		/// <param name="name">The name to use for this binding</param>
		/// <param name="description">The description to use for this binding</param>
		public TerminalBindingAttribute(string name, string description)
		{
			_Name = name;
			_Description = description;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the name to use for this binding
		/// </summary>
		/// <remarks>If omitted, defaults to the name of the method or property</remarks>
		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}
		
		/// <summary>
		/// Gets/Sets the description of this binding
		/// </summary>
		/// <remarks>If the Provider specifies a resource manager, this is the index to the localised description text</remarks>
		public string Description
		{
			get { return _Description; }
			set { _Description = value; }
		}
	}
}
