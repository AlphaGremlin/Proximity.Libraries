/****************************************\
 AsyncLinq.cs
 Created: 2014-07-14
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Description of AsyncLinq.
	/// </summary>
	public static class AsyncLinq
	{
		public IAsyncEnumerable<TItem> ToAsync<TItem>(this IEnumerable<TItem> source)
		{
			return new AsyncEnumerable<TItem>(source.Select(Task.FromResult));
		}
		
		public AsyncEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> selector)
		{	//****************************************
			var MyTaskSet = source.Select(selector);
			//****************************************
			
			return new AsyncEnumerable<TResult>(MyTaskSet);
		}
	}
}
