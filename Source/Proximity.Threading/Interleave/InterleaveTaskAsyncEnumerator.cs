using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	/// <summary>
	/// Provides an interleaving enumerator
	/// </summary>
	/// <typeparam name="T">The type of the task result</typeparam>
	public sealed class InterleaveTaskAsyncEnumerator<T> : IAsyncEnumerator<Task<T>>, IInterleaveAsyncEnumerator<Task<T>>
	{ //****************************************
		private InterleaveCore<Task<T>> _Core;
		//****************************************

		internal InterleaveTaskAsyncEnumerator(IEnumerable<Task<T>> source, CancellationToken enumerableToken, CancellationToken enumeratorToken)
		{
			_Core = new InterleaveCore<Task<T>>(this, enumerableToken, enumeratorToken);

			var Index = 0;

			foreach (var Task in source)
			{
				_Core.Register();

				InterleaveTaskWaiter<T>.GetOrCreate(this, Task, Index++).Attach();
			}

			_Core.CompleteAttach();
		}

		//****************************************

		/// <summary>
		/// Moves to the next completed task
		/// </summary>
		/// <returns>A ValueTask that returns True when another task has completed, or False if no tasks remain</returns>
		public ValueTask<bool> MoveNextAsync() => _Core.MoveNextAsync();

		/// <summary>
		/// Cleans up the enumerator
		/// </summary>
		/// <returns>A ValueTask that returns when cleanup is complete</returns>
		public ValueTask DisposeAsync() => _Core.DisposeAsync();

		//****************************************

		void IInterleaveAsyncEnumerator.PrepareCurrent() => _Core.PrepareCurrent();

		void IInterleaveAsyncEnumerator<Task<T>>.CompleteTask(Task<T> task, int index) => _Core.CompleteTask(task, index);

		void IInterleaveAsyncEnumerator.Cancel() => _Core.Cancel();

		//****************************************

		/// <summary>
		/// Gets the currently available task
		/// </summary>
		public Task<T> Current => _Core.CurrentResult;
	}
}
