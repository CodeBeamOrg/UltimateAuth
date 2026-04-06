using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Core;
public readonly record struct PasswordHash : IParsable<PasswordHash>
{
    public string Algorithm { get; }
    public string Hash { get; }

    private PasswordHash(string algorithm, string hash)
    {
        Algorithm = algorithm;
        Hash = hash;
    }

    public static PasswordHash Create(string algorithm, string hash)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
            throw new UAuthValidationException("hash_algorithm_required");

        if (string.IsNullOrWhiteSpace(hash))
            throw new UAuthValidationException("hash_required");

        return new PasswordHash(algorithm, hash);
    }

    public static PasswordHash Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException("Invalid PasswordHash format.");

        return result;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out PasswordHash result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return false;
        }

        var parts = s.Split('$', 2);

        if (parts.Length != 2)
        {
            // backward compatibility
            result = new PasswordHash("legacy", s);
            return true;
        }

        result = new PasswordHash(parts[0], parts[1]);
        return true;
    }

    public override string ToString() => $"{Algorithm}${Hash}";

    public static implicit operator string(PasswordHash value) => value.ToString();
}
