namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed record CredentialDto(
        CredentialType Type,
        CredentialSecurityStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastUsedAt,
        DateTimeOffset? RestrictedUntil,
        DateTimeOffset? ExpiresAt,
        string? Source);
}
