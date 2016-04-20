/****************************************\
 RemoteTaskCompletionSource.cs
 Created: 2014-05-14
\****************************************/
#if NET45 && !MOBILE && !PORTABLE
using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Security;
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
	internal sealed class RemoteTaskCompletionSource<TResult> : MarshalByRefObject
	{	//****************************************
		private readonly TaskCompletionSource<TResult> _TaskSource;
		
		private readonly MarshalByRefObject _RemoteTask;
		//****************************************
		
		[SecuritySafeCritical]
		internal RemoteTaskCompletionSource(MarshalByRefObject remoteTask)
		{
			Debug.Assert(RemotingServices.IsObjectOutOfAppDomain(remoteTask), "Attempt to unwrap remote task inside the owning AppDomain");
			
			_RemoteTask = remoteTask;
			
			// Create the local TaskCompletionSource, and attach ourselves to it.
			// Otherwise, remoting will be the only thing with a reference to us, and we may end up garbage collected
			_TaskSource = new TaskCompletionSource<TResult>(this);
		}
		
		//****************************************

		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null; // Last until the Remote Task we're attached to is completed
		}

		//****************************************
		
		internal void SetCancelled()
		{
			_TaskSource.TrySetCanceled();

			Unregister();
		}
		
		internal void SetException(Exception ex)
		{
			_TaskSource.TrySetException(ex);

			Unregister();
		}

		internal void SetResult(TResult result)
		{
			_TaskSource.TrySetResult(result);

			Unregister();
		}

		//****************************************

		[SecuritySafeCritical]
		private void Unregister()
		{
			RemotingServices.Disconnect(this);
		}

		//****************************************
		
		internal Task<TResult> Task
		{
			get { return _TaskSource.Task; }
		}
	}
}
#endif