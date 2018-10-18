using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
	/// Provides access to a cancellation token in another AppDomain
	/// </summary>
	/// <remarks>Exists in the calling AppDomain, and must be wrapped in a Using statement around any Awaits in order to preserve the lifetime of the remote object</remarks>
	public sealed class RemoteCancellationToken : MarshalByRefObject, IDisposable
	{ //****************************************
		private readonly CancellationTokenRegistration _Registration;
		
		[SecurityCritical]
		private ImmutableHashSet<RemoteCancellationTokenSource> _TokenSources = ImmutableHashSet<RemoteCancellationTokenSource>.Empty;
		//****************************************
		
		/// <summary>
		/// Creates a new Remote Cancellation Token suitable for passing between AppDomains
		/// </summary>
		/// <param name="token">The Cancellation Token this Remote token will listen to cancellation requests for</param>
		[SecuritySafeCritical]
		public RemoteCancellationToken(CancellationToken token)
		{
			Token = token;
			Token.Register(OnCancel);
		}
		
		/// <summary>
		/// Creates a new Remote Cancellation Token suitable for passing between AppDomains
		/// </summary>
		/// <param name="tokenSource">The Cancellation Token Source this Remote token will listen to cancellation requests for</param>
		public RemoteCancellationToken(CancellationTokenSource tokenSource) : this(tokenSource.Token)
		{
		}

		//****************************************

		/// <inheritdoc />
		[SecurityCritical]
		public override object InitializeLifetimeService() => null; // Last until the Cancellation Token we're attached to is cancelled or we're disposed

		/// <summary>
		/// Disposes of this Remote Cancellation Token
		/// </summary>
		[SecuritySafeCritical]
		public void Dispose()
		{
			_Registration.Dispose();

			Unregister();
		}
		
		//****************************************

		[SecuritySafeCritical]
		internal void Attach(RemoteCancellationTokenSource tokenSource)
		{
			Debug.Assert(RemotingServices.IsObjectOutOfAppDomain(tokenSource), "Attempt to unwrap remote token source inside the owning AppDomain");
			
			ImmutableInterlockedEx.Add(ref _TokenSources, tokenSource);
			
			// Cancel the remote token source if we're already cancelled
			// This will end up calling Detach, so do it at the end to ensure bookkeeping is consistent
			if (Token.IsCancellationRequested)
				tokenSource.Cancel();
		}

		[SecuritySafeCritical]
		internal void Detach(RemoteCancellationTokenSource tokenSource)
		{
			ImmutableInterlockedEx.Remove(ref _TokenSources, tokenSource);
		}
		
		//****************************************

		[SecuritySafeCritical]
		private void OnCancel()
		{
			// Since this token may be awaited by multiple remote calls, cancel each remote token source
			foreach(var MyRemoteSource in _TokenSources)
				MyRemoteSource.Cancel();

			// All sources have been cancelled, but others might still attach (and immediately cancel, of course), so we can't Unregister
		}

		//****************************************

		[SecuritySafeCritical]
		private void Unregister() => RemotingServices.Disconnect(this);

		//****************************************

		/// <summary>
		/// Gets whether cancellation has been requested on the remote token
		/// </summary>
		public bool IsCancellationRequested => Token.IsCancellationRequested;

		/// <summary>
		/// Gets whether the remote token can be cancelled
		/// </summary>
		public bool CanBeCanceled => Token.CanBeCanceled;

		internal CancellationToken Token { get; }

		//****************************************

		/// <summary>
		/// Transforms a Cancellation Token into a RemoteCancellationToken suitable for passing between AppDomains
		/// </summary>
		/// <param name="token">The Cancellation Token to transfork</param>
		/// <returns>A RemoteCancellationToken that can pass cancellation requests to another AppDomain</returns>
		/// <remarks>When calling remote objects supporting cancellation, the RemoteCancellationToken must persist for the lifetime of the call. Wrap the call with a Using statement and await the RemoteTask inside.</remarks>
		public static RemoteCancellationToken FromLocal(CancellationToken token) => new RemoteCancellationToken(token);

		/// <summary>
		/// Transforms a Cancellation Token Source into a RemoteCancellationToken suitable for passing between AppDomains
		/// </summary>
		/// <param name="tokenSource">The Cancellation Token Source to transfork</param>
		/// <returns>A RemoteCancellationToken that can pass cancellation requests to another AppDomain</returns>
		/// <remarks>When calling remote objects supporting cancellation, the RemoteCancellationToken must persist for the lifetime of the call. Wrap the call with a Using statement and await the RemoteTask inside.</remarks>
		public static RemoteCancellationToken FromLocal(CancellationTokenSource tokenSource) => new RemoteCancellationToken(tokenSource.Token);
	}
}