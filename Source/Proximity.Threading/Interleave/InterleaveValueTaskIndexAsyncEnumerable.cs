using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	/// <summary>
	/// Provides an interleaving enumerable over an enumeration of ValueTasks with an index
	/// </summary>
	/// <typeparam name="T">The type of the task result</typeparam>
	public readonly struct InterleaveValueTaskIndexAsyncEnumerable<T> : IAsyncEnumerable<(ValueTask<T> result, int index)>
	{//****************************************
		private readonly IEnumerable<ValueTask<T>> _Source;
		private readonly CancellationToken _Token;
		//****************************************

		internal InterleaveValueTaskIndexAsyncEnumerable(IEnumerable<ValueTask<T>> source, CancellationToken token)
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
		public InterleaveValueTaskIndexAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new(_Source, _Token, cancellationToken);

		//****************************************

		IAsyncEnumerator<(ValueTask<T>, int)> IAsyncEnumerable<(ValueTask<T> result, int index)>.GetAsyncEnumerator(CancellationToken cancellationToken) => new InterleaveValueTaskIndexAsyncEnumerator<T>(_Source, _Token, cancellationToken);
	}
}
