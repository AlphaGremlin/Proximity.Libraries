using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	/// <summary>
	/// Provides an <see cref="IBufferWriter{Byte}"/> that writes to a <see cref="Stream"/> with async calls and buffering
	/// </summary>
	public sealed class StreamAsyncBufferWriter : IBufferWriter<byte>, IDisposable
#if !NETSTANDARD2_0
		, IAsyncDisposable
#endif
	{ //****************************************
		private const int DefaultMaxOutstandingBytes = 1024 * 1024;
		//****************************************
		private readonly ConcurrentQueue<ReadOnlyMemory<byte>> _PendingWrites = new();
		private readonly AsyncAutoResetEvent _FlushWaiter = new();
		private readonly int _MinimumWriteSize, _MaxOutstandingBytes;

		private byte[]? _PendingBuffer;
		private int _PendingBytes;

		private int _OutstandingBytes;
		private int _WriterState; // 0 for no pending writes, 1 for pending write, 2 if writing

		private ArraySegment<byte> _OutstandingBuffer;
		private ValueTaskAwaiter _WriteAwaiter;
		private Exception? _WriteException;

		private readonly WaitCallback _OnInitiateWrite;
		private readonly Action _OnContinueWrite;
		//****************************************

		/// <summary>
		/// Creates a new Stream Async Buffer Writer
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to write to</param>
		public StreamAsyncBufferWriter(Stream stream) : this(stream, 128, DefaultMaxOutstandingBytes)
		{
		}

		/// <summary>
		/// Creates a new Stream Async Buffer Writer
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to write to</param>
		/// <param name="minimumWriteSize">The minimum bytes before we begin an async write. Does not apply to <see cref="O:Flush"/> or <see cref="O:FlushAsync"/>.</param>
		/// <param name="maxOutstandingBytes">The maximum number of outstanding bytes before operations can block</param>
		public StreamAsyncBufferWriter(Stream stream, int minimumWriteSize, int maxOutstandingBytes)
		{
			if (minimumWriteSize < 0)
				throw new ArgumentOutOfRangeException(nameof(minimumWriteSize));

			if (maxOutstandingBytes <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxOutstandingBytes));

			if (minimumWriteSize > maxOutstandingBytes)
				throw new ArgumentOutOfRangeException(nameof(minimumWriteSize));

			Stream = stream;
			_MinimumWriteSize = minimumWriteSize;
			_MaxOutstandingBytes = maxOutstandingBytes;

			_OnInitiateWrite = OnInitiateWrite;
			_OnContinueWrite = OnContinueWrite;
		}

		//****************************************

		/// <summary>
		/// Triggers an asynchronous write of <paramref name="count"/> bytes in the buffer returned from <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/> to the underlying Stream
		/// </summary>
		/// <param name="count">The number of data items written</param>
		public void Advance(int count)
		{
			if (_PendingBuffer == null)
				throw new InvalidOperationException("Call to Advance without corresponding call to GetMemory or GetSpan");

			if (count < 0 || count > _PendingBuffer.Length - _PendingBytes)
				throw new ArgumentOutOfRangeException(nameof(count));

			_PendingBytes += count;

			if (_PendingBytes > _MinimumWriteSize)
				Flush();
		}

		/// <summary>
		/// Cleans up the writer, blocking until all writes are finished and returning any rented resources
		/// </summary>
		public void Dispose()
		{
			Flush();

			if (_PendingBuffer != null)
			{
				ArrayPool<byte>.Shared.Return(_PendingBuffer);

				_PendingBuffer = null;
			}

			// Wait for all data to be written
			while (_OutstandingBytes > 0)
				_FlushWaiter.TryWait(Timeout.InfiniteTimeSpan);
		}

#if !NETSTANDARD2_0
		/// <summary>
		/// Cleans up the writer, returning any rented resources
		/// </summary>
		/// <returns></returns>
		public async ValueTask DisposeAsync()
		{
			await FlushAsync();

			if (_PendingBuffer != null)
			{
				ArrayPool<byte>.Shared.Return(_PendingBuffer);

				_PendingBuffer = null;
			}

			// Wait for all data to be written
			while (_OutstandingBytes > 0)
				await _FlushWaiter.Wait(Timeout.InfiniteTimeSpan);
		}
#endif

		/// <summary>
		/// Flushes any buffered data
		/// </summary>
		public bool Flush(CancellationToken token = default) => Flush(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Flushes any buffered data
		/// </summary>
		public bool Flush(TimeSpan timeout, CancellationToken token = default)
		{
			if (_PendingBytes == 0)
				return true;

			// If we have too much data outstanding, we block
			if (timeout != Timeout.InfiniteTimeSpan && timeout != TimeSpan.Zero)
			{
				while (_OutstandingBytes > _MaxOutstandingBytes)
				{
					if (!_FlushWaiter.TryWait(timeout, token))
						return false;
				}
			}
			else
			{
				while (_OutstandingBytes > _MaxOutstandingBytes)
				{
					if (!_FlushWaiter.TryWait(timeout, token))
						return false;
				}
			}

			CheckForException();

			// Push more data
			Interlocked.Add(ref _OutstandingBytes, _PendingBytes);

			_PendingWrites.Enqueue(_PendingBuffer.AsMemory(0, _PendingBytes));
			_PendingBuffer = null;
			_PendingBytes = 0;

			// Set the state to 1 (pending write)
			if (Interlocked.Exchange(ref _WriterState, 1) == 0)
			{
				// If the previous state was 0 (no pending writes), we need to start it executing
				ThreadPool.UnsafeQueueUserWorkItem(_OnInitiateWrite, null);
			}

			return true;
		}

		/// <summary>
		/// Flushes any buffered data
		/// </summary>
		public ValueTask<bool> FlushAsync(CancellationToken token = default) => FlushAsync(Timeout.InfiniteTimeSpan, token);

		/// <summary>
		/// Flushes any buffered data
		/// </summary>
		public async ValueTask<bool> FlushAsync(TimeSpan timeout, CancellationToken token = default)
		{
			if (_PendingBytes == 0)
				return true;

			// If we have too much data outstanding, we block
			if (timeout != Timeout.InfiniteTimeSpan && timeout != TimeSpan.Zero)
			{
				while (_OutstandingBytes > _MaxOutstandingBytes)
				{
					if (!await _FlushWaiter.Wait(timeout, token))
						return false;
				}
			}
			else
			{
				while (_OutstandingBytes > _MaxOutstandingBytes)
				{
					if (!await _FlushWaiter.Wait(timeout, token))
						return false;
				}
			}

			CheckForException();

			// Push more data
			Interlocked.Add(ref _OutstandingBytes, _PendingBytes);

			_PendingWrites.Enqueue(_PendingBuffer.AsMemory(0, _PendingBytes));
			_PendingBuffer = null;
			_PendingBytes = 0;

			// Set the state to 1 (pending write)
			if (Interlocked.Exchange(ref _WriterState, 1) == 0)
			{
				// If the previous state was 0 (no pending writes), we need to start it executing
				ThreadPool.UnsafeQueueUserWorkItem(_OnInitiateWrite, null);
			}

			return true;
		}

		/// <summary>
		/// Retrieves a buffer to write to that is at least the requested size
		/// </summary>
		/// <param name="sizeHint">The minimum length of the returned buffer. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		public Memory<byte> GetMemory(int sizeHint = 0) => GetSegment(sizeHint);

		/// <summary>
		/// Retrieves a buffer to write to that is at least the requested size
		/// </summary>
		/// <param name="sizeHint">The minimum length of the returned buffer. If 0, a non-empty buffer is returned.</param>
		/// <returns>The requested buffer</returns>
		public Span<byte> GetSpan(int sizeHint = 0) => GetSegment(sizeHint).Span;

		//****************************************

		private Memory<byte> GetSegment(int sizeHint)
		{
			if (_PendingBuffer == null)
			{
				if (sizeHint < _MinimumWriteSize)
					sizeHint = _MinimumWriteSize;

				_PendingBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
			}
			else if (_PendingBuffer.Length < sizeHint)
			{
				// Buffer is not large enough to supply the requested number of bytes
				Flush(Timeout.InfiniteTimeSpan); // Attempt an infinitely blocking flush

				// Successfully flushed any pending bytes
				if (_PendingBuffer != null)
				{
					// There's a buffer but nothing to flush because someone did a zero-byte write. Return it since it's not big enough
					ArrayPool<byte>.Shared.Return(_PendingBuffer);
					_PendingBuffer = null;
				}

				_PendingBuffer = ArrayPool<byte>.Shared.Rent(sizeHint);
			}
			else if (_PendingBuffer.Length - _PendingBytes < sizeHint)
			{
				// Attempt an infinitely blocking flush
				Flush(Timeout.InfiniteTimeSpan);
			}

			return _PendingBuffer.AsMemory(_PendingBytes);
		}

		private void OnInitiateWrite(object? state) => ProcessWrites();

		private void OnContinueWrite()
		{
			// Finalise the write we were waiting on
			FinishWrite();

			CompleteOutstandingWrite();

			// Check if there's any more writes to execute
			ProcessWrites();
		}

		private void ProcessWrites()
		{
			for (; ; )
			{
				// Set the state to 2 (writing)
				Interlocked.Exchange(ref _WriterState, 2);

				ReadOnlyMemory<byte> OutstandingBuffer;

				while (!_PendingWrites.TryDequeue(out OutstandingBuffer))
				{
					// No data to write (may have been preempted between adding to the queue and updating the state flag)
					// Try and set the state back to 0 (no pending writes)
					if (Interlocked.CompareExchange(ref _WriterState, 0, 2) != 1)
						return; // State was changed back to 0, so we can exit

					// The state is 1 (pending write), so loop and try to grab it
				}

				// We have an buffer to write. We'll need to save it so we can release the memory back to the array pool
				if (!MemoryMarshal.TryGetArray(OutstandingBuffer, out var BufferSegment))
					throw new InvalidOperationException("Couldn't find the underlying buffer");

				// Begin the write
				_OutstandingBuffer = BufferSegment;

				if (_WriteException != null)
				{
					CompleteOutstandingWrite();

					continue;
				}

				try
				{
#if NETSTANDARD2_0
					var WriteTask = new ValueTask(Stream.WriteAsync(BufferSegment.Array, BufferSegment.Offset, BufferSegment.Count));
#else
					var WriteTask = Stream.WriteAsync(OutstandingBuffer);
#endif

					_WriteAwaiter = WriteTask.GetAwaiter();

					if (!_WriteAwaiter.IsCompleted)
					{
						// Write did not complete synchronously, so queue the continuation and return
						_WriteAwaiter.OnCompleted(_OnContinueWrite);

						return;
					}

					// Write completed synchronously. We're already executing, so finish it, then loop back and get another operation
					FinishWrite();

					CompleteOutstandingWrite();
				}
				catch (Exception e)
				{
					// Handle when a write fails
					_WriteException = e;

					CompleteOutstandingWrite();
				}
			}
		}

		private void FinishWrite()
		{
			// Finish the awaiting
			try
			{
				_WriteAwaiter.GetResult();
			}
			catch (Exception e)
			{
				// Handle when a write fails
				_WriteException = e;
			}
		}

		private void CompleteOutstandingWrite()
		{
			// Write is complete/failed, return the rented buffer
			ArrayPool<byte>.Shared.Return(_OutstandingBuffer.Array!);

			// Update the outstanding bytes count
			Interlocked.Add(ref _OutstandingBytes, -_OutstandingBuffer.Count);

			_OutstandingBuffer = default;

			// If a flush is blocking, wake it up so it checks Outstanding Bytes again
			_FlushWaiter.Set();
		}

		private void CheckForException()
		{
			if (_WriteException == null)
				return;

			throw new IOException("Failed to write to the underlying Stream", _WriteException);
		}

		//****************************************

		/// <summary>
		/// Gets the underlying <see cref="Stream"/> being written to
		/// </summary>
		public Stream Stream { get; }
	}
}
