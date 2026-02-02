namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialMetadata
{
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public string? Source { get; init; }
}
