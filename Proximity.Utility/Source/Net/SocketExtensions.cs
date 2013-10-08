/****************************************\
 SocketExtensions.cs
 Created: 2013-10-01
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
	/// Provides extensions for sockets to have an awaitable interface
	/// </summary>
	/// <remarks>Based on the code by Stephen Taub at http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx</remarks>
	public static class SocketExtensions
	{
		/// <summary>
		/// Performs an asynchronous operation to accept a socket connection
		/// </summary>
		/// <param name="socket">The socket receiving the connection</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.AcceptAsync" /> for more information</remarks>
		public static SocketAwaitable AcceptAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.AcceptAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Performs an asynchronous operation to connect to a socket
		/// </summary>
		/// <param name="socket">The socket to begin connecting</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ConnectAsync(SocketAsyncEventArgs)" /> for more information</remarks>
		public static SocketAwaitable ConnectAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ConnectAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to disconnect to a socket
		/// </summary>
		/// <param name="socket">The socket to disconnect</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.DisconnectAsync" /> for more information</remarks>
		public static SocketAwaitable DisconnectAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.DisconnectAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ReceiveAsync" /> for more information</remarks>
		public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ReceiveAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket from a specific device
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ReceiveFromAsync" /> for more information</remarks>
		public static SocketAwaitable ReceiveFromAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ReceiveFromAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to receive data from a socket
		/// </summary>
		/// <param name="socket">The socket to receive data on</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.ReceiveMessageFromAsync" /> for more information</remarks>
		public static SocketAwaitable ReceiveMessageFromAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ReceiveMessageFromAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to send data over a socket
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.SendAsync" /> for more information</remarks>
		public static SocketAwaitable SendAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.SendAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to send a collection of data over a socket
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.SendPacketsAsync" /> for more information</remarks>
		public static SocketAwaitable SendPacketsAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.SendPacketsAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		/// <summary>
		/// Begins an asynchronous operation to send data over a socket to a specific device
		/// </summary>
		/// <param name="socket">The socket to send data to</param>
		/// <param name="awaitable">An awaitable around the arguments object we're using</param>
		/// <returns>The passed in awaitable</returns>
		/// <remarks>See <see cref="Socket.SendToAsync" /> for more information</remarks>
		public static SocketAwaitable SendToAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.SendToAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
	}
}
