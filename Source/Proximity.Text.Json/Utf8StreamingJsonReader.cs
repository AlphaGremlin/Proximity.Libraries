using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Proximity.Text.Json
{
	/// <summary>
	/// Provides a JSON reader utility able to consume JSON in a streaming fashion
	/// </summary>
	public sealed class Utf8StreamingJsonReader : IDisposable
	{ //****************************************
		private readonly IBufferReader<byte> _Reader;
		private readonly bool _Dispose;

		// The buffer currently supplied by the Reader
		private ReadOnlyMemory<byte> _LastBuffer;

		// The segments describing any leftover data from previous reads
		private LeftoverSegment? _LeftoverHead, _LeftoverTail;
		private bool _HasExtraTail;
		//****************************************

		private Utf8StreamingJsonReader(IBufferReader<byte> reader, bool dispose)
		{
			_Reader = reader;
			_Dispose = dispose;
		}

		//****************************************

		public static void Test(System.IO.Stream input)
		{
			using var Source = new System.IO.StreamBufferReader(input);
			using var Stream = Utf8StreamingJsonReader.From(Source, out var Reader);

			for (Stream.Read(ref Reader); Reader.TokenType != JsonTokenType.EndObject; Stream.Read(ref Reader))
			{
				// ...
			}


		}

		/// <summary>
		/// Cleans up any buffers rented for this reader
		/// </summary>
		public void Dispose()
		{
			ReadOnlySequenceSegment<byte>? Segment = _LeftoverHead;

			while (Segment != null)
			{
				if (MemoryMarshal.TryGetArray(Segment.Memory, out var MyBuffer))
					ArrayPool<byte>.Shared.Return(MyBuffer.Array!);

				if (_HasExtraTail && Segment == _LeftoverTail)
					Segment = null;
				else
					Segment = Segment.Next;
			}

			_LeftoverHead = null;
			_LeftoverTail = null;
			_HasExtraTail = false;
			_LastBuffer = default;

			if (_Dispose)
				(_Reader as IDisposable)?.Dispose();
		}

		/// <summary>
		/// Reads the current token, ensuring enough data is pulled from the underlying reader
		/// </summary>
		/// <param name="reader">A reference to the UTF-8 JSON Reader used to read JSON</param>
		public void Read(ref Utf8JsonReader reader)
		{
			while (!reader.Read())
			{
				// Insufficient data in the buffer for this token, pull more from the Reader
				var Consumed = (int)reader.BytesConsumed;

				if (Consumed > 0)
				{
					var RemovedLength = 0;

					// Trim what leftover data we can
					if (_LeftoverHead != null)
					{
						// Remove segments and decrease Consumed until we run out of segments, or run out of Consumed bytes
						while (_LeftoverHead.Memory.Length < Consumed)
						{
							var OldLength = _LeftoverHead.Memory.Length;

							Consumed -= OldLength;
							RemovedLength += OldLength;

							if (MemoryMarshal.TryGetArray(_LeftoverHead.Memory, out var LeftoverBuffer))
								ArrayPool<byte>.Shared.Return(LeftoverBuffer.Array!);

							if (_HasExtraTail && _LeftoverHead == _LeftoverTail)
								_LeftoverHead = null;
							else 
								_LeftoverHead = (LeftoverSegment?)_LeftoverHead.Next;

							if (_LeftoverHead == null)
								break;
						}

						var NextSegment = _LeftoverHead;

						if (NextSegment != null)
						{
							// Correct the remainder on the header
							NextSegment.CorrectMemory(Consumed);
							NextSegment.CorrectRunningIndex(RemovedLength);
							NextSegment = (LeftoverSegment?)NextSegment.Next;

							// Correct the Running Index on subsequent entries
							while (NextSegment != null)
							{
								NextSegment.CorrectRunningIndex(Consumed + RemovedLength);

								if (_HasExtraTail && NextSegment == _LeftoverTail)
									NextSegment = null;
								else
									NextSegment = (LeftoverSegment?)NextSegment.Next;
							}

							Consumed = 0;
						}
						else
						{
							// We removed all saved segments, leaving only the contents of the current buffer
							_LeftoverTail = null;
							_HasExtraTail = false;
						}
					}
				}

				// Have we consumed all of the supplied buffer?
				if (Consumed == _LastBuffer.Length)
				{
					// No need to pass anything on to the next reader
					_Reader.Advance(_LastBuffer.Length);

					_LastBuffer = _Reader.GetMemory(0);
					_HasExtraTail = false;

					// If this is the final block, this will probably fail on the next read, since the consumer called Read after the final token
					reader = new Utf8JsonReader(_LastBuffer.Span, _LastBuffer.IsEmpty, reader.CurrentState);
				}
				else
				{
					// Before we advance, capture anything leftover and record a segment
					var Remainder = _LastBuffer.Length - Consumed;

					var Buffer = ArrayPool<byte>.Shared.Rent(Remainder);

					_LastBuffer.Slice(Consumed).CopyTo(Buffer);

					if (_LeftoverTail == null)
					{
						// No start segment, so create it
						_LeftoverTail = _LeftoverHead = new LeftoverSegment(Buffer.AsMemory(0, Remainder));
					}
					else
					{
						// We have an existing head segment, chain a new segment to it
						_LeftoverTail = new LeftoverSegment(_LeftoverTail, Buffer.AsMemory(0, Remainder));
					}

					// Now it's safe to advance and grab the next buffer
					_Reader.Advance(Consumed);

					_LastBuffer = _Reader.GetMemory(0);

					if (_LastBuffer.IsEmpty)
					{
						// No more data, finalise the reader
						reader = new Utf8JsonReader(new ReadOnlySequence<byte>(_LeftoverHead!, 0, _LeftoverTail, _LeftoverTail.Memory.Length), true, reader.CurrentState);

						_HasExtraTail = false;

						// The next read (when we hit the end of the while and loop back) will throw if the JSON is truncated
					}
					else
					{
						// More data, continue the reader
						// Create a temp segment representing any previous data, along with our new buffer
						var TempSegment = new LeftoverSegment(_LeftoverTail, _LastBuffer);

						_HasExtraTail = true;

						reader = new Utf8JsonReader(new ReadOnlySequence<byte>(_LeftoverHead!, 0, TempSegment, TempSegment.Memory.Length), false, reader.CurrentState);
					}
				}
			}
		}

		/// <summary>
		/// Skips the current token, ensuring enough data is skipped from the underlying reader
		/// </summary>
		/// <param name="reader">A reference to the UTF-8 JSON Reader used to read JSON</param>
		public void Skip(ref Utf8JsonReader reader)
		{
			if (reader.TokenType == JsonTokenType.PropertyName)
				Read(ref reader);

			if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
			{
				var StartDepth = reader.CurrentDepth;

				do
				{
					Read(ref reader);
				}
				while (StartDepth < reader.CurrentDepth);
			}
		}

		/// <summary>
		/// Skips to the end of the current depth
		/// </summary>
		/// <param name="reader">A reference to the UTF-8 JSON Reader used to read JSON</param>
		/// <remarks>If inside an Object or Array, reads until the end of that array, leaving the reader positioned on the EndObject/EndArray token</remarks>
		public void SkipToEnd(ref Utf8JsonReader reader)
		{
			if (reader.CurrentDepth == 0)
				throw new InvalidOperationException("Not inside an object or array");

			while (reader.TokenType != JsonTokenType.EndObject && reader.TokenType != JsonTokenType.EndArray)
			{
				Skip(ref reader);
				Read(ref reader);
			}
		}

		/// <summary>
		/// Writes the current value to the given writer
		/// </summary>
		/// <param name="reader">The underlying reader this stream is managing</param>
		/// <param name="writer">The writer to write the value to</param>
		/// <remarks>If this value is a PropertyName, writes the property name and the following value. If the value is StartObject or StartArray, writes all child tokens.</remarks>
		public void WriteTo(ref Utf8JsonReader reader, Utf8JsonWriter writer)
		{
			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				writer.WritePropertyName(reader.GetString()!);
				Read(ref reader);
			}

			switch (reader.TokenType)
			{
			case JsonTokenType.Comment:
				writer.WriteCommentValue(reader.GetString()!);
				break;

			case JsonTokenType.False:
				writer.WriteBooleanValue(false);
				break;

			case JsonTokenType.Null:
				writer.WriteNullValue();
				break;

			case JsonTokenType.Number:
				if (reader.TryGetDecimal(out var DecimalNumber))
					writer.WriteNumberValue(DecimalNumber);
				else if (reader.TryGetDouble(out var DoubleNumber))
					writer.WriteNumberValue(DoubleNumber);
				else
					throw new JsonException("Failed to parse number");
				break;

			case JsonTokenType.StartArray:
				writer.WriteStartArray();

				for (Read(ref reader); reader.TokenType != JsonTokenType.EndArray; Read(ref reader))
					WriteTo(ref reader, writer);

				writer.WriteEndArray();
				break;

			case JsonTokenType.StartObject:
				writer.WriteStartObject();

				for (Read(ref reader); reader.TokenType != JsonTokenType.EndObject; Read(ref reader))
					WriteTo(ref reader, writer);

				writer.WriteEndObject();
				break;

			case JsonTokenType.String:
				writer.WriteStringValue(reader.GetString());
				break;

			case JsonTokenType.True:
				writer.WriteBooleanValue(true);
				break;

			default:
				throw new JsonException("Unexpected token type");
			}
		}

		//****************************************

		private Utf8JsonReader GetInitialReader(JsonReaderOptions options)
		{
			_LastBuffer = _Reader.GetMemory(0);

			var Reader = new Utf8JsonReader(new ReadOnlySequence<byte>(_LastBuffer), false, new JsonReaderState(options));

			return Reader;
		}

		//****************************************

		public static Utf8StreamingJsonReader From(byte[] buffer, out Utf8JsonReader jsonReader) => From(new BufferReader<byte>(buffer), true, default, out jsonReader);

		public static Utf8StreamingJsonReader From(byte[] buffer, JsonReaderOptions options, out Utf8JsonReader jsonReader) => From(new BufferReader<byte>(buffer), true, options, out jsonReader);

		public static Utf8StreamingJsonReader From(ReadOnlyMemory<byte> buffer, out Utf8JsonReader jsonReader) => From(new BufferReader<byte>(buffer), true, default, out jsonReader);

		public static Utf8StreamingJsonReader From(ReadOnlyMemory<byte> buffer, JsonReaderOptions options, out Utf8JsonReader jsonReader) => From(new BufferReader<byte>(buffer), true, options, out jsonReader);

		public static Utf8StreamingJsonReader From(ReadOnlySequence<byte> buffer, out Utf8JsonReader jsonReader) => From(new BufferReader<byte>(buffer), true, default, out jsonReader);

		public static Utf8StreamingJsonReader From(ReadOnlySequence<byte> buffer, JsonReaderOptions options, out Utf8JsonReader jsonReader) => From(new BufferReader<byte>(buffer), true, options, out jsonReader);

		public static Utf8StreamingJsonReader From(IBufferReader<byte> reader, out Utf8JsonReader jsonReader) => From(reader, false, default, out jsonReader);

		public static Utf8StreamingJsonReader From(IBufferReader<byte> reader, JsonReaderOptions options, out Utf8JsonReader jsonReader) => From(reader, false, options, out jsonReader);

		private static Utf8StreamingJsonReader From(IBufferReader<byte> reader, bool dispose, JsonReaderOptions options, out Utf8JsonReader jsonReader)
		{
			var Reader = new Utf8StreamingJsonReader(reader, dispose);

			jsonReader = Reader.GetInitialReader(options);

			return Reader;
		}

		//****************************************

		private sealed class LeftoverSegment : ReadOnlySequenceSegment<byte>
		{
			internal LeftoverSegment(ReadOnlyMemory<byte> memory)
			{
				Memory = memory;
			}

			internal LeftoverSegment(LeftoverSegment previous, ReadOnlyMemory<byte> memory)
			{
				Memory = memory;
				RunningIndex = previous.RunningIndex + previous.Memory.Length;
				previous.Next = this;
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
