using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

/// <summary>
/// A string json converter that could read values of string,number or boolean.
/// </summary>
internal sealed class HighPerfJsonStringConverter : JsonConverter<string>
{
    public static HighPerfJsonStringConverter Instance { get; } = new HighPerfJsonStringConverter();
    private HighPerfJsonStringConverter() { }

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.True => reader.GetBoolean().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.False => reader.GetBoolean().ToString(CultureInfo.InvariantCulture),
            _ => throw new JsonException("Do not support convert value to string."),
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
