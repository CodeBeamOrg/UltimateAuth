using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class ConfiguredJwtVerificationKeyProvider
    : IJwtVerificationKeyProvider
{
    private readonly StaticJwtVerificationKeyProvider _inner;

    public ConfiguredJwtVerificationKeyProvider(IOptions<UAuthResourceApiOptions> options)
    {
        var configured = options.Value.Cryptography.JwtVerificationKeys;

        if (configured is null || configured.Count == 0)
        {
            throw new InvalidOperationException("At least one JWT verification key must be configured.");
        }

        var keys = configured.Select(x =>
        {
            if (x is null || string.IsNullOrWhiteSpace(x.KeyId))
            {
                throw new InvalidOperationException("Every JWT verification key requires a key ID.");
            }

            byte[] bytes;

            try
            {
                bytes = Convert.FromBase64String(x.Key);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"JWT verification key '{x.KeyId}' must be Base64 encoded.", ex);
            }

            if (bytes.Length < 32)
            {
                throw new InvalidOperationException($"JWT verification key '{x.KeyId}' must contain at least 32 bytes.");
            }

            return (SecurityKey)new SymmetricSecurityKey(bytes)
            {
                KeyId = x.KeyId
            };
        });

        _inner = new StaticJwtVerificationKeyProvider(keys);
    }

    public IReadOnlyCollection<SecurityKey> GetVerificationKeys()
        => _inner.GetVerificationKeys();
}
