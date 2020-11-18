using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	internal sealed class InterleaveTaskWaiter<T>
	{ //****************************************
		private static readonly ConcurrentBag<InterleaveTaskWaiter<T>> Waiters = new ConcurrentBag<InterleaveTaskWaiter<T>>();
		//****************************************
		private readonly Action _Continue;

		private Task<T>? _Task;
		private IInterleaveAsyncEnumerator<Task<T>>? _Enumerator;
		private int _Index;
		//****************************************

		public InterleaveTaskWaiter() => _Continue = OnContinue;

		//****************************************

		public void Attach()
		{
			// For normal tasks we don't keep the awaiter around
			var Awaiter = _Task!.ConfigureAwait(false).GetAwaiter();

			if (Awaiter.IsCompleted)
				OnContinue();
			else
				Awaiter.OnCompleted(_Continue);
		}

		//****************************************

		private void OnContinue()
		{
			try
			{
				_Enumerator!.CompleteTask(_Task!, _Index);
			}
			finally
			{
				_Task = null;
				_Enumerator = null;
				_Index = 0;

				Waiters.Add(this);
			}
		}

		//****************************************

		internal static InterleaveTaskWaiter<T> GetOrCreate(IInterleaveAsyncEnumerator<Task<T>> enumerator, Task<T> task, int index)
		{
			if (!Waiters.TryTake(out var Waiter))
				Waiter = new InterleaveTaskWaiter<T>();

			Waiter._Enumerator = enumerator;
			Waiter._Task = task;
			Waiter._Index = index;

			return Waiter;
		}
	}
}
