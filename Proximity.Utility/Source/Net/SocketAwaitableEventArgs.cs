﻿/****************************************\
 SocketAwaitableEventArgs.cs
 Created: 2014-12-16
\****************************************/
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
		
		/// <summary>
		/// Performs an asynchronous operation to accept a socket connection
		/// </summary>
		/// <param name="socket">The socket receiving the connection</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
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
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.SendToAsync" /> for more information</remarks>
		public SocketAwaitableEventArgs SendToAsync(Socket socket)
		{
			Reset();
			
			if (!socket.SendToAsync(this)) 
				_IsCompleted = true;
			
			return this;
		}
		
		//****************************************
		
		void INotifyCompletion.OnCompleted(Action continuation)
		{
			if (_Continuation == HasCompleted || Interlocked.CompareExchange(ref _Continuation, continuation, null) == HasCompleted)
			{
				Task.Run(continuation);
			}
		}
		
		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			var MyContinuation = _Continuation ?? Interlocked.CompareExchange<Action>(ref _Continuation, HasCompleted, null);
			
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
			internal set { _IsCompleted = value; }
		}
	}
}
