using System;
using System.Buffers;
using System.Collections.Generic;

namespace System.Text
{
	/// <summary>
	/// Provides an <see cref="IBufferWriter{Char}"/> that writes to a <see cref="StringBuilder"/>
	/// </summary>
	public sealed class StringBuilderWriter : IBufferWriter<char>, IDisposable
	{ //****************************************
		private char[] _Buffer;

		private readonly bool _ClearOnDispose, _ClearBuffers;
		//****************************************

		/// <summary>
		/// Creates a new String Builder Writer targeting a new <see cref="StringBuilder"/>
		/// </summary>
		public StringBuilderWriter() : this(new StringBuilder(), false, false)
		{
		}

		/// <summary>
		/// Creates a new String Builder Writer targeting a new <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="clearBuffers">True to clear buffers before returning them to the pool</param>
		public StringBuilderWriter(bool clearBuffers) : this(new StringBuilder(), clearBuffers, false)
		{
		}

		/// <summary>
		/// Creates a new String Builder Writer targeting a new <see cref="StringBuilder"/> with the given starting capacity
		/// </summary>
		/// <param name="capacity">The initial capacity of the <see cref="StringBuilder"/></param>
		public StringBuilderWriter(int capacity) : this(new StringBuilder(capacity), false, false)
		{
		}

		/// <summary>
		/// Creates a new String Builder Writer targeting a new <see cref="StringBuilder"/> with the given starting capacity
		/// </summary>
		/// <param name="capacity">The initial capacity of the <see cref="StringBuilder"/></param>
		/// <param name="clearBuffers">True to clear buffers before returning them to the pool</param>
		public StringBuilderWriter(int capacity, bool clearBuffers) : this(new StringBuilder(capacity), clearBuffers, false)
		{
		}

		/// <summary>
		/// Creates a new String Builder Writer targeting a specific <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="builder">The <see cref="StringBuilder"/> to write to</param>
		public StringBuilderWriter(StringBuilder builder) : this(builder, false, false)
		{
		}

		/// <summary>
		/// Creates a new String Builder Writer targeting a specific <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="builder">The <see cref="StringBuilder"/> to write to</param>
		/// <param name="clearBuffers">True to clear buffers before returning them to the pool</param>
		/// <param name="clearOnDispose">True to clear the StringBuilder when disposed</param>
		public StringBuilderWriter(StringBuilder builder, bool clearBuffers, bool clearOnDispose)
		{
			Builder = builder ?? throw new ArgumentNullException(nameof(builder));

			_Buffer = Array.Empty<char>();
			_ClearBuffers = clearBuffers;
			_ClearOnDispose = clearOnDispose;
		}

		//****************************************

		void IDisposable.Dispose() => Reset(_ClearOnDispose);

		/// <summary>
		/// Resets the String Builder Writer, returning its buffers to the pool
		/// </summary>
		/// <remarks>The String Builder Writer can be reused after disposal</remarks>
		public void Reset() => Reset(true);

		/// <summary>
		/// Advances the Buffer Writer and writes to the underlying String Builder
		/// </summary>
		/// <param name="count">The number of characters written to the current buffer</param>
		public void Advance(int count)
		{
			if (count == 0)
				return;

			if (count > _Buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			Builder.Append(_Buffer.AsSpan(0, count));
		}

		/// <summary>
		/// Gets a Memory to write to
		/// </summary>
		/// <param name="sizeHint">A hint as to the number of characters desired</param>
		/// <returns>A Memory that can be written to</returns>
		public Memory<char> GetMemory(int sizeHint)
		{
			ArrayPool<char>.Shared.Resize(ref _Buffer, sizeHint, false, _ClearBuffers);

			return _Buffer;
		}

		/// <summary>
		/// Gets a Span to write to
		/// </summary>
		/// <param name="sizeHint">A hint as to the number of characters desired</param>
		/// <returns>A span that can be written to</returns>
		public Span<char> GetSpan(int sizeHint) => GetMemory(sizeHint).Span;

		/// <summary>
		/// Returns the data currently written to the Buffer Writer
		/// </summary>
		/// <returns>The content of the String Builder</returns>
		public override string ToString() => Builder.ToString();

		//****************************************

		private void Reset(bool clearBuilder)
		{
			if (_Buffer.Length > 0)
			{
				ArrayPool<char>.Shared.Return(_Buffer, _ClearBuffers);

				_Buffer = Array.Empty<char>();
			}

			if (clearBuilder)
				Builder.Clear();
		}

		//****************************************

		/// <summary>
		/// Gets the String Builder being written to
		/// </summary>
		/// <remarks>It is safe to call all methods on the String Builder during writes.</remarks>
		public StringBuilder Builder { get; }
	}
}
