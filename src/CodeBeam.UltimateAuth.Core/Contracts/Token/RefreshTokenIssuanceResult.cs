namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Represents the result of issuing a refresh token for trusted server-side operations.
/// </summary>
/// <remarks>
/// Contains both the raw refresh token and its hash.
/// The raw token is intended for one-time delivery to the client,
/// while the hash is used exclusively for server-side persistence and rotation.
/// This type is not intended to be exposed directly in client responses.
/// Use <see cref="RefreshTokenInfo"/> for client-facing token information.
/// </remarks>
public sealed class RefreshTokenIssuanceResult
{
    /// <summary>
    /// Gets the raw refresh token generated for one-time delivery to the client.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Gets the hash of the refresh token used for server-side persistence, validation, and rotation.
    /// Must never be exposed to the client.
    /// </summary>
    public required string TokenHash { get; init; }

    /// <summary>
    /// Gets the expiration time of the refresh token in UTC.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
