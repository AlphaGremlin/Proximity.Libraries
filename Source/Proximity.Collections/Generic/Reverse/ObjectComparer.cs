using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Collections.Reverse
{
	/// <summary>
	/// Provides a reversed comparer for types with no IComparable implementation
	/// </summary>
	internal sealed class ObjectComparer<TValue> : ReverseComparer<TValue>
	{
		public ObjectComparer()
		{
		}
		
		//****************************************
		
		public override int Compare(TValue x, TValue y)
		{
			return Comparer<TValue>.Default.Compare(y, x);
		}
	}
}
