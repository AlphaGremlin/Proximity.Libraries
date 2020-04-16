using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Sources;

namespace Proximity.Threading
{
	internal sealed class LockDisposer : IValueTaskSource
	{ //****************************************
		private ManualResetValueTaskSourceCore<VoidStruct> _TaskSource = new ManualResetValueTaskSourceCore<VoidStruct>();

		private int _IsDisposed;
		//****************************************

		public void SwitchToComplete()
		{
			if (Interlocked.Exchange(ref _IsDisposed, 1) == 0)
				_TaskSource.SetResult(default);
		}

		//****************************************

		void IValueTaskSource.GetResult(short token) => _TaskSource.GetResult(token);

		ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _TaskSource.GetStatus(token);

		void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

		//****************************************

		public short Token => _TaskSource.Version;

		public bool IsDisposed => _IsDisposed == 1;
	}
}
