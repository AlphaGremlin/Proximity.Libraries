/****************************************\
 AsyncNetworkListener.cs
 Created: 2014-10-02
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Net
{
	/// <summary>
	/// Provides a base class for async socket listeners
	/// </summary>
	
	public sealed class AsyncNetworkListener : IDisposable
	{	//****************************************
		private readonly Socket _Socket;
		private readonly EndPoint _EndPoint;
		
		private readonly SocketAwaitableEventArgs _EventArgs = new SocketAwaitableEventArgs();
		
		private readonly Action<AsyncNetworkListener, Socket> _Callback;
		
		private bool _IsListening;
		//****************************************
		
		/// <summary>
		/// Creates a new Async Network Listener
		/// </summary>
		/// <param name="socket">A socket to listen on</param>
		/// <param name="endPoint">The details of the endpoint to bind to</param>
		/// <param name="callback">The callback to raise when a connection comes in</param>
		public AsyncNetworkListener(Socket socket, EndPoint endPoint, Action<AsyncNetworkListener, Socket> callback)
		{
			_Socket = socket;
			_EndPoint = endPoint;
			_Callback = callback;
			
			_Socket.Bind(endPoint);
		}
		
		//****************************************
		
		/// <summary>
		/// Begin listening for incoming connections
		/// </summary>
		/// <param name="maxPending"></param>
		public void Listen(int maxPending)
		{
			_Socket.Listen(maxPending);
			
			_IsListening = true;
			
			AcceptConnection(false);
		}
		
		//****************************************

		
		private void AcceptConnection(bool inComplete)
		{
			while (_IsListening)
			{
				// If we're not coming from a completion, then call Accept
				if (!inComplete)
				{
					try
					{
						_EventArgs.AcceptSocket = null;
						;
			
						// If we don't get a new connection immediately, queue the continuation and return
						if (!_EventArgs.Accept(_Socket))
						{
							_EventArgs.OnCompleted(ProcessCompleteAccept);
							
							return;
						}
					}
					catch (ObjectDisposedException)
					{
						_IsListening = false;
						
						return;
					}
					catch (Exception)
					{
						// TODO: Exception handling
						_IsListening = false;
						
						return;
					}
				}
				
				// Either we've come from the completion, or we called Accept and it immediately succeeded
				switch (_EventArgs.SocketError)
				{
				case SocketError.Success:
					// Got a connection
					ThreadPool.UnsafeQueueUserWorkItem(ProcessConnection, _EventArgs.AcceptSocket);
					break;
					
				case SocketError.OperationAborted: // Listener is shutting down
					_IsListening = false;
					return;
					
				case SocketError.ConnectionReset: // Can happen due to a port-scan
				default: // TODO: What about other errors?
					break;
				}
				
				// Now loop back and call accept again
				inComplete = false;
			}
		}
		
		private void ProcessCompleteAccept()
		{
			AcceptConnection(true);
		}
		
		private void ProcessConnection(object state)
		{	//****************************************
			var MySocket = (Socket)state;
			//****************************************
			
			if (MySocket != null)
				_Callback(this, MySocket);
		}
		
		//****************************************
		
		/// <summary>
		/// Disposes of the listener
		/// </summary>
		public void Dispose()
		{
			_EventArgs.Dispose();
			_Socket.Dispose();
		}
		
		//****************************************
		
		/// <summary>
		/// Gets whether we're listening on the desired port
		/// </summary>
		public bool IsListening
		{
			get { return _IsListening; }
		}

		/// <summary>
		/// Gets the original endpoint passed in
		/// </summary>
		public EndPoint OriginalEndPoint
		{
			get { return _EndPoint; }
		}

		/// <summary>
		/// Gets the endpoint being listened to
		/// </summary>
		public EndPoint EndPoint
		{
			get { return _Socket.LocalEndPoint; }
		}
	}
}
#endif