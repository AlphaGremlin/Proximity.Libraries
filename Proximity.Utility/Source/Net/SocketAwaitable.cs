/****************************************\
 SocketAwaitable.cs
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
		
		//****************************************
		
		void INotifyCompletion.OnCompleted(Action continuation)
		{
			if (_Continuation == HasCompleted || Interlocked.CompareExchange(ref _Continuation, continuation, null) == HasCompleted)
			{
				Task.Run(continuation);
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
