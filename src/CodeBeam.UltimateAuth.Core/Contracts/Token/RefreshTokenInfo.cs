namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Represents refresh token information intended for client-facing authentication results.
/// </summary>
/// <remarks>
/// Contains only the raw refresh token and its expiration time.
/// Server-side security and persistence details, such as the token hash are intentionally excluded.
/// Actual token delivery is governed by the configured response policy.
/// </remarks>
public sealed class RefreshTokenInfo
{
    /// <summary>
    /// Gets the raw refresh token to be delivered to the client through the configured delivery mechanism.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Gets the expiration time of the refresh token in UTC.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
