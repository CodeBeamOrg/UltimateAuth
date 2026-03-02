namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record CredentialActionRequest
{
    public Guid Id { get; set; }
    public string? Reason { get; set; }
}
