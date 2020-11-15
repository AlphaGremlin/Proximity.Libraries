using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Interleave
{
	internal sealed class InterleaveValueTaskWaiter<T>
	{ //****************************************
		private static readonly ConcurrentBag<InterleaveValueTaskWaiter<T>> Waiters = new ConcurrentBag<InterleaveValueTaskWaiter<T>>();
		//****************************************
		private readonly Action _Continue;

		private ValueTask<T> _Task;
		private ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter _Awaiter;

		private IInterleaveAsyncEnumerator<ValueTask<T>>? _Enumerator;
		private int _Index;
		//****************************************

		public InterleaveValueTaskWaiter() => _Continue = OnContinue;

		//****************************************

		public void Attach()
		{
			_Awaiter = _Task!.ConfigureAwait(false).GetAwaiter();

			if (_Awaiter.IsCompleted)
				OnContinue();
			else
				_Awaiter.OnCompleted(_Continue);
		}

		//****************************************

		private void OnContinue()
		{
			try
			{
				// Since we've already awaited the passed task, we need to create a new ValueTask from the result that the caller can await.
				// We do this so exceptions are thrown in an area under the caller's control, rather than from inside the ForEach processing
				var Result = _Awaiter.GetResult();

				_Enumerator!.CompleteTask(new ValueTask<T>(Result), _Index);
			}
			catch (Exception e)
			{
				// Sadly there's no ValueTask.FromException
				var Source = ValueTaskCompletionSource.New<T>();

				Source.TrySetException(e);

				_Enumerator!.CompleteTask(Source.Task, _Index);

				Source.Dispose(); // We have no further use for the Completion Source. Mark it for releasing once the Task has had its result read
			}
			finally
			{
				_Task = default;
				_Awaiter = default;
				_Enumerator = null;
				_Index = 0;

				Waiters.Add(this);
			}
		}

		//****************************************

		internal static InterleaveValueTaskWaiter<T> GetOrCreate(IInterleaveAsyncEnumerator<ValueTask<T>> enumerator, ValueTask<T> task, int index)
		{
			if (!Waiters.TryTake(out var Waiter))
				Waiter = new InterleaveValueTaskWaiter<T>();

			Waiter._Enumerator = enumerator;
			Waiter._Task = task;
			Waiter._Index = index;

			return Waiter;
		}
	}
}
