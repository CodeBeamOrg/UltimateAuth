using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    public interface IJwtSigningKeyProvider
    {
        JwtSigningKey Resolve(string? keyId);
    }

    public sealed class JwtSigningKey
    {
        public required string KeyId { get; init; }
        public required SecurityKey Key { get; init; }
        public required string Algorithm { get; init; }
    }

}
