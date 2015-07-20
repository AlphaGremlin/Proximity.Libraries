/****************************************\
 LogCategory.cs
 Created: 4-06-2009
\****************************************/
#if !MOBILE && !PORTABLE
using System;
//****************************************

namespace Proximity.Utility.Logging
{
	/// <summary>
	/// A Category applied to Log Entries
	/// </summary>
	public class LogCategory
	{	//****************************************
		private string _Name;
		//****************************************
		
		/// <summary>
		/// Creates a new logging category
		/// </summary>
		/// <param name="name">The category name to assign</param>
		public LogCategory(string name)
		{
			_Name = name;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the name of this category
		/// </summary>
		public string Name
		{
			get { return _Name; }
		}
	}
}
#endif