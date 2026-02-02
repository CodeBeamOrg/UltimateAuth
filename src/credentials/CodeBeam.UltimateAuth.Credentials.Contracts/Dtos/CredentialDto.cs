namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialDto
{
    public CredentialType Type { get; init; }

    public CredentialSecurityStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastUsedAt { get; init; }

    public DateTimeOffset? RestrictedUntil { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public string? Source { get; init; }
}
