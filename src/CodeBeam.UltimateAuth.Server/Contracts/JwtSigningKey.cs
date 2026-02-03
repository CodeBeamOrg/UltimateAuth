using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Contracts;

public sealed class JwtSigningKey
{
    public required string KeyId { get; init; }
    public required SecurityKey Key { get; init; }
    public required string Algorithm { get; init; }
}
