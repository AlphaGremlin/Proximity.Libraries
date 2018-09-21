/****************************************\
 SocketAwaitable.cs
 Created: 2013-10-01
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Net
{
	/// <summary>
	/// Provides an awaitable object around the Socket XxAsync methods
	/// </summary>
	/// <remarks>Based on the code by Stephen Taub at http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx</remarks>
	public sealed class SocketAwaitable : INotifyCompletion
	{	//****************************************
		private readonly static Action HasCompleted = () => { };
		//****************************************
		private readonly SocketAsyncEventArgs _EventArgs;
		private bool _IsCompleted;
		private Action _Continuation;
		//****************************************
		
		/// <summary>
		/// Creates a new awaitable object
		/// </summary>
		/// <param name="eventArgs">The event arguments this awaitable will wrap</param>
		public SocketAwaitable(SocketAsyncEventArgs eventArgs)
		{
			if (eventArgs == null)
				throw new ArgumentNullException("eventArgs");
			
			_EventArgs = eventArgs;
			_EventArgs.Completed += OnCompletedAction;
		}
		
		//****************************************
		
		internal void Reset()
		{
			_IsCompleted = false;
			_Continuation = null;
		}
		
		/// <summary>
		/// Gets an awaiter to await this Socket event
		/// </summary>
		/// <returns>The awaiter</returns>
		public SocketAwaitable GetAwaiter()
		{
			return this;
		}
		
		/// <summary>
		/// Gets the result of this awaitable
		/// </summary>
		/// <exception cref="SocketException">Throws a socket exception if the action failed</exception>
		public void GetResult()
		{
			if (_EventArgs.SocketError != SocketError.Success)
				throw new SocketException((int)_EventArgs.SocketError);
		}
#if (ANDROID || IOS)
		/// <summary>
		/// Performs an asynchronous operation to connect to a socket
		/// </summary>
		/// <param name="socketType">The type of socket</param>
		/// <param name="protocolType">The protocol of the socket</param>
		/// <returns>The passed in awaitable</returns>
		public SocketAwaitable ConnectAsync(SocketType socketType, ProtocolType protocolType)
		{
			Reset();

			var MySocket = new Socket(_EventArgs.RemoteEndPoint.AddressFamily, socketType, protocolType);

			if (MySocket.ConnectAsync(_EventArgs))
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
		public SocketAwaitable ConnectAsync(SocketType socketType, ProtocolType protocolType)
		{
			Reset();

			if (!Socket.ConnectAsync(socketType, protocolType, _EventArgs))
				_IsCompleted = true;

			return this;
		}
#endif

		//****************************************
		
		void INotifyCompletion.OnCompleted(Action continuation)
		{
			if (_Continuation == HasCompleted || Interlocked.CompareExchange(ref _Continuation, continuation, null) == HasCompleted)
			{
#if NET40
				TaskEx.Run(continuation);
#else
				Task.Run(continuation);
#endif
			}
		}
		
		private void OnCompletedAction(object sender, SocketAsyncEventArgs e)
		{
			var MyContinuation = _Continuation ?? Interlocked.CompareExchange<Action>(ref _Continuation, HasCompleted, null);
			
			if (MyContinuation != null)
				MyContinuation();
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
		
		internal SocketAsyncEventArgs EventArgs
		{
			get { return _EventArgs; }
		}
	}
}
#endif