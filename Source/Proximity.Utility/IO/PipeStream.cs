/****************************************\
 PipeStream.cs
 Created: 2016-02-09
\****************************************/
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility.Collections;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.IO
{
	/// <summary>
	/// Provides a pair of streams that maintain a rolling buffer where data can be written to one and the read from the other
	/// </summary>
	/// <remarks>
	/// <para>Supports Length and Position on both streams, though since data is discarded once read, seeking is not supported.</para>
	/// <para>The ReadStream will report Length as the amount of data written so far, and Position as the amount of data read so far.</para>
	/// <para>The WriteStream will report Length and Position as the amount of data written so far</para>
	/// <para>ReadStream supports ReadTimeout for blocking reads</para>
	/// </remarks>
	public sealed class PipeStream
	{	//****************************************
		private const int WriteByteBufferSize = 128;
		//****************************************
		private readonly bool _OwnsWrittenBuffers;
		private readonly InputStream _WriteStream;
		private readonly OutputStream _ReadStream;

		private readonly ConcurrentQueue<byte[]> _DataStore = new ConcurrentQueue<byte[]>();
		private readonly AsyncCollection<byte[]> _DataCollection;

		private long _BytesRead, _BytesWritten;

		private TaskCompletionSource<VoidStruct> _CompleteTask;
		//****************************************

		/// <summary>
		/// Creates a new Pipe Stream
		/// </summary>
		public PipeStream() : this(false)
		{
		}

		/// <summary>
		/// Creates a new Pipe Stream
		/// </summary>
		/// <param name="ownsWrittenBuffers">Whether the stream takes ownership of buffers given to WriteStream.Write</param>
		public PipeStream(bool ownsWrittenBuffers)
		{
			_OwnsWrittenBuffers = ownsWrittenBuffers;
			_DataCollection = new AsyncCollection<byte[]>(_DataStore);

			_WriteStream = new InputStream(this);
			_ReadStream = new OutputStream(this);
		}

		//****************************************

		/// <summary>
		///  Completes the Pipe Stream
		/// </summary>
		/// <remarks>Once all data is read, the Read Stream will return zero for bytes read</remarks>
		public void Complete()
		{
			_DataCollection.CompleteAdding();

			if (_CompleteTask != null)
				_CompleteTask.TrySetResult(VoidStruct.Empty);
		}

		/// <summary>
		/// Waits for the pipe stream to be fully written to
		/// </summary>
		/// <param name="token">A cancellation token to abort waiting</param>
		/// <returns>A Task that completes when the pipe stream has been written to and Completed</returns>
		public Task WaitForComplete(CancellationToken token)
		{
			if (_DataCollection.IsAddingCompleted)
				return VoidStruct.EmptyTask;

			var Result = Interlocked.CompareExchange(ref _CompleteTask, new TaskCompletionSource<VoidStruct>(), null);
			
			if (_DataCollection.IsAddingCompleted)
				Result.TrySetResult(VoidStruct.Empty);

			return Result.Task.When(token);
		}

		//****************************************

		/// <summary>
		/// Gets the stream that can write to the Pipe Stream
		/// </summary>
		public Stream WriteStream
		{
			get { return _WriteStream; }
		}

		/// <summary>
		/// Gets the stream that can read from the Pipe Stream
		/// </summary>
		public Stream ReadStream
		{
			get { return _ReadStream; }
		}

		/// <summary>
		/// Gets whether the stream takes ownership of buffers given to WriteStream.Write
		/// </summary>
		/// <remarks>If false, PipeStream will always copy written data first</remarks>
		public bool OwnsWrittenBuffers
		{
			get { return _OwnsWrittenBuffers; }
		}

		/// <summary>
		/// Gets whether the stream has been completed for writing
		/// </summary>
		public bool IsComplete
		{
			get { return _DataCollection.IsAddingCompleted; }
		}

		/// <summary>
		/// Gets whether the stream has been completed and all data is read
		/// </summary>
		public bool IsEOF
		{
			get { return _DataCollection.IsCompleted && _ReadStream.IsComplete; }
		}

		//****************************************

		private abstract class BaseStream : Stream
		{	//****************************************
			private readonly PipeStream _PipeStream;
			//****************************************

			protected BaseStream(PipeStream pipeStream)
			{
				_PipeStream = pipeStream;
			}

			//****************************************

			public override void Flush()
			{
			}

#if !NET40
			public override Task FlushAsync(CancellationToken cancellationToken)
			{
				return VoidStruct.EmptyTask;
			}
#endif

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			//****************************************

			public override bool CanSeek
			{
				get { return false; }
			}

			protected PipeStream PipeStream
			{
				get { return _PipeStream; }
			}
		}

		private sealed class InputStream : BaseStream
		{
			internal InputStream(PipeStream pipeStream) : base(pipeStream)
			{
			}

			//****************************************

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				// If it's safe to do so and we're allowed, just store the buffer itself and save a copy
				if (PipeStream._OwnsWrittenBuffers && offset == 0 && buffer.Length == count)
				{
					PipeStream._DataCollection.Add(buffer);
				}
				else
				{
					// Unsafe to store the whole buffer, or we're not allowed to. Copy the written bytes
					var Target = new byte[count];

					Array.Copy(buffer, offset, Target, 0, count);

					PipeStream._DataCollection.Add(Target);
				}

				Interlocked.Add(ref PipeStream._BytesWritten, count);
			}

#if !NET40
			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{
				Write(buffer, offset, count);

				return VoidStruct.EmptyTask;
			}
#endif

			public override void WriteByte(byte value)
			{
				// Can't get any better than this without some major rework
				Write(new byte[] { value }, 0, 1);
			}

			//****************************************

			public override bool CanRead
			{
				get { return false; }
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			public override long Length
			{
				get { return PipeStream._BytesWritten; }
			}

			public override long Position
			{
				get { return PipeStream._BytesWritten; }
				set { throw new NotSupportedException(); }
			}
		}

		private sealed class OutputStream : BaseStream
		{	//****************************************
			private ArraySegment<byte> _Remainder;
			private int _ReadTimeout = Timeout.Infinite;
			//****************************************

			internal OutputStream(PipeStream pipeStream) : base(pipeStream)
			{
			}

			//****************************************

			public override int Read(byte[] buffer, int offset, int count)
			{	//****************************************
				var DataSource = PipeStream._DataCollection;
				int ReadBytes, OutBytes = 0;
				byte[] OutData;
				//****************************************

				// If there is any pending data, read it out
				if (_Remainder.Count != 0)
				{
					ReadBytes = Math.Min(count, _Remainder.Count);

					Array.Copy(_Remainder.Array, _Remainder.Offset, buffer, offset, ReadBytes);

					OutBytes += ReadBytes;

					// If we read all the remainder, clear it
					if (ReadBytes == _Remainder.Count)
						_Remainder = default(ArraySegment<byte>);
					else
						_Remainder = new ArraySegment<byte>(_Remainder.Array, _Remainder.Offset + ReadBytes, _Remainder.Count - ReadBytes);

					offset += ReadBytes;
					count -= ReadBytes;
				}

				// Check that we have buffer space to write more data to
				while (count > 0)
				{
					// More buffer space available, try and retrieve an item.
					if (OutBytes == 0)
					{
						// Since we've not read anything, we block until there's data available or the collection is completed
						if (!DataSource.TryTake(out OutData, new TimeSpan(_ReadTimeout)))
							return 0; // Collection completed and empty, return a zero read to signal the end of the stream
					}
					else
					{
						// We've already read out some data, so we shouldn't block if there's no more to get
						if (!DataSource.TryTake(out OutData))
							break;
					}

					// Read as much data as possible
					ReadBytes = Math.Min(count, OutData.Length);

					Array.Copy(OutData, 0, buffer, offset, ReadBytes);

					OutBytes += ReadBytes;

					// If there's data left over, we've filled the buffer
					if (OutData.Length > ReadBytes)
					{
						// Store the remainder and finish
						_Remainder = new ArraySegment<byte>(OutData, ReadBytes, OutData.Length - ReadBytes);

						break;
					}

					// Update our buffer targets
					offset += ReadBytes;
					count -= ReadBytes;
				}

				Interlocked.Add(ref PipeStream._BytesRead, OutBytes);

				return OutBytes;
			}

#if !NET40
			public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{	//****************************************
				var DataSource = PipeStream._DataCollection;
				int ReadBytes, OutBytes = 0;
				byte[] OutData;
				//****************************************

				// If there is any pending data, read it out
				if (_Remainder.Count != 0)
				{
					ReadBytes = Math.Min(count, _Remainder.Count);

					Array.Copy(_Remainder.Array, _Remainder.Offset, buffer, offset, ReadBytes);

					OutBytes += ReadBytes;

					// If we read all the remainder, clear it
					if (ReadBytes == _Remainder.Count)
						_Remainder = default(ArraySegment<byte>);
					else
						_Remainder = new ArraySegment<byte>(_Remainder.Array, _Remainder.Offset + ReadBytes, _Remainder.Count - ReadBytes);

					offset += ReadBytes;
					count -= ReadBytes;
				}

				// Check that we have buffer space to write more data to
				while (count > 0)
				{
					// More buffer space available, try and retrieve an item.
					if (OutBytes == 0)
					{
						try
						{
							// Since we've not read anything, we wait until there's data available or the collection is completed
							OutData = await DataSource.Take(cancellationToken);
						}
						catch (InvalidOperationException)
						{
							return 0; // Collection completed and empty, return a zero read to signal the end of the stream
						}
					}
					else
					{
						// We've already read out some data, so we shouldn't block if there's no more to get
						if (!DataSource.TryTake(out OutData))
							break;
					}

					// Read as much data as possible
					ReadBytes = Math.Min(count, OutData.Length);

					Array.Copy(OutData, 0, buffer, offset, ReadBytes);

					OutBytes += ReadBytes;

					// If there's data left over, we've filled the buffer
					if (OutData.Length > ReadBytes)
					{
						// Store the remainder and finish
						_Remainder = new ArraySegment<byte>(OutData, ReadBytes, OutData.Length - ReadBytes);

						break;
					}

					// Update our buffer targets
					offset += ReadBytes;
					count -= ReadBytes;
				}

				Interlocked.Add(ref PipeStream._BytesRead, OutBytes);

				return OutBytes;
			}
#endif

			public override int ReadByte()
			{	//****************************************
				byte Result;
				byte[] OutData;
				//****************************************

				// If there's a remainer, snip off one byte
				if (_Remainder.Count != 0)
				{
					Result = _Remainder.Array[_Remainder.Offset];

					if (_Remainder.Count == 1)
						_Remainder = default(ArraySegment<byte>);
					else
						_Remainder = new ArraySegment<byte>(_Remainder.Array, _Remainder.Offset + 1, _Remainder.Count - 1);

					return Result;
				}

				// Since we've not read anything, we block until there's data available or the collection is completed
				if (!PipeStream._DataCollection.TryTake(out OutData, new TimeSpan(_ReadTimeout)))
					return -1; // Collection completed and empty, return a zero read to signal the end of the stream

				Result = OutData[0];

				// If we got more than one byte out, store the remainder
				if (OutData.Length > 1)
				{
					_Remainder = new ArraySegment<byte>(OutData, 1, OutData.Length - 1);
				}

				return Result;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			//****************************************

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanWrite
			{
				get { return false; }
			}

			public override bool CanTimeout
			{
				get { return _ReadTimeout != 0; }
			}

			public override int ReadTimeout
			{
				get { return _ReadTimeout; }
				set
				{
					if (value != Timeout.Infinite && value < 0)
						throw new ArgumentOutOfRangeException("value", "Timeout must be Timeout.Infinite (-1) or a number equal or greater than zero");

					_ReadTimeout = value;
				}
			}

			public override long Length
			{
				get { return PipeStream._BytesWritten; }
			}

			public override long Position
			{
				get { return PipeStream._BytesRead; }
				set { throw new NotSupportedException(); }
			}

			public bool IsComplete
			{
				get { return _Remainder.Array == null; }
			}
		}
	}
}
