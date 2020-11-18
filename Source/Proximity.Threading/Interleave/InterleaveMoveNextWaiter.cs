using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading.Tasks.Interleave
{
	internal sealed class InterleaveMoveNextWaiter : IValueTaskSource<bool>
	{ //****************************************
		private static readonly ConcurrentBag<InterleaveMoveNextWaiter> Waiters = new ConcurrentBag<InterleaveMoveNextWaiter>();
		//****************************************
		private ManualResetValueTaskSourceCore<bool> _TaskSource = new ManualResetValueTaskSourceCore<bool>();

		private IInterleaveAsyncEnumerator? _Enumerator;
		//****************************************

		private InterleaveMoveNextWaiter() => _TaskSource.RunContinuationsAsynchronously = true;

		//****************************************

		internal void SwitchToCompleted() => _TaskSource.SetResult(true);

		internal void SwitchToCancelled(CancellationToken token) => _TaskSource.SetException(new OperationCanceledException(token));

		//****************************************

		public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

		public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		public bool GetResult(short token)
		{
			try
			{
				var Result = _TaskSource.GetResult(token);

				_Enumerator!.PrepareCurrent();

				return Result;
			}
			finally
			{
				_Enumerator = null;
				_TaskSource.Reset();

				Waiters.Add(this);
			}
		}

		//****************************************

		public short Version => _TaskSource.Version;

		//****************************************

		internal static InterleaveMoveNextWaiter GetOrCreate(IInterleaveAsyncEnumerator enumerator)
		{
			if (!Waiters.TryTake(out var Waiter))
				Waiter = new InterleaveMoveNextWaiter();

			Waiter._Enumerator = enumerator;

			return Waiter;
		}
	}
}
