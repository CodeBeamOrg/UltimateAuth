namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record RevokeCredentialRequest
{
    public Guid Id { get; init; }

    // TODO: Add them after passkey etc. are implemented
    // <summary>
    // Optional human-readable reason for audit/logging purposes.
    // </summary>
    //public string? Reason { get; init; }
}
