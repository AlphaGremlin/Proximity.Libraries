/****************************************\
 AsyncEnumerable.cs
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
	/// Description of AsyncEnumerable.
	/// </summary>
	public sealed class AsyncEnumerable<TItem> : IAsyncEnumerable<TItem>
	{	//****************************************
		private readonly IEnumerable<Task<TItem>> _TaskSet;
		//****************************************
		
		public AsyncEnumerable(IEnumerable<Task<TItem>> taskSet)
		{
			_TaskSet = taskSet;
		}
		
		//****************************************
		
		IAsyncEnumerator<TItem> IAsyncEnumerable<TItem>.GetEnumerator()
		{
			return new AsyncEnumerator(_TaskSet);
		}
		
		//****************************************
		
		private sealed class AsyncEnumerator : IAsyncEnumerator<TItem>
		{	//****************************************
			private readonly IEnumerable<Task<TItem>> _TaskSet;
			
			private TItem _Current;
			//****************************************
			
			internal AsyncEnumerator(IEnumerable<Task<TItem>> taskSet)
			{
				_TaskSet = taskSet;
			}
			
			//****************************************
			
			Task<bool> IAsyncEnumerator.MoveNext()
			{
				throw new NotImplementedException();
			}
			
			Task IAsyncEnumerator.Reset()
			{
				
			}
			
			void IDisposable.Dispose()
			{
				throw new NotImplementedException();
			}
			
			//****************************************
			
			TItem IAsyncEnumerator<TItem>.Current
			{
				get { return _Current; }
			}
			
		}
		
		
		public AsyncEnumerable<TResult> Select<TResult>(Func<TItem, TResult> selector)
		{
			
			
		}
		
		public AsyncEnumerable<TResult> Select<TResult>(Func<TItem, Task<TResult>> selector)
		{
			
			
		}
		
		public AsyncEnumerable<TItem> Where(Func<TItem, bool> predicate)
		{
			return new AsyncEnumerable(Where(_TaskSet, predicate));
			
		}
		
		public AsyncEnumerable<TItem> Where(Func<TItem, Task<bool>> predicate)
		{
			
			
		}
		
		public Task<IEnumerable<TItem>> ToEnumerable()
		{
			
		}
		
		//****************************************
		
		private IEnumerable<Task<TItem>> Where(Func<TItem, bool> predicate)
		{
			
			foreach (var MyTask in _TaskSet.Interleave())
			{
				
				
			}
			
			
			
			foreach (var MyTask in _TaskSet)
			{
				MyTask.ContinueWith(task => task.Result);
			}
		}
		
	}
}
