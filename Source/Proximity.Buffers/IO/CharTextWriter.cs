using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0060 // Remove unused parameter

namespace System.IO
{
	/// <summary>
	/// Implements a TextWriter that writes to an <see cref="IBufferWriter{Char}"/>
	/// </summary>
	public sealed class CharTextWriter : TextWriter
	{ //****************************************
		private const int DefaultBufferSize = 1024;
		//****************************************
		private readonly IBufferWriter<char> _Writer;

		private readonly int _MinBufferSize, _MaxBufferSize;

		private int _PendingChars;
		private Memory<char> _PendingBuffer;
		//****************************************

		/// <summary>
		/// Creates a new Buffer Writer
		/// </summary>
		/// <param name="writer">The writer to write characters to</param>
		/// <param name="maxBufferSize">The maximum buffer size to request from the Text Writer</param>
		public CharTextWriter(IBufferWriter<char> writer, int maxBufferSize)
		{
			if (maxBufferSize < 1)
				throw new ArgumentOutOfRangeException(nameof(maxBufferSize));

			_Writer = writer ?? throw new ArgumentNullException(nameof(writer));
			_MaxBufferSize = maxBufferSize;
			_MinBufferSize = Math.Min(DefaultBufferSize, maxBufferSize);
		}

		/// <summary>
		/// Creates a new Buffer Writer
		/// </summary>
		/// <param name="writer">The writer to write characters to</param>
		public CharTextWriter(IBufferWriter<char> writer) : this(writer, DefaultBufferSize)
		{
		}

		//****************************************

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				Flush();
		}

		/// <inheritdoc />
		public override void Flush()
		{
			if (_PendingChars > 0)
			{
				_Writer.Advance(_PendingChars);

				_PendingChars = 0;
				_PendingBuffer = default;
			}
		}

		/// <inheritdoc />
		public override Task FlushAsync()
		{
			Flush();

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override void Write(char value)
		{
			EnsureMinimum(1);

			_PendingBuffer.Span[_PendingChars++] = value;
		}

		/// <inheritdoc />
		public override void Write(char[]? buffer) => Write(buffer.AsSpan(), false);

		/// <inheritdoc />
		public override void Write(char[] buffer, int index, int count) => Write(buffer.AsSpan(index, count), false);

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void Write(ReadOnlySpan<char> buffer) => Write(buffer, false);

		/// <inheritdoc />
		public override void Write(string? value) => Write(value.AsSpan());

		/// <inheritdoc />
		public override Task WriteAsync(char value)
		{
			Write(value);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteAsync(char[] buffer, int index, int count) => WriteAsync(buffer.AsMemory(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
		{
			Write(buffer.Span, false);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteAsync(string? value) => WriteAsync(value.AsMemory());

		/// <inheritdoc />
		public override void WriteLine()
		{
			var Length = CoreNewLine.Length;

			EnsureMinimum(Length);

			CoreNewLine.AsSpan().CopyTo(_PendingBuffer.Span.Slice(_PendingChars));

			_PendingChars += Length;
		}

		/// <inheritdoc />
		public override void WriteLine(char value)
		{
			var Length = CoreNewLine.Length + 1;
			EnsureMinimum(Length);

			_PendingBuffer.Span[_PendingChars] = value;
			CoreNewLine.AsSpan().CopyTo(_PendingBuffer.Span.Slice(_PendingChars + 1));

			_PendingChars += Length;
		}

		/// <inheritdoc />
		public override void WriteLine(char[]? buffer) => Write(buffer.AsSpan(), true);

		/// <inheritdoc />
		public override void WriteLine(char[] buffer, int index, int count) => Write(buffer.AsSpan(index, count), true);

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void WriteLine(ReadOnlySpan<char> buffer) => Write(buffer, true);

		/// <inheritdoc />
		public override void WriteLine(string? value) => Write(value.AsSpan(), true);

		/// <inheritdoc />
		public override Task WriteLineAsync()
		{
			WriteLine();

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(char value)
		{
			WriteLine(value);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(char[] buffer, int index, int count) => WriteLineAsync(buffer.AsMemory(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
		{
			Write(buffer.Span, true);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(string? value) => WriteLineAsync(value.AsMemory());

		//****************************************

		private void EnsureMinimum(int hint)
		{
			hint = Math.Min(_MaxBufferSize, hint);

			if (_PendingChars + hint >= _PendingBuffer.Length)
			{
				if (_PendingChars > 0)
				{
					_Writer.Advance(_PendingChars);
					_PendingChars = 0;
				}

				hint = Math.Max(_MinBufferSize, hint);

				_PendingBuffer = _Writer.GetMemory(hint);
			}
		}

		private void Write(ReadOnlySpan<char> buffer, bool newLine)
		{
			while (!buffer.IsEmpty)
			{
				EnsureMinimum(buffer.Length);

				var Output = _PendingBuffer.Span.Slice(_PendingChars);
				var Length = Math.Min(Output.Length, buffer.Length);

				buffer.Slice(0, Length).CopyTo(Output);
				buffer = buffer.Slice(Length);

				_PendingChars += Length;
			}

			if (newLine)
				WriteLine();
		}

		//****************************************

		/// <summary>
		/// Gets the encoding used by this Char Text Writer
		/// </summary>
		/// <remarks>Since there is no encoding peformed, always returns <see cref="Encoding.Default"/></remarks>
		public override Encoding Encoding => Encoding.Default;
	}
}
