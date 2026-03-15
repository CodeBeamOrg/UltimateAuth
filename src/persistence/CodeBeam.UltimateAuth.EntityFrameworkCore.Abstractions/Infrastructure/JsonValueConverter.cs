using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore;

public sealed class JsonValueConverter<T> : ValueConverter<T, string>
{
    public JsonValueConverter()
        : base(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null)!)
    {
    }
}
