using CodeBeam.UltimateAuth.Core.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class AuthSessionIdJsonConverter : JsonConverter<AuthSessionId>
{
    public override AuthSessionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("AuthSessionId must be a string.");

        var value = reader.GetString();

        if (!AuthSessionId.TryCreate(value, out var id))
            throw new JsonException($"Invalid AuthSessionId value: '{value}'");

        return id;
    }

    public override void Write(Utf8JsonWriter writer, AuthSessionId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
