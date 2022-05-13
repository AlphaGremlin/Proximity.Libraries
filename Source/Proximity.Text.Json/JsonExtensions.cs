using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace System.Text.Json
{
	public static class JsonExtensions
	{
		public static void Deconstruct(this in JsonProperty property, out string name, out JsonElement value)
		{
			name = property.Name;
			value = property.Value;
		}

		public static void SkipToEnd(this ref Utf8JsonReader reader)
		{
			if (!reader.IsFinalBlock)
				throw new InvalidOperationException("Cannot Skip to End on a non-final block");

			if (reader.CurrentDepth == 0)
				throw new InvalidOperationException("Not inside an object or array");

			while (reader.TokenType != JsonTokenType.EndObject && reader.TokenType != JsonTokenType.EndArray)
			{
				reader.Skip();
				reader.Read();
			}
		}

		public static void WriteBoolean(this Utf8JsonWriter writer, string name, bool? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteBoolean(name, number.Value);
		}

		public static void WriteBoolean(this Utf8JsonWriter writer, JsonEncodedText name, bool? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteBoolean(name, number.Value);
		}

		public static void WriteBoolean(this Utf8JsonWriter writer, ReadOnlySpan<char> name, bool? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteBoolean(name, number.Value);
		}

		public static void WriteBooleanValue(this Utf8JsonWriter writer, bool? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteBooleanValue(number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, string name, decimal? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, string name, double? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, string name, float? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, string name, int? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, string name, long? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, string name, uint? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, string name, ulong? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, JsonEncodedText name, decimal? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, JsonEncodedText name, double? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, JsonEncodedText name, float? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, JsonEncodedText name, int? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, JsonEncodedText name, long? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, JsonEncodedText name, uint? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, JsonEncodedText name, ulong? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, ReadOnlySpan<char> name, decimal? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, ReadOnlySpan<char> name, double? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, ReadOnlySpan<char> name, float? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, ReadOnlySpan<char> name, int? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, ReadOnlySpan<char> name, long? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, ReadOnlySpan<char> name, uint? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumber(this Utf8JsonWriter writer, ReadOnlySpan<char> name, ulong? number)
		{
			if (number == null)
				writer.WriteNull(name);
			else
				writer.WriteNumber(name, number.Value);
		}

		public static void WriteNumberValue(this Utf8JsonWriter writer, decimal? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteNumberValue(number.Value);
		}

		public static void WriteNumberValue(this Utf8JsonWriter writer, double? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteNumberValue(number.Value);
		}

		public static void WriteNumberValue(this Utf8JsonWriter writer, float? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteNumberValue(number.Value);
		}

		public static void WriteNumberValue(this Utf8JsonWriter writer, int? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteNumberValue(number.Value);
		}

		public static void WriteNumberValue(this Utf8JsonWriter writer, long? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteNumberValue(number.Value);
		}

		public static void WriteNumberValue(this Utf8JsonWriter writer, uint? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteNumberValue(number.Value);
		}

		public static void WriteNumberValue(this Utf8JsonWriter writer, ulong? number)
		{
			if (number == null)
				writer.WriteNullValue();
			else
				writer.WriteNumberValue(number.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, string name, Guid? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, string name, DateTime? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, string name, DateTimeOffset? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, JsonEncodedText name, Guid? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, JsonEncodedText name, DateTime? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, JsonEncodedText name, DateTimeOffset? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, ReadOnlySpan<char> name, Guid? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, ReadOnlySpan<char> name, DateTime? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteString(this Utf8JsonWriter writer, ReadOnlySpan<char> name, DateTimeOffset? value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value.Value);
		}

		public static void WriteStringValue(this Utf8JsonWriter writer, Guid? value)
		{
			if (value == null)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.Value);
		}

		public static void WriteStringValue(this Utf8JsonWriter writer, DateTime? value)
		{
			if (value == null)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.Value);
		}

		public static void WriteStringValue(this Utf8JsonWriter writer, DateTimeOffset? value)
		{
			if (value == null)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.Value);
		}

		public static void WriteTo(this ref Utf8JsonReader reader, Utf8JsonWriter writer)
		{
			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				writer.WritePropertyName(reader.GetString()!);
				reader.Read();
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

				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
					reader.WriteTo(writer);

				writer.WriteEndArray();
				break;

			case JsonTokenType.StartObject:
				writer.WriteStartObject();

				while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
					reader.WriteTo(writer);

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
	}
}
