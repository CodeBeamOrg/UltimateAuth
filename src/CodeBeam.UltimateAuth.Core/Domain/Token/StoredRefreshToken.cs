namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents a persisted refresh token bound to a session.
    /// Stored as a hashed value for security reasons.
    /// </summary>
    public sealed record StoredRefreshToken
    {
        public string TokenHash { get; init; } = default!;

        public AuthSessionId SessionId { get; init; } = default!;

        public DateTimeOffset ExpiresAt { get; init; }

        public DateTimeOffset? RevokedAt { get; init; }

        public bool IsRevoked => RevokedAt.HasValue;
    }
}
