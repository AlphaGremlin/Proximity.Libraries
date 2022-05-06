using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proximity.Threading.Tests.Mocks
{
	internal sealed class VerifyingAsyncWriteStream : Stream
	{ //****************************************
		private readonly TimeSpan _Delay;
		private long _Length;

		private TaskCompletionSource _CurrentOperation;
		//****************************************

		public VerifyingAsyncWriteStream(TimeSpan delay)
		{
			_Delay = delay;
		}

		//****************************************

		public void Exercise()
		{
			var OldOperation = Interlocked.Exchange(ref _CurrentOperation, null);

			if (OldOperation == null)
				throw new InvalidOperationException();

			OldOperation.SetResult();
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			var TaskSource = new TaskCompletionSource(state);

			InternalWriteAsync(buffer.AsMemory(offset, count), callback, TaskSource);

			return TaskSource.Task;

			async void InternalWriteAsync(ReadOnlyMemory<byte> buffer, AsyncCallback callback, TaskCompletionSource completionSource)
			{
				try
				{
					await WriteAsync(buffer, CancellationToken.None);

					completionSource.SetResult();
				}
				catch (Exception e)
				{
					completionSource.SetException(e);
				}

				callback(completionSource.Task);
			}
		}

		protected override void Dispose(bool disposing)
		{
			// Check for concurrent operations
			if (_CurrentOperation != null)
				throw new InvalidOperationException();

			base.Dispose(disposing);
		}

		public override ValueTask DisposeAsync()
		{
			// Check for concurrent operations
			if (_CurrentOperation != null)
				throw new InvalidOperationException();

			return base.DisposeAsync();
		}

		public override void EndWrite(IAsyncResult asyncResult) => ((Task)asyncResult).Wait();

		public override void Flush()
		{
			// Check for concurrent operations
			if (_CurrentOperation != null)
				throw new InvalidOperationException();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			var NewOperation = new TaskCompletionSource();

			// Check for concurrent operations
			if (Interlocked.CompareExchange(ref _CurrentOperation, NewOperation, null) != null)
				throw new InvalidOperationException();

			if (_Delay != Timeout.InfiniteTimeSpan)
				Task.Delay(_Delay, cancellationToken).ContinueWith(FinishOperation, NewOperation);

			return NewOperation.Task;
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => buffer.AsSpan(offset, count);

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			// Check for concurrent operations
			if (_CurrentOperation != null)
				throw new InvalidOperationException();

			_Length += buffer.Length;
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			var NewOperation = new TaskCompletionSource();

			// Check for concurrent operations
			if (Interlocked.CompareExchange(ref _CurrentOperation, NewOperation, null) != null)
				throw new InvalidOperationException();

			if (_Delay != Timeout.InfiniteTimeSpan)
				Task.Delay(_Delay, cancellationToken).ContinueWith(FinishOperation, NewOperation);

			_Length += buffer.Length;

			return new ValueTask(NewOperation.Task);
		}

		public override void WriteByte(byte value)
		{
			Span<byte> SingleByte = stackalloc byte[] { value };

			Write(SingleByte);
		}

		//****************************************

		private void FinishOperation(Task ancestor, object state)
		{
			var OldOperation = Interlocked.Exchange(ref _CurrentOperation, null);

			if (OldOperation != state)
				throw new InvalidOperationException();

			if (ancestor.IsCanceled)
				OldOperation.SetCanceled();
			else
				OldOperation.SetResult();
		}
		
		//****************************************

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => _Length;

		public override long Position { get => _Length; set => throw new NotSupportedException(); }
	}
}
