/****************************************\
 OperationProviderAttribute.cs
 Created: 2011-09-28
\****************************************/
using System;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Identifies a class belonging to a specific action group
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class ActionGroupAttribute : Attribute
	{	//****************************************
		private string _Name;
		//****************************************
		
		/// <summary>
		/// Identifies a class belonging to a specific action group
		/// </summary>
		/// <param name="name">The name of the group to join</param>
		public ActionGroupAttribute(string name)
		{
			_Name = name;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the name of the action group this class belongs to
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
	}
}
