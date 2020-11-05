using System;
using System.Collections.Generic;
using System.Text;

namespace System.Buffers
{
	/// <summary>
	/// Provides some extensions for Buffer Writer
	/// </summary>
	public static class BufferWriterExtensions
	{
		/// <summary>
		/// Writes a ReadOnlySequence to the Buffer Writer
		/// </summary>
		/// <typeparam name="T">The type of element</typeparam>
		/// <param name="writer">The writer to write to</param>
		/// <param name="value">The ReadOnlySequence to read from</param>
		public static void Write<T>(this IBufferWriter<T> writer, ReadOnlySequence<T> value)
		{
			foreach (var Segment in value)
				writer.Write(Segment.Span);
		}
	}
}
