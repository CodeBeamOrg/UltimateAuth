using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core;

public sealed class PasswordHashJsonConverter : JsonConverter<PasswordHash>
{
    public override PasswordHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("PasswordHash must be a string.");

        var value = reader.GetString();

        if (!PasswordHash.TryParse(value, null, out var result))
            throw new JsonException($"Invalid PasswordHash: '{value}'");

        return result;
    }

    public override void Write(Utf8JsonWriter writer, PasswordHash value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
