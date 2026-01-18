namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed record CredentialMetadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    string? Source);
