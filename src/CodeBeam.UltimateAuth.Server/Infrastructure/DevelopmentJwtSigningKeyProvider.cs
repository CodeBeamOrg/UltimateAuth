using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class DevelopmentJwtSigningKeyProvider : IJwtSigningKeyProvider
{
    private const string DevelopmentKeyId = "dev-uauth";

    private readonly JwtSigningKey _key;

    public DevelopmentJwtSigningKeyProvider()
    {
        var rawKey = Encoding.UTF8.GetBytes("DEV_ONLY__ULTIMATEAUTH__DO_NOT_USE_IN_PROD");

        _key = new JwtSigningKey
        {
            KeyId = DevelopmentKeyId,
            Algorithm = SecurityAlgorithms.HmacSha256,
            Key = new SymmetricSecurityKey(rawKey)
            {
                KeyId = DevelopmentKeyId
            }
        };
    }

    public JwtSigningKey Resolve(string? keyId)
    {
        if (keyId is not null && !string.Equals(keyId, DevelopmentKeyId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unknown JWT signing key ID: '{keyId}'.");
        }

        return _key;
    }
}
