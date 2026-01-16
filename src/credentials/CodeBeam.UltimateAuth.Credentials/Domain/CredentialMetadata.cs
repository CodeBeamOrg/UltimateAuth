namespace CodeBeam.UltimateAuth.Credentials;

public sealed record CredentialMetadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    string? Source);
