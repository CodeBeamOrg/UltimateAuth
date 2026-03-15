using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Domain;

[JsonConverter(typeof(TokenIdJsonConverter))]
public readonly record struct TokenId(Guid Value) : IParsable<TokenId>
{
    public static TokenId New() => new(Guid.NewGuid());

    public static TokenId From(Guid value)
        => value == Guid.Empty
            ? throw new ArgumentException("TokenId cannot be empty.", nameof(value))
            : new TokenId(value);

    public static bool TryCreate(string raw, out TokenId id)
    {
        if (Guid.TryParse(raw, out var guid) && guid != Guid.Empty)
        {
            id = new TokenId(guid);
            return true;
        }

        id = default;
        return false;
    }

    public static TokenId Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var id))
            return id;

        throw new FormatException("Invalid TokenId.");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out TokenId result)
    {
        if (!string.IsNullOrWhiteSpace(s) &&
            Guid.TryParse(s, out var guid) &&
            guid != Guid.Empty)
        {
            result = new TokenId(guid);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString("N");
}
