using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proximity.Threading
{
	internal sealed class InterleaveValueTask<TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerable<(TResult result, int index)>
	{ //****************************************
		private readonly IEnumerable<ValueTask<TResult>> _Source;
		private readonly CancellationToken _Token;
		//****************************************

		public InterleaveValueTask(IEnumerable<ValueTask<TResult>> source, CancellationToken token)
		{
			_Source = source ?? throw new ArgumentNullException(nameof(source));
			_Token = token;
		}

		//****************************************

		IAsyncEnumerator<TResult> IAsyncEnumerable<TResult>.GetAsyncEnumerator(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		IAsyncEnumerator<(TResult result, int index)> IAsyncEnumerable<(TResult result, int index)>.GetAsyncEnumerator(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		//****************************************

		private sealed class Enumerator : IAsyncEnumerator<TResult>, IAsyncEnumerator<(TResult result, int index)>
		{

		}
	}
}
