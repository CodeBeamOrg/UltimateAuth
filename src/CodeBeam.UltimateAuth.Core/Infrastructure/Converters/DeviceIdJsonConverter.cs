using CodeBeam.UltimateAuth.Core.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class DeviceIdJsonConverter : JsonConverter<DeviceId>
{
    public override DeviceId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (!DeviceId.TryCreate(value, out var id))
            throw new JsonException("Invalid DeviceId");

        return id;
    }

    public override void Write(Utf8JsonWriter writer, DeviceId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
