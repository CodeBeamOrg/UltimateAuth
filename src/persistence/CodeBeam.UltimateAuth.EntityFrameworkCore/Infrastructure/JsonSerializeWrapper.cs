using System.Text.Json;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

internal static class JsonSerializerWrapper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }

    public static string? SerializeNullable<T>(T? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, Options);
    }

    public static T? DeserializeNullable<T>(string? json)
    {
        return json is null ? default : JsonSerializer.Deserialize<T>(json, Options);
    }
}
