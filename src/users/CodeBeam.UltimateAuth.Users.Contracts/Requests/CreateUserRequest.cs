namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record CreateUserRequest
{
    /// <summary>
    /// Primary identifier (username, email, external id).
    /// Interpretation is application-specific.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// Optional password.
    /// If null, user may be invited or use external login.
    /// </summary>
    public string? Password { get; init; }

    public string? DisplayName { get; set; }

    public string? TenantId { get; init; }

    /// <summary>
    /// Initial user status.
    /// Defaults to Active.
    /// </summary>
    public UserStatus InitialStatus { get; init; } = UserStatus.Active;

    /// <summary>
    /// Optional initial profile data.
    /// </summary>
    public UserProfileInput? Profile { get; init; }

    /// <summary>
    /// Optional custom metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
