/****************************************\
 SocketAwaitableEventArgs.cs
 Created: 2014-12-16
\****************************************/
#if !PORTABLE
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Net
{
	/// <summary>
	/// Provides a SocketAsyncEventArgs that can be awaited
	/// </summary>
	/// <remarks>Based on the code by Stephen Taub at http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx</remarks>
	public sealed class SocketAwaitableEventArgs : SocketAsyncEventArgs, INotifyCompletion
	{	//****************************************
		private readonly static Action HasCompleted = () => { };
		//****************************************
		private bool _IsCompleted;
		private Action _Continuation;
		//****************************************
		
		/// <summary>
		/// Creates a new Socket Awaitable Event Args
		/// </summary>
		public SocketAwaitableEventArgs() : base()
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets an awaiter to await this Socket event
		/// </summary>
		/// <returns>The awaiter</returns>
		public SocketAwaitableEventArgs GetAwaiter()
		{
			return this;
		}
		
		/// <summary>
		/// Gets the result of this awaitable
		/// </summary>
		/// <exception cref="SocketException">Throws a socket exception if the action failed</exception>
		public void GetResult()
		{
			if (base.SocketError != SocketError.Success)
				throw new SocketException((int)base.SocketError);
		}

#if MOBILE
		/// <summary>
		/// Performs an asynchronous operation to connect to a socket
		/// </summary>
		/// <param name="socketType">The type of socket</param>
		/// <param name="protocolType">The protocol of the socket</param>
		/// <returns>The passed in awaitable</returns>
		public SocketAwaitableEventArgs ConnectAsync(SocketType socketType, ProtocolType protocolType)
		{
			Reset();

			var MySocket = new Socket(RemoteEndPoint.AddressFamily, socketType, protocolType);

			if (MySocket.ConnectAsync(this))
				_IsCompleted = true;

			return this;
		}
#else
		/// <summary>
		/// Performs an asynchronous operation to connect to a socket
		/// </summary>
		/// <param name="socketType">The type of socket</param>
		/// <param name="protocolType">The protocol of the socket</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ConnectAsync(SocketType,ProtocolType,SocketAsyncEventArgs)" /> for more information</remarks>
		public SocketAwaitableEventArgs ConnectAsync(SocketType socketType, ProtocolType protocolType)
		{
			Reset();

			if (!Socket.ConnectAsync(socketType, protocolType, this))
				_IsCompleted = true;

			return this;
		}
#endif
		
		/// <summary>
		/// Performs an asynchronous operation to accept a socket connection
		/// </summary>
		/// <param name="socket">The socket receiving the connection</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.AcceptAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs AcceptAsync(Socket socket)
		{
			Reset();
			
			if (!socket.AcceptAsync(this))
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Performs an asynchronous operation to connect to a socket
		/// </summary>
		/// <param name="socket">The socket to begin connecting</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ConnectAsync(SocketAsyncEventArgs)" /> for more information</remarks>
		public SocketAwaitableEventArgs ConnectAsync(Socket socket)
		{
			Reset();
			
			if (!socket.ConnectAsync(this))
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to disconnect to a socket
		/// </summary>
		/// <param name="socket">The socket to disconnect</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.DisconnectAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs DisconnectAsync(Socket socket)
		{
			Reset();
			
			if (!socket.DisconnectAsync(this))
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ReceiveAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs ReceiveAsync(Socket socket)
		{
			Reset();
			
			if (!socket.ReceiveAsync(this))
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket from a specific device
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ReceiveFromAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs ReceiveFromAsync(Socket socket)
		{
			Reset();
			
			if (!socket.ReceiveFromAsync(this)) 
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ReceiveMessageFromAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs ReceiveMessageFromAsync(Socket socket)
		{
			Reset();
			
			if (!socket.ReceiveMessageFromAsync(this)) 
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to send data over a socket
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.SendAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs SendAsync(Socket socket)
		{
			Reset();
			
			if (!socket.SendAsync(this)) 
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to send a collection of data over a socket
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.SendPacketsAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs SendPacketsAsync(Socket socket)
		{
			Reset();
			
			if (!socket.SendPacketsAsync(this)) 
				_IsCompleted = true;
			
			return this;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to send data over a socket to a specific device
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.SendToAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs SendToAsync(Socket socket)
		{
			Reset();
			
			if (!socket.SendToAsync(this)) 
				_IsCompleted = true;
			
			return this;
		}

		/// <summary>
		/// Queues an action to run when the socket operation is completed
		/// </summary>
		/// <param name="continuation">The continuation to call</param>
		public void OnCompleted(Action continuation)
		{	//****************************************
			Action OldContinuation, NewContinuation;
			//****************************************
			
			do
			{
				OldContinuation = _Continuation;
				
				if (OldContinuation == HasCompleted)
				{
#if NET40
					TaskEx.Run(continuation);
#else
					Task.Run(continuation);
#endif
					
					return;
				}
				
				NewContinuation = (Action)Delegate.Combine(OldContinuation, continuation);
			} while (Interlocked.CompareExchange<Action>(ref _Continuation, NewContinuation, OldContinuation) != OldContinuation);
		}
		
		//****************************************
		
		/// <inheritdoc />
		protected override void OnCompleted(SocketAsyncEventArgs e)
		{	//****************************************
			var MyContinuation = Interlocked.Exchange<Action>(ref _Continuation, HasCompleted);
			//****************************************

			if (MyContinuation != null)
				MyContinuation();
			
			base.OnCompleted(e);
		}
		
		//****************************************
		
		private void Reset()
		{
			_IsCompleted = false;
			_Continuation = null;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets whether the awaitable operation has completed
		/// </summary>
		/// <remarks>If true, can be recycled</remarks>
		public bool IsCompleted
		{
			get { return _IsCompleted; }
		}
	}
}
#endif