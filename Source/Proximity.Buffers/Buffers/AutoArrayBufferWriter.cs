using Proximity.Buffers;

namespace System.Buffers
{
	/// <summary>
	/// Provides a Auto Array Buffer Writer that generates a <see cref="ReadOnlyMemory{T}" />
	/// </summary>
	/// <typeparam name="T">The type of buffer element to write</typeparam>
	public sealed class AutoArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
	{ //****************************************
		private const int MinimumBlockSize = 1024;
		//****************************************
		private readonly ArrayPool<T> _Pool;

		private T[]? _CurrentBuffer;
		private int _CurrentOffset;
		//****************************************

		/// <summary>
		/// Creates a new Auto Array Buffer Writer using the shared ArrayPool
		/// </summary>
		public AutoArrayBufferWriter() : this(ArrayPool<T>.Shared, 0)
		{
		}

		/// <summary>
		/// Creates a new Auto ArrayBuffer Writer using a given ArrayPool
		/// </summary>
		/// <param name="pool">The ArrayPool to use</param>
		/// <remarks>If <paramref name="pool"/> is null, does not use pooled arrays. Useful when the Sequence will exist outside the lifetime of the writer and cannot be disposed</remarks>
		public AutoArrayBufferWriter(ArrayPool<T>? pool) : this(pool, 0)
		{
		}

		/// <summary>
		/// Creates a new Auto Array Buffer Writer using a given ArrayPool
		/// </summary>
		/// <param name="pool">The ArrayPool to use</param>
		/// <param name="initialCapacity">The starting size of the buffer to rent</param>
		/// <remarks>If <paramref name="pool"/> is null, does not use pooled arrays. Useful when the Sequence will exist outside the lifetime of the writer and cannot be disposed</remarks>
		public AutoArrayBufferWriter(ArrayPool<T>? pool, int initialCapacity)
		{
			if (initialCapacity <= 0)
				throw new ArgumentOutOfRangeException(nameof(initialCapacity));

			_Pool = pool ?? DummyPool<T>.Shared;

			_CurrentBuffer = _Pool.Rent(Math.Max(initialCapacity, MinimumBlockSize));
		}

		//****************************************

		void IDisposable.Dispose() => Reset(false);

		/// <summary>
		/// Advances the writer
		/// </summary>
		/// <param name="count">The number of elements written to the current buffer</param>
		public void Advance(int count)
		{
			if (_CurrentBuffer == null)
				throw new InvalidOperationException("Cannot advance without data");

			if (count < 0 || count > _CurrentBuffer.Length - _CurrentOffset)
				throw new ArgumentOutOfRangeException(nameof(count));

			_CurrentOffset += count;
		}

		/// <summary>
		/// Flushes the writer, returning the current buffer as an <see cref="AutoArraySegment{T}"/> and resetting the writer
		/// </summary>
		/// <returns>An <see cref="AutoArraySegment{T}"/> returning the written data</returns>
		/// <remarks>The writer will rent a new buffer the same size as the currently written data. The original buffer will not be copied, and can be released using <see cref="AutoArraySegment{T}"/></remarks>
		public AutoArraySegment<T> Flush() => Flush(false);

		/// <summary>
		/// Flushes the writer, returning all current buffers as an <see cref="AutoArraySegment{T}"/> and resetting the writer
		/// </summary>
		/// <param name="clearBuffers">True to clear the buffers when the <see cref="AutoArraySegment{T}"/> is disposed</param>
		/// <returns>An <see cref="AutoSequence{T}"/> returning the written data</returns>
		/// <remarks>The writer will rent a new buffer the same size as the currently written data. The original buffer will not be copied, and can be released using <see cref="AutoArraySegment{T}"/></remarks>
		public AutoArraySegment<T> Flush(bool clearBuffers)
		{
			if (_CurrentBuffer == null)
				return default;

			var Segment = new AutoArraySegment<T>(new ArraySegment<T>(_CurrentBuffer, 0, _CurrentOffset), _Pool, clearBuffers);

			// Release the current buffers into the hands of the AutoSequence
			_CurrentBuffer = _Pool.Rent(_CurrentOffset);
			_CurrentOffset = 0;

			return Segment;
		}

		/// <summary>
		/// Resets the writer, returning its buffers to the pool
		/// </summary>
		/// <remarks>Writing to this writer will allocate a new buffer. Otherwise, <see cref="Reset()"/> is equivalent to Dispose</remarks>
		public void Reset() => Reset(false);

		/// <summary>
		/// Resets the writer, returning its buffers to the pool
		/// </summary>
		/// <param name="clearBuffers">True to clear any data in the buffer</param>
		/// <remarks>Writing to this writer will allocate a new buffer. Otherwise, <see cref="Reset(bool)"/> is equivalent to Dispose</remarks>
		public void Reset(bool clearBuffers)
		{
			if (_CurrentBuffer == null)
				return;

			_Pool.Return(_CurrentBuffer, clearBuffers);

			_CurrentBuffer = null;
			_CurrentOffset = 0;
		}

		/// <summary>
		/// Resets the writer, returning its buffers to the pool and renting a new one
		/// </summary>
		/// <param name="initialCapacity">The starting size of the buffer to rent</param>
		/// <remarks>This is not equivalent to Dispose. Call <see cref="Reset()"/> or <see cref="Reset(bool)"/> to ensure safe cleanup</remarks>
		public void Reset(int initialCapacity) => Reset(initialCapacity, false);

		/// <summary>
		/// Resets the writer, returning its buffers to the pool
		/// </summary>
		/// <param name="initialCapacity">The starting size of the buffer to rent</param>
		/// <param name="clearBuffers">True to clear any data in the buffer</param>
		/// <remarks>This is not equivalent to Dispose. Call <see cref="Reset()"/> or <see cref="Reset(bool)"/> to ensure safe cleanup</remarks>
		public void Reset(int initialCapacity, bool clearBuffers)
		{
			if (initialCapacity <= 0)
				throw new ArgumentOutOfRangeException(nameof(initialCapacity));

			if (_CurrentBuffer == null)
				return;

			_Pool.Return(_CurrentBuffer, clearBuffers);

			_CurrentBuffer = _Pool.Rent(Math.Max(initialCapacity, MinimumBlockSize));
			_CurrentOffset = 0;
		}

		/// <summary>
		/// Gets a <see cref="Memory{T}"/> to write to
		/// </summary>
		/// <param name="sizeHint">A hint as to the number of elements desired</param>
		/// <returns>A <see cref="Memory{T}"/> that can be written to</returns>
		public Memory<T> GetMemory(int sizeHint)
		{
			_CurrentBuffer ??= _Pool.Rent(Math.Max(sizeHint, MinimumBlockSize));

			// Can we honour the requested size? If they ask for zero, we only allocate another segment once the current one is completely full
			if (sizeHint > _CurrentBuffer.Length - _CurrentOffset || (sizeHint == 0 && _CurrentBuffer.Length == _CurrentOffset))
			{
				var NewBuffer = _Pool.Rent(Math.Max(sizeHint + _CurrentBuffer.Length, _CurrentBuffer.Length * 2));

				Array.Copy(_CurrentBuffer, 0, NewBuffer, 0, _CurrentOffset);

				_Pool.Return(_CurrentBuffer);

				_CurrentBuffer = NewBuffer;
			}
			
			return _CurrentBuffer.AsMemory(_CurrentOffset);
		}

		/// <summary>
		/// Gets a Span to write to
		/// </summary>
		/// <param name="sizeHint">A hint as to the number of elements desired</param>
		/// <returns>A span that can be written to</returns>
		public Span<T> GetSpan(int sizeHint) => GetMemory(sizeHint).Span;

		/// <summary>
		/// Gets a <see cref="ReadOnlyMemory{T}"/> containing all written data
		/// </summary>
		/// <returns>A rented buffer containing all written data</returns>
		/// <remarks>Will not perform allocations. The buffer will become invalid after <see cref="O:Reset"/> is called.</remarks>
		public ReadOnlyMemory<T> ToMemory()
		{
			if (_CurrentBuffer == null)
				return default;

			return _CurrentBuffer.AsMemory(0, _CurrentOffset);
		}

		/// <summary>
		/// Gets a <see cref="ArraySegment{T}"/> containing all written data
		/// </summary>
		/// <returns>A rented buffer containing all written data</returns>
		/// <remarks>Will not perform allocations. The buffer will become invalid after <see cref="O:Reset"/> is called.</remarks>
		public ArraySegment<T> ToArraySegment()
		{
			if (_CurrentBuffer == null)
				return new ArraySegment<T>(Array.Empty<T>());

			return new ArraySegment<T>(_CurrentBuffer, 0, _CurrentOffset);
		}

		/// <summary>
		/// Gets an array containing all written data
		/// </summary>
		/// <returns>A single array containing all written data</returns>
		/// <remarks>The buffer is not rented from the array pool. Does not 'finish' the final segment, so writing can potentially continue without allocating/renting.</remarks>
		public T[] ToArray() => ToMemory().ToArray();

		/// <summary>
		/// Gets an array segment containing all written data with automated disposal
		/// </summary>
		/// <returns>A single array segment containing all written data</returns>
		/// <remarks>The buffer is rented from the array pool. Does not 'finish' the final segment, so writing can potentially continue without allocating/renting.</remarks>
		public AutoArraySegment<T> ToAutoArray()
		{
			var BufferLength = Length;
			var Buffer = _Pool.Rent(BufferLength);

			ToMemory().CopyTo(Buffer);

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
		/// <param name="clearBuffers">True to clear the empty areas</param>
		public void TrimStart(int length, bool clearBuffers)
		{
			if (length < 0 || length > _CurrentOffset)
				throw new ArgumentOutOfRangeException(nameof(length));

			if (length == 0)
				return;

			if (_CurrentBuffer == null)
				throw new InvalidOperationException("Buffer inconsistent");

			// If we're not trimming all the written data, we need to move what's left
			if (length != _CurrentOffset)
				Array.Copy(_CurrentBuffer, length, _CurrentBuffer, 0, _CurrentOffset - length);

			if (clearBuffers)
				// Remove all data from the buffer
				Array.Clear(_CurrentBuffer, _CurrentOffset - length, length);

			_CurrentOffset = 0;
		}

		//****************************************

		/// <summary>
		/// Gets the number of elements written
		/// </summary>
		public int Length => _CurrentOffset;

		/// <summary>
		/// Gets the number of elements available in the current buffer
		/// </summary>
		public int Capacity => _CurrentBuffer == null ? 0 : _CurrentBuffer.Length;

		/// <summary>
		/// Gets the number of elements available until a new buffer must be rented
		/// </summary>
		public int FreeCapacity => _CurrentBuffer == null ? 0 : _CurrentBuffer.Length - _CurrentOffset;

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
