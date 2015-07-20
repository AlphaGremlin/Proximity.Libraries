/****************************************\
 RemoteTaskCompletionSource.cs
 Created: 2014-05-14
\****************************************/
#if NET45 && !MOBILE && !PORTABLE
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility.Collections;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides access to a cancellation token in another AppDomain
	/// </summary>
	/// <remarks>Exists in the calling AppDomain, and must be wrapped in a Using statement around any Awaits in order to preserve the lifetime of the remote object</remarks>
	public sealed class RemoteCancellationToken : MarshalByRefObject, IDisposable, ISponsor
	{	//****************************************
		private readonly CancellationToken _Token;
		
		private readonly CancellationTokenRegistration _Registration;
		
		private ImmutableHashSet<RemoteCancellationTokenSource> _TokenSources = ImmutableHashSet<RemoteCancellationTokenSource>.Empty;
		private bool _IsDisposed;
		//****************************************
		
		/// <summary>
		/// Creates a new Remote Cancellation Token suitable for passing between AppDomains
		/// </summary>
		/// <param name="token">The Cancellation Token this Remote token will listen to cancellation requests for</param>
		public RemoteCancellationToken(CancellationToken token)
		{
			_Token = token;
			_Token.Register(OnCancel);
		}
		
		/// <summary>
		/// Creates a new Remote Cancellation Token suitable for passing between AppDomains
		/// </summary>
		/// <param name="tokenSource">The Cancellation Token Source this Remote token will listen to cancellation requests for</param>
		public RemoteCancellationToken(CancellationTokenSource tokenSource) : this(tokenSource.Token)
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of this Remote Cancellation Token
		/// </summary>
		public void Dispose()
		{
			_Registration.Dispose();

			_IsDisposed = true;
		}
		
		//****************************************
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		internal void Attach(RemoteCancellationTokenSource tokenSource)
		{
			Debug.Assert(RemotingServices.IsObjectOutOfAppDomain(tokenSource), "Attempt to unwrap remote token source inside the owning AppDomain");
			
			// Sponsor the remote token source so it doesn't get disconnected if the operation takes a long time
			var MyLease = (ILease)RemotingServices.GetLifetimeService(tokenSource);
			
			MyLease.Register(this);
			
			ImmutableInterlockedEx.Add(ref _TokenSources, tokenSource);
			
			// Cancel the remote token source if we're already cancelled
			// This will end up calling Detach, so do it at the end to ensure bookkeeping is consistent
			if (_Token.IsCancellationRequested)
				tokenSource.Cancel();
		}
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		internal void Detach(RemoteCancellationTokenSource tokenSource)
		{
			ImmutableInterlockedEx.Remove(ref _TokenSources, tokenSource);
			
			var MyLease = (ILease)RemotingServices.GetLifetimeService(tokenSource);
			
			MyLease.Unregister(this);
		}
		
		//****************************************
		
		private void OnCancel()
		{
			// Since this token may be awaited by multiple remote calls, cancel each remote token source
			foreach(var MyRemoteSource in _TokenSources)
				MyRemoteSource.Cancel();
		}
		
		//****************************************
		
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.Infrastructure)]
		TimeSpan ISponsor.Renewal(ILease lease)
		{
			// Ensure we keep the remote token source connection alive until we've been disposed
			if (_IsDisposed)
				return TimeSpan.Zero;
			
			return lease.RenewOnCallTime;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets whether cancellation has been requested on the remote token
		/// </summary>
		public bool IsCancellationRequested
		{
			get { return _Token.IsCancellationRequested; }
		}
		
		/// <summary>
		/// Gets whether the remote token can be cancelled
		/// </summary>
		public bool CanBeCanceled
		{
			get { return _Token.CanBeCanceled; }
		}
		
		internal CancellationToken Token
		{
			get { return _Token; }
		}
		
		//****************************************
		
		/// <summary>
		/// Transforms a Cancellation Token into a RemoteCancellationToken suitable for passing between AppDomains
		/// </summary>
		/// <param name="token">The Cancellation Token to transfork</param>
		/// <returns>A RemoteCancellationToken that can pass cancellation requests to another AppDomain</returns>
		/// <remarks>When calling remote objects supporting cancellation, the RemoteCancellationToken must persist for the lifetime of the call. Wrap the call with a Using statement and await the RemoteTask inside.</remarks>
		public static RemoteCancellationToken FromLocal(CancellationToken token)
		{
			return new RemoteCancellationToken(token);
		}
		
		/// <summary>
		/// Transforms a Cancellation Token Source into a RemoteCancellationToken suitable for passing between AppDomains
		/// </summary>
		/// <param name="tokenSource">The Cancellation Token Source to transfork</param>
		/// <returns>A RemoteCancellationToken that can pass cancellation requests to another AppDomain</returns>
		/// <remarks>When calling remote objects supporting cancellation, the RemoteCancellationToken must persist for the lifetime of the call. Wrap the call with a Using statement and await the RemoteTask inside.</remarks>
		public static RemoteCancellationToken FromLocal(CancellationTokenSource tokenSource)
		{
			return new RemoteCancellationToken(tokenSource.Token);
		}
	}
}
#endif