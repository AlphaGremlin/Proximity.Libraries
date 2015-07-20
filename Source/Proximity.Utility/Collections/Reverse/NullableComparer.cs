/****************************************\
 NullableComparer.cs
 Created: 2012-05-22
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections.Reverse
{
	/// <summary>
	/// Provides a generic reversed comparer for nullable value types implementing IComparable
	/// </summary>
	internal sealed class NullableComparer<TValue> : ReverseComparer<TValue?> where TValue : struct, IComparable<TValue>
	{
		public NullableComparer()
		{
		}
		
		//****************************************
		
		public override int Compare(TValue? x, TValue? y)
		{
			if (x == null)
				return (y == null) ? 0 : 1;
			else if (y == null)
				return -1;
			
			return y.Value.CompareTo(x.Value);
		}
	}
}
