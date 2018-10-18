using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Remoting
{
	/// <summary>
	/// Receives notifications from a cancellation token in another AppDomain
	/// </summary>
	internal sealed class RemoteCancellationTokenSource : MarshalByRefObject, IDisposable
	{	//****************************************
		private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
		
		private readonly RemoteCancellationToken _Token;
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
		}

		[SecurityCritical]
		public override object InitializeLifetimeService() => null; // Last until the Remote Cancellation Token we're attached to is cancelled or we're disposed

		//****************************************

		[SecuritySafeCritical]
		private void Unregister() => RemotingServices.Disconnect(this);

		//****************************************

		internal void Attach() =>
			// Attach to the remote token so it will cancel us
			_Token.Attach(this);

		internal void Cancel() =>
			// Received a cancellation from the parent AppDomain, raise the local CancellationTokenSource
			_TokenSource.Cancel();

		//****************************************

		internal CancellationToken Token => _TokenSource.Token;
	}
}