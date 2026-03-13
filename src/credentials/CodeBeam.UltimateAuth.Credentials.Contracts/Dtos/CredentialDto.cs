using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialDto
{
    public Guid Id { get; set; }
    public CredentialType Type { get; init; }

    public CredentialSecurityStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastUsedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; init; }

    public string? Source { get; init; }

    public long Version { get; init; }
}
