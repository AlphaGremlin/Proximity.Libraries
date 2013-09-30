/****************************************\
 ConsoleMethod.cs
 Created: 30-01-2008
\****************************************/
using System;
//****************************************

namespace Proximity.Console
{
	/// <summary>
	/// Defines information about a console binding (commands or variables)
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class ConsoleBindingAttribute : Attribute
	{	//****************************************
		private string _Description;
		private bool _Persist;
		//****************************************
		
		/// <summary>
		/// Defines information about a console binding
		/// </summary>
		/// <param name="description">The description to use for this binding</param>
		/// <remarks>Persistance defaults to false</remarks>
		public ConsoleBindingAttribute(string description)
		{
			_Description = description;
			_Persist = false;
		}
		
		/// <summary>
		/// Defines information about a console binding
		/// </summary>
		/// <param name="description">The description to use for this binding</param>
		/// <param name="persist">Whether to persist the binding value in a <see cref="ConsoleState" /></param>
		public ConsoleBindingAttribute(string description, bool persist)
		{
			_Description = description;
			_Persist = persist;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the description of this binding
		/// </summary>
		public string Description
		{
			get { return _Description; }
		}
		
		/// <summary>
		/// Gets whether to persist the binding value in a <see cref="ConsoleState" />
		/// </summary>
		public bool Persist
		{
			get { return _Persist; }
		}
	}
}
