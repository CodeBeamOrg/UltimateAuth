using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Domain;

// AuthSessionId is a opaque token, because it's more sensitive data. SessionChainId and SessionRootId are Guid.
[JsonConverter(typeof(AuthSessionIdJsonConverter))]
public readonly record struct AuthSessionId : IParsable<AuthSessionId>
{
    public string Value { get; }

    private AuthSessionId(string value)
    {
        Value = value;
    }

    public static bool TryCreate(string? raw, out AuthSessionId id)
    {
        if (IsValid(raw))
        {
            id = new AuthSessionId(raw!);
            return true;
        }

        id = default;
        return false;
    }

    public static AuthSessionId Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var id))
            return id;

        throw new FormatException("Invalid AuthSessionId.");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out AuthSessionId result)
    {
        if (IsValid(s))
        {
            result = new AuthSessionId(s!);
            return true;
        }

        result = default;
        return false;
    }

    private static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length < 32)
            return false;

        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(AuthSessionId id) => id.Value;
}
