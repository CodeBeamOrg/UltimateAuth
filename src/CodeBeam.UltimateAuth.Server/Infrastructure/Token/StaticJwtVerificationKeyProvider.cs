using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

/// <summary>
/// Holds a trusted, immutable snapshot of JWT verification keys.
/// Keys must originate from trusted application configuration.
/// </summary>
public sealed class StaticJwtVerificationKeyProvider : IJwtVerificationKeyProvider
{
    private readonly SecurityKey[] _keys;

    public StaticJwtVerificationKeyProvider(IEnumerable<SecurityKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        _keys = keys.ToArray();

        if (_keys.Length == 0)
        {
            throw new ArgumentException("At least one JWT verification key is required.", nameof(keys));
        }

        if (_keys.Any(x => x is null || string.IsNullOrWhiteSpace(x.KeyId)))
        {
            throw new ArgumentException("Every verification key must have a key ID.", nameof(keys));
        }

        if (_keys.Select(x => x.KeyId).Distinct(StringComparer.Ordinal).Count() != _keys.Length)
        {
            throw new ArgumentException("Duplicate JWT verification key IDs are not allowed.", nameof(keys));
        }
    }

    public IReadOnlyCollection<SecurityKey> GetVerificationKeys()
    {
        return Array.AsReadOnly(_keys);
    }
}
