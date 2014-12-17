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
		
		private readonly SocketAwaitableEventArgs _WriteEventArgs = new SocketAwaitableEventArgs(), _ReadEventArgs = new SocketAwaitableEventArgs();
		
		private Task _LastWrite;
		private byte[] _WriteBuffer;
		//****************************************

		/// <summary>
		/// Creates a new Async Network Stream
		/// </summary>
		/// <param name="socket">The socket to wrap</param>
		public AsyncNetworkStream(Socket socket)
		{
			_Socket = socket;
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
			
			// The caller may manipulate buffer after we exit, so we need to copy it
			if (_WriteBuffer == null || _WriteBuffer.Length < count)
				_WriteBuffer = new byte[count];
			
			Array.Copy(buffer, offset, _WriteBuffer, 0, count);

			SendData(_WriteBuffer, 0, count); // No need to wait on the task, as we queue the write until later
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
			SendData(new byte[] { value }, 0, 1);
		}

		//****************************************
		
		private Task<int> ReadData(byte[] buffer, int index, int count, AsyncCallback callback = null, object state = null)
		{	//****************************************
			var MyTaskSource = new TaskCompletionSource<int>(state);
			//****************************************
			
			// Prepare a receive buffer
			_ReadEventArgs.SetBuffer(buffer, index, count);

			try
			{
				// Start waiting for some data
				_ReadEventArgs.ReceiveAsync(_Socket);
	
				if (_ReadEventArgs.IsCompleted)
				{
					ProcessCompletedReceive(MyTaskSource, callback);
				}
				else
				{
					((INotifyCompletion)_ReadEventArgs).OnCompleted(() => ProcessCompletedReceive(MyTaskSource, callback));
				}
	
				return MyTaskSource.Task;
			}
			catch (Exception e)
			{
				// Ensure the completed task we return has the appropriate state
				MyTaskSource.SetException(e);
	
				if (callback != null)
					callback(MyTaskSource.Task);
	
				return MyTaskSource.Task;
			}
		}
		
		private void ProcessCompletedReceive(TaskCompletionSource<int> source, AsyncCallback callback)
		{
			if (_ReadEventArgs.SocketError == SocketError.Success)
				source.SetResult(_ReadEventArgs.BytesTransferred);
			else
				source.SetException(new SocketException((int)_ReadEventArgs.SocketError));

			// Raise the async callback (if any)
			if (callback != null)
				callback(source.Task);
		}

		private Task SendData(byte[] buffer, int offset, int count, AsyncCallback callback = null, object state = null)
		{	//****************************************
			var MyCompletionSource = new TaskCompletionSource<VoidStruct>(state);
			//****************************************
			
			// Swap out the previous write task with ours
			var OldTask = Interlocked.Exchange(ref _LastWrite, MyCompletionSource.Task);
			
			// Has that write completed?
			if (OldTask == null || OldTask.IsCompleted)
			{
				// Yes, directly queue it
				QueueSendData(buffer, offset, count, callback, MyCompletionSource);
			}
			else
			{
				// No, wait until it finishes to queue our write
				OldTask.ContinueWith((innerTask) => QueueSendData(buffer, offset, count, callback, MyCompletionSource));
			}

			return MyCompletionSource.Task;
		}

		private void QueueSendData(byte[] array, int offset, int count, AsyncCallback callback, TaskCompletionSource<VoidStruct> completionSource)
		{
			_WriteEventArgs.BufferList = null;
			_WriteEventArgs.SetBuffer(array, offset, count);

			try
			{
				_WriteEventArgs.SendAsync(_Socket);

				if (_WriteEventArgs.IsCompleted)
				{
					ProcessCompletedSend(completionSource, callback);
				}
				else
				{
					((INotifyCompletion)_WriteEventArgs).OnCompleted(() => ProcessCompletedSend(completionSource, callback));
				}
			}
			catch (Exception e)
			{
				completionSource.SetException(e);
	
				if (callback != null)
					callback(completionSource.Task);
			}
		}
		
		private void ProcessCompletedSend(TaskCompletionSource<VoidStruct> source, AsyncCallback callback)
		{
			if (_WriteEventArgs.SocketError == SocketError.Success)
				source.SetResult(VoidStruct.Empty);
			else
				source.SetException(new SocketException((int)_WriteEventArgs.SocketError));

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
