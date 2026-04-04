using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Domain;

[JsonConverter(typeof(UserKeyJsonConverter))]
public readonly record struct UserKey : IParsable<UserKey>
{
    public string Value { get; }

    private UserKey(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a UserKey from a GUID (default and recommended).
    /// </summary>
    public static UserKey FromGuid(Guid value) => new(value.ToString("N"));

    /// <summary>
    /// Creates a UserKey from a canonical string.
    /// Caller is responsible for stability and uniqueness.
    /// </summary>
    public static UserKey FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("UserKey cannot be empty.", nameof(value));

        return new UserKey(value);
    }

    /// <summary>
    /// Generates a new GUID-based UserKey.
    /// </summary>
    public static UserKey New() => FromGuid(Guid.NewGuid());

    public static bool TryParse(string? s, IFormatProvider? provider, out UserKey result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return false;
        }

        if (Guid.TryParse(s, out var guid))
        {
            result = FromGuid(guid);
            return true;
        }

        result = FromString(s);
        return true;
    }

    public static UserKey Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid UserKey value: '{s}'");

        return result;
    }

    public override string ToString() => Value;

    public static implicit operator string(UserKey key) => key.Value;
}
