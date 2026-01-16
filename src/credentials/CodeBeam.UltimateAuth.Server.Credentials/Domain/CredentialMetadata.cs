namespace CodeBeam.UltimateAuth.Server.Credentials;

public sealed record CredentialMetadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    string? Source);
