using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthCryptographyOptions
{
    /// <summary>
    /// Base64-encoded, 32-byte or longer random key.
    /// Used for HS256 JWT signing.
    /// </summary>
    public string? JwtSigningKey { get; set; } = UAuthCryptographyConstants.DevelopmentJwtSigningKey;

    /// <summary>
    /// Base64-encoded, 32-byte or longer random key.
    /// Used for HMAC-SHA256 token hashing.
    /// </summary>
    public string? TokenHashingKey { get; set; } = UAuthCryptographyConstants.DevelopmentTokenHashingKey;

    public UAuthCryptographyOptions Clone() => new()
    {
        JwtSigningKey = JwtSigningKey,
        TokenHashingKey = TokenHashingKey
    };
}
