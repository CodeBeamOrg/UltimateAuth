namespace CodeBeam.UltimateAuth.Core.Domain;

/// <summary>
/// Represents the minimal, immutable user snapshot required by the UltimateAuth Core
/// during authentication discovery and subject binding.
///
/// This type is NOT a domain user model.
/// It contains only normalized, opinionless fields that determine whether
/// a user can participate in authentication flows.
///
/// AuthUserRecord is produced by the Users domain as a boundary projection
/// and is never mutated by the Core.
/// </summary>
public sealed record AuthUserRecord<TUserId>
{
    /// <summary>
    /// Application-level user identifier.
    /// </summary>
    public required TUserId Id { get; init; }

    /// <summary>
    /// Primary login identifier (username, email, etc).
    /// Used only for discovery and uniqueness checks.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// Indicates whether the user is considered active for authentication purposes.
    /// Domain-specific statuses are normalized into this flag by the Users domain.
    /// </summary>
    public required bool IsActive { get; init; }

    /// <summary>
    /// Indicates whether the user is deleted.
    /// Deleted users are never eligible for authentication.
    /// </summary>
    public required bool IsDeleted { get; init; }

    /// <summary>
    /// The timestamp when the user was originally created.
    /// Provided for invariant validation and auditing purposes.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The timestamp when the user was deleted, if applicable.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; init; }
}
