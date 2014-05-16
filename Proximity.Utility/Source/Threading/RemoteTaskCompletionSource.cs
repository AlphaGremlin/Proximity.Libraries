/****************************************\
 RemoteTaskCompletionSource.cs
 Created: 2014-05-14
\****************************************/
using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a Task Completion Source within the calling AppDomain that receives the result of the Task in the target AppDomain
	/// </summary>
	/// <remarks>This object lives in the AppDomain making the call</remarks>
	internal sealed class RemoteTaskCompletionSource<TResult> : MarshalByRefObject, ISponsor
	{	//****************************************
		private readonly TaskCompletionSource<TResult> _TaskSource;
		//****************************************
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		internal RemoteTaskCompletionSource(MarshalByRefObject remoteTask)
		{
			Debug.Assert(RemotingServices.IsObjectOutOfAppDomain(remoteTask), "Attempt to unwrap remote task inside the owning AppDomain");
			
			// Create the local TaskCompletionSource, and attach ourselves to it.
			// Otherwise, remoting will be the only thing with a reference to us, and we may end up garbage collected
			_TaskSource = new TaskCompletionSource<TResult>(this);
			
			try
			{
				// We need to take a lease to ensure the remote task object stays alive
				var MyLease = (ILease)RemotingServices.GetLifetimeService(remoteTask);
				
				MyLease.Register(this);
			}
			catch (RemotingException e)
			{
				// Since this class sponsors the RemoteTask, if it's not called in time, the remote object becomes disconnected
				// Consumers of a RemoteTask must create a task either implicitly (by awaiting it via RemoteTaskExtensions.GetAwaiter),
				// or explicitly by calling RemoteTaskExtensions.ToTask, or by casting to Task/Task<TResult>
				throw new ApplicationException("Remote Tasks must be immediately awaited or converted to a local Task via ToTask() or casting", e);
			}
		}
		
		//****************************************

		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		TimeSpan ISponsor.Renewal(ILease lease)
		{
			if (_TaskSource.Task.IsCompleted)
				return TimeSpan.Zero;
			
			return lease.RenewOnCallTime;
		}
		
		//****************************************
		
		internal bool TrySetCancelled()
		{
			return _TaskSource.TrySetCanceled();
		}
		
		internal bool TrySetException(Exception ex)
		{
			return _TaskSource.TrySetException(ex);
		}

		internal bool TrySetResult(TResult result)
		{
			return _TaskSource.TrySetResult(result);
		}
		
		//****************************************
		
		internal Task<TResult> Task
		{
			get { return _TaskSource.Task; }
		}
	}
}
