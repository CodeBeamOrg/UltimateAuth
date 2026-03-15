using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class TenantKeyJsonConverter : JsonConverter<TenantKey>
{
    public override TenantKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("TenantKey must be a string.");

        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("TenantKey cannot be null or empty.");

        // IMPORTANT:
        // JSON transport = internal framework boundary
        // Do NOT use FromExternal here.
        return TenantKey.FromInternal(value);
    }

    public override void Write(Utf8JsonWriter writer, TenantKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
