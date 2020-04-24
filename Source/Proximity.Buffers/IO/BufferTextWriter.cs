using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
	/// <summary>
	/// Implements a TextWriter that writes to an <see cref="IBufferWriter{Char}"/>
	/// </summary>
	public sealed class BufferTextWriter : TextWriter
	{ //****************************************
		private const int DefaultBufferSize = 1024;
		//****************************************
		private readonly IBufferWriter<char> _Writer;

		private readonly int _MaxBufferSize;
		//****************************************

		/// <summary>
		/// Creates a new Buffer Writer
		/// </summary>
		public BufferTextWriter(IBufferWriter<char> writer, int maxBufferSize)
		{
			_Writer = writer;
			_MaxBufferSize = maxBufferSize;
		}

		/// <summary>
		/// Creates a new Buffer Writer
		/// </summary>
		public BufferTextWriter(IBufferWriter<char> writer) : this(writer, DefaultBufferSize)
		{
		}

		//****************************************

		/// <inheritdoc />
		public override void Flush()
		{
		}

		/// <inheritdoc />
		public override Task FlushAsync() => Task.CompletedTask;

		/// <inheritdoc />
		public override void Write(char value)
		{
			var Output = _Writer.GetSpan(1);

			Output[0] = value;

			_Writer.Advance(1);
		}

		/// <inheritdoc />
		public override void Write(char[] buffer) => Write(buffer.AsSpan());

		/// <inheritdoc />
		public override void Write(char[] buffer, int index, int count) => Write(buffer.AsSpan(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void Write(ReadOnlySpan<char> buffer)
		{
			while (!buffer.IsEmpty)
			{
				var Output = _Writer.GetSpan(_MaxBufferSize > 0 ? Math.Min(_MaxBufferSize, buffer.Length) : buffer.Length);

				var Length = Math.Min(Output.Length, buffer.Length);

				buffer.Slice(0, Length).CopyTo(Output);

				_Writer.Advance(Length);

				buffer = buffer.Slice(Length);
			}
		}

		/// <inheritdoc />
		public override void Write(string value) => Write(value.AsSpan());

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
			Write(buffer.Span);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteAsync(string value) => WriteAsync(value.AsMemory());

		/// <inheritdoc />
		public override void WriteLine(char value)
		{
			Write(value);
			Write(CoreNewLine);
		}

		/// <inheritdoc />
		public override void WriteLine(char[] buffer) => WriteLine(buffer.AsSpan());

		/// <inheritdoc />
		public override void WriteLine(char[] buffer, int index, int count) => WriteLine(buffer.AsSpan(index, count));

		/// <inheritdoc />
		public
#if !NETSTANDARD2_0
			override
#endif
			void WriteLine(ReadOnlySpan<char> buffer)
		{
			Write(buffer);
			Write(CoreNewLine);
		}

		/// <inheritdoc />
		public override void WriteLine(string value) => WriteLine(value.AsSpan());

		/// <inheritdoc />
		public override Task WriteLineAsync()
		{
			Write(CoreNewLine);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(char value)
		{
			Write(value);
			Write(CoreNewLine);

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
			Write(buffer);
			Write(CoreNewLine);

			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task WriteLineAsync(string value) => WriteLineAsync(value.AsMemory());

		//****************************************

		/// <summary>
		/// Gets the encoding used by this Char Text Writer
		/// </summary>
		/// <remarks>Null, since there is no encoding peformed</remarks>
		public override Encoding? Encoding => null!;
	}
}
