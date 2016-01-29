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
	/// Receives notifications from a cancellation token in another AppDomain
	/// </summary>
	internal sealed class RemoteCancellationTokenSource : MarshalByRefObject, ISponsor, IDisposable
	{	//****************************************
		private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
		
		private readonly RemoteCancellationToken _Token;
		
		private bool _IsDisposed;
		//****************************************
		
		[SecuritySafeCritical]
		internal RemoteCancellationTokenSource(RemoteCancellationToken remoteToken)
		{
			Debug.Assert(RemotingServices.IsObjectOutOfAppDomain(remoteToken), "Attempt to unwrap remote token inside the owning AppDomain");
			
			_Token = remoteToken;
		}
		
		//****************************************

		[SecuritySafeCritical]
		public void Dispose()
		{
			// Task has completed (possibly by cancellation), we can detach from the remote token
			_Token.Detach(this);

			Unregister();
			
			_IsDisposed = true;
		}

		[SecurityCritical]
		public override object InitializeLifetimeService()
		{	//****************************************
			var MyLease = (ILease)base.InitializeLifetimeService();
			//****************************************

			MyLease.Register(this);

			return MyLease;
		}

		//****************************************
		
		[SecurityCritical]
		TimeSpan ISponsor.Renewal(ILease lease)
		{
			// Ensure we keep the remote token connection alive until we've been disposed upon completion of the task
			if (_IsDisposed)
				return TimeSpan.Zero;
			
			return lease.RenewOnCallTime;
		}

		[SecuritySafeCritical]
		private void Unregister()
		{	//****************************************
			var MyLease = (ILease)RemotingServices.GetLifetimeService(this);
			//****************************************

			if (MyLease != null)
				MyLease.Unregister(this);
		}

		//****************************************
		
		internal void Attach()
		{
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
#endif