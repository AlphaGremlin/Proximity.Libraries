﻿/****************************************\
 ObjectComparer.cs
 Created: 2012-05-22
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Description of ObjectComparer.
	/// </summary>
	internal sealed class ObjectComparer<TValue> : ReverseComparer<TValue>
	{
		public ObjectComparer()
		{
		}
		
		//****************************************
		
		public override int Compare(TValue x, TValue y)
		{
			return Comparer.Default.Compare(y, x);
		}
	}
}
