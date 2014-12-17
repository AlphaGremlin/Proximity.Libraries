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
	/// Description of AsyncListener.
	/// </summary>
	public abstract class AsyncNetworkListener : IDisposable
	{	//****************************************
		private readonly Socket _Socket;
		private readonly EndPoint _EndPoint;
		
		private SocketAwaitableEventArgs _EventArgs = new SocketAwaitableEventArgs();
		//****************************************
		
		public AsyncNetworkListener(Socket socket, EndPoint endPoint)
		{
			_Socket = socket;
			_EndPoint = endPoint;
			
			_Socket.Bind(endPoint);
		}
		
		//****************************************
		
		public void Listen(int maxPending)
		{
			_Socket.Listen(maxPending);
			
			BeginAcceptConnection(false);
		}
		
		//****************************************
		
		protected abstract void AcceptConnection(Socket socket);
		
		//****************************************
		
		private void BeginAcceptConnection(bool inAccept)
		{
			try
			{
				_EventArgs.AcceptSocket = null;
				_EventArgs.AcceptAsync(_Socket);
	
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
			}
			catch (Exception e)
			{
				// TODO: Exception handling
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
				
			case SocketError.ConnectionReset: // Can happen due to a port-scan
				break;
				
			case SocketError.OperationAborted:
				return;
				
			default:
				// TODO: Handle failure
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
				AcceptConnection(MySocket);
		}

		//****************************************
		
		public virtual void Dispose()
		{
			_EventArgs.Dispose();
			_Socket.Dispose();
		}
	}
}
