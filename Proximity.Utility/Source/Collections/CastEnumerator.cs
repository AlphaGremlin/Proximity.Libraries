/****************************************\
 CastEnumerator.cs
 Created: 2015-02-05
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents an enumerator that casts up a generic type
	/// </summary>
	internal class CastEnumerator<TSource, TTarget> : IEnumerator<TTarget>
		where TSource : class
		where TTarget : class, TSource
	{	//****************************************
		private readonly IEnumerator<TSource> _Source;
		//****************************************

		internal CastEnumerator(IEnumerator<TSource> source)
		{
			_Source = source;
		}

		//****************************************

		void IDisposable.Dispose()
		{

		}

		public bool MoveNext()
		{
			while (_Source.MoveNext())
			{
				if (_Source.Current is TTarget)
					return true;
			}

			return false;
		}

		public void Reset()
		{
			_Source.Reset();
		}

		//****************************************

		object IEnumerator.Current
		{
			get { return _Source.Current; }
		}

		public TTarget Current
		{
			get { return (TTarget)_Source.Current; }
		}
	}
}
