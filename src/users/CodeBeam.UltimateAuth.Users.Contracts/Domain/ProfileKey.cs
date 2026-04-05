using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Users.Contracts;

[JsonConverter(typeof(ProfileKeyJsonConverter))]
public readonly record struct ProfileKey : IParsable<ProfileKey>
{
    public string Value { get; }

    private ProfileKey(string value)
    {
        Value = value;
    }

    public static ProfileKey Default => new("default");

    public static bool TryCreate(string? raw, out ProfileKey key)
    {
        if (IsValid(raw))
        {
            key = new ProfileKey(Normalize(raw!));
            return true;
        }

        key = default;
        return false;
    }

    public static ProfileKey Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var key))
            return key;

        throw new FormatException("Invalid ProfileKey.");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out ProfileKey result)
    {
        if (IsValid(s))
        {
            result = new ProfileKey(Normalize(s!));
            return true;
        }

        result = default;
        return false;
    }

    private static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length > 64)
            return false;

        return true;
    }

    private static string Normalize(string value)
        => value.Trim().ToLowerInvariant();

    public override string ToString() => Value;

    public static implicit operator string(ProfileKey key) => key.Value;
}