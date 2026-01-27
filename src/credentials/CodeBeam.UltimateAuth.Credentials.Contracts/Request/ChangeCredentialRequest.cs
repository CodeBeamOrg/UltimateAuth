namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ChangeCredentialRequest
{
    public CredentialType Type { get; init; }

    public string CurrentSecret { get; init; } = default!;
    public string NewSecret { get; init; } = default!;
}
