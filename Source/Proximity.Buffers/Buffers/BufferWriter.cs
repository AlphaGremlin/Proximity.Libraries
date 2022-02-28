using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Proximity.Buffers;

namespace System.Buffers
{
	/// <summary>
	/// Provides a Buffer Writer that generates a <see cref="ReadOnlySequence{T}" />
	/// </summary>
	/// <typeparam name="T">The type of buffer element to write</typeparam>
	public sealed class BufferWriter<T> : IBufferWriter<T>, IDisposable
	{ //****************************************
		private readonly ArrayPool<T> _Pool;

		private readonly int _MinimumBlockSize;

		private Memory<T> _CurrentBuffer;
		private int _CurrentOffset;

		private BufferSegment? _HeadSegment, _TailSegment;
		//****************************************

		/// <summary>
		/// Creates a new Buffer Writer using the shared ArrayPool
		/// </summary>
		public BufferWriter() : this(ArrayPool<T>.Shared, 1024)
		{
		}

		/// <summary>
		/// Creates a new Buffer Writer using a given ArrayPool
		/// </summary>
		/// <param name="pool">The ArrayPool to use</param>
		/// <remarks>If <paramref name="pool"/> is null, does not use pooled arrays. Useful when the Sequence will exist outside the lifetime of the writer and cannot be disposed</remarks>
		public BufferWriter(ArrayPool<T>? pool) : this(pool, 1024)
		{
		}

		/// <summary>
		/// Creates a new Buffer Writer using a given ArrayPool
		/// </summary>
		/// <param name="pool">The ArrayPool to use</param>
		/// <param name="minBlockSize">The minimum block size</param>
		/// <remarks>If <paramref name="pool"/> is null, does not use pooled arrays. Useful when the Sequence will exist outside the lifetime of the writer and cannot be disposed</remarks>
		public BufferWriter(ArrayPool<T>? pool, int minBlockSize)
		{
			if (minBlockSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(minBlockSize));

			_Pool = pool ?? DummyPool<T>.Shared;
			_MinimumBlockSize = minBlockSize;
		}

		//****************************************

		void IDisposable.Dispose() => Reset(false);

		/// <summary>
		/// Advances the Buffer Writer
		/// </summary>
		/// <param name="count">The number of elements written to the current buffer</param>
		public void Advance(int count)
		{
			if (count < 0 || count > _CurrentBuffer.Length - _CurrentOffset)
				throw new ArgumentOutOfRangeException(nameof(count));

			_CurrentOffset += count;
		}

		/// <summary>
		/// Flushes the Buffer Writer, returning all current buffers as an <see cref="AutoSequence{T}"/> and resetting the writer
		/// </summary>
		/// <returns>An <see cref="AutoSequence{T}"/> returning the current state of the Buffer Writer</returns>
		/// <remarks>The Buffer Writer will be empty after this call, and can be safely written without affecting the result. Buffers will not be copied, and can be released using <see cref="AutoSequence{T}"/></remarks>
		public AutoSequence<T> Flush() => Flush(false);

		/// <summary>
		/// Flushes the Buffer Writer, returning all current buffers as an <see cref="AutoSequence{T}"/> and resetting the writer
		/// </summary>
		/// <param name="clearBuffers">True to clear the buffers when the <see cref="AutoSequence{T}"/> is disposed</param>
		/// <returns>An <see cref="AutoSequence{T}"/> returning the current state of the Buffer Writer</returns>
		/// <remarks>The Buffer Writer will be empty after this call, and can be safely written without affecting the result. Buffers will not be copied, and can be released using <see cref="AutoSequence{T}"/></remarks>
		public AutoSequence<T> Flush(bool clearBuffers)
		{
			if (_HeadSegment == null)
			{
				// Is there any data in this Writer?
				if (_CurrentOffset == 0)
					return new AutoSequence<T>(ReadOnlySequence<T>.Empty, _Pool, clearBuffers);

				// Create a new head segment to hold the data
				_HeadSegment = _TailSegment = new BufferSegment(_CurrentBuffer.Slice(0, _CurrentOffset), 0);
			}
			// We have a head segment. Is there any outstanding data?
			else if (_CurrentOffset > 0)
			{
				// Add a new tail segment
				var OldTail = _TailSegment!;

				_TailSegment = new BufferSegment(_CurrentBuffer.Slice(0, _CurrentOffset), OldTail.RunningIndex + OldTail.Memory.Length);

				OldTail.JoinTo(_TailSegment);
			}

			var Sequence = new AutoSequence<T>(new ReadOnlySequence<T>(_HeadSegment, 0, _TailSegment!, _TailSegment!.Memory.Length), _Pool, clearBuffers);

			// Release the current buffers into the hands of the AutoSequence
			_CurrentBuffer = Memory<T>.Empty;
			_CurrentOffset = 0;
			_TailSegment = _HeadSegment = null;

			return Sequence;
		}

		/// <summary>
		/// Flushes the Buffer Writer, returning the current buffer (if possible) as an <see cref="AutoArraySegment{T}"/> and resetting the writer
		/// </summary>
		/// <returns>A single array segment containing all written data</returns>
		/// <remarks>The Buffer Writer will be empty after this call, and can be safely written without affecting the result. Buffers will not be copied if the writer has only used a single array, and can be released using <see cref="AutoArraySegment{T}"/></remarks>
		public AutoArraySegment<T> FlushArray() => FlushArray(false);

		/// <summary>
		/// Flushes the Buffer Writer, returning the current buffer (if possible) as an <see cref="AutoArraySegment{T}"/> and resetting the writer
		/// </summary>
		/// <param name="clearBuffers">True to clear the buffers when the <see cref="AutoArraySegment{T}"/> is disposed</param>
		/// <returns>A single array segment containing all written data</returns>
		/// <remarks>The Buffer Writer will be empty after this call, and can be safely written without affecting the result. Buffers will not be copied if the writer has only used a single array, and can be released using <see cref="AutoArraySegment{T}"/></remarks>
		public AutoArraySegment<T> FlushArray(bool clearBuffers)
		{
			if (TryGetMemory(out var Memory))
			{
				if (MemoryMarshal.TryGetArray(Memory, out var Segment))
				{
					var Result = AutoArraySegment.Over(Segment, _Pool);

					// Release the current buffer into the hands of the AutoSequence
					_CurrentBuffer = Memory<T>.Empty;
					_CurrentOffset = 0;
					_TailSegment = _HeadSegment = null;

					return Result;
				}

				// Should never happen
			}

			// More than one buffer is in use, so we need to copy to a single segment
			var BufferLength = (int)Length;
			var Buffer = _Pool.Rent(BufferLength);

			// Copy our current chain of buffers into it
			new ReadOnlySequence<T>(_HeadSegment!, 0, _TailSegment!, _TailSegment!.Memory.Length).CopyTo(Buffer);

			// Copy the final segment (if any)
			if (_CurrentOffset > 0)
				_CurrentBuffer.Slice(0, _CurrentOffset).CopyTo(Buffer.AsMemory((int)_TailSegment.RunningIndex + _TailSegment.Memory.Length));

			Reset(clearBuffers); // Reset the writer

			return AutoArraySegment.Over(new ArraySegment<T>(Buffer, 0, BufferLength), _Pool);
		}

		/// <summary>
		/// Resets the Buffer Writer, returning its buffers to the pool
		/// </summary>
		/// <remarks>The Buffer Writer can be reused after disposal</remarks>
		public void Reset() => Reset(false);

		/// <summary>
		/// Resets the Buffer Writer, returning its buffers to the pool
		/// </summary>
		/// <remarks>The Buffer Writer can be reused after disposal</remarks>
		public void Reset(bool clearBuffers)
		{
			ReadOnlySequenceSegment<T>? Segment = _HeadSegment;

			while (Segment != null)
			{
				if (MemoryMarshal.TryGetArray(Segment.Memory, out var MyBuffer))
					_Pool.Return(MyBuffer.Array!, clearBuffers);

				Segment = Segment.Next;
			}

			if (!_CurrentBuffer.IsEmpty)
			{
				if (MemoryMarshal.TryGetArray<T>(_CurrentBuffer, out var MyBuffer))
					_Pool.Return(MyBuffer.Array!, clearBuffers);
			}

			_CurrentBuffer = Memory<T>.Empty;
			_CurrentOffset = 0;
			_TailSegment = _HeadSegment = null;
		}

		/// <summary>
		/// Gets a Memory to write to
		/// </summary>
		/// <param name="sizeHint">A hint as to the number of elements desired</param>
		/// <returns>A Memory that can be written to</returns>
		public Memory<T> GetMemory(int sizeHint)
		{
			if (_CurrentBuffer.IsEmpty)
				_CurrentBuffer = _Pool.Rent(Math.Max(sizeHint, _MinimumBlockSize));

			// Can we honour the requested size? If they ask for zero, we only allocate another segment once the current one is completely full
			if (sizeHint > _CurrentBuffer.Length - _CurrentOffset || (sizeHint == 0 && _CurrentBuffer.Length == _CurrentOffset))
			{
				// Not enough buffer space, allocate a segment to store the existing buffer
				if (_TailSegment == null)
				{
					// No start segment, so create it
					_TailSegment = _HeadSegment = new BufferSegment(_CurrentBuffer.Slice(0, _CurrentOffset), 0);
				}
				else
				{
					// We have an existing head segment, chain a new segment to it
					var OldTail = _TailSegment;

					_TailSegment = new BufferSegment(_CurrentBuffer.Slice(0, _CurrentOffset), OldTail.RunningIndex + OldTail.Memory.Length);

					OldTail.JoinTo(_TailSegment);
				}

				// Create a new buffer to write to
				_CurrentBuffer = _Pool.Rent(Math.Max(sizeHint, _MinimumBlockSize));
				_CurrentOffset = 0;
			}
			
			return _CurrentBuffer.Slice(_CurrentOffset);
		}

		/// <summary>
		/// Gets a Span to write to
		/// </summary>
		/// <param name="sizeHint">A hint as to the number of elements desired</param>
		/// <returns>A span that can be written to</returns>
		public Span<T> GetSpan(int sizeHint) => GetMemory(sizeHint).Span;

		/// <summary>
		/// Gets a sequence representing the written data
		/// </summary>
		/// <remarks>The sequence will become invalid after <see cref="O:Reset"/> is called.</remarks>
		public ReadOnlySequence<T> ToSequence()
		{
			if (_HeadSegment == null)
			{
				// Is there any data in this Writer?
				if (_CurrentOffset == 0)
					return ReadOnlySequence<T>.Empty;

				// Yes, so return just that
				return new ReadOnlySequence<T>(_CurrentBuffer.Slice(0, _CurrentOffset));
			}

			// We have a head segment. Is there any outstanding data?
			if (_CurrentOffset > 0)
			{
			// Add a new tail segment
				var OldTail = _TailSegment!;

				_TailSegment = new BufferSegment(_CurrentBuffer.Slice(0, _CurrentOffset), OldTail.RunningIndex + OldTail.Memory.Length);

				OldTail.JoinTo(_TailSegment);

				// Empty the current buffer, so a call to Reset doesn't call Return twice
				_CurrentBuffer = Memory<T>.Empty;
				_CurrentOffset = 0;
			}

			// Return the sequence
			return new ReadOnlySequence<T>(_HeadSegment, 0, _TailSegment!, _TailSegment!.Memory.Length);
		}

		/// <summary>
		/// Gets a <see cref="ReadOnlyMemory{T}"/> containing all written data
		/// </summary>
		/// <returns>A rented buffer containing all written data</returns>
		/// <remarks>Will avoid allocations/copying if possible. The buffer will become invalid after <see cref="O:Reset"/> is called.</remarks>
		public ReadOnlyMemory<T> ToMemory()
		{
			if (TryGetMemory(out var Memory))
				return Memory.ToArray();

			var BufferLength = (int)Length;

			// Grab a buffer that represents the entire contents of this Writer
			var Buffer = _Pool.Rent(BufferLength);

			// Copy our current chain of buffers into it
			new ReadOnlySequence<T>(_HeadSegment!, 0, _TailSegment!, _TailSegment!.Memory.Length).CopyTo(Buffer);

			// Copy the final segment (if any)
			if (_CurrentOffset > 0)
				_CurrentBuffer.Slice(0, _CurrentOffset).CopyTo(Buffer.AsMemory((int)_TailSegment.RunningIndex + _TailSegment.Memory.Length));

			// Release our old buffers
			Reset();

			// Replace our root buffer with the new buffer we just allocated
			_CurrentBuffer = Buffer;
			_CurrentOffset = BufferLength;

			return Buffer.AsMemory(0, BufferLength);
		}

		/// <summary>
		/// Gets a <see cref="ArraySegment{T}"/> containing all written data
		/// </summary>
		/// <returns>A rented buffer containing all written data</returns>
		/// <remarks>Will avoid allocations/copying if possible. The buffer will become invalid after <see cref="O:Reset"/> is called.</remarks>
		public ArraySegment<T> ToArraySegment()
		{
			var Memory = ToMemory();

			if (MemoryMarshal.TryGetArray(Memory, out var Segment))
				return Segment;

			// Should never happen
			return new ArraySegment<T>(Memory.ToArray());
		}

		/// <summary>
		/// Gets an array containing all written data
		/// </summary>
		/// <returns>A single array containing all written data</returns>
		/// <remarks>The buffer is not rented from the array pool. Does not 'finish' the final segment, so writing can potentially continue without allocating/renting.</remarks>
		public T[] ToArray()
		{
			if (TryGetMemory(out var Memory))
				return Memory.ToArray();

			var BufferLength = (int)Length;

			// Grab a buffer that represents the entire contents of this Writer
			var Buffer = new T[BufferLength];

			// Copy our current chain of buffers into it
			new ReadOnlySequence<T>(_HeadSegment!, 0, _TailSegment!, _TailSegment!.Memory.Length).CopyTo(Buffer);

			// Copy the final segment (if any)
			if (_CurrentOffset > 0)
				_CurrentBuffer.Slice(0, _CurrentOffset).CopyTo(Buffer.AsMemory((int)_TailSegment.RunningIndex + _TailSegment.Memory.Length));

			return Buffer;
		}

		/// <summary>
		/// Gets an array segment containing all written data with automated disposal
		/// </summary>
		/// <returns>A single array segment containing all written data</returns>
		/// <remarks>The buffer is rented from the array pool. Does not 'finish' the final segment, so writing can potentially continue without allocating/renting.</remarks>
		public AutoArraySegment<T> ToAutoArray()
		{
			// Grab a buffer that represents the entire contents of this Writer
			var BufferLength = (int)Length;
			var Buffer = _Pool.Rent(BufferLength);

			if (TryGetMemory(out var Memory))
			{
				Memory.CopyTo(Buffer);
			}
			else
			{
				// Copy our current chain of buffers into it
				new ReadOnlySequence<T>(_HeadSegment!, 0, _TailSegment!, _TailSegment!.Memory.Length).CopyTo(Buffer);

				// Copy the final segment (if any)
				if (_CurrentOffset > 0)
					_CurrentBuffer.Slice(0, _CurrentOffset).CopyTo(Buffer.AsMemory((int)_TailSegment.RunningIndex + _TailSegment.Memory.Length));
			}

			return AutoArraySegment.Over(new ArraySegment<T>(Buffer, 0, BufferLength), _Pool);
		}

		/// <summary>
		/// Discards data written to the start of the buffer
		/// </summary>
		/// <param name="length">The amount of data to discard</param>
		public void TrimStart(int length) => TrimStart(length, false);

		/// <summary>
		/// Discards data written to the start of the buffer
		/// </summary>
		/// <param name="length">The amount of data to discard</param>
		/// <param name="clearBuffers">True to clear released buffers</param>
		public void TrimStart(int length, bool clearBuffers)
		{
			var Length = this.Length;

			if (length > Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			if (length == Length)
			{
				Reset();

				return;
			}

			if (_HeadSegment != null)
			{
				var RemovedLength = 0;

				// Remove segments until we find one longer than Length, or we run out of segments
				while (_HeadSegment.Memory.Length < length)
				{
					var OldLength = _HeadSegment.Memory.Length;

					length -= OldLength;
					RemovedLength += OldLength;

					if (MemoryMarshal.TryGetArray(_HeadSegment.Memory, out var MyBuffer))
						_Pool.Return(MyBuffer.Array!, clearBuffers);

					_HeadSegment = (BufferSegment)_HeadSegment.Next;

					if (_HeadSegment == null)
						break;
				}

				var NextSegment = _HeadSegment;

				if (NextSegment != null)
				{
					// Correct the remainder on the header
					NextSegment.CorrectMemory(length);
					NextSegment.CorrectRunningIndex(RemovedLength);
					NextSegment = (BufferSegment)NextSegment.Next;

					// Correct the Running Index on subsequent entries
					while (NextSegment != null)
					{
						NextSegment.CorrectRunningIndex(length + RemovedLength);

						NextSegment = (BufferSegment)NextSegment.Next;
					}

					return;
				}

				// We removed all saved segments, leaving only the contents of the current buffer
			}

			// We could copy internally to the buffer, but it's faster to just ignore the data already written
			// If we're clearing the buffers, also clear the data we're ignoring
			if (clearBuffers)
				_CurrentBuffer.Slice(0, length).Span.Clear();

			_CurrentBuffer = _CurrentBuffer.Slice(length);
			_CurrentOffset -= length;
		}

		//****************************************

		private bool TryGetMemory(out ReadOnlyMemory<T> memory)
		{
			if (_HeadSegment == null)
			{
				// Is there any data in this Writer?
				if (_CurrentOffset == 0)
					memory = ReadOnlyMemory<T>.Empty;
				else
				// Yes, so return just that
					memory = _CurrentBuffer.Slice(0, _CurrentOffset);

				return true;
			}

			// We have a head segment. Is there any outstanding data?
			if (_CurrentOffset == 0)
			{
				// No. If we're just a head, return that directly
				if (_HeadSegment == _TailSegment)
				{
					memory = _HeadSegment.Memory;

					return true;
				}
			}

			memory = default;

			return false;
		}

		//****************************************

		/// <summary>
		/// Gets the number of elements written
		/// </summary>
		public long Length => _TailSegment == null ? _CurrentOffset : _TailSegment.RunningIndex + _TailSegment.Memory.Length + _CurrentOffset;

		//****************************************

		private sealed class BufferSegment : ReadOnlySequenceSegment<T>
		{
			internal BufferSegment(Memory<T> buffer, long runningIndex)
			{
				Memory = buffer;
				RunningIndex = runningIndex;
			}

			//****************************************

			internal void JoinTo(BufferSegment segment)
			{
				Next = segment;
			}

			internal void CorrectRunningIndex(int length)
			{
				if (RunningIndex >= length)
					RunningIndex -= length;
			}

			internal void CorrectMemory(int offset) => Memory = Memory.Slice(offset);
		}
	}
}
