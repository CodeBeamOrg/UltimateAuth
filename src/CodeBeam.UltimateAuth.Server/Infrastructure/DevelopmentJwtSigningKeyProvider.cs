using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DevelopmentJwtSigningKeyProvider : IJwtSigningKeyProvider
    {
        private readonly JwtSigningKey _key;

        public DevelopmentJwtSigningKeyProvider()
        {
            var rawKey = Encoding.UTF8.GetBytes("DEV_ONLY__ULTIMATEAUTH__DO_NOT_USE_IN_PROD");

            _key = new JwtSigningKey
            {
                KeyId = "dev-uauth",
                Algorithm = SecurityAlgorithms.HmacSha256,
                Key = new SymmetricSecurityKey(rawKey)
                {
                    KeyId = "dev-uauth"
                }
            };
        }

        public JwtSigningKey Resolve(string? keyId)
        {
            // signing veya verify için tek key
            return _key;
        }
    }
}
