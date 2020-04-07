using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Proximity.Threading
{
	internal sealed class InterleaveTask<TResult> : IAsyncEnumerable<Task<TResult>>, IAsyncEnumerable<(Task<TResult> result, int index)>, IAsyncEnumerable<ValueTask<TResult>>, IAsyncEnumerable<(ValueTask<TResult> result, int index)>
	{ //****************************************
		private static readonly ConcurrentBag<InnerWaiter> _InnerWaiters = new ConcurrentBag<InnerWaiter>();
		private static readonly ConcurrentBag<OuterWaiter> _OuterWaiters = new ConcurrentBag<OuterWaiter>();
		//****************************************
		private readonly IEnumerable<ValueTask<TResult>> _Source;
		private readonly CancellationToken _Token;
		//****************************************

		public InterleaveTask(IEnumerable<Task<TResult>> source, CancellationToken token)
		{
			_Source = (source ?? throw new ArgumentNullException(nameof(source))).Select(task => new ValueTask<TResult>(task));
			_Token = token;
		}

		public InterleaveTask(IEnumerable<ValueTask<TResult>> source, CancellationToken token)
		{
			_Source = source ?? throw new ArgumentNullException(nameof(source));
			_Token = token;
		}

		//****************************************

		IAsyncEnumerator<Task<TResult>> IAsyncEnumerable<Task<TResult>>.GetAsyncEnumerator(CancellationToken cancellationToken) => new Enumerator(_Source, _Token, cancellationToken);

		IAsyncEnumerator<(Task<TResult> result, int index)> IAsyncEnumerable<(Task<TResult> result, int index)>.GetAsyncEnumerator(CancellationToken cancellationToken) => new Enumerator(_Source, _Token, cancellationToken);

		IAsyncEnumerator<ValueTask<TResult>> IAsyncEnumerable<ValueTask<TResult>>.GetAsyncEnumerator(CancellationToken cancellationToken) => new Enumerator(_Source, _Token, cancellationToken);

		IAsyncEnumerator<(ValueTask<TResult> result, int index)> IAsyncEnumerable<(ValueTask<TResult> result, int index)>.GetAsyncEnumerator(CancellationToken cancellationToken) => new Enumerator(_Source, _Token, cancellationToken);

		//****************************************

		private sealed class Enumerator : IAsyncEnumerator<Task<TResult>>, IAsyncEnumerator<(Task<TResult> result, int index)>, IAsyncEnumerator<ValueTask<TResult>>, IAsyncEnumerator<(ValueTask<TResult> result, int index)>
		{ //****************************************
			private int _Remainder;

			private readonly ConcurrentQueue<(ValueTask<TResult> result, int index)> _Queue = new ConcurrentQueue<(ValueTask<TResult> result, int index)>();
			private readonly LinkedCancellationToken _Token;
			private CancellationTokenRegistration _Registration;

			private (ValueTask<TResult> result, int index) _Current;

			private OuterWaiter? _Waiter;
			//****************************************

			public Enumerator(IEnumerable<ValueTask<TResult>> source, CancellationToken enumerableToken, CancellationToken enumeratorToken)
			{
				var Index = 0;

				foreach (var Task in source)
				{
					Interlocked.Increment(ref _Remainder);

					InnerWaiter.GetOrCreate(this, Task, Index++).Attach();
				}

				_Token = LinkedCancellationToken.Create(enumerableToken, enumeratorToken);

				if (_Token.Token.CanBeCanceled)
				{
					_Registration = _Token.Token.Register(OnCancel);
				}
			}

			//****************************************

			public ValueTask<bool> MoveNextAsync()
			{
				// Have we consumed every result?
				if (_Remainder == 0)
					return new ValueTask<bool>(false);

				Interlocked.Decrement(ref _Remainder);

				//There are still results outstanding, are there any queued?
				if (_Queue.TryDequeue(out var NextResult))
				{
					_Current = NextResult;

					return new ValueTask<bool>(true);
				}

				// Nothing queued, let's create a waiter
				OuterWaiter? Waiter = OuterWaiter.GetOrCreate(this);

				Interlocked.Exchange(ref _Waiter, Waiter);

				// Double-check nothing is queued
				if (_Queue.TryDequeue(out NextResult))
				{
					// Try and activate the waiter (it may already be activated by a task completing)
					if (Interlocked.Exchange(ref _Waiter, null) != null)
						Waiter.SwitchToCompleted();

					_Current = NextResult;
				}

				return new ValueTask<bool>(Waiter, Waiter.Version);
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

			internal void OnTaskCompleted(ValueTask<TResult> result, int index)
			{
				_Queue.Enqueue((result, index));

				var Waiter = Interlocked.Exchange(ref _Waiter, null);

				if (Waiter != null)
					Waiter.SwitchToCompleted();
			}

			//****************************************

			private void OnCancel()
			{

			}

			//****************************************

			Task<TResult> IAsyncEnumerator<Task<TResult>>.Current => _Current.result.AsTask(); // If this is already a Task, does not allocate

			(Task<TResult> result, int index) IAsyncEnumerator<(Task<TResult> result, int index)>.Current => (_Current.result.AsTask(), _Current.index);

			ValueTask<TResult> IAsyncEnumerator<ValueTask<TResult>>.Current => _Current.result;

			(ValueTask<TResult> result, int index) IAsyncEnumerator<(ValueTask<TResult> result, int index)>.Current => _Current;
		}

		private sealed class InnerWaiter
		{ //****************************************
			private readonly Action _Continue;

			private ValueTask<TResult> _Task;
			private Enumerator? _Enumerator;
			private int _Index;
			//****************************************

			public InnerWaiter()
			{
				_Continue = OnContinue;
			}

			//****************************************

			public void Attach()
			{
				var Awaiter = _Task.ConfigureAwait(false).GetAwaiter();

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
					_Enumerator!.OnTaskCompleted(_Task, _Index);
				}
				finally
				{
					_Task = default;
					_Enumerator = null;
					_Index = 0;

					_InnerWaiters.Add(this);
				}
			}

			//****************************************

			internal static InnerWaiter GetOrCreate(Enumerator enumerator, ValueTask<TResult> task, int index)
			{
				if (!_InnerWaiters.TryTake(out var Waiter))
					Waiter = new InnerWaiter();

				Waiter._Enumerator = enumerator;
				Waiter._Task = task;
				Waiter._Index = index;

				return Waiter;
			}
		}

		private sealed class OuterWaiter : IValueTaskSource<bool>
		{ //****************************************
			private ManualResetValueTaskSourceCore<bool> _TaskSource = new ManualResetValueTaskSourceCore<bool>();

			private Enumerator? _Enumerator;
			//****************************************

			private OuterWaiter() => _TaskSource.RunContinuationsAsynchronously = true;

			//****************************************

			internal void SwitchToCompleted() => _TaskSource.SetResult(true);

			//****************************************

			public ValueTaskSourceStatus GetStatus(short token) => _TaskSource.GetStatus(token);

			public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _TaskSource.OnCompleted(continuation, state, token, flags);

			public bool GetResult(short token)
			{
				try
				{
					return _TaskSource.GetResult(token);
				}
				finally
				{
					_Enumerator = null;
					_TaskSource.Reset();

					_OuterWaiters.Add(this);
				}
			}

			//****************************************

			public short Version => _TaskSource.Version;

			//****************************************

			internal static OuterWaiter GetOrCreate(Enumerator enumerator)
			{
				if (!_OuterWaiters.TryTake(out var Waiter))
					Waiter = new OuterWaiter();

				Waiter._Enumerator = enumerator;

				return Waiter;
			}
		}
	}
}
