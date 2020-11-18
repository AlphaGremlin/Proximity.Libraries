using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	/// <summary>
	/// Provides an interleaving enumerable over an enumeration of ValueTasks
	/// </summary>
	/// <typeparam name="T">The type of the task result</typeparam>
	public readonly struct InterleaveValueTaskAsyncEnumerable<T> : IAsyncEnumerable<ValueTask<T>>
	{//****************************************
		private readonly IEnumerable<ValueTask<T>> _Source;
		private readonly CancellationToken _Token;
		//****************************************

		internal InterleaveValueTaskAsyncEnumerable(IEnumerable<ValueTask<T>> source, CancellationToken token)
		{
			_Source = source ?? throw new ArgumentNullException(nameof(source));
			_Token = token;
		}

		//****************************************

		/// <summary>
		/// Gets an enumerator to enumerate the interleaved tasks
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to abort enumeration</param>
		/// <returns>An async enumerator returning the tasks in the order of completion</returns>
		/// <remarks>This will consume the given ValueTasks, and thus cannot be called multiple times</remarks>
		public InterleaveValueTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new InterleaveValueTaskAsyncEnumerator<T>(_Source, _Token, cancellationToken);

		//****************************************

		IAsyncEnumerator<ValueTask<T>> IAsyncEnumerable<ValueTask<T>>.GetAsyncEnumerator(CancellationToken cancellationToken) => new InterleaveValueTaskAsyncEnumerator<T>(_Source, _Token, cancellationToken);
	}
}
