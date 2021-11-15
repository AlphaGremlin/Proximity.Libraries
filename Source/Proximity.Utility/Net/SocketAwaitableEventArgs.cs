/****************************************\
 SocketAwaitableEventArgs.cs
 Created: 2014-12-16
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security;
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

#if !MOBILE // These methods don't exist in Xamarin
		/// <summary>
		/// Starts an asynchronous operation to connect to a socket
		/// </summary>
		/// <param name="socketType">The type of socket</param>
		/// <param name="protocolType">The protocol of the socket</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.ConnectAsync(SocketType,ProtocolType,SocketAsyncEventArgs)" /> for more information</remarks>
		public bool Connect(SocketType socketType, ProtocolType protocolType)
		{
			Reset();

			return Complete(Socket.ConnectAsync(socketType, protocolType, this));
		}

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

			Complete(Socket.ConnectAsync(socketType, protocolType, this));

			return this;
		}
#endif

		/// <summary>
		/// Performs an asynchronous operation to accept a socket connection
		/// </summary>
		/// <param name="socket">The socket receiving the connection</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.AcceptAsync" /> for more information</remarks>
		public bool Accept(Socket socket)
		{
			Reset();

			return Complete(socket.AcceptAsync(this));
		}

		/// <summary>
		/// Performs an asynchronous operation to accept a socket connection
		/// </summary>
		/// <param name="socket">The socket receiving the connection</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.AcceptAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs AcceptAsync(Socket socket)
		{
			Reset();

			Complete(socket.AcceptAsync(this));

			return this;
		}

		/// <summary>
		/// Performs an asynchronous operation to connect to a socket
		/// </summary>
		/// <param name="socket">The socket to begin connecting</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.ConnectAsync(SocketAsyncEventArgs)" /> for more information</remarks>
		public bool Connect(Socket socket)
		{
			Reset();

			return Complete(socket.ConnectAsync(this));
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

			Complete(socket.ConnectAsync(this));

			return this;
		}

		/// <summary>
		/// Begins an asynchronous operation to disconnect to a socket
		/// </summary>
		/// <param name="socket">The socket to disconnect</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.DisconnectAsync" /> for more information</remarks>
		public bool Disconnect(Socket socket)
		{
			Reset();

			return Complete(socket.DisconnectAsync(this));
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

			Complete(socket.DisconnectAsync(this));

			return this;
		}

		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.ReceiveAsync" /> for more information</remarks>
		public bool Receive(Socket socket)
		{
			Reset();

			return Complete(socket.ReceiveAsync(this));
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

			Complete(socket.ReceiveAsync(this));

			return this;
		}

		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket from a specific device
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.ReceiveFromAsync" /> for more information</remarks>
		public bool ReceiveFrom(Socket socket)
		{
			Reset();

			return Complete(socket.ReceiveFromAsync(this));
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

			Complete(socket.ReceiveFromAsync(this));

			return this;
		}

		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.ReceiveMessageFromAsync" /> for more information</remarks>
		public bool ReceiveMessageFrom(Socket socket)
		{
			Reset();

			return Complete(socket.ReceiveMessageFromAsync(this));
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

			Complete(socket.ReceiveMessageFromAsync(this));

			return this;
		}

		/// <summary>
		/// Begins an asynchronous operation to send data over a socket
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.SendAsync" /> for more information</remarks>
		public bool Send(Socket socket)
		{
			Reset();

			return Complete(socket.SendAsync(this));
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

			Complete(socket.SendAsync(this));

			return this;
		}

		/// <summary>
		/// Begins an asynchronous operation to send a collection of data over a socket
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.SendPacketsAsync" /> for more information</remarks>
		public bool SendPackets(Socket socket)
		{
			Reset();

			return Complete(socket.SendPacketsAsync(this));
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

			Complete(socket.SendPacketsAsync(this));

			return this;
		}

		/// <summary>
		/// Begins an asynchronous operation to send data over a socket to a specific device
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <returns>True if we completed synchronously, otherwise False</returns>
		/// <remarks>See <see cref="Socket.SendToAsync" /> for more information</remarks>
		public bool SendTo(Socket socket)
		{
			Reset();

			return Complete(socket.SendToAsync(this));
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

			Complete(socket.SendToAsync(this));

			return this;
		}

		/// <summary>
		/// Sets an action to run when the socket operation is completed
		/// </summary>
		/// <param name="continuation">The continuation to call</param>
		public void OnCompleted(Action continuation)
		{
			// If we're set to HasCompleted, we either completed synchronously or in the background before the continuation was queued
			// Otherwise, try and swap the continuation from null to our method. If this fails, we completed in the background
			if (_Continuation == HasCompleted || Interlocked.CompareExchange(ref _Continuation, continuation, null) == HasCompleted)
			{
#if NET40
				TaskEx.Run(continuation);
#else
				Task.Run(continuation);
#endif
			}
		}

		//****************************************

		/// <inheritdoc />
		
		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			// Try and retrieve our continuation or, if it's null, set it to HasCompleted so if/when a continuation is set, we'll call it
			var MyContinuation = _Continuation ?? Interlocked.CompareExchange<Action>(ref _Continuation, HasCompleted, null);

			// If a contiuation is set, call it
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

		private bool Complete(bool isWaiting)
		{
			if (isWaiting)
				return false;

			_IsCompleted = true;

			if (Interlocked.CompareExchange<Action>(ref _Continuation, HasCompleted, null) != null)
				throw new InvalidOperationException("Operation is already in progress");

			return true;
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