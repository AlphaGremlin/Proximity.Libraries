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
		public static SocketAwaitable AcceptAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.AcceptAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable ConnectAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ConnectAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable DisconnectAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.DisconnectAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ReceiveAsync(awaitable.EventArgs))
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable ReceiveFromAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ReceiveFromAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable ReceiveMessageFromAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.ReceiveMessageFromAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable SendAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.SendAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable SendPacketsAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.SendPacketsAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
		
		public static SocketAwaitable SendToAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			
			if (!socket.SendToAsync(awaitable.EventArgs)) 
				awaitable.IsCompleted = true;
			
			return awaitable;
		}
	}
}
