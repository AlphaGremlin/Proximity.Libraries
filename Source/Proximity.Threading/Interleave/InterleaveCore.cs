using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	internal struct InterleaveCore<T> : IAsyncDisposable
	{ //****************************************
		private readonly ConcurrentQueue<(T result, int index)> _Queue;

		private readonly IInterleaveAsyncEnumerator _Enumerator;
		private readonly LinkedCancellationToken _Token;
		private CancellationTokenRegistration _Registration;

		private int _Remainder;

		private InterleaveMoveNextWaiter? _Waiter;
		private (T result, int index) _Current;
		//****************************************

		internal InterleaveCore(IInterleaveAsyncEnumerator enumerator, CancellationToken enumerableToken, CancellationToken enumeratorToken)
		{
			_Enumerator = enumerator;
			_Queue = new ConcurrentQueue<(T, int)>();

			_Token = LinkedCancellationToken.Create(enumerableToken, enumeratorToken);

			_Registration = default;
			_Remainder = 0;

			_Waiter = null;
			_Current = default;
		}

		//****************************************

		internal void Register() => Interlocked.Increment(ref _Remainder);

		internal void CompleteAttach()
		{
			if (_Token.Token.CanBeCanceled)
				_Registration = _Token.Token.Register(_Enumerator.Cancel);
		}

		internal ValueTask<bool> MoveNextAsync()
		{
			// Have we consumed every result?
			if (_Remainder == 0)
				return new ValueTask<bool>(false);

			Interlocked.Decrement(ref _Remainder);

			//There are still results outstanding, are there any queued?
			if (_Queue.TryDequeue(out _Current))
				return new ValueTask<bool>(true);

			// Nothing queued, let's create a waiter
			var Waiter = InterleaveMoveNextWaiter.GetOrCreate(_Enumerator);

			Interlocked.Exchange(ref _Waiter, Waiter);

			// Double-check nothing is queued
			if (!_Queue.IsEmpty)
			{
				// Try and activate the waiter (it may already be activated by a task completing)
				Interlocked.Exchange(ref _Waiter, null)?.SwitchToCompleted();
			}

			return new ValueTask<bool>(Waiter, Waiter.Version);
		}

		internal void PrepareCurrent()
		{
			if (!_Queue.TryDequeue(out _Current))
				throw new InvalidOperationException("Interleave operation is inconsistent");
		}

		internal void CompleteTask(T result, int index)
		{
			_Queue.Enqueue((result, index));

			Interlocked.Exchange(ref _Waiter, null)?.SwitchToCompleted();
		}

		internal void Cancel()
		{
			Interlocked.Exchange(ref _Waiter, null)?.SwitchToCancelled(_Token.Token);
		}

		public ValueTask DisposeAsync()
		{
			_Token.Dispose();

#if NETSTANDARD2_0
			_Registration.Dispose();

			return default;
#else
				return _Registration.DisposeAsync();
#endif
		}

		//****************************************

		public T CurrentResult => _Current.result;

		public int CurrentIndex => _Current.index;
	}
}
