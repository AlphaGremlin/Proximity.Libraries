/****************************************\
 RemoteTask.cs
 Created: 2014-05-14
\****************************************/
#if NET45 || NET462
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides access to a Task running in another AppDomain
	/// </summary>
	/// <remarks>This object lives in the AppDomain where the Task is running</remarks>
	public sealed class RemoteTask : MarshalByRefObject
	{ //****************************************
		private readonly Task _Task;
		//****************************************

		internal RemoteTask(Task task)
		{
			_Task = task;
		}

		internal RemoteTask(Task task, RemoteCancellationTokenSource tokenSource) : this(task)
		{
			// We only want to attach once the task callback has run, otherwise we cause a memory leak since the below Dispose will never run, causing an eternal sponsorship
			tokenSource.Attach();

			// We're responsible for cleaning up the cancellatin token source upon completion of the task
			task.ContinueWith((innerTask, state) => ((RemoteCancellationTokenSource)state).Dispose(), tokenSource, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
		}

		//****************************************

		[SecuritySafeCritical]
		private void Attach(RemoteTaskCompletionSource<VoidStruct> taskSource)
		{
			// Attach to the local task and pass the results on to the remote task source
			_Task.ContinueWith(CompleteLocalTask, taskSource, TaskContinuationOptions.ExecuteSynchronously);
		}

		//****************************************

		/// <inheritdoc />
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null; // Last until the Task we're attached to is completed
		}

		//****************************************

		[SecuritySafeCritical]
		private void CompleteLocalTask(Task task, object state)
		{ //****************************************
			var Source = (RemoteTaskCompletionSource<VoidStruct>)state;
			//****************************************

			try
			{
				if (task.IsFaulted)
				{
					// Requires permissions to pass exceptions (that aren't being thrown) across app-domain boundaries
					new SecurityPermission(SecurityPermissionFlag.SerializationFormatter).Assert();
					try
					{
						Source.SetException(task.Exception.InnerException);
					}
					finally
					{
						PermissionSet.RevertAssert();
					}
				}
				else if (task.IsCanceled)
				{
					Source.SetCancelled();
				}
				else
				{
					Source.SetResult(default(VoidStruct));
				}
			}
			catch (Exception e) // Can fail if the exception object is not serialisable to the calling AppDomain
			{
				Source.SetException(e);
			}

			Unregister();
		}

		[SecuritySafeCritical]
		private void Unregister()
		{
			RemotingServices.Disconnect(this);
		}

		//****************************************
		
		/// <summary>
		/// Wraps an async callback in a RemoteTask
		/// </summary>
		/// <param name="callback">The callback returning a task to wrap</param>
		/// <returns>A remote task to pass across AppDomain boundaries</returns>
		public static RemoteTask Start(Func<Task> callback)
		{
			return new RemoteTask(callback());
		}

		/// <summary>
		/// Wraps an async callback in a RemoteTask
		/// </summary>
		/// <param name="callback">The callback returning a task to wrap</param>
		/// <param name="remoteToken">A remote cancellation token to convert into a local cancellation token</param>
		/// <returns>A remote task to pass across AppDomain boundaries</returns>
		[SecuritySafeCritical]
		public static RemoteTask Start(Func<CancellationToken, Task> callback, RemoteCancellationToken remoteToken)
		{
			if (!RemotingServices.IsObjectOutOfAppDomain(remoteToken))
				return new RemoteTask(callback(remoteToken.Token));
			
			// Creates a cancellation token source in this AppDomain, and passes responsibility for cleaning it up to RemoteTask
			var MyTokenSource = new RemoteCancellationTokenSource(remoteToken);
			
			return new RemoteTask(callback(MyTokenSource.Token), MyTokenSource);
		}

		/// <summary>
		/// Wraps an async callback in a RemoteTask
		/// </summary>
		/// <param name="callback">The callback returning a task to wrap</param>
		/// <returns>A remote task to pass across AppDomain boundaries</returns>
		public static RemoteTask<TResult> Start<TResult>(Func<Task<TResult>> callback)
		{
			return new RemoteTask<TResult>(callback());
		}
		
		/// <summary>
		/// Wraps an async callback in a RemoteTask
		/// </summary>
		/// <param name="callback">The callback returning a task to wrap</param>
		/// <param name="remoteToken">A remote cancellation token to convert into a local cancellation token</param>
		/// <returns>A remote task to pass across AppDomain boundaries</returns>
		[SecurityCritical]
		public static RemoteTask<TResult> Start<TResult>(Func<CancellationToken, Task<TResult>> callback, RemoteCancellationToken remoteToken)
		{
			if (!RemotingServices.IsObjectOutOfAppDomain(remoteToken))
				return new RemoteTask<TResult>(callback(remoteToken.Token));
			
			var MyTokenSource = new RemoteCancellationTokenSource(remoteToken);
			
			return new RemoteTask<TResult>(callback(MyTokenSource.Token), MyTokenSource);
		}
		
		/// <summary>
		/// Wraps an RemoteTask with an awaitable Task that exists within this AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to wrap</param>
		/// <returns>A task that will reflect the status of the RemoteTask</returns>
		/// <remarks>Beware of unloading the target AppDomain in a continuation of this Task, as it's likely running with the AppDomain in the stack, causing a strange ThreadAbortException</remarks>
		public static implicit operator Task(RemoteTask remoteTask)
		{
			return CreateTask(remoteTask);
		}
		
		/// <summary>
		/// Transforms a Task into a RemoteTask suitable for passing between AppDomains
		/// </summary>
		/// <param name="task">The task to transform</param>
		/// <returns>A RemoteTask that can be awaited from another AppDomain</returns>
		/// <remarks>To create a RemoteTask that listens to a RemoteCancellationToken, use one of the <see cref="O:RemoteTask.Start" /> overrides</remarks>
		public static implicit operator RemoteTask(Task task)
		{
			return new RemoteTask(task);
		}
		
		/// <summary>
		/// Creates a Remote Task that has already completed with no result
		/// </summary>
		/// <returns>A completed Remote Task</returns>
		public static RemoteTask FromResult()
		{
			return new RemoteTask(Task.FromResult(default(VoidStruct)));
		}
		
		/// <summary>
		/// Creates a Remote Task that has already completed with a result
		/// </summary>
		/// <param name="result">The result to assign to the task</param>
		/// <returns>A completed Remote Task with the given result</returns>
		public static RemoteTask<TResult> FromResult<TResult>(TResult result)
		{
			return new RemoteTask<TResult>(Task.FromResult(result));
		}
		
		//****************************************
		
		[SecuritySafeCritical]
		internal static Task CreateTask(RemoteTask remoteTask)
		{
			if (!RemotingServices.IsTransparentProxy(remoteTask))
				return remoteTask._Task; // Local object, so we can safely pass the Task
			
			var TaskSource = new RemoteTaskCompletionSource<VoidStruct>(remoteTask);
			
			// Inform our Remote Task to call the Local Task Completion Source
			remoteTask.Attach(TaskSource);
			
			return TaskSource.Task;
		}
	}
	
	/// <summary>
	/// Provides access to a Task running in another AppDomain
	/// </summary>
	/// <remarks>This object lives in the AppDomain where the Task is running</remarks>
	public sealed class RemoteTask<TResult> : MarshalByRefObject
	{//****************************************
		private readonly Task<TResult> _Task;
		//****************************************
		
		internal RemoteTask(Task<TResult> task)
		{
			_Task = task;
		}
		
		internal RemoteTask(Task<TResult> task, RemoteCancellationTokenSource tokenSource) : this(task)
		{
			// We only want to attach once the task callback has run, otherwise we cause a memory leak since the below Dispose will never run, causing an eternal sponsorship
			tokenSource.Attach();
			
			// We're responsible for cleaning up the cancellation token source upon completion of the task
			task.ContinueWith((innerTask, state) => ((RemoteCancellationTokenSource)state).Dispose(), tokenSource, TaskContinuationOptions.ExecuteSynchronously);
		}

		//****************************************
		
		[SecuritySafeCritical]
		private void Attach(RemoteTaskCompletionSource<TResult> taskSource)
		{
			// Attach to the local task and pass the results on to the remote task source
			_Task.ContinueWith(CompleteLocalTask, taskSource, TaskContinuationOptions.ExecuteSynchronously);
		}

		//****************************************

		/// <inheritdoc />
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null; // Last until the Task we're attached to is completed
		}

		//****************************************

		[SecuritySafeCritical]
		private void CompleteLocalTask(Task<TResult> task, object state)
		{	//****************************************
			var Source = (RemoteTaskCompletionSource<TResult>)state;
			//****************************************

			try
			{
				if (task.IsFaulted)
				{
					// Requires permissions to pass exceptions (that aren't being thrown) across app-domain boundaries
					new SecurityPermission(SecurityPermissionFlag.SerializationFormatter).Assert();
					try
					{
						Source.SetException(task.Exception.InnerException);
					}
					finally
					{
						PermissionSet.RevertAssert();
					}
				}
				else if (task.IsCanceled)
				{
					Source.SetCancelled();
				}
				else
				{
					// We don't assert SerializationFormatter permissions here
					Source.SetResult(task.Result);
				}
			}
			catch (Exception e) // Can fail if the result or exception object is not serialisable or marshalable to the calling AppDomain
			{
				Source.SetException(e);
			}

			Unregister();
		}

		[SecuritySafeCritical]
		private void Unregister()
		{
			RemotingServices.Disconnect(this);
		}

		//****************************************
		
		/// <summary>
		/// Wraps an async callback in a RemoteTask
		/// </summary>
		/// <param name="callback">The callback returning a task to wrap</param>
		/// <returns>A remote task to pass across AppDomain boundaries</returns>
		public static RemoteTask<TResult> Start(Func<Task<TResult>> callback)
		{
			return new RemoteTask<TResult>(callback());
		}
		
		/// <summary>
		/// Wraps an async callback in a RemoteTask
		/// </summary>
		/// <param name="callback">The callback returning a task to wrap</param>
		/// <param name="remoteToken">A remote cancellation token to convert into a local cancellation token</param>
		/// <returns>A remote task to pass across AppDomain boundaries</returns>
		[SecuritySafeCritical]
		public static RemoteTask<TResult> Start(Func<CancellationToken, Task<TResult>> callback, RemoteCancellationToken remoteToken)
		{
			if (!RemotingServices.IsObjectOutOfAppDomain(remoteToken))
				return new RemoteTask<TResult>(callback(remoteToken.Token));
			
			var MyTokenSource = new RemoteCancellationTokenSource(remoteToken);
			
			return new RemoteTask<TResult>(callback(MyTokenSource.Token), MyTokenSource);
		}
		
		/// <summary>
		/// Wraps a RemoteTask with an awaitable Task that exists within this AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to wrap</param>
		/// <returns>A task that will reflect the status of the RemoteTask</returns>
		/// <remarks>Beware of unloading the target AppDomain in a continuation of this Task, as it's likely running with the AppDomain in the stack, causing a strange ThreadAbortException</remarks>
		public static implicit operator Task<TResult>(RemoteTask<TResult> remoteTask)
		{
			return CreateTask(remoteTask);
		}
		
		/// <summary>
		/// Transforms a Task into a RemoteTask suitable for passing between AppDomains
		/// </summary>
		/// <param name="task">The task to transform</param>
		/// <returns>A RemoteTask that can be awaited from another AppDomain</returns>
		/// <remarks>To create a RemoteTask that listens to a RemoteCancellationToken, use one of the <see cref="O:RemoteTask&lt;TResult&gt;.Start" /> overrides</remarks>
		public static implicit operator RemoteTask<TResult>(Task<TResult> task)
		{
			return new RemoteTask<TResult>(task);
		}
		
		//****************************************

		[SecuritySafeCritical]
		internal static Task<TResult> CreateTask(RemoteTask<TResult> remoteTask)
		{
			if (!RemotingServices.IsTransparentProxy(remoteTask))
				return remoteTask._Task; // Local object, so we can safely pass the Task
			
			var TaskSource = new RemoteTaskCompletionSource<TResult>(remoteTask);
			
			// Inform our Remote Task to call the Local Task Completion Source when it's done
			remoteTask.Attach(TaskSource);
			
			return TaskSource.Task;
		}
	}
}
#endif