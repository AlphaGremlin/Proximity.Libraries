/****************************************\
 RemoteTask.cs
 Created: 2014-05-14
\****************************************/
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
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
	public sealed class RemoteTask : MarshalByRefObject, ISponsor
	{	//****************************************
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
			task.ContinueWith((innerTask, state) => ((RemoteCancellationTokenSource)state).Dispose(), tokenSource, TaskContinuationOptions.ExecuteSynchronously);
		}
		
		//****************************************
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		private void Attach(RemoteTaskCompletionSource<VoidStruct> taskSource)
		{
			// Sponsor the remote task source so it doesn't get disconnected if the operation takes a long time
			var MyLease = (ILease)RemotingServices.GetLifetimeService(taskSource);
			
			MyLease.Register(this);
			
			// Attach to the local task and pass the results on to the remote task source
			_Task.ContinueWith(
				(innerTask, state) =>
				{
					var Source = (RemoteTaskCompletionSource<VoidStruct>)state;
					
					if (innerTask.IsFaulted)
						Source.TrySetException(innerTask.Exception.InnerException);
					else if (innerTask.IsCanceled)
						Source.TrySetCancelled();
					else
						Source.TrySetResult(default(VoidStruct));
				}
				, taskSource, TaskContinuationOptions.ExecuteSynchronously);
		}
		
		//****************************************

		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		TimeSpan ISponsor.Renewal(ILease lease)
		{
			// Ensure we keep the remote task source connection alive until our task is completed
			if (_Task.IsCompleted)
				return TimeSpan.Zero;
			
			return lease.RenewOnCallTime;
		}
		
		//****************************************
		
		/// <summary>
		/// Provides a dummy structure for TaskCompletionSource
		/// </summary>
		[Serializable]
		private struct VoidStruct
		{
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
		public static RemoteTask Start(Func<CancellationToken, Task> callback, RemoteCancellationToken remoteToken)
		{
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
		public static RemoteTask<TResult> Start<TResult>(Func<CancellationToken, Task<TResult>> callback, RemoteCancellationToken remoteToken)
		{
			var MyTokenSource = new RemoteCancellationTokenSource(remoteToken);
			
			return new RemoteTask<TResult>(callback(MyTokenSource.Token), MyTokenSource);
		}
		
		/// <summary>
		/// Wraps an RemoteTask with an awaitable Task that exists within this AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to wrap</param>
		/// <returns>A task that will reflect the status of the RemoteTask</returns>
		public static implicit operator Task(RemoteTask remoteTask)
		{
			return CreateTask(remoteTask);
		}
		
		//****************************************
		
		internal static Task CreateTask(RemoteTask remoteTask)
		{
			Debug.Assert(RemotingServices.IsTransparentProxy(remoteTask), "Attempt to unwrap remote task inside the owning AppDomain");
			
			var TaskSource = new RemoteTaskCompletionSource<RemoteTask.VoidStruct>(remoteTask);
			
			// Inform our Remote Task to call the Local Task Completion Source
			remoteTask.Attach(TaskSource);
			
			return TaskSource.Task;
		}
	}
	
	/// <summary>
	/// Provides access to a Task running in another AppDomain
	/// </summary>
	/// <remarks>This object lives in the AppDomain where the Task is running</remarks>
	public sealed class RemoteTask<TResult> : MarshalByRefObject, ISponsor
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
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		private void Attach(RemoteTaskCompletionSource<TResult> taskSource)
		{
			// Sponsor the remote task source so it doesn't get disconnected if the operation takes a long time
			var MyLease = (ILease)RemotingServices.GetLifetimeService(taskSource);
			
			MyLease.Register(this);
			
			// Attach to the local task and pass the results on to the remote task source
			_Task.ContinueWith(
				(innerTask, state) =>
				{
					var Source = (RemoteTaskCompletionSource<TResult>)state;
					
					try
					{
						if (innerTask.IsFaulted)
							Source.TrySetException(innerTask.Exception.InnerException);
						else if (innerTask.IsCanceled)
							Source.TrySetCancelled();
						else
							Source.TrySetResult(innerTask.Result);
					}
					catch (Exception e) // Can fail if the result object is not serialisable or marshalable
					{
						Source.TrySetException(e);
					}
				}
				, taskSource, TaskContinuationOptions.ExecuteSynchronously);
		}

		//****************************************

		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		TimeSpan ISponsor.Renewal(ILease lease)
		{
			// Ensure we keep the remote task source connection alive until our task is completed
			if (_Task.IsCompleted)
				return TimeSpan.Zero;
			
			return lease.RenewOnCallTime;
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
		public static RemoteTask<TResult> Start(Func<CancellationToken, Task<TResult>> callback, RemoteCancellationToken remoteToken)
		{
			var MyTokenSource = new RemoteCancellationTokenSource(remoteToken);
			
			return new RemoteTask<TResult>(callback(MyTokenSource.Token), MyTokenSource);
		}
		
		/// <summary>
		/// Wraps a RemoteTask with an awaitable Task that exists within this AppDomain
		/// </summary>
		/// <param name="remoteTask">The remote task to wrap</param>
		/// <returns>A task that will reflect the status of the RemoteTask</returns>
		public static implicit operator Task<TResult>(RemoteTask<TResult> remoteTask)
		{
			return CreateTask(remoteTask);
		}
		
		//****************************************
		
		internal static Task<TResult> CreateTask(RemoteTask<TResult> remoteTask)
		{
			Debug.Assert(RemotingServices.IsTransparentProxy(remoteTask), "Attempt to unwrap remote task inside the owning AppDomain");
			
			var TaskSource = new RemoteTaskCompletionSource<TResult>(remoteTask);
			
			// Inform our Remote Task to call the Local Task Completion Source when it's done
			remoteTask.Attach(TaskSource);
			
			return TaskSource.Task;
		}
	}
}
