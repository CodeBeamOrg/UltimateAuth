using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public sealed class JsonValueConverter<T> : ValueConverter<T, string>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public JsonValueConverter()
        : base(v => Serialize(v), v => Deserialize(v))
    {
    }

    private static string Serialize(T value) => JsonSerializer.Serialize(value, Options);

    private static T Deserialize(string json) => JsonSerializer.Deserialize<T>(json, Options)!;
}
