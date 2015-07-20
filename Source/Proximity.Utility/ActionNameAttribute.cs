/****************************************\
 ActionNameAttribute.cs
 Created: 2011-09-28
\****************************************/
using System;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Identifies a named action
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class ActionNameAttribute : Attribute
	{	//****************************************
		private string _Name;
		//****************************************
		
		/// <summary>
		/// Identifies a named action
		/// </summary>
		/// <param name="name">The short name of this action method</param>
		public ActionNameAttribute(string name)
		{
			_Name = name;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the short name of this action method
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
	}
}
