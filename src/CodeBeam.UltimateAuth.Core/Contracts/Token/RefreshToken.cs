namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Transport model for refresh token. Returned to client once upon creation.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Plain refresh token value (returned to client once).
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Hash of the refresh token to be persisted.
    /// </summary>
    public required string TokenHash { get; init; }

    /// <summary>
    /// Expiration time.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
