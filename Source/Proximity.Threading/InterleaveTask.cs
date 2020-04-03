using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Proximity.Threading
{
	internal sealed class InterleaveTask<TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerable<(TResult result, int index)>
	{ //****************************************
		private static readonly ConcurrentBag<InnerWaiter> _InnerWaiters = new ConcurrentBag<InnerWaiter>();
		//****************************************
		private readonly IEnumerable<Task<TResult>> _Source;
		private readonly CancellationToken _Token;
		//****************************************

		public InterleaveTask(IEnumerable<Task<TResult>> source, CancellationToken token)
		{
			_Source = source ?? throw new ArgumentNullException(nameof(source));
			_Token = token;
		}

		//****************************************

		IAsyncEnumerator<TResult> IAsyncEnumerable<TResult>.GetAsyncEnumerator(CancellationToken cancellationToken) => new Enumerator(_Source, _Token, cancellationToken);

		IAsyncEnumerator<(TResult result, int index)> IAsyncEnumerable<(TResult result, int index)>.GetAsyncEnumerator(CancellationToken cancellationToken) => new Enumerator(_Source, _Token, cancellationToken);

		//****************************************

		private sealed class Enumerator : IAsyncEnumerator<TResult>, IAsyncEnumerator<(TResult result, int index)>
		{ //****************************************
			private int _Remainder;

			private readonly LinkedCancellationToken _Token;
			private CancellationTokenRegistration _Registration;
			//****************************************

			public Enumerator(IEnumerable<Task<TResult>> source, CancellationToken enumerableToken, CancellationToken enumeratorToken)
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
				throw new NotImplementedException();
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

			internal void OnTaskCompleted(Task<TResult> task, int index)
			{
			}

			internal void OnTaskFaulted(Task<TResult> task)
			{
			}

			//****************************************

			private void OnCancel()
			{

			}

			//****************************************

			TResult IAsyncEnumerator<TResult>.Current => throw new NotImplementedException();

			(TResult result, int index) IAsyncEnumerator<(TResult result, int index)>.Current => throw new NotImplementedException();
		}

		private sealed class InnerWaiter
		{ //****************************************
			private readonly Action _Continue;

			private Task<TResult>? _Task;
			private Enumerator? _Enumerator;
			private int _Index;

			private ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter _Awaiter;
			//****************************************

			public InnerWaiter()
			{
				_Continue = OnContinue;
			}

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
					_Awaiter.GetResult();

					_Enumerator!.OnTaskCompleted(_Task!, _Index);
				}
				catch (Exception e)
				{
					_Enumerator!.OnTaskFaulted(_Task!);
				}
				finally
				{
					Release();
				}
			}

			private void Release()
			{
				_Task = null;
				_Enumerator = null;
				_Index = 0;
				_Awaiter = default;

				_InnerWaiters.Add(this);
			}

			//****************************************

			internal static InnerWaiter GetOrCreate(Enumerator enumerator, Task<TResult> task, int index)
			{
				if (!_InnerWaiters.TryTake(out var Waiter))
					Waiter = new InnerWaiter();

				Waiter._Enumerator = enumerator;
				Waiter._Task = task;
				Waiter._Index = index;

				return Waiter;
			}

		}

	}
}
