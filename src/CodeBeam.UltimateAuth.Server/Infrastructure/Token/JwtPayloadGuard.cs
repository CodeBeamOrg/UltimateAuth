using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal static class JwtPayloadGuard
{
    private static readonly HashSet<string> ReservedClaims =
        new(StringComparer.Ordinal)
        {
            "sub",
            "tenant",
            "iss",
            "aud",
            "exp",
            "nbf",
            "iat",
            "jti",
            "sid",
            "device_id"
        };

    public static bool IsValid(string token)
    {
        try
        {
            var segments = token.Split('.');

            if (segments.Length != 3)
                return false;

            var headerBytes = Base64UrlEncoder.DecodeBytes(segments[0]);

            var payloadBytes = Base64UrlEncoder.DecodeBytes(segments[1]);

            using var header = JsonDocument.Parse(headerBytes);
            using var payload = JsonDocument.Parse(payloadBytes);

            if (header.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (payload.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!HasUniqueProperties(header.RootElement))
                return false;

            if (!HasUniqueProperties(payload.RootElement))
                return false;

            foreach (var property in payload.RootElement.EnumerateObject())
            {
                if (!ReservedClaims.Contains(property.Name))
                    continue;

                if (property.Name is "sub" or "tenant")
                {
                    if (property.Value.ValueKind != JsonValueKind.String)
                    {
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(property.Value.GetString()))
                    {
                        return false;
                    }
                }

                if (property.Name is "exp" or "nbf" or "iat")
                {
                    if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt64(out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex) when (
            ex is FormatException or
            JsonException or
            ArgumentException or
            OverflowException)
        {
            return false;
        }
    }

    private static bool HasUniqueProperties(JsonElement element)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
        {
            if (!names.Add(property.Name))
                return false;
        }

        return true;
    }
}
