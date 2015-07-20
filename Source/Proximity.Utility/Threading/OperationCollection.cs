/****************************************\
 PrimeProtocol.cs
 Created: 2013-10-09
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a collection of cancellable Task operations
	/// </summary>
	public sealed class OperationCollection<TKey, TResult>
	{	//****************************************
		private readonly ConcurrentDictionary<TKey, Operation> _Operations = new ConcurrentDictionary<TKey, Operation>();
		//****************************************
		
		/// <summary>
		/// Creates a new operation collection
		/// </summary>
		public OperationCollection()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Cancels all pending operations
		/// </summary>
		public void CancelAll()
		{
			foreach(var MyOperation in _Operations.Values)
			{
				MyOperation.Cancel();
			}
		}
		
		/// <summary>
		/// Starts an operation
		/// </summary>
		/// <param name="reference">The reference for this operation</param>
		/// <param name="token">A cancellation token that can abort this operation</param>
		/// <returns>A task representing the status of the operation</returns>
		public Task<TResult> StartOperation(TKey reference, CancellationToken token)
		{	//****************************************
			var MyRequest = new Operation(this, reference);
			//****************************************
			
			_Operations.TryAdd(reference, MyRequest);
			
			MyRequest.Initialise(token);
			
			return MyRequest.Task;
		}
		
		/// <summary>
		/// Completes an previously started operation
		/// </summary>
		/// <param name="reference">The reference for this operation</param>
		/// <param name="result">The result to assign to the operation</param>
		/// <returns>True if the operation was completed, or False if it was not found/has been cancelled</returns>
		public bool CompleteOperation(TKey reference, TResult result)
		{	//****************************************
			Operation MyOperation;
			//****************************************
			
			if (!_Operations.TryRemove(reference, out MyOperation))
				return false;
			
			MyOperation.Complete(result);
			
			return true;
		}
		
		//****************************************
		
		private class Operation
		{	//****************************************
			private readonly OperationCollection<TKey, TResult> _Owner;
			private readonly TKey _Reference;
			private readonly TaskCompletionSource<TResult> _OperationTask;
			private CancellationTokenRegistration _Registration;
			//****************************************
			
			internal Operation(OperationCollection<TKey, TResult> owner, TKey reference)
			{
				_Owner = owner;
				_Reference = reference;
				_OperationTask = new TaskCompletionSource<TResult>();
			}
			
			//****************************************
			
			internal void Initialise(CancellationToken token)
			{
				// Have to do this after the operation is in the collection
				if (token.CanBeCanceled)
					_Registration = token.Register(Cancel);
			}
			
			internal void Complete(TResult result)
			{
				_OperationTask.SetResult(result);
				
				_Registration.Dispose();
			}
			
			internal void Cancel()
			{	//****************************************
				Operation MyRequest;
				//****************************************
			
				if (_Owner._Operations.TryRemove(_Reference, out MyRequest))
				{
					_OperationTask.SetCanceled();
					
					_Registration.Dispose();
				}
			}
			
			//****************************************
			
			internal Task<TResult> Task
			{
				get { return _OperationTask.Task; }
			}
		}
	}
}
