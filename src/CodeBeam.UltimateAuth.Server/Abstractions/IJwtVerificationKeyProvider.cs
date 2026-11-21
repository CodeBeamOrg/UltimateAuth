using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Abstractions;

/// <summary>
/// Provides trusted JWT verification keys.
/// Implementations must never resolve keys from untrusted URLs or token-supplied key material.
/// </summary>
public interface IJwtVerificationKeyProvider
{
    IReadOnlyCollection<SecurityKey> GetVerificationKeys();
}
