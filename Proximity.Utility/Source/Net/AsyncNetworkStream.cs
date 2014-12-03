/****************************************\
 AsyncNetworkStream.cs
 Created: 2014-10-02
\****************************************/
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Net
{
	/// <summary>
	/// Provides a NetworkStream-esque interface that uses SocketAwaitable and tasks
	/// </summary>
	public sealed class AsyncNetworkStream : Stream
	{	//****************************************
		private readonly Socket _Socket;
		
		private readonly SocketAsyncEventArgs _WriteEventArgs = new SocketAsyncEventArgs(), _ReadEventArgs = new SocketAsyncEventArgs();
		private readonly SocketAwaitable _WriteAwaitable, _ReadAwaitable;
		//****************************************

		/// <summary>
		/// Creates a new Async Network Stream
		/// </summary>
		/// <param name="socket">The socket to wrap</param>
		public AsyncNetworkStream(Socket socket)
		{
			_Socket = socket;
			
			_WriteAwaitable = new SocketAwaitable(_WriteEventArgs);
			_ReadAwaitable = new SocketAwaitable(_ReadEventArgs);
		}

		//****************************************
		
		private Task SendData(byte[] array, int offset, int count, AsyncCallback callback = null, object state = null)
		{
			_WriteEventArgs.BufferList = null;
			_WriteEventArgs.SetBuffer(array, offset, count);

			try
			{
				var Awaiter = _Socket.SendAsync(_WriteAwaitable);

				return WaitForSend(callback, state);
			}
			catch (Exception e)
			{
				var MyTaskSource = new TaskCompletionSource<VoidStruct>(state);
	
				MyTaskSource.SetException(e);
	
				if (callback != null)
					callback(MyTaskSource.Task);
	
				return MyTaskSource.Task;
			}
		}
		
		private Task<int> ReadData(byte[] buffer, int index, int count, AsyncCallback callback = null, object state = null)
		{
			// Prepare a receive buffer
			_ReadEventArgs.SetBuffer(buffer, index, count);

			try
			{
				// Start waiting for some data
				var Awaiter = _Socket.ReceiveAsync(_ReadAwaitable);

				return WaitForReceive(callback, state);
			}
			catch (Exception e)
			{
				// Ensure the completed task we return has the appropriate state
				var MyTaskSource = new TaskCompletionSource<int>(state);
	
				MyTaskSource.SetException(e);
	
				if (callback != null)
					callback(MyTaskSource.Task);
	
				return MyTaskSource.Task;
			}
		}
		
		//****************************************

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			
			if (disposing)
			{
				_ReadEventArgs.Dispose();
				_WriteEventArgs.Dispose();
			}
		}
		
		//****************************************

		/// <inheritdoc />
		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return ReadData(buffer, offset, count, callback, state);
		}

		/// <inheritdoc />
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return SendData(buffer, offset, count, callback, state);
		}

		/// <inheritdoc />
		public override int EndRead(IAsyncResult asyncResult)
		{
			return ((Task<int>)asyncResult).Result;
		}

		/// <inheritdoc />
		public override void EndWrite(IAsyncResult asyncResult)
		{
			((Task)asyncResult).Wait();
		}
		
		/// <inheritdoc />
		public override void Flush()
		{
		}
		
		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult<VoidStruct>(VoidStruct.Empty);
		}

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count == 0)
				return 0;

			var MyTask = ReadData(buffer, offset, count);

			return MyTask.Result;
		}
		
		/// <inheritdoc />
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.Run(delegate() { return 0; }, cancellationToken);
			}
			
			return ReadData(buffer, offset, count);
		}
		
		/// <inheritdoc />
		public override int ReadByte()
		{
			var MyBuffer = new byte[0];

			var MyTask = ReadData(MyBuffer, 0, 1);

			if (MyTask.Result == 0)
				return -1;

			return MyBuffer[0];
		}

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (count == 0)
				return;

			SendData(buffer, offset, count).Wait();
		}

		/// <inheritdoc />
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.Run(delegate() { }, cancellationToken);
			}
			
			return SendData(buffer, offset, count);
		}
		
		/// <inheritdoc />
		public override void WriteByte(byte value)
		{
			SendData(new byte[] { value }, 0, 1).Wait();
		}

		//****************************************
		
		private Task<int> WaitForReceive(AsyncCallback callback, object state)
		{
			var MyTaskSource = new TaskCompletionSource<int>(state);

			if (_ReadAwaitable.IsCompleted)
			{
				ProcessCompletedReceive(MyTaskSource, callback);
			}
			else
			{
				((INotifyCompletion)_ReadAwaitable).OnCompleted(() => ProcessCompletedReceive(MyTaskSource, callback));
			}

			return MyTaskSource.Task;
		}

		private void ProcessCompletedReceive(TaskCompletionSource<int> source, AsyncCallback callback)
		{
			try
			{
				_ReadAwaitable.GetResult();

				//System.Diagnostics.Debug.WriteLine("Received: " + BitConverter.ToString(state._ReadEventArgs.Buffer, 0, state._ReadEventArgs.BytesTransferred));

				source.SetResult(_ReadEventArgs.BytesTransferred);
			}
			catch (Exception e)
			{
				source.SetException(e);
			}

			// Raise the async callback (if any)
			if (callback != null)
				callback(source.Task);
		}

		private Task WaitForSend(AsyncCallback callback, object state)
		{
			var MyTaskSource = new TaskCompletionSource<VoidStruct>(state);

			if (_WriteAwaitable.IsCompleted)
			{
				ProcessCompletedSend(MyTaskSource, callback);
			}
			else
			{
				((INotifyCompletion)_WriteAwaitable).OnCompleted(() => ProcessCompletedSend(MyTaskSource, callback));
			}

			return MyTaskSource.Task;
		}

		private void ProcessCompletedSend(TaskCompletionSource<VoidStruct> source, AsyncCallback callback)
		{
			try
			{
				_WriteAwaitable.GetResult();

				source.SetResult(VoidStruct.Empty);
			}
			catch (Exception e)
			{
				source.SetException(e);
			}

			// Raise the async callback (if any)
			if (callback != null)
				callback(source.Task);
		}

		//****************************************

		/// <inheritdoc />
		public override bool CanRead
		{
			get { return true; }
		}

		/// <inheritdoc />
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <inheritdoc />
		public override bool CanWrite
		{
			get { return true; }
		}

		/// <inheritdoc />
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		/// <inheritdoc />
		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}
		
		/// <summary>
		/// Gets the socket this AsyncNetworkStream is wrapping
		/// </summary>
		public Socket Socket
		{
			get { return _Socket; }
		}
	}
}
