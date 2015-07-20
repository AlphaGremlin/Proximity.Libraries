/****************************************\
 WeakEventArgs.cs
 Created: 26-08-2010
\****************************************/
using System;
using System.Threading;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Provides event arguments to communicate success/failure back to the raising test
	/// </summary>
	public class WeakEventArgs : EventArgs
	{	//****************************************
		private int _WasInvoked = 0;
		//****************************************
		
		public WeakEventArgs()
		{
		}
		
		//****************************************
		
		public void Increment()
		{
			Interlocked.Increment(ref _WasInvoked);
		}
		
		public int InvokeCount
		{
			get { return _WasInvoked; }
		}
	}
}
