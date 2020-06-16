using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading.Tasks
{
	/// <summary>
	/// Provides a Value Task Completion Source
	/// </summary>
	public readonly struct ValueTaskCompletionSource : IDisposable
	{ //****************************************
		private readonly ValueTaskCompletionInstance<VoidStruct> _Instance;
		private readonly short _Token;
		//****************************************

		private ValueTaskCompletionSource(ValueTaskCompletionInstance<VoidStruct> instance, short token) => (_Instance, _Token) = (instance, token);

		//****************************************

		/// <summary>
		/// Releases the Value Task Completion Source
		/// </summary>
		/// <remarks>If a value has not been set, will set the result to <see cref="ObjectDisposedException"/></remarks>
		public void Dispose() => _Instance.TryDispose(_Token);

		/// <summary>
		/// Completes the Value Task Completion Source
		/// </summary>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetResult() => _Instance.TrySetResult(default, _Token);

		/// <summary>
		/// Faults the Value Task Completion Source
		/// </summary>
		/// <param name="exception">The exception to throw</param>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetException(Exception exception) => _Instance.TrySetException(exception, _Token);

		/// <summary>
		/// Cancels the Value Task Completion Source
		/// </summary>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetCancelled() => _Instance.TrySetException(new OperationCanceledException(), _Token);

		/// <summary>
		/// Cancels the Value Task Completion Source
		/// </summary>
		/// <param name="token">The cancellation token to report as the reason for cancellation</param>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetCancelled(CancellationToken token) => _Instance.TrySetException(new OperationCanceledException(token), _Token);

		//****************************************

		/// <summary>
		/// Gets the Value Task being completed
		/// </summary>
		public ValueTask Task => new ValueTask(_Instance, _Token);

		/// <summary>
		/// Gets whether the Value Task Completion Source has a result
		/// </summary>
		public bool IsCompleted => _Instance.IsCompleted(_Token);

		//****************************************

		/// <summary>
		/// Creates a new Value Task Completion Source
		/// </summary>
		/// <param name="runContinuationsAsynchronously">True to always run continuations asynchronously</param>
		/// <param name="token">A cancellation token that will automatically set this Task Completion Source to Cancelled</param>
		/// <returns>A new Value Task Completion Source</returns>
		public static ValueTaskCompletionSource New(bool runContinuationsAsynchronously = false, CancellationToken token = default)
		{
			var NewInstance = ValueTaskCompletionInstance<VoidStruct>.GetOrCreate(runContinuationsAsynchronously, token);

			return new ValueTaskCompletionSource(NewInstance, NewInstance.Version);
		}

		/// <summary>
		/// Creates a new Value Task Completion Source
		/// </summary>
		/// <typeparam name="TResult">The type of result</typeparam>
		/// <param name="runContinuationsAsynchronously">True to always run continuations asynchronously</param>
		/// <param name="token">A cancellation token that will automatically set this Task Completion Source to Cancelled</param>
		/// <returns>A new Value Task Completion Source</returns>
		public static ValueTaskCompletionSource<TResult> New<TResult>(bool runContinuationsAsynchronously = false, CancellationToken token = default)
		{
			var NewInstance = ValueTaskCompletionInstance<TResult>.GetOrCreate(runContinuationsAsynchronously, token);

			return new ValueTaskCompletionSource<TResult>(NewInstance, NewInstance.Version);
		}
	}

	/// <summary>
	/// Provides a Value Task Completion Source
	/// </summary>
	/// <typeparam name="TResult">The result type</typeparam>
	public readonly struct ValueTaskCompletionSource<TResult> : IDisposable
	{ //****************************************
		private readonly ValueTaskCompletionInstance<TResult> _Instance;
		private readonly short _Token;
		//****************************************

		internal ValueTaskCompletionSource(ValueTaskCompletionInstance<TResult> instance, short token) => (_Instance, _Token) = (instance, token);

		//****************************************

		/// <summary>
		/// Releases the Value Task Completion Source
		/// </summary>
		/// <remarks>If a value has not been set, will set the result to <see cref="ObjectDisposedException"/></remarks>
		public void Dispose() => _Instance.TryDispose(_Token);

		/// <summary>
		/// Completes the Value Task Completion Source
		/// </summary>
		/// <param name="result">The result to return</param>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetResult(TResult result) => _Instance.TrySetResult(result, _Token);

		/// <summary>
		/// Faults the Value Task Completion Source
		/// </summary>
		/// <param name="exception">The exception to throw</param>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetException(Exception exception) => _Instance.TrySetException(exception, _Token);

		/// <summary>
		/// Cancels the Value Task Completion Source
		/// </summary>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetCancelled() => _Instance.TrySetException(new OperationCanceledException(), _Token);

		/// <summary>
		/// Cancels the Value Task Completion Source
		/// </summary>
		/// <param name="token">The cancellation token to report as the reason for cancellation</param>
		/// <returns>True if the result was set, otherwise False</returns>
		public bool TrySetCancelled(CancellationToken token) => _Instance.TrySetException(new OperationCanceledException(token), _Token);

		//****************************************

		/// <summary>
		/// Gets the Value Task being completed
		/// </summary>
		public ValueTask<TResult> Task => new ValueTask<TResult>(_Instance, _Token);

		/// <summary>
		/// Gets whether the Value Task Completion Source has a result
		/// </summary>
		public bool IsCompleted => _Instance.IsCompleted(_Token);
	}
}
