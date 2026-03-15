using CodeBeam.UltimateAuth.Core.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class TokenIdJsonConverter : JsonConverter<TokenId>
{
    public override TokenId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("TokenId must be a string.");

        var raw = reader.GetString();

        if (!TokenId.TryCreate(raw!, out var id))
            throw new JsonException($"Invalid TokenId value: '{raw}'");

        return id;
    }

    public override void Write(Utf8JsonWriter writer, TokenId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString("N"));
    }
}
