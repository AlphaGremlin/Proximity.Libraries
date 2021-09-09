using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	/// <summary>
	/// Provides an interleaving enumerable over an enumeration of Tasks with an index
	/// </summary>
	/// <typeparam name="T">The type of the task result</typeparam>
	public readonly struct InterleaveTaskIndexAsyncEnumerable<T> : IAsyncEnumerable<(Task<T> result, int index)>
	{//****************************************
		private readonly IEnumerable<Task<T>> _Source;
		private readonly CancellationToken _Token;
		//****************************************

		internal InterleaveTaskIndexAsyncEnumerable(IEnumerable<Task<T>> source, CancellationToken token)
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
		public InterleaveTaskIndexAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new(_Source, _Token, cancellationToken);

		//****************************************

		IAsyncEnumerator<(Task<T>, int)> IAsyncEnumerable<(Task<T> result, int index)>.GetAsyncEnumerator(CancellationToken cancellationToken) => new InterleaveTaskIndexAsyncEnumerator<T>(_Source, _Token, cancellationToken);
	}
}
