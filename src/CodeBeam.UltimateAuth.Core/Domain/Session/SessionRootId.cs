using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Domain;

[JsonConverter(typeof(SessionRootIdJsonConverter))]
public readonly record struct SessionRootId(Guid Value) : IParsable<SessionRootId>
{
    public static SessionRootId New() => new(Guid.NewGuid());

    public static SessionRootId From(Guid value)
        => value == Guid.Empty
            ? throw new ArgumentException("SessionRootId cannot be empty.", nameof(value))
            : new SessionRootId(value);

    public static bool TryCreate(string raw, out SessionRootId id)
    {
        if (Guid.TryParse(raw, out var guid) && guid != Guid.Empty)
        {
            id = new SessionRootId(guid);
            return true;
        }

        id = default;
        return false;
    }

    public static SessionRootId Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var id))
            return id;

        throw new FormatException("Invalid SessionRootId.");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out SessionRootId result)
    {
        if (!string.IsNullOrWhiteSpace(s) &&
            Guid.TryParse(s, out var guid) &&
            guid != Guid.Empty)
        {
            result = new SessionRootId(guid);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString("N");
}
