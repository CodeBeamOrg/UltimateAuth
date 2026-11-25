using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class ConfiguredJwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private const string DefaultKeyId = "uauth-hs256-v1";

    private readonly JwtSigningKey _signingKey;

    public ConfiguredJwtSigningKeyProvider(IOptions<UAuthServerOptions> options)
    {
        var key = UAuthKeyMaterial.Decode(options.Value.Cryptography.JwtSigningKey, "Cryptography.JwtSigningKey");

        var securityKey = new SymmetricSecurityKey(key)
        {
            KeyId = DefaultKeyId
        };

        _signingKey = new JwtSigningKey
        {
            KeyId = DefaultKeyId,
            Algorithm = SecurityAlgorithms.HmacSha256,
            Key = securityKey
        };
    }

    public JwtSigningKey Resolve(string? keyId)
    {
        if (keyId is not null && !string.Equals(keyId, DefaultKeyId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unknown JWT signing key ID: '{keyId}'.");
        }

        return _signingKey;
    }
}
