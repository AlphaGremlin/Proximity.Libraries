/****************************************\
 AsyncNetworkStream.cs
 Created: 2014-10-02
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Concurrent;
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
	internal sealed class AsyncNetworkStream : Stream
	{	//****************************************
		private readonly static ConcurrentStack<SmartEventArgs> _ArgsPool = new ConcurrentStack<SmartEventArgs>();
		//****************************************
		private readonly Socket _Socket;

		private SmartEventArgs _ReadEventArgs, _WriteEventArgs;

		private long _InBytes = 0, _OutBytes = 0;
		private readonly Action _CompleteRead, _CompleteWrite;
		//****************************************

		/// <summary>
		/// Creates a new Async Network Stream
		/// </summary>
		/// <param name="socket">The socket to wrap</param>
		public AsyncNetworkStream(Socket socket)
		{
			_Socket = socket;

			_CompleteRead = CompleteReadOperation;
			_CompleteWrite = CompleteWriteOperation;
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
			return WriteData(buffer, offset, count, callback, state);
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
			return WriteData(buffers, callback, state);
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

#if NET40
		/// <inheritdoc />
		public Task FlushAsync(CancellationToken cancellationToken)
#else
		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken)
#endif
		{
			return VoidStruct.EmptyTask;
		}

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count == 0)
				return 0;

			try
			{
				// Use GetResult() so exceptions are thrown without being wrapped in AggregateException
				var InBytes = _Socket.Receive(buffer, offset, count, SocketFlags.None);

				Interlocked.Add(ref _InBytes, InBytes);

				return InBytes;
			}
			catch (Exception e)
			{
				if (!(e is ThreadAbortException) && !(e is StackOverflowException) && !(e is OutOfMemoryException))
					throw new IOException("Receive failed", e);

				throw;
			}
		}

#if NET40
		/// <inheritdoc />
		public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
#else
		/// <inheritdoc />
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
#endif
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

		/// <summary>
		/// Asynchronously reads from a Socket with minimal allocations
		/// </summary>
		/// <param name="buffer">The buffer to write the received data into</param>
		/// <param name="offset">The offset into the buffer to begin writing</param>
		/// <param name="count">The maximum number of bytes that can be written</param>
		/// <returns>An awaitable that completes when the Socket has been read from</returns>
		public ReadAwaitable ReadAwait(byte[] buffer, int offset, int count)
		{
			BeginOperation(ref _ReadEventArgs);

			try
			{
				// Prepare the receive buffer
				_ReadEventArgs.SetBuffer(buffer, offset, count);
				_ReadEventArgs.UserToken = this;

				// Begin the receive operation
				_ReadEventArgs.Receive(_Socket);
			}
			catch (ObjectDisposedException)
			{
				EndOperation(ref _ReadEventArgs);

				return new ReadAwaitable(null); // Socket was closed, return a null awaiter
			}
			catch
			{
				EndOperation(ref _ReadEventArgs);

				throw;
			}

			return new ReadAwaitable(_ReadEventArgs);
		}

		/// <inheritdoc />
		public override int ReadByte()
		{
			var MyBuffer = new byte[0];

			var ReadBytes = Read(MyBuffer, 0, 1);

			if (ReadBytes == 0)
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

			try
			{
				// Use GetResult() so exceptions are thrown without being wrapped in AggregateException
				_Socket.Send(buffer, offset, count, SocketFlags.None);

				Interlocked.Add(ref _OutBytes, count);
			}
			catch (Exception e)
			{
				if (!(e is ThreadAbortException) && !(e is StackOverflowException) && !(e is OutOfMemoryException))
					throw new IOException("Send failed", e);

				throw;
			}
		}

		/// <summary>
		/// Synchronously writes a set of buffers to the Socket
		/// </summary>
		/// <param name="buffers">The set of buffers to write</param>
		public void Write(IList<ArraySegment<byte>> buffers)
		{
			if (buffers.Count == 0)
				return;

			try
			{
				_Socket.Send(buffers, SocketFlags.None);

				for (int Index = 0; Index < buffers.Count; Index++)
					Interlocked.Add(ref _OutBytes, buffers[Index].Count);
			}
			catch (Exception e)
			{
				if (!(e is ThreadAbortException) && !(e is StackOverflowException) && !(e is OutOfMemoryException))
					throw new IOException("Send failed", e);

				throw;
			}
		}

#if NET40
		/// <inheritdoc />
		public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
#else
		/// <inheritdoc />
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
#endif
		{
			if (cancellationToken.IsCancellationRequested)
			{
#if NET40
				return TaskEx.Run(() => { }, cancellationToken);
#else
				return Task.Run(() => { }, cancellationToken);
#endif
			}

			return WriteData(buffer, offset, count);
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
				return Task.Run(() => { }, cancellationToken);
#endif
			}

			return WriteData(buffers);
		}

		/// <summary>
		/// Asynchronously writes a buffer to the Socket with minimal allocations
		/// </summary>
		/// <param name="buffer">The buffer to write to the Socket</param>
		/// <param name="offset">The offset into the buffer to write from</param>
		/// <param name="count">The number of bytes to write</param>
		/// <returns>An awaitable that completes when the Socket has been written to</returns>
		public WriteAwaitable WriteAwait(byte[] buffer, int offset, int count)
		{
			BeginOperation(ref _WriteEventArgs);

			try
			{
				// Prepare the receive buffer
				_WriteEventArgs.SetBuffer(buffer, offset, count);
				_WriteEventArgs.UserToken = this;

				// Begin the receive operation
				_WriteEventArgs.Send(_Socket);
			}
			catch (ObjectDisposedException)
			{
				EndOperation(ref _WriteEventArgs);

				return new WriteAwaitable(null); // Socket was closed, return a null awaiter
			}
			catch
			{
				EndOperation(ref _WriteEventArgs);

				throw;
			}

			return new WriteAwaitable(_WriteEventArgs);
		}

		/// <inheritdoc />
		public override void WriteByte(byte value)
		{
			// There is no synchronous sending with SocketAsyncEventArgs
			Write(new byte[] { value }, 0, 1);
		}

		//****************************************

		private void BeginOperation(ref SmartEventArgs targetArgs)
		{	//****************************************
			SmartEventArgs MyArgs;
			//****************************************

			// Retrieve an existing args from the pool
			if (!_ArgsPool.TryPop(out MyArgs))
				// None found, so create a new one
				MyArgs = new SmartEventArgs();

			// Ensure we're not already executing this operation
			if (Interlocked.CompareExchange(ref targetArgs, MyArgs, null) != null)
			{
				// Return the args we just retrieved/created
				_ArgsPool.Push(MyArgs);

				throw new InvalidOperationException("Operation already in progress");
			}
		}

		private void EndOperation(ref SmartEventArgs targetArgs)
		{	//****************************************
			var MyArgs = Interlocked.Exchange(ref targetArgs, null);
			//****************************************

			if (MyArgs == null)
				return; // End without a start?!

			try
			{
				// Clean the send buffers, so SocketAsyncEventArgs doesn't hold a pinned reference to it
				if (MyArgs.Buffer != null)
					MyArgs.SetBuffer(null, 0, 0);
				else if (MyArgs.BufferList != null)
					MyArgs.BufferList = null;

				MyArgs.UserToken = null;

				// Return it to the writer pool
				_ArgsPool.Push(MyArgs);
			}
			catch
			{
				// Something bad happened, so cleanup
				MyArgs.Dispose();
			}
		}

		/*
		 * Read await completes synchronously: 0 allocations
		 * Read await completes synchronously with error: 1 allocation (Exception)
		 * Read completes synchronously: 1 allocation (Task)
		 * Read completes synchronously with state: 2 allocations (TCS+Task)
		 * Read completes synchronously with error: 3 allocations (TCS+Task+Exception)
		 * Read await waits: 0 allocations
		 * Read await waits with error: 1 allocation (Exception)
		 * Read waits: 2 allocations (TCS+Task)
		 * Read waits with error: 3 allocations (TCS+Task+Exception)
		 */

		private Task<int> ReadData(byte[] buffer, int index, int count, AsyncCallback callback = null, object state = null)
		{
			BeginOperation(ref _ReadEventArgs);

			try
			{
				// Prepare the receive buffer
				_ReadEventArgs.SetBuffer(buffer, index, count);

				// Start waiting for some data
				if (_ReadEventArgs.Receive(_Socket))
				{
					// We completed synchronously, so go straight to completion
					return CompleteReadOperation(callback, state, null);
				}
			}
			catch (ObjectDisposedException) // Socket was closed, no need to continue
			{
				return CompleteRead(callback, state, null, null, 0);
			}
			catch (Exception e)
			{
				return CompleteRead(callback, state, null, e, 0);
			}

			// We're running asynchronously, so create our Task
			var MyOperation = new AsyncOperation(callback, state); // 2 allocations (TaskCompletionSource and Task)

			_ReadEventArgs.UserToken = MyOperation;
			_ReadEventArgs.OnCompleted(_CompleteRead);

			return MyOperation.Task;
		}

		private int CompleteReadAwait()
		{
			try
			{
				if (_ReadEventArgs.SocketError != SocketError.Success)
					throw new IOException("Receive failed", new SocketException((int)_ReadEventArgs.SocketError));

				Interlocked.Add(ref _InBytes, _ReadEventArgs.BytesTransferred);

				return _ReadEventArgs.BytesTransferred;
			}
			finally
			{
				// End the read operation before we set the result, since that can call continuations that perform more reads
				EndOperation(ref _ReadEventArgs);
			}
		}

		private void CompleteReadOperation()
		{
			var MyOperation = (AsyncOperation)_ReadEventArgs.UserToken;

			CompleteReadOperation(MyOperation.Callback, MyOperation.Task.AsyncState, MyOperation);
		}

		private Task<int> CompleteReadOperation(AsyncCallback callback, object state, TaskCompletionSource<int> taskSource)
		{
			// Handle the result
			if (_ReadEventArgs.SocketError == SocketError.Success)
			{
				// Success
				Interlocked.Add(ref _InBytes, _ReadEventArgs.BytesTransferred);

				return CompleteRead(callback, state, taskSource, null, _ReadEventArgs.BytesTransferred);
			}

			return CompleteRead(callback, state, taskSource, new IOException("Receive failed", new SocketException((int)_ReadEventArgs.SocketError)), 0);
		}

		private Task<int> CompleteRead(AsyncCallback callback, object state, TaskCompletionSource<int> taskSource, Exception exception, int result)
		{	//****************************************
			Task<int> MyResult;
			//****************************************

			// End the read operation before we set the result, since that can call continuations that perform more reads
			EndOperation(ref _ReadEventArgs);

			// If there's no exception, we were disposed of, so complete as if we got a final read
			if (exception == null)
			{
				// If there's a state, we need to create a task completion source that wraps it
				if (state != null && taskSource == null)
					taskSource = new TaskCompletionSource<int>(state);

				if (taskSource != null)
				{
					taskSource.SetResult(result);

					MyResult = taskSource.Task;
				}
				else
				{
#if NET40
					MyResult = TaskEx.FromResult(result);
#else
					MyResult = Task.FromResult(result);
#endif
				}
			}
			else
			{
				// We're returning an exception, so we always need a TaskCompletionSource
				if (taskSource == null)
					taskSource = new TaskCompletionSource<int>(state);

				taskSource.SetException(exception);

				MyResult = taskSource.Task;
			}

			try
			{
				// Raise the async callback (if any)
				if (callback != null)
					callback(MyResult);
			}
			catch (Exception e)
			{
				// Callback can raise exceptions. If it's our exception, ignore
				if (MyResult.Exception == null || MyResult.Exception.InnerException != e)
					throw;
			}

			return MyResult;
		}

		/*
		 * Write await completes synchronously: 0 allocations
		 * Write await completes synchronously with error: 1 allocation (Exception)
		 * Write completes synchronously: 0 allocations
		 * Write completes synchronously with state: 2 allocations (TCS+Task)
		 * Write completes synchronously with error: 3 allocations (TCS+Task+Exception)
		 * Write await waits: 0 allocations
		 * Write await waits with error: 1 allocation (Exception)
		 * Write waits: 2 allocations (TCS+Task)
		 * Write waits with error: 3 allocations (TCS+Task+Exception)
		 * 
		 * NOTE: All async-capable writes include one extra allocation due to the WriterPool Push operation
		 */

		private Task WriteData(IList<ArraySegment<byte>> buffers, AsyncCallback callback = null, object state = null)
		{
			BeginOperation(ref _WriteEventArgs);

			try
			{
				// Prepare the writer
				_WriteEventArgs.BufferList = buffers;

				// Start writing our data
				if (_WriteEventArgs.Send(_Socket))
				{
					// We completed synchronously, so go straight to completion
					return CompleteWriteOperation(callback, state, null);
				}
			}
			catch (ObjectDisposedException) // Socket was closed, no need to continue
			{
				return CompleteWrite(callback, state, null, null);
			}
			catch (Exception e)
			{
				return CompleteWrite(callback, state, null, e);
			}

			// We're running asynchronously, so create our Task
			var MyOperation = new AsyncOperation(callback, state); // 2 allocations (TaskCompletionSource and Task)

			_WriteEventArgs.UserToken = MyOperation;
			_WriteEventArgs.OnCompleted(_CompleteWrite);

			return MyOperation.Task;
		}

		private Task WriteData(byte[] buffer, int offset, int count, AsyncCallback callback = null, object state = null)
		{
			BeginOperation(ref _WriteEventArgs);

			try
			{
				// Prepare the writer
				_WriteEventArgs.SetBuffer(buffer, offset, count);

				// Start writing our data
				if (_WriteEventArgs.Send(_Socket))
				{
					// We completed synchronously, so go straight to completion
					return CompleteWriteOperation(callback, state, null);
				}
			}
			catch (ObjectDisposedException) // Socket was closed, no need to continue
			{
				return CompleteWrite(callback, state, null, null);
			}
			catch (Exception e)
			{
				return CompleteWrite(callback, state, null, e);
			}

			var MyOperation = new AsyncOperation(callback, state); // 2 allocations (TaskCompletionSource and Task)

			_WriteEventArgs.UserToken = MyOperation;
			_WriteEventArgs.OnCompleted(_CompleteWrite);

			return MyOperation.Task;
		}

		private void CompleteWriteAwait()
		{
			try
			{
				if (_WriteEventArgs.SocketError != SocketError.Success)
					throw new IOException("Send failed", new SocketException((int)_WriteEventArgs.SocketError));

				Interlocked.Add(ref _OutBytes, _WriteEventArgs.BytesTransferred);
			}
			finally
			{
				EndOperation(ref _WriteEventArgs);
			}
		}

		private void CompleteWriteOperation()
		{
			var Operation = (AsyncOperation)_WriteEventArgs.UserToken;

			CompleteWriteOperation(Operation.Callback, Operation.Task.AsyncState, Operation);
		}

		private Task CompleteWriteOperation(AsyncCallback callback, object state, TaskCompletionSource<int> taskSource)
		{
			// Handle the result
			if (_WriteEventArgs.SocketError == SocketError.Success)
			{
				// Success
				Interlocked.Add(ref _OutBytes, _WriteEventArgs.BytesTransferred);

				return CompleteWrite(callback, state, taskSource, null);
			}

			return CompleteWrite(callback, state, taskSource, new IOException("Send failed", new SocketException((int)_WriteEventArgs.SocketError)));
		}

		private Task CompleteWrite(AsyncCallback callback, object state, TaskCompletionSource<int> taskSource, Exception exception)
		{	//****************************************
			Task MyResult;
			//****************************************

			// End the write operation before we set the result, since that can call continuations that perform more writes
			EndOperation(ref _WriteEventArgs);

			// If there's no exception, we were disposed of, so complete as if we got a final read
			if (exception == null)
			{
				// If there's a state, we need to create a task completion source that wraps it
				if (state != null && taskSource == null)
					taskSource = new TaskCompletionSource<int>(state); // 2 allocations (TaskCompletionSource and Task)

				if (taskSource != null)
				{
					taskSource.SetResult(0);

					MyResult = taskSource.Task;
				}
				else
				{
					MyResult = VoidStruct.EmptyTask;
				}
			}
			else
			{
				// We're returning an exception, so we always need a TaskCompletionSource
				if (taskSource == null)
					taskSource = new TaskCompletionSource<int>(state);

				taskSource.SetException(exception);

				MyResult = taskSource.Task;
			}

			try
			{
				// Raise the async callback (if any)
				if (callback != null)
					callback(MyResult);
			}
			catch (Exception e)
			{
				// Callback can raise exceptions. If it's our exception, ignore
				if (MyResult.Exception == null || MyResult.Exception.InnerException != e)
					throw;
			}

			return MyResult;
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

		//****************************************

		/// <summary>
		/// Empties the SocketEventArgs object pool
		/// </summary>
		/// <remarks>SocketEventArgs being used by currently executing operations will not be affected</remarks>
		public static void EmptyPool()
		{	//****************************************
			SmartEventArgs MyEventArgs;
			//****************************************

			while (_ArgsPool.TryPop(out MyEventArgs))
			{
				MyEventArgs.Dispose();
			}
		}

		//****************************************

		/// <summary>
		/// Provides an awaitable for read operations
		/// </summary>
		public struct ReadAwaitable : INotifyCompletion
		{	//****************************************
			private readonly SmartEventArgs _EventArgs;
			//****************************************

			internal ReadAwaitable(SmartEventArgs eventArgs)
			{
				_EventArgs = eventArgs;
			}

			//****************************************

			/// <summary>
			/// Gets the awaiter for this awaitable
			/// </summary>
			/// <returns>This awaitable</returns>
			public ReadAwaitable GetAwaiter()
			{
				return this;
			}

			/// <summary>
			/// Gets the result of the read operation
			/// </summary>
			/// <returns>The number of bytes read, or zero</returns>
			public int GetResult()
			{
				if (_EventArgs == null)
					return 0;

				return ((AsyncNetworkStream)_EventArgs.UserToken).CompleteReadAwait();
			}

			/// <summary>
			/// Attaches a completion to this Awaitable
			/// </summary>
			/// <param name="action">The action to raise when the read operation has completed</param>
			public void OnCompleted(Action action)
			{
				// Attach to our EventArgs. If the event has completed already, will raise on another thread
				_EventArgs.OnCompleted(action);
			}

			//****************************************

			/// <summary>
			/// Gets whether the operation has completed
			/// </summary>
			public bool IsCompleted
			{
				get { return _EventArgs == null || _EventArgs.IsCompleted; }
			}
		}

		/// <summary>
		/// Provides an awaitable for write operations
		/// </summary>
		public struct WriteAwaitable : INotifyCompletion
		{	//****************************************
			private readonly SmartEventArgs _EventArgs;
			//****************************************

			internal WriteAwaitable(SmartEventArgs eventArgs)
			{
				_EventArgs = eventArgs;
			}

			//****************************************

			/// <summary>
			/// Gets the awaiter for this awaitable
			/// </summary>
			/// <returns>This awaitable</returns>
			public WriteAwaitable GetAwaiter()
			{
				return this;
			}

			/// <summary>
			/// Gets the result of the write operation
			/// </summary>
			public void GetResult()
			{
				if (_EventArgs != null)
					((AsyncNetworkStream)_EventArgs.UserToken).CompleteWriteAwait();
			}

			/// <summary>
			/// Attaches a completion to this Awaitable
			/// </summary>
			/// <param name="action">The action to raise when the write operation has completed</param>
			public void OnCompleted(Action action)
			{
				// Attach to our EventArgs. If the event has completed already, will raise on another thread
				_EventArgs.OnCompleted(action);
			}

			//****************************************

			/// <summary>
			/// Gets whether the operation has completed
			/// </summary>
			public bool IsCompleted
			{
				get { return _EventArgs == null || _EventArgs.IsCompleted; }
			}
		}

		private sealed class AsyncOperation : TaskCompletionSource<int>
		{	//****************************************
			private readonly AsyncCallback _Callback;
			//****************************************

			internal AsyncOperation(AsyncCallback callback, object state) : base(state)
			{
				_Callback = callback;
			}

			//****************************************

			internal AsyncCallback Callback
			{
				get { return _Callback; }
			}
		}

		[SecuritySafeCritical]
		internal sealed class SmartEventArgs : SocketAsyncEventArgs
		{	//****************************************
			private readonly static Action HasCompleted = () => { };
			//****************************************
			private Action _Continuation;
			//****************************************

			internal bool Receive(Socket socket)
			{
				_Continuation = null;

				if (socket.ReceiveAsync(this))
					return false;

				// We completed synchronously, so set our continuation To HasCompleted
				if (Interlocked.CompareExchange(ref _Continuation, HasCompleted, null) != null)
					// Our continuation has been set, someone is using this object concurrently
					throw new InvalidOperationException("Operation is already in progress");

				return true;
			}

			internal bool Send(Socket socket)
			{
				_Continuation = null;

				if (socket.SendAsync(this))
					return false;

				// We completed synchronously, so set our continuation To HasCompleted
				if (Interlocked.CompareExchange(ref _Continuation, HasCompleted, null) != null)
					// Our continuation has been set, someone is using this object concurrently
					throw new InvalidOperationException("Operation is already in progress");

				return true;
			}

			public void OnCompleted(Action continuation)
			{
				// If our continuation is HasCompleted, OnCompleted has already executed
				// If it's not, try and set it to the given continuation, as long as it's not completed
				if (_Continuation != HasCompleted && Interlocked.CompareExchange(ref _Continuation, continuation, null) != HasCompleted)
					// NOTE: If OnCompleted has already been called, the CompareExchange will never succeed (so only the first continuation will ever get raised)
					return;

				// If the continuation was HasCompleted, or got set to HasCompleted, run our continuation
				// Since we don't want to blow the stack doing this, run it asynchronously
				ThreadPool.QueueUserWorkItem((state) => ((Action)state)(), continuation); // 1 allocation
			}

			[SecuritySafeCritical]
			protected override void OnCompleted(SocketAsyncEventArgs e)
			{
				// Set our continuation to HasCompleted, and return what was previously there
				var MyContinuation = Interlocked.Exchange(ref _Continuation, HasCompleted);

				// If a continuation was previously set, call it
				if (MyContinuation != null)
					MyContinuation();

				base.OnCompleted(e);
			}

			//****************************************

			public bool IsCompleted
			{
				get { return object.ReferenceEquals(_Continuation, HasCompleted); }
			}
		}
	}
}
#endif