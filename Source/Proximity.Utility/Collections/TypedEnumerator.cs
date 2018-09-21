/****************************************\
 TypedEnumerator.cs
 Created: 2011-08-08
\****************************************/
using System;
using System.Collections;
using System.Collections.Generic;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Represents an enumerator that casts down a generic type
	/// </summary>
	internal class TypedEnumerator<TSource, TTarget> : IEnumerator<TTarget> where TSource : class, TTarget where TTarget : class
	{	//****************************************
		private readonly IEnumerator<TSource> _Source;
		//****************************************
		
		internal TypedEnumerator(IEnumerator<TSource> source)
		{
			_Source = source;
		}
		
		//****************************************
		
		void IDisposable.Dispose()
		{
			
		}
		
		public bool MoveNext()
		{
			return _Source.MoveNext();
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
