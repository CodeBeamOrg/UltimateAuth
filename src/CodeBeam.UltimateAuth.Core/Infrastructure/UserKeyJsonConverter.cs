using CodeBeam.UltimateAuth.Core.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class UserKeyJsonConverter : JsonConverter<UserKey>
{
    public override UserKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("UserKey must be a string.");

        return UserKey.FromString(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, UserKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
