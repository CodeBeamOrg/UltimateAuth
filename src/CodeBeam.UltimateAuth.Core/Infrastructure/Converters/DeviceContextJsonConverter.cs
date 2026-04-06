using System.Text.Json;
using System.Text.Json.Serialization;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class DeviceContextJsonConverter : JsonConverter<DeviceContext>
{
    public override DeviceContext Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("DeviceContext must be an object.");

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        DeviceId? deviceId = null;
        if (root.TryGetProperty("deviceId", out var deviceIdProp))
        {
            var raw = deviceIdProp.GetString();

            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (!DeviceId.TryCreate(raw, out var parsed))
                    throw new JsonException("Invalid DeviceId");

                deviceId = parsed;
            }
        }

        string? deviceType = root.TryGetProperty("deviceType", out var dt) ? dt.GetString() : null;
        string? platform = root.TryGetProperty("platform", out var pf) ? pf.GetString() : null;
        string? os = root.TryGetProperty("operatingSystem", out var osProp) ? osProp.GetString() : null;
        string? browser = root.TryGetProperty("browser", out var br) ? br.GetString() : null;
        string? ip = root.TryGetProperty("ipAddress", out var ipProp) ? ipProp.GetString() : null;

        if (deviceId is not DeviceId resolvedDeviceId)
            return DeviceContext.Anonymous();

        return DeviceContext.Create(
            resolvedDeviceId,
            deviceType,
            platform,
            os,
            browser,
            ip);
    }

    public override void Write(Utf8JsonWriter writer, DeviceContext value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.DeviceId is not null)
            writer.WriteString("deviceId", (string)value.DeviceId);

        writer.WriteString("deviceType", value.DeviceType);
        writer.WriteString("platform", value.Platform);
        writer.WriteString("operatingSystem", value.OperatingSystem);
        writer.WriteString("browser", value.Browser);
        writer.WriteString("ipAddress", value.IpAddress);

        writer.WriteEndObject();
    }
}