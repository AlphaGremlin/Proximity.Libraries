/****************************************\
 WrappedComparer.cs
 Created: 2012-05-22
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Provides a reversed comparer wrapping an existing comparer
	/// </summary>
	internal sealed class WrappedComparer<TValue> : ReverseComparer<TValue>
	{	//****************************************
		private IComparer<TValue> _Comparer;
		//****************************************
		
		internal WrappedComparer(IComparer<TValue> comparer)
		{
			_Comparer = comparer;
		}
		
		//****************************************
		
		public override int Compare(TValue x, TValue y)
		{
			return _Comparer.Compare(y, x);
		}
	}
}
