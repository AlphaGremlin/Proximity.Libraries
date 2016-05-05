/****************************************\
 AsyncNetworkStream.cs
 Created: 2014-10-02
\****************************************/
#if !PORTABLE
using System;
using System.Collections.Generic;
using System.IO;
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
	/// Provides a NetworkStream-esque interface that uses SocketAwaitable and tasks
	/// </summary>
	[SecuritySafeCritical]
	public sealed class AsyncNetworkStream : Stream
	{	//****************************************
		private readonly Socket _Socket;
		
		private readonly SocketAwaitableEventArgs _WriteEventArgs = new SocketAwaitableEventArgs(), _ReadEventArgs = new SocketAwaitableEventArgs();
		
		private Task _LastWrite;
		private long _PendingWriteBytes = 0;

		private long _InBytes = 0, _OutBytes = 0;
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
		[SecuritySafeCritical]
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

		/// <summary>
		/// Asynchronously writes a set of buffers to the Socket
		/// </summary>
		/// <param name="buffers">The set of buffers to write</param>
		/// <param name="callback">A callback to raise when writing has completed</param>
		/// <param name="state">A state object to identify this callback</param>
		/// <returns>An async result representing the write operation</returns>
		public IAsyncResult BeginWrite(IList<ArraySegment<byte>> buffers, AsyncCallback callback, object state)
		{
			return SendData(buffers, callback, state);
		}

		/// <inheritdoc />
		public override int EndRead(IAsyncResult asyncResult)
		{
			// Use GetResult() so exceptions are thrown without being wrapped in AggregateException
			return ((Task<int>)asyncResult).GetAwaiter().GetResult();
		}

		/// <inheritdoc />
		public override void EndWrite(IAsyncResult asyncResult)
		{
			// Use GetResult() so exceptions are thrown without being wrapped in AggregateException
			((Task)asyncResult).GetAwaiter().GetResult();
		}
		
		/// <inheritdoc />
		public override void Flush()
		{
		}

		/// <inheritdoc />
		public
#if !NET40
		override
#endif
		Task FlushAsync(CancellationToken cancellationToken)
		{
			return VoidStruct.EmptyTask;
		}

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count == 0)
				return 0;

			// Use GetResult() so exceptions are thrown without being wrapped in AggregateException
			return ReadData(buffer, offset, count).GetAwaiter().GetResult();
		}

		/// <inheritdoc />
		public
#if !NET40
		override
#endif
		Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
#if NET40
				return TaskEx.Run(() => 0, cancellationToken);
#else
				return Task.Run(() => 0, cancellationToken);
#endif
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
			
			// There is no synchronous sending with SocketAsyncEventArgs
			// Use GetResult() so exceptions are thrown without being wrapped in AggregateException
			SendData(buffer, offset, count).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Synchronously writes a set of buffers to the Socket
		/// </summary>
		/// <param name="buffers">The set of buffers to write</param>
		public void Write(IList<ArraySegment<byte>> buffers)
		{
			if (buffers.Count == 0)
				return;

			// There is no synchronous sending with SocketAsyncEventArgs
			// Use GetResult() so exceptions are thrown without being wrapped in AggregateException
			SendData(buffers).GetAwaiter().GetResult();
		}

		/// <inheritdoc />
		public
#if !NET40
		override
#endif
		Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
#if NET40
				return TaskEx.Run(() => { }, cancellationToken);
#else
				return Task.Run(() => {}, cancellationToken);
#endif
			}

			return SendData(buffer, offset, count);
		}
		
		/// <summary>
		/// Asynchronously writes a set of buffers to the Socket
		/// </summary>
		/// <param name="buffers">The set of buffers to write</param>
		/// <param name="cancellationToken">A cancellation token to abort the write operation</param>
		/// <returns>A task representing the write operation</returns>
		public Task WriteAsync(IList<ArraySegment<byte>> buffers, CancellationToken cancellationToken)
		{
			if (buffers == null)
				throw new ArgumentNullException("buffers");

			if (cancellationToken.IsCancellationRequested)
			{
#if NET40
				return TaskEx.Run(() => { }, cancellationToken);
#else
				return Task.Run(() => {}, cancellationToken);
#endif
			}
			
			return SendData(buffers);
		}
		
		/// <inheritdoc />
		public override void WriteByte(byte value)
		{
			// There is no synchronous sending with SocketAsyncEventArgs
			SendData(new byte[] { value }, 0, 1).GetAwaiter().GetResult();
		}

		//****************************************
		
		private Task<int> ReadData(byte[] buffer, int index, int count, AsyncCallback callback = null, object state = null)
		{	//****************************************
			var MyOperation = new ReadOperation(this, callback, state); // 2 allocations (TaskCompletionSource and Task)
			//****************************************
			
			// Prepare a receive buffer
			_ReadEventArgs.SetBuffer(buffer, index, count);

			try
			{
				// Start waiting for some data
				_ReadEventArgs.ReceiveAsync(_Socket);
	
				if (_ReadEventArgs.IsCompleted)
				{
					MyOperation.ProcessCompletedReceive();
				}
				else
				{
					((INotifyCompletion)_ReadEventArgs).OnCompleted(MyOperation.ProcessCompletedReceive); // 1 allocation (Action)
				}
	
				return MyOperation.Task;
			}
			catch (ObjectDisposedException) // Socket was closed, no need to continue
			{
				MyOperation.SetResult(0);

				return MyOperation.Task;
			}
			catch (Exception e)
			{
				MyOperation.Fail(e);
				
				return MyOperation.Task;
			}
		}
		
		private Task SendData(IList<ArraySegment<byte>> buffers, AsyncCallback callback = null, object state = null)
		{	//****************************************
			SendBulkOperation MyOperation;
			int TotalBytes = 0;
			//****************************************

			// Total the bytes to be sent
			foreach (var MyBuffer in buffers)
				TotalBytes += buffers.Count;

			Interlocked.Add(ref _PendingWriteBytes, TotalBytes);

			MyOperation = new SendBulkOperation(this, buffers, TotalBytes, callback, state); // 2 allocations (TaskCompletionSource and Task)

			//****************************************
			
			// Swap out the previous write task with ours
			var OldTask = Interlocked.Exchange(ref _LastWrite, MyOperation.Task);
			
			// Has that write completed?
			if (OldTask == null || OldTask.IsCompleted)
			{
				// Yes, directly queue it
				DoSendData(MyOperation);
			}
			else
			{
				// No, wait until it finishes to queue our write
				OldTask.ContinueWith(MyOperation.DoSendData); // 2 allocations (Task and Action)
			}

			return MyOperation.Task;
		}
		
		private Task SendData(byte[] buffer, int offset, int count, AsyncCallback callback = null, object state = null)
		{	//****************************************
			var MyOperation = new SendOperation(this, buffer, offset, count, callback, state); // 2 allocations (TaskCompletionSource and Task)
			//****************************************

			// Add the bytes to be sent
			Interlocked.Add(ref _PendingWriteBytes, count);

			// Swap out the previous write task with ours
			var OldTask = Interlocked.Exchange(ref _LastWrite, MyOperation.Task);
			
			// Has that write completed?
			if (OldTask == null || OldTask.IsCompleted)
			{
				// Yes, directly queue it
				DoSendData(MyOperation);
			}
			else
			{
				// No, wait until it finishes to queue our write
				OldTask.ContinueWith(MyOperation.DoSendData); // 2 allocations (Task and Action)
			}

			return MyOperation.Task;
		}

		private void DoSendData(SendOperation operation)
		{
			try
			{
				_WriteEventArgs.BufferList = null;
				operation.Apply();
			
				_WriteEventArgs.SendAsync(_Socket);

				if (_WriteEventArgs.IsCompleted)
				{
					operation.ProcessCompletedSend();
				}
				else
				{
					_WriteEventArgs.OnCompleted(operation.ProcessCompletedSend); // 1 allocation (Action)
				}
			}
			catch (ObjectDisposedException)
			{
				operation.Dispose();
			}
			catch (Exception e)
			{
				operation.Fail(e);
			}
		}
		
		private void DoSendData(SendBulkOperation operation)
		{
			try
			{
				_WriteEventArgs.SetBuffer(null, 0, 0);
				operation.Apply();

				_WriteEventArgs.SendAsync(_Socket);

				if (_WriteEventArgs.IsCompleted)
				{
					operation.ProcessCompletedSend();
				}
				else
				{
					_WriteEventArgs.OnCompleted(operation.ProcessCompletedSend); // 1 allocation (Action)
				}
			}
			catch (ObjectDisposedException)
			{
				operation.Dispose();
			}
			catch (Exception e)
			{
				operation.Fail(e);
			}
		}

		private void CompleteReceive(long totalBytes)
		{
			Interlocked.Add(ref _InBytes, totalBytes);
		}

		private void CompleteSend(long bytesSent)
		{
			Interlocked.Add(ref _PendingWriteBytes, -bytesSent);
			Interlocked.Add(ref _OutBytes, bytesSent);
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

		/// <summary>
		/// Gets the total number of bytes read from the underlying socket
		/// </summary>
		public long ReadBytes
		{
			get { return _InBytes; }
		}

		/// <summary>
		/// Gets the total number of bytes written to the underlying socket
		/// </summary>
		public long WrittenBytes
		{
			get { return _OutBytes; }
		}

		/// <summary>
		/// Gets the bytes that are awaiting being sent
		/// </summary>
		/// <remarks>This does not necessarily include data that is in the send buffer for the Socket</remarks>
		public long PendingWriteBytes
		{
			get { return _PendingWriteBytes; }
		}

		//****************************************
		
		private sealed class ReadOperation : TaskCompletionSource<int>
		{	//****************************************
			private readonly AsyncNetworkStream _Stream;
			private readonly AsyncCallback _Callback;
			//****************************************
			
			internal ReadOperation(AsyncNetworkStream stream, AsyncCallback callback, object state) : base(state)
			{
				_Stream = stream;
				_Callback = callback;
			}
			
			//****************************************
			
			internal void ProcessCompletedReceive()
			{	//****************************************
				var EventArgs = _Stream._ReadEventArgs;
				//****************************************

				try
				{
					EventArgs.SetBuffer(null, 0, 0);
				}
				catch (ObjectDisposedException)
				{
				}

				if (EventArgs.SocketError == SocketError.Success)
				{
					_Stream.CompleteReceive(EventArgs.BytesTransferred);

					SetResult(EventArgs.BytesTransferred);
				}
				else
				{
					SetException(new IOException("Receive failed", new SocketException((int)EventArgs.SocketError)));
				}

				try
				{
					// Raise the async callback (if any)
					if (_Callback != null)
						_Callback(Task);
				}
				catch (Exception e)
				{
					// Callback can raise exceptions. If it's our exception, ignore
					if (Task.Exception == null || Task.Exception.InnerException != e)
						throw;
				}
			}
			
			internal void Fail(Exception e)
			{
				// Ensure the completed task we return has the appropriate state
				SetException(e);

				try
				{
					// Raise the async callback (if any)
					if (_Callback != null)
						_Callback(Task);
				}
				catch (Exception)
				{
					// Callback can raise exceptions. If it's our exception, ignore
					if (Task.Exception == null || Task.Exception.InnerException != e)
						throw;
				}
			}
		}
		
		private abstract class BaseSendOperation : TaskCompletionSource<VoidStruct>
		{	//****************************************
			private readonly AsyncNetworkStream _Stream;
			private readonly AsyncCallback _Callback;
			//****************************************
			
			internal BaseSendOperation(AsyncNetworkStream stream, AsyncCallback callback, object state) : base(state)
			{
				_Stream = stream;
				_Callback = callback;
			}
			
			//****************************************

			internal void Dispose()
			{
				_Stream.CompleteSend(TotalBytes);

				SetResult(VoidStruct.Empty);
			}

			internal void Fail(Exception e)
			{
				_Stream.CompleteSend(TotalBytes);

				// Ensure the completed task we return has the appropriate state
				SetException(e);

				try
				{
					// Raise the async callback (if any)
					if (_Callback != null)
						_Callback(Task);
				}
				catch (Exception)
				{
					// Callback can raise exceptions. If it's our exception, ignore
					if (Task.Exception == null || Task.Exception.InnerException != e)
						throw;
				}
			}
			
			internal void ProcessCompletedSend()
			{	//****************************************
				var EventArgs = _Stream._WriteEventArgs;
				//****************************************

				_Stream.CompleteSend(TotalBytes);

				// Clean the buffer, so SocketAsyncEventArgs don't hold a pinned reference to it
				try
				{
					EventArgs.SetBuffer(null, 0, 0);
				}
				catch (ObjectDisposedException)
				{
				}

				if (EventArgs.SocketError == SocketError.Success)
					SetResult(VoidStruct.Empty);
				else
					SetException(new IOException("Send failed", new SocketException((int)EventArgs.SocketError)));

				try
				{
					// Raise the async callback (if any)
					if (_Callback != null)
						_Callback(Task);
				}
				catch (Exception e)
				{
					// Callback can raise exceptions. If it's our exception, ignore
					if (Task.Exception == null || Task.Exception.InnerException != e)
						throw;
				}
			}
			
			//****************************************
			
			protected AsyncNetworkStream Stream
			{
				get { return _Stream; }
			}

			internal abstract long TotalBytes { get; }
		}
		
		private sealed class SendOperation : BaseSendOperation
		{	//****************************************
			private readonly byte[] _Buffer;
			private readonly int _Offset, _Count;
			//****************************************
			
			internal SendOperation(AsyncNetworkStream stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(stream, callback, state)
			{
				_Buffer = buffer;
				_Offset = offset;
				_Count = count;
			}
			
			//****************************************
			
			internal void DoSendData(Task ancestor)
			{
				Stream.DoSendData(this);
			}
			
			internal void Apply()
			{
				Stream._WriteEventArgs.SetBuffer(_Buffer, _Offset, _Count);
			}

			//****************************************

			internal override long TotalBytes
			{
				get { return _Count; }
			}
		}
		
		private sealed class SendBulkOperation : BaseSendOperation
		{	//****************************************
			private readonly IList<ArraySegment<byte>> _Buffers;
			private readonly long _TotalBytes;
			//****************************************
			
			internal SendBulkOperation(AsyncNetworkStream stream, IList<ArraySegment<byte>> buffers, long totalBytes, AsyncCallback callback, object state) : base(stream, callback, state)
			{
				_Buffers = buffers;
				_TotalBytes = totalBytes;
			}
			
			//****************************************
			
			internal void DoSendData(Task ancestor)
			{
				Stream.DoSendData(this);
			}
			
			internal void Apply()
			{
				Stream._WriteEventArgs.BufferList = _Buffers;
			}

			//****************************************

			internal override long TotalBytes
			{
				get { return _TotalBytes; }
			}
		}
	}
}
#endif