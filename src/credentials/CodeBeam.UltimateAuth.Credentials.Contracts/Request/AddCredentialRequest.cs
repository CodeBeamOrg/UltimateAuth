namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record AddCredentialRequest()
{
    public required string Secret { get; init; }

    // TODO: Add them after passkey etc. are implemented
    //public CredentialType Type { get; init; }
    //public string? Source { get; init; }
}
