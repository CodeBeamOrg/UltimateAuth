namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialDescriptorDto
{
    public CredentialType Type { get; init; }

    public string Identifier { get; init; } = default!;

    public bool IsActive { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }

    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
