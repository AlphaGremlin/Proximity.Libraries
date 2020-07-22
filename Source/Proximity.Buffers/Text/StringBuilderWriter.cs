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
		//****************************************

		/// <summary>
		/// Creates a new String Builder Writer targeting a new <see cref="StringBuilder"/>
		/// </summary>
		public StringBuilderWriter() : this(new StringBuilder())
		{
		}

		/// <summary>
		/// Creates a new String Builder Writer targeting a new <see cref="StringBuilder"/> with the given starting capacity
		/// </summary>
		/// <param name="capacity">The initial capacity of the <see cref="StringBuilder"/></param>
		public StringBuilderWriter(int capacity) : this(new StringBuilder(capacity))
		{
		}

		/// <summary>
		/// Creates a new String Builder Writer targeting a specific <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="builder">The <see cref="StringBuilder"/> to write to</param>
		public StringBuilderWriter(StringBuilder builder)
		{
			Builder = builder ?? throw new ArgumentNullException(nameof(builder));

			_Buffer = Array.Empty<char>();
		}

		//****************************************

		void IDisposable.Dispose() => Reset(false);

		/// <summary>
		/// Resets the String Builder Writer, returning its buffers to the pool
		/// </summary>
		/// <remarks>The String Builder Writer can be reused after disposal</remarks>
		public void Reset() => Reset(false);

		/// <summary>
		/// Resets the String Builder Writer, returning its buffer to the pool
		/// </summary>
		/// <remarks>The String Builder Writer can be reused after disposal</remarks>
		public void Reset(bool clearBuffer)
		{
			if (_Buffer.Length > 0)
			{
				ArrayPool<char>.Shared.Return(_Buffer, clearBuffer);

				_Buffer = Array.Empty<char>();
			}

			Builder.Clear();
		}

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
			if (_Buffer.Length < sizeHint)
			{
				if (_Buffer.Length > 0)
				{
					ArrayPool<char>.Shared.Return(_Buffer, false);
				}

				_Buffer = ArrayPool<char>.Shared.Rent(sizeHint);
			}

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

		/// <summary>
		/// Gets the String Builder being written to
		/// </summary>
		/// <remarks>It is safe to call all methods on the String Builder during writes.</remarks>
		public StringBuilder Builder { get; }
	}
}
