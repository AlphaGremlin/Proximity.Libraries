/****************************************\
 ConfigEnumerator.cs
 Created: 12-09-2009
\****************************************/
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
		private IEnumerator InnerEnumerator;
		//****************************************
		
		internal ConfigEnumerator(ICollection outputSet)
		{
			InnerEnumerator = outputSet.GetEnumerator();
		}
		
		//****************************************
		
		void IDisposable.Dispose()
		{
			
		}
		
		public bool MoveNext()
		{
			return InnerEnumerator.MoveNext();
		}
		
		public void Reset()
		{
			InnerEnumerator.Reset();
		}
		
		//****************************************
		
		object IEnumerator.Current
		{
			get { return InnerEnumerator.Current; }
		}
		
		public TValue Current
		{
			get { return (TValue)InnerEnumerator.Current; }
		}
	}
}
