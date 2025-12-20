using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultJwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IJwtSigningKeyProvider _keyProvider;
        private readonly JsonWebTokenHandler _handler = new();

        public DefaultJwtTokenGenerator(IJwtSigningKeyProvider keyProvider)
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

                SigningCredentials = new SigningCredentials(
                    signingKey.Key,
                    signingKey.Algorithm)
            };

            tokenDescriptor.AdditionalHeaderClaims = new Dictionary<string, object>
                {
                    ["kid"] = signingKey.KeyId
                };

            return _handler.CreateToken(tokenDescriptor);
        }

        private static IDictionary<string, object> BuildClaims(UAuthJwtTokenDescriptor descriptor)
        {
            var claims = new Dictionary<string, object>
            {
                ["sub"] = descriptor.Subject
            };

            if (descriptor.TenantId is not null)
            {
                claims["tenant"] = descriptor.TenantId;
            }

            if (descriptor.Claims is not null)
            {
                foreach (var kv in descriptor.Claims)
                {
                    claims[kv.Key] = kv.Value;
                }
            }

            return claims;
        }
    }

}
