using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RVM.TcgDex.Serialization;

/// <summary>
/// Fields the API sends either as string or as number (<c>localId</c>, <c>level</c>, <c>damage</c>:
/// <c>"40"</c> on one card, <c>90</c> on another). Read as the literal text, so <c>"001"</c> stays
/// <c>001</c>.
/// </summary>
internal sealed class FlexibleStringConverter : JsonConverter<string>
{
    public override bool HandleNull => false;

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray()),
            _ => throw new JsonException($"Expected string or number, got {reader.TokenType}."),
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
