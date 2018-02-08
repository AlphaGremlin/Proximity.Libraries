/****************************************\
 ConfigEnumerator.cs
 Created: 12-09-2009
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Configuration
{
	/// <summary>
	/// Generic enumerator for configuration element collections
	/// </summary>
	internal struct ConfigEnumerator<TValue> : IEnumerator<TValue>
	{	//****************************************
		private readonly IEnumerator _InnerEnumerator;
		//****************************************
		
		internal ConfigEnumerator(ICollection outputSet)
		{
			_InnerEnumerator = outputSet.GetEnumerator();
		}
		
		//****************************************
		
		void IDisposable.Dispose()
		{
			
		}
		
		public bool MoveNext()
		{
			return _InnerEnumerator.MoveNext();
		}
		
		public void Reset()
		{
			_InnerEnumerator.Reset();
		}
		
		//****************************************
		
		object IEnumerator.Current
		{
			get { return _InnerEnumerator.Current; }
		}
		
		public TValue Current
		{
			get { return (TValue)_InnerEnumerator.Current; }
		}
	}
}
#endif