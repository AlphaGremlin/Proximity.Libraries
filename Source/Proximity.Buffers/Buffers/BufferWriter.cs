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
					_Pool.Return(MyBuffer.Array, clearBuffers);

				Segment = Segment.Next;
			}

			if (_CurrentOffset != 0)
			{
				if (MemoryMarshal.TryGetArray<T>(_CurrentBuffer, out var MyBuffer))
					_Pool.Return(MyBuffer.Array, clearBuffers);
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

			// Can we honour the requested size?
			if (sizeHint > _CurrentBuffer.Length - _CurrentOffset)
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
		/// <remarks>The sequence may mutate and become invalid if the BufferWriter is written to after generation</remarks>
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
			if (_CurrentOffset == 0)
				// No, so just return what we have
				return new ReadOnlySequence<T>(_HeadSegment, 0, _TailSegment, _TailSegment!.Memory.Length);

			// Does the current tail segment have a cached segment that matches our outstanding data??
			if (_TailSegment!.Next == null || _TailSegment.Next.Memory.Length != _CurrentOffset)
				// No, so add/replace it
				_TailSegment.JoinTo(new BufferSegment(_CurrentBuffer.Slice(0, _CurrentOffset), _TailSegment.RunningIndex + _TailSegment.Memory.Length));

			// Return the sequence
			return new ReadOnlySequence<T>(_HeadSegment, 0, _TailSegment.Next, _TailSegment!.Next!.Memory.Length);
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
		}
	}
}
