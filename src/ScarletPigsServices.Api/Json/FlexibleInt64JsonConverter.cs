using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScarletPigsServices.Api.Json;

/// <summary>
/// Reads a 64-bit integer from either a JSON number or a JSON string, and always writes a number.
/// Applied per property rather than registered globally, so existing endpoints keep strict binding.
/// </summary>
public sealed class FlexibleInt64JsonConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadValue(ref reader);

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);

    internal static long ReadValue(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt64();

            case JsonTokenType.String:
                var text = reader.GetString();
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }

                throw new JsonException($"'{text}' is not a valid 64-bit integer.");

            default:
                throw new JsonException($"Expected a number or a numeric string but found {reader.TokenType}.");
        }
    }
}

/// <summary>
/// Reads an array of 64-bit integers where each element may be a number or a numeric string.
/// </summary>
public sealed class FlexibleInt64ListJsonConverter : JsonConverter<IReadOnlyList<long>>
{
    public override IReadOnlyList<long> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return [];
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected an array but found {reader.TokenType}.");
        }

        var values = new List<long>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            values.Add(FlexibleInt64JsonConverter.ReadValue(ref reader));
        }

        return values;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<long> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteNumberValue(item);
        }

        writer.WriteEndArray();
    }
}
