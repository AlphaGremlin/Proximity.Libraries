using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	/// <summary>
	/// Provides an interleaving enumerable over an enumeration of Tasks
	/// </summary>
	/// <typeparam name="T">The type of the task result</typeparam>
	public readonly struct InterleaveTaskAsyncEnumerable<T> : IAsyncEnumerable<Task<T>>
	{//****************************************
		private readonly IEnumerable<Task<T>> _Source;
		private readonly CancellationToken _Token;
		//****************************************

		internal InterleaveTaskAsyncEnumerable(IEnumerable<Task<T>> source, CancellationToken token)
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
		public InterleaveTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new InterleaveTaskAsyncEnumerator<T>(_Source, _Token, cancellationToken);

		//****************************************

		IAsyncEnumerator<Task<T>> IAsyncEnumerable<Task<T>>.GetAsyncEnumerator(CancellationToken cancellationToken) => new InterleaveTaskAsyncEnumerator<T>(_Source, _Token, cancellationToken);
	}
}
