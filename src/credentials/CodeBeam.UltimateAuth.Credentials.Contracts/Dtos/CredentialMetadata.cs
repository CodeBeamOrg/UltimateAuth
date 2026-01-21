namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialMetadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    string? Source);
