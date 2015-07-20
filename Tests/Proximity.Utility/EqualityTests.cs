/****************************************\
 EqualityTests.cs
 Created: 2012-07-17
\****************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Proximity.Utility.Reflection;
//****************************************

namespace Proximity.Utility.Tests
{
	/// <summary>
	/// Description of EqualityTests.
	/// </summary>
	[TestFixture()]
	public class EqualityTests
	{
		[Test()]
		public void BasicObjectSimpleEqual()
		{
			var Left = new CustomObjectBasic(0, decimal.Zero, null);
			var Right = new CustomObjectBasic(0, decimal.Zero, null);
			
			Assert.IsTrue(Equality<CustomObjectBasic>.Equals(Left, Right), "Reported Different");
		}
		
		[Test()]
		public void BasicObjectSimpleUnequal()
		{
			var Left = new CustomObjectBasic(0, decimal.Zero, null);
			var Right = new CustomObjectBasic(1, decimal.Zero, null);
			
			Assert.IsFalse(Equality<CustomObjectBasic>.Equals(Left, Right), "Reported Same");
		}
		
		//****************************************
		
		private class CustomObjectBasic
		{	//****************************************
			private int _First;
			private decimal _Second;
			private object _Third;
			//****************************************
			
			public CustomObjectBasic(int first, decimal second, object third)
			{
				_First = first;
				_Second = second;
				_Third = third;
			}
		}
	}
}
