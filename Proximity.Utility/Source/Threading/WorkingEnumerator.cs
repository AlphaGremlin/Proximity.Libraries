/****************************************\
 WorkingEnumerator.cs
 Created: 2011-08-02
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Generic enumerator for the workling list
	/// </summary>
	internal struct WorkingEnumerator<TValue> : IEnumerator<TValue>
	{	//****************************************
		private TValue[] _WorkingArray;
		private TValue _CurrentValue;
		private int _WorkingLength, _CurrentIndex;
		//****************************************
		
		public WorkingEnumerator(TValue[] workingArray, int workingLength)
		{
			_WorkingArray = workingArray;
			_WorkingLength = workingLength;
			_CurrentIndex = -1;
			_CurrentValue = default(TValue);
		}
		
		//****************************************
		
		void IDisposable.Dispose()
		{
			
		}
		
		public bool MoveNext()
		{
			if (++_CurrentIndex >= _WorkingLength)
				return false;
			
			_CurrentValue = _WorkingArray[_CurrentIndex];
			
			return true;
		}
		
		public void Reset()
		{
			_CurrentIndex = -1;
			_CurrentValue = default(TValue);
		}
		
		//****************************************
		
		object IEnumerator.Current
		{
			get { return _CurrentValue; }
		}
		
		public TValue Current
		{
			get { return _CurrentValue; }
		}
	}
}
