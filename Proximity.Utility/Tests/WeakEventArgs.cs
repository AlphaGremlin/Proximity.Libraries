/****************************************\
 WeakEventArgs.cs
 Created: 26-08-2010
\****************************************/
using System;
using System.Collections.Generic;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Provides event arguments to communicate success/failure back to the raising test
	/// </summary>
	public class WeakEventArgs : EventArgs
	{	//****************************************
		private bool _WasInvoked = false;
		//****************************************
		
		public WeakEventArgs()
		{
		}
		
		//****************************************
		
		public bool WasInvoked
		{
			get { return _WasInvoked; }
			set { _WasInvoked = true; }
		}
	}
}
