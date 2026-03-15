using CodeBeam.UltimateAuth.Core.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class SessionRootIdJsonConverter : JsonConverter<SessionRootId>
{
    public override SessionRootId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("SessionChainId must be a string.");

        var raw = reader.GetString();

        if (!SessionRootId.TryCreate(raw!, out var id))
            throw new JsonException($"Invalid SessionChainId value: '{raw}'");

        return id;
    }

    public override void Write(Utf8JsonWriter writer, SessionRootId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString("N"));
    }
}
