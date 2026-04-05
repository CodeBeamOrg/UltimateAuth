using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class ProfileKeyJsonConverter : JsonConverter<ProfileKey>
{
    public override ProfileKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("ProfileKey must be a string.");

        var value = reader.GetString();

        if (!ProfileKey.TryCreate(value, out var key))
            throw new JsonException($"Invalid ProfileKey value: '{value}'");

        return key;
    }

    public override void Write(Utf8JsonWriter writer, ProfileKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
