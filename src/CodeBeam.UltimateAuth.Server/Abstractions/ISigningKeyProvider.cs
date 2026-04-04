using CodeBeam.UltimateAuth.Server.Contracts;

namespace CodeBeam.UltimateAuth.Server.Abstractions;

public interface IJwtSigningKeyProvider
{
    JwtSigningKey Resolve(string? keyId);
}
