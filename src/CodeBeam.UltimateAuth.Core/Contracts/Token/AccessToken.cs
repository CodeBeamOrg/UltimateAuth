namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Represents an issued access token (JWT or opaque).
/// </summary>
public sealed class AccessToken
{
    /// <summary>
    /// The actual token value sent to the client.
    /// </summary>
    public required string Token { get; init; }

    // TODO: TokenKind enum?
    /// <summary>
    /// Token format (JWT or opaque).
    /// Used for diagnostics and middleware behavior.
    /// </summary>
    public TokenFormat Format { get; init; }

    /// <summary>
    /// Expiration time of the token.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Optional session identifier associated with the token.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Optional token scope.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Determines whether the token has expired.
    /// </summary>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
