using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Domain;

[JsonConverter(typeof(SessionChainIdJsonConverter))]
public readonly record struct SessionChainId(Guid Value) : IParsable<SessionChainId>
{
    public static SessionChainId New() => new(Guid.NewGuid());

    /// <summary>
    /// Indicates that the chain must be assigned by the store.
    /// </summary>
    public static readonly SessionChainId Unassigned = new(Guid.Empty);

    public bool IsUnassigned => Value == Guid.Empty;

    public static SessionChainId From(Guid value)
        => value == Guid.Empty
            ? throw new ArgumentException("ChainId cannot be empty.", nameof(value))
            : new SessionChainId(value);

    public static bool TryCreate(string raw, out SessionChainId id)
    {
        if (Guid.TryParse(raw, out var guid) && guid != Guid.Empty)
        {
            id = new SessionChainId(guid);
            return true;
        }

        id = default;
        return false;
    }

    public static SessionChainId Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var id))
            return id;

        throw new FormatException("Invalid SessionChainId.");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out SessionChainId result)
    {
        if (!string.IsNullOrWhiteSpace(s) &&
            Guid.TryParse(s, out var guid) &&
            guid != Guid.Empty)
        {
            result = new SessionChainId(guid);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString("N");
}
