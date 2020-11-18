using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Configuration
{
	/// <summary>
	/// Generic enumerator for configuration element collections
	/// </summary>
	public struct ConfigurationEnumerator<T> : IEnumerator<T>
	{	//****************************************
		private readonly IEnumerator _InnerEnumerator;
		//****************************************
		
		internal ConfigurationEnumerator(ICollection outputSet)
		{
			_InnerEnumerator = outputSet.GetEnumerator();
		}
		
		//****************************************
		
		void IDisposable.Dispose()
		{
			
		}

		/// <inheritdoc />
		public bool MoveNext() => _InnerEnumerator.MoveNext();

		/// <inheritdoc />
		public void Reset() => _InnerEnumerator.Reset();

		//****************************************

		object IEnumerator.Current => _InnerEnumerator.Current;

		/// <inheritdoc />
		public T Current => (T)_InnerEnumerator.Current;
	}
}
