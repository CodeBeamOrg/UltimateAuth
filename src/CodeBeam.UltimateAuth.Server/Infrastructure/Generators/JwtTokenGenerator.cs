using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IJwtSigningKeyProvider _keyProvider;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenGenerator(IJwtSigningKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    public string CreateToken(UAuthJwtTokenDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        ValidateDescriptor(descriptor);

        var signingKey = _keyProvider.Resolve(descriptor.KeyId)
            ?? throw new InvalidOperationException("JWT signing key provider returned null.");

        if (string.IsNullOrWhiteSpace(signingKey.KeyId) ||
            signingKey.Key is null ||
            string.IsNullOrWhiteSpace(signingKey.Algorithm))
        {
            throw new InvalidOperationException("JWT signing key configuration is incomplete.");
        }

        if (descriptor.KeyId is not null &&
            !string.Equals(
                descriptor.KeyId,
                signingKey.KeyId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("JWT signing key ID does not match the requested key.");
        }

        if (!string.IsNullOrEmpty(signingKey.Key.KeyId) &&
            !string.Equals(
                signingKey.Key.KeyId,
                signingKey.KeyId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("JWT signing key metadata is inconsistent.");
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            TokenType = "uauth-access+jwt",
            Issuer = descriptor.Issuer,
            Audience = descriptor.Audience,
            Subject = null,
            NotBefore = descriptor.IssuedAt.UtcDateTime,
            IssuedAt = descriptor.IssuedAt.UtcDateTime,
            Expires = descriptor.ExpiresAt.UtcDateTime,
            Claims = BuildClaims(descriptor),
            SigningCredentials = new SigningCredentials(signingKey.Key, signingKey.Algorithm)
        };

        return _handler.CreateToken(tokenDescriptor);
    }

    private static void ValidateDescriptor(UAuthJwtTokenDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Subject))
        {
            throw new ArgumentException(
                "JWT subject must be specified.",
                nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Issuer))
        {
            throw new ArgumentException(
                "JWT issuer must be specified.",
                nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Audience))
        {
            throw new ArgumentException(
                "JWT audience must be specified.",
                nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.Tenant.Value))
        {
            throw new ArgumentException(
                "JWT tenant must be specified.",
                nameof(descriptor));
        }

        if (descriptor.ExpiresAt <= descriptor.IssuedAt)
        {
            throw new ArgumentException(
                "JWT expiration must be after issuance.",
                nameof(descriptor));
        }
    }

    private static IDictionary<string, object> BuildClaims(UAuthJwtTokenDescriptor descriptor)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal);

        if (descriptor.Claims is not null)
        {
            foreach (var (type, value) in descriptor.Claims)
            {
                if (type is
                    "sub" or "tenant" or
                    "iss" or "aud" or
                    "exp" or "nbf" or "iat" or "device_id")
                {
                    continue;
                }

                claims[type] = value;
            }
        }

        claims["sub"] = descriptor.Subject;
        claims["tenant"] = descriptor.Tenant.Value;

        if (descriptor.DeviceId is DeviceId deviceId)
        {
            claims["device_id"] = deviceId.Value;
        }

        return claims;
    }
}
