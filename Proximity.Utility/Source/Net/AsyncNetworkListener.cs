/****************************************\
 AsyncNetworkListener.cs
 Created: 2014-10-02
\****************************************/
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
			
			BeginAcceptConnection(false);
		}
		
		//****************************************
		
		private void BeginAcceptConnection(bool inAccept)
		{
			try
			{
				_EventArgs.AcceptSocket = null;
				_EventArgs.AcceptAsync(_Socket);
				_IsListening = true;
	
				if (_EventArgs.IsCompleted)
				{
					// If we've come directly from ProcessComplete, don't call directly again since we can blow the stack
					if (inAccept)
						ThreadPool.QueueUserWorkItem((state) => ProcessCompleteAccept(false));
					else
						ProcessCompleteAccept(true);
				}
				else
				{
					((INotifyCompletion)_EventArgs).OnCompleted(() => ProcessCompleteAccept(false));
				}
			}
			catch (ObjectDisposedException)
			{
				_IsListening = false;
			}
			catch (Exception)
			{
				// TODO: Exception handling
				_IsListening = false;
			}
		}
		
		private void ProcessCompleteAccept(bool inAccept)
		{	//****************************************
			Socket MySocket = null;
			//****************************************
			
			switch (_EventArgs.SocketError)
			{
			case SocketError.Success:
				MySocket = _EventArgs.AcceptSocket;
				break;
				
			case SocketError.OperationAborted: // Listener is shutting down
				_IsListening = false;
				return;
				
			case SocketError.ConnectionReset: // Can happen due to a port-scan
			default: // What about other errors?
				break;
			}
			
			//****************************************
			
			// Start accepting again
			if (inAccept)
			{
				// We're within BeginAccept already, so push that to another thread
				ThreadPool.QueueUserWorkItem((state) => BeginAcceptConnection(false), null);
			}
			else
			{
				// Call BeginAccept immediately, in non-re-entrant mode
				BeginAcceptConnection(true);
			}
			
			//****************************************
			
			// Handle the new connection
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
	}
}
