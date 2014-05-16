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
	/// Receives notifications from a cancellation token in another AppDomain
	/// </summary>
	internal sealed class RemoteCancellationTokenSource : MarshalByRefObject, ISponsor, IDisposable
	{	//****************************************
		private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
		
		private readonly RemoteCancellationToken _Token;
		
		private bool _IsDisposed;
		//****************************************
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		internal RemoteCancellationTokenSource(RemoteCancellationToken remoteToken)
		{
			Debug.Assert(RemotingServices.IsObjectOutOfAppDomain(remoteToken), "Attempt to unwrap remote token inside the owning AppDomain");
			
			_Token = remoteToken;
		}
		
		//****************************************
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		public void Dispose()
		{
			// Task has completed (possibly by cancellation), we can detach from the remote token
			_Token.Detach(this);
			
			// No longer need to sponsor the remote token connection
			var MyLease = (ILease)RemotingServices.GetLifetimeService(_Token);
			
			MyLease.Unregister(this);
			
			_IsDisposed = true;
		}
		
		//****************************************
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		TimeSpan ISponsor.Renewal(ILease lease)
		{
			// Ensure we keep the remote token connection alive until we've been disposed upon completion of the task
			if (_IsDisposed)
				return TimeSpan.Zero;
			
			return lease.RenewOnCallTime;
		}
		
		//****************************************
		
		internal void Attach()
		{
			// Sponsor the remote token so it doesn't get disconnected if the operation takes a long time
			var MyLease = (ILease)RemotingServices.GetLifetimeService(_Token);
			
			MyLease.Register(this);
			
			// Attach to the remote token so it will cancel us
			_Token.Attach(this);
		}
		
		internal void Cancel()
		{
			// Received a cancellation from the parent AppDomain, raise the local CancellationTokenSource
			_TokenSource.Cancel();
		}
		
		//****************************************
		
		internal CancellationToken Token
		{
			get { return _TokenSource.Token; }
		}
	}
}
