using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public sealed class NullableJsonValueConverter<T> : ValueConverter<T?, string?>
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public NullableJsonValueConverter()
        : base(v => Serialize(v), v => Deserialize(v))
    {
    }

    private static string? Serialize(T? value) => value == null ? null : JsonSerializer.Serialize(value, Options);

    private static T? Deserialize(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, Options);
}
