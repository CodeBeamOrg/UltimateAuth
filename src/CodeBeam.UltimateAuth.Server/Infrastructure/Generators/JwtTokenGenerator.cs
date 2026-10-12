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
        _keyProvider = keyProvider;
    }

    public string CreateToken(UAuthJwtTokenDescriptor descriptor)
    {
        var signingKey = _keyProvider.Resolve(descriptor.KeyId);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
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

    private static IDictionary<string, object> BuildClaims(UAuthJwtTokenDescriptor descriptor)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal);

        if (descriptor.Claims is not null)
        {
            foreach (var (type, value) in descriptor.Claims)
            {
                // These claims are controlled by SecurityTokenDescriptor
                // or explicitly defined by UAuthJwtTokenDescriptor.
                if (type is "sub" or "tenant" or "iss" or "aud" or "exp" or "nbf" or "iat")
                    continue;

                claims[type] = value;
            }
        }

        // Framework-owned identity claims always take precedence.
        claims["sub"] = descriptor.Subject;
        claims["tenant"] = descriptor.Tenant.Value;

        return claims;
    }
}
